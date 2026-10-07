import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,rmSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {createReefServer} from './server.mjs';
test('SQLite accounts, isolation, revision conflicts, four seats and restart persistence',async()=>{
  const directory=mkdtempSync(join(tmpdir(),'reef-test-')),database=join(directory,'test.sqlite');let server;
  async function start(){server=createReefServer({database,authLimit:100});await new Promise(r=>server.listen(0,'127.0.0.1',r));}
  async function close(){await new Promise(r=>server.close(r));}
  async function api(method,path,body,token){const r=await fetch(`http://127.0.0.1:${server.address().port}${path}`,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{})},body:body?JSON.stringify(body):undefined});return{status:r.status,...await r.json()};}
  try{
    await start();assert.equal((await api('GET','/health')).storage,'sqlite');assert.equal((await api('GET','/v1/profile')).status,401);
    const users=[];for(let i=0;i<5;i++){const u=await api('POST','/v1/register',{username:`tester_${i}`,password:'test-password-strong'});assert.equal(u.ok,true);assert.equal(u.profile.coins,100000000);users.push(u);}
    assert.equal((await api('POST','/v1/register',{username:'tester_0',password:'test-password-strong'})).status,409);
    assert.equal((await api('POST','/v1/login',{username:'tester_0',password:'wrong-password'})).status,401);
    assert.equal((await api('POST','/v1/login',{username:"' OR 1=1",password:'test-password-strong'})).status,400);
    const p={...users[0].profile,coins:123456789};assert.equal((await api('PUT','/v1/profile',{profile:p,revision:0},users[0].token)).revision,1);
    assert.equal((await api('PUT','/v1/profile',{profile:p,revision:0},users[0].token)).status,409);
    assert.equal((await api('PUT','/v1/profile',{profile:{...p,coins:-1},revision:1},users[0].token)).status,400);
    assert.equal((await api('PUT','/v1/profile',{revision:1},users[0].token)).status,400);
    assert.equal((await api('GET','/v1/profile',null,users[1].token)).profile.coins,100000000);
    const created=await api('POST','/v1/rooms',{},users[0].token),code=created.room.code;
    const joins=await Promise.all(users.slice(1).map(u=>api('POST',`/v1/rooms/${code}/join`,{},u.token)));
    assert.equal(joins.filter(r=>r.ok).length,3);assert.equal(joins.filter(r=>r.error==='ROOM_FULL').length,1);
    assert.deepEqual((await api('GET',`/v1/rooms/${code}`,null,users[0].token)).room.seats.map(s=>s.seat),[0,1,2,3]);
    assert.equal((await api('POST',`/v1/rooms/${code}/join`,{},users[0].token)).ok,true);
    assert.equal((await api('POST','/v1/rooms',{},users[0].token)).status,409);
    assert.equal((await api('POST',`/v1/rooms/${code}/leave`,{},users[0].token)).ok,true);
    assert.equal((await api('GET',`/v1/rooms/${code}`,null,users[1].token)).room.seats.length,3);
    await api('POST','/v1/logout',{},users[0].token);assert.equal((await api('GET','/v1/profile',null,users[0].token)).status,401);
    await close();await start();const login=await api('POST','/v1/login',{username:'tester_0',password:'test-password-strong'});
    assert.equal(login.profile.coins,123456789);assert.equal(login.revision,1);
  }finally{if(server?.listening)await close();rmSync(directory,{recursive:true,force:true});}
});
