import http from 'node:http';
import { DatabaseSync } from 'node:sqlite';
import { randomBytes, randomUUID, scrypt, timingSafeEqual, createHash } from 'node:crypto';
import { promisify } from 'node:util';
import { mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const derive=promisify(scrypt), hash=s=>createHash('sha256').update(s).digest('hex');
const fail=(status,error)=>Object.assign(new Error(error),{status});
export function createReefServer({database=resolve('data/reef.sqlite'),authLimit=30}={}) {
  mkdirSync(dirname(database),{recursive:true});const db=new DatabaseSync(database);
  db.exec(`PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;
    CREATE TABLE IF NOT EXISTS users(id TEXT PRIMARY KEY,username TEXT NOT NULL UNIQUE COLLATE NOCASE,salt TEXT NOT NULL,password_hash TEXT NOT NULL,created INTEGER NOT NULL);
    CREATE TABLE IF NOT EXISTS profiles(user_id TEXT PRIMARY KEY REFERENCES users(id),revision INTEGER NOT NULL DEFAULT 0,json TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS sessions(token_hash TEXT PRIMARY KEY,user_id TEXT NOT NULL REFERENCES users(id),expires INTEGER NOT NULL);
    CREATE TABLE IF NOT EXISTS rooms(code TEXT PRIMARY KEY,owner TEXT NOT NULL REFERENCES users(id),created INTEGER NOT NULL);
    CREATE TABLE IF NOT EXISTS seats(room TEXT NOT NULL REFERENCES rooms(code) ON DELETE CASCADE,user_id TEXT NOT NULL UNIQUE REFERENCES users(id),seat INTEGER NOT NULL CHECK(seat BETWEEN 0 AND 3),seen INTEGER NOT NULL,PRIMARY KEY(room,seat));`);
  const counters=new Map();
  const get=(sql,...args)=>db.prepare(sql).get(...args),run=(sql,...args)=>db.prepare(sql).run(...args);
  const tx=fn=>{db.exec('BEGIN IMMEDIATE');try{const value=fn();db.exec('COMMIT');return value;}catch(e){db.exec('ROLLBACK');throw e;}};
  function profile(username,p={}) {
    const base={version:1,coins:100000000,diamonds:9999,level:1,exp:0,unlockedGuns:255,selectedGun:0,cosmetics:0,selectedCosmetic:0,music:.45,sfx:.7,questDay:'',dailyClaim:'',username,questProgress:[0,0,0,0,0,0],questClaimed:[false,false,false,false,false,false],achievementClaimed:false,totalSessions:0,lifetimeKills:0,vipGrantVersion:1};
    for(const k of Object.keys(base))if(Object.hasOwn(p,k))base[k]=p[k];base.username=username;
    const limits={coins:999999999999,diamonds:10000000,level:100000,exp:100000000,unlockedGuns:255,selectedGun:7,cosmetics:255,selectedCosmetic:255,totalSessions:100000000,lifetimeKills:999999999999,vipGrantVersion:1,version:1};
    for(const [k,max] of Object.entries(limits))if(!Number.isSafeInteger(base[k])||base[k]<0||base[k]>max)throw fail(400,'PROFILE_INVALID');
    for(const k of ['music','sfx'])if(!Number.isFinite(base[k])||base[k]<0||base[k]>1)throw fail(400,'PROFILE_INVALID');
    for(const k of ['questDay','dailyClaim'])if(typeof base[k]!=='string'||!/^$|^\d{4}-\d{2}-\d{2}$/.test(base[k]))throw fail(400,'PROFILE_INVALID');
    if(!Array.isArray(base.questProgress)||base.questProgress.length!==6||!base.questProgress.every(n=>Number.isSafeInteger(n)&&n>=0&&n<=1e12)||!Array.isArray(base.questClaimed)||base.questClaimed.length!==6||!base.questClaimed.every(b=>typeof b==='boolean')||typeof base.achievementClaimed!=='boolean')throw fail(400,'PROFILE_INVALID');
    return base;
  }
  function session(user){const token=randomBytes(32).toString('base64url');run('DELETE FROM sessions WHERE expires<?',Date.now());run('INSERT INTO sessions VALUES(?,?,?)',hash(token),user.id,Date.now()+86400000);const p=get('SELECT * FROM profiles WHERE user_id=?',user.id);return{ok:true,token,userId:user.id,username:user.username,revision:p.revision,profile:JSON.parse(p.json)};}
  function identity(req){const token=(req.headers.authorization||'').replace(/^Bearer /,'');const user=get('SELECT u.* FROM sessions s JOIN users u ON u.id=s.user_id WHERE token_hash=? AND expires>?',hash(token),Date.now());if(!user)throw fail(401,'LOGIN_REQUIRED');return user;}
  function cleanup(){run('DELETE FROM seats WHERE seen<?',Date.now()-120000);run('DELETE FROM rooms WHERE code NOT IN(SELECT room FROM seats)');}
  function room(code){const r=get('SELECT * FROM rooms WHERE code=?',code);if(!r)throw fail(404,'ROOM_NOT_FOUND');return{code:r.code,capacity:4,mode:'lobby-only',seats:db.prepare('SELECT s.seat,u.username,u.id AS userId FROM seats s JOIN users u ON u.id=s.user_id WHERE room=? ORDER BY seat').all(code)};}
  async function body(req){let data='';for await(const part of req){data+=part;if(Buffer.byteLength(data)>131072)throw fail(413,'BODY_TOO_LARGE');}try{return JSON.parse(data||'{}');}catch{throw fail(400,'INVALID_JSON');}}
  async function route(req){
    const path=new URL(req.url,'http://localhost').pathname;
    if(req.method==='GET'&&path==='/health')return{ok:true,service:'Luma Reef',storage:'sqlite',roomCapacity:4};
    if(req.method==='POST'&&(path==='/v1/register'||path==='/v1/login')){
      const key=req.socket.remoteAddress,now=Date.now();for(const [k,v] of counters)if(v.until<now)counters.delete(k);const c=counters.get(key)||{count:0,until:now+60000};c.count++;counters.set(key,c);if(c.count>authLimit)throw fail(429,'TRY_LATER');
      const b=await body(req);if(typeof b.username!=='string'||!/^[a-zA-Z0-9_]{3,24}$/.test(b.username)||typeof b.password!=='string'||b.password.length<8||b.password.length>128)throw fail(400,'CREDENTIALS_FORMAT');
      let user=get('SELECT * FROM users WHERE username=?',b.username);
      if(path==='/v1/register'){
        if(user)throw fail(409,'USERNAME_TAKEN');const salt=randomBytes(16).toString('hex'),passwordHash=(await derive(b.password,salt,64)).toString('hex');
        const initial=profile(b.username,b.profile||{});initial.coins=Math.max(100000000,initial.coins);initial.diamonds=Math.max(9999,initial.diamonds);initial.vipGrantVersion=1;
        user={id:randomUUID(),username:b.username};try{tx(()=>{run('INSERT INTO users VALUES(?,?,?,?,?)',user.id,user.username,salt,passwordHash,now);run('INSERT INTO profiles VALUES(?,0,?)',user.id,JSON.stringify(initial));});}catch(e){if(e.code?.includes('SQLITE'))throw fail(409,'USERNAME_TAKEN');throw e;}
      }else{
        const derived=await derive(b.password,user?.salt||'00000000000000000000000000000000',64),expected=Buffer.from(user?.password_hash||'0'.repeat(128),'hex');
        if(!timingSafeEqual(derived,expected)||!user)throw fail(401,'INVALID_CREDENTIALS');
      }
      return session(user);
    }
    const user=identity(req);
    if(path==='/v1/profile'&&req.method==='GET'){const p=get('SELECT * FROM profiles WHERE user_id=?',user.id);return{ok:true,profile:JSON.parse(p.json),revision:p.revision};}
    if(path==='/v1/profile'&&req.method==='PUT'){
      const b=await body(req);if(!b.profile||typeof b.profile!=='object'||Array.isArray(b.profile))throw fail(400,'PROFILE_INVALID');const p=profile(user.username,b.profile);if(!Number.isSafeInteger(b.revision)||b.revision<0)throw fail(400,'REVISION_REQUIRED');
      const changed=run('UPDATE profiles SET json=?,revision=revision+1 WHERE user_id=? AND revision=?',JSON.stringify(p),user.id,b.revision);
      if(changed.changes!==1)throw fail(409,'PROFILE_CONFLICT');return{ok:true,revision:b.revision+1};
    }
    if(path==='/v1/logout'&&req.method==='POST'){run('DELETE FROM sessions WHERE token_hash=?',hash((req.headers.authorization||'').replace(/^Bearer /,'')));run('DELETE FROM seats WHERE user_id=?',user.id);cleanup();return{ok:true};}
    if(path==='/v1/rooms'&&req.method==='GET'){cleanup();return{ok:true,rooms:db.prepare('SELECT code FROM rooms ORDER BY created DESC LIMIT 30').all().map(r=>room(r.code))};}
    if(path==='/v1/rooms'&&req.method==='POST')return tx(()=>{cleanup();if(get('SELECT room FROM seats WHERE user_id=?',user.id))throw fail(409,'ALREADY_IN_ROOM');const code=randomBytes(4).toString('hex').toUpperCase();run('INSERT INTO rooms VALUES(?,?,?)',code,user.id,Date.now());run('INSERT INTO seats VALUES(?,?,0,?)',code,user.id,Date.now());return{ok:true,room:room(code)};});
    const match=path.match(/^\/v1\/rooms\/([A-F0-9]{8})(?:\/(join|leave|heartbeat))?$/);
    if(match){const [,code,action]=match;
      if(req.method==='GET'&&!action){cleanup();return{ok:true,room:room(code)};}
      if(req.method==='POST'&&action==='join')return tx(()=>{cleanup();const current=get('SELECT * FROM seats WHERE user_id=?',user.id);if(current){if(current.room===code){run('UPDATE seats SET seen=? WHERE user_id=?',Date.now(),user.id);return{ok:true,room:room(code)};}throw fail(409,'ALREADY_IN_ROOM');}
        const r=room(code);if(r.seats.length>=4)throw fail(409,'ROOM_FULL');const seat=[0,1,2,3].find(n=>!r.seats.some(s=>s.seat===n));run('INSERT INTO seats VALUES(?,?,?,?)',code,user.id,seat,Date.now());return{ok:true,room:room(code)};});
      if(req.method==='POST'&&action==='leave'){run('DELETE FROM seats WHERE room=? AND user_id=?',code,user.id);cleanup();return{ok:true};}
      if(req.method==='POST'&&action==='heartbeat'){const r=run('UPDATE seats SET seen=? WHERE room=? AND user_id=?',Date.now(),code,user.id);if(!r.changes)throw fail(403,'NOT_IN_ROOM');return{ok:true,room:room(code)};}
    }
    throw fail(404,'NOT_FOUND');
  }
  const server=http.createServer(async(req,res)=>{try{const data=await route(req);res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify(data));}catch(e){res.writeHead(e.status||500,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({ok:false,error:e.status?e.message:'SERVER_ERROR'}));if(!e.status)console.error('Server operation failed:',e.code||e.name);}});
  server.requestTimeout=10000;server.headersTimeout=10000;server.on('close',()=>db.close());return server;
}
if(process.argv[1]&&resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  const server=createReefServer({database:process.env.REEF_DB||resolve(dirname(fileURLToPath(import.meta.url)),'data/reef.sqlite')});
  const host=process.env.REEF_HOST||'127.0.0.1',port=Number(process.env.REEF_PORT||8787);server.listen(port,host,()=>console.log(`Luma Reef API ready at http://${host}:${port} (SQLite / development)`));
  for(const signal of ['SIGINT','SIGTERM'])process.on(signal,()=>server.close(()=>process.exit(0)));
}
