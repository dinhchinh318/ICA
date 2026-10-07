using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using LumaReef.Data;
using LumaReef.Fish;
using LumaReef.Bullet;
using LumaReef.Gun;
using LumaReef.Spawn;
using LumaReef.Gameplay;
using LumaReef.Economy;
using LumaReef.Skill;
using LumaReef.Quest;
using LumaReef.Save;
using LumaReef.VFX;
using LumaReef.UI;
namespace LumaReef.Core
{
    public sealed partial class ReefApp : MonoBehaviour
    {
        public static ReefApp Instance { get; private set; }
        public ReefCatalog Catalog { get; private set; }
        public Wallet Wallet { get; private set; }
        public PlayerSave SaveData { get; private set; }
        public GunLoadout Loadout { get; private set; }
        public SkillController Skills { get; private set; }
        public CombatResolver Combat { get; private set; }
        public LumaReef.Audio.AudioManager Audio { get; private set; }
        public bool Auto { get; private set; }
        public bool Lock { get; private set; }
        public int FishCount=>fish.Count;
        public int BulletCount=>bullets.Count;
        public int EffectCount=>effects.ActiveCount+rewards.ActiveCount;
        public FishActor Boss { get { for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active&&fish.Items[i].Data.category==FishCategory.Boss)return fish.Items[i]; return null; } }
        FixedPool<FishActor> fish;
        FixedPool<BulletActor> bullets;
        SpawnDirector spawner;
        SpecialFishController specials;
        QuestController quests;
        ISaveStore store;
        PendingProfileStore pendingProfile;
        EffectsPool effects;
        RewardViewPool rewards;
        CameraFXManager cameraFX;
        Cannon cannon;
        Camera cameraView;
        GameManager gameManager;
        ReefUI ui;
        Transform world;
        FishActor target;
        Rect arena=new Rect(-5.4f,-9.6f,10.8f,19.2f);
        float saveTimer,trailTimer,emptyTimer;
        bool loading,showColliders,dirty;
        bool smoke;
        bool smokeFailed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Instance!=null)return;
            var go=new GameObject("Luma Reef / composition root"); DontDestroyOnLoad(go); go.AddComponent<ReefApp>();
        }
        void Awake()
        {
            Instance=this; smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--reef-smoke")>=0;
            if(smoke)Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)smokeFailed=true;};
            Catalog=Resources.Load<ReefCatalog>("ReefCatalog");
            if(Catalog==null) { Debug.LogError("Run Luma Reef > Generate / Repair Project in the Editor."); enabled=false; return; }
            gameManager=gameObject.AddComponent<GameManager>();
            store=new JsonSaveStore(smoke?System.IO.Path.Combine(Application.temporaryCachePath,"LumaReefSmoke"):Application.persistentDataPath);
            SaveData=smoke?new PlayerSave():store.Load(); Wallet=new Wallet(SaveData.coins,SaveData.diamonds);
            Loadout=new GunLoadout(Catalog.room,SaveData.selectedGun,SaveData.unlockedGuns);
            Skills=new SkillController(Catalog.skills); quests=new QuestController(SaveData,Catalog.quests,Wallet);
            world=new GameObject("Reef world").transform; world.SetParent(transform);
            var cameraGo=new GameObject("Reef Camera",typeof(Camera),typeof(AudioListener)); cameraGo.transform.SetParent(transform);
            cameraView=cameraGo.GetComponent<Camera>(); cameraView.orthographic=true; cameraView.orthographicSize=9.6f; cameraView.backgroundColor=new Color(.02f,.09f,.17f); cameraView.transform.position=new Vector3(0,0,-10); cameraGo.tag="MainCamera";
            cameraFX=new CameraFXManager(cameraView); BuildEnvironment();
            fish=new FixedPool<FishActor>(Catalog.room.maxFish,i=>new FishActor(world));
            bullets=new FixedPool<BulletActor>(Catalog.room.bulletCapacity,i=>new BulletActor(world,Catalog.disk,Catalog.particleMaterial));
            effects=new EffectsPool(world,Catalog.particleMaterial,Catalog.room.effectCapacity,Catalog.effectSprites);
            rewards=new RewardViewPool(world,Catalog.disk,Catalog.font,Catalog.room.coinCapacity,Catalog.room.textCapacity);
            cannon=new Cannon(world,Catalog.disk,Catalog.barrel,Catalog.cannonSprites);
            Combat=new CombatResolver(fish,Wallet,new OfflineCombatAuthority(Environment.TickCount));
            specials=new SpecialFishController(Catalog.room,Combat,fish,Wallet);
            spawner=new SpawnDirector(Catalog.room,fish); Audio=new LumaReef.Audio.AudioManager(transform,Catalog.audio,SaveData.music,SaveData.sfx);
            ui=new ReefUI(this,Catalog.font);
            Wallet.Changed+=()=>dirty=true;
            Combat.Killed+=OnKilled;Combat.Impact+=(at,radius,color)=>{effects.Net(at,radius,color,Loadout.Current.level);Audio.Play(SoundCue.Hit);};
            Combat.BigWin+=OnBigWin;
            specials.Lightning+=(from,to)=>{effects.Lightning(from,to); Audio.Play(SoundCue.Lightning);};
            specials.Treasure+=(at,reward)=>{rewards.Play(at,reward,true); effects.Burst(at,1.5f,ReefUI.Gold); quests.Add(QuestMetric.Earnings,reward); ui.Toast("TREASURE FOUND  +"+reward.ToString("N0"));};
            spawner.Announcement+=value=>{ui.Announce(value); if(spawner.HasBoss){Audio.Play(SoundCue.Warning); cameraFX.Pulse(.13f); effects.Burst(new Vector2(0,3),3,ReefUI.Gold);}};
            Skills.Activated+=OnSkill;
            SeedFish(); gameManager.SetState(GameState.Menu);
            AuthenticateAndStart();
        }
        
        void AuthenticateAndStart()
        {
            if(!smoke && SaveData.vipGrantVersion<1){Wallet.Credit(100000000);Wallet.AddDiamonds(9999);SaveData.vipGrantVersion=1;SaveNow();}
            if(smoke)StartCoroutine(SmokeTest());else StartCoroutine(LoadScene("MainMenuScene",false));
        }
        void ApplyProfile(PlayerSave data)
        {
            SaveData=data;Wallet=new Wallet(data.coins,data.diamonds);Wallet.Changed+=()=>dirty=true;
            Loadout=new GunLoadout(Catalog.room,data.selectedGun,data.unlockedGuns);
            Combat=new CombatResolver(fish,Wallet,new OfflineCombatAuthority(Environment.TickCount));
            specials=new SpecialFishController(Catalog.room,Combat,fish,Wallet);quests=new QuestController(data,Catalog.quests,Wallet);
            Combat.Killed+=OnKilled;Combat.Impact+=(at,radius,color)=>{effects.Net(at,radius,color,Loadout.Current.level);Audio.Play(SoundCue.Hit);};Combat.BigWin+=OnBigWin;
            specials.Lightning+=(from,to)=>{effects.Lightning(from,to);Audio.Play(SoundCue.Lightning);};
            specials.Treasure+=(at,reward)=>{rewards.Play(at,reward,true);quests.Add(QuestMetric.Earnings,reward);};
            Audio.Volume(data.music,data.sfx);fish.Clear();bullets.Clear();SeedFish();
        }
        bool accountBusy,syncBusy,syncBlocked,cloudPending;
        string roomCode;
        float roomHeartbeat;
        public void Account()
        {
            OpenPanel("ACCOUNT");
        }
        public void Authenticate(string username,string password,bool register)
        {
            if(accountBusy||syncBusy||LumaReef.Network.DatabaseManager.LoggedIn)return;SaveNow();accountBusy=true;StartCoroutine(Login(username,password,register));
        }
        IEnumerator Login(string username,string password,bool register)
        {
            yield return LumaReef.Network.DatabaseManager.Authenticate(username,password,register,register?SaveData:null,reply=>{
                if(!reply.ok){accountBusy=false;ui.Toast(LumaReef.Network.DatabaseManager.Explain(reply.error));return;}
                roomCode=null;syncBlocked=false;
                string directory=System.IO.Path.Combine(smoke?System.IO.Path.Combine(Application.temporaryCachePath,"LumaReefSmoke"):Application.persistentDataPath,"accounts",reply.userId);
                store=new JsonSaveStore(directory);pendingProfile=new PendingProfileStore(directory);var pending=pendingProfile.Load();
                bool recover=pending!=null&&pending.profile!=null&&pending.revision==reply.revision;
                bool conflict=pending!=null&&pending.profile!=null&&!recover&&JsonUtility.ToJson(pending.profile)!=JsonUtility.ToJson(reply.profile);
                if(conflict)pendingProfile.ArchiveConflict();
                ApplyProfile(recover?pending.profile:reply.profile);store.Save(SaveData);dirty=recover;ui.CloseModal();
                if(pending!=null&&!recover&&!conflict)pendingProfile.Clear();
                ui.Toast(conflict?"Đã tải bản server mới hơn. Bản chưa đồng bộ được giữ trong pending-profile.json.":"ĐÃ ĐĂNG NHẬP • "+reply.username);
                accountBusy=false;if(recover)StartCoroutine(SyncProfile());
            });
        }
        IEnumerator SyncProfile()
        {
            syncBusy=true;cloudPending=false;string sent=JsonUtility.ToJson(SaveData);pendingProfile?.Write(LumaReef.Network.DatabaseManager.Revision,SaveData);yield return LumaReef.Network.DatabaseManager.SaveProfile(SaveData,reply=>{
                syncBusy=false;if(reply.ok){if(sent==JsonUtility.ToJson(SaveData))pendingProfile?.Clear();else pendingProfile?.Write(reply.revision,SaveData);}if(!reply.ok){dirty=true;if(reply.error=="PROFILE_CONFLICT"||reply.error=="LOGIN_REQUIRED")syncBlocked=true;ui.Toast(LumaReef.Network.DatabaseManager.Explain(reply.error));}
            });
            if(cloudPending&&!syncBlocked)StartCoroutine(SyncProfile());
        }
        public void Logout()
        {
            if(accountBusy)return;SaveNow();accountBusy=true;StartCoroutine(EndSession());
        }
        IEnumerator EndSession()
        {
            while(syncBusy)yield return null;
            yield return LumaReef.Network.DatabaseManager.Request("POST","/v1/logout","{}",reply=>{});
            LumaReef.Network.DatabaseManager.Clear();pendingProfile=null;roomCode=null;cloudPending=false;syncBlocked=false;
            store=new JsonSaveStore(Application.persistentDataPath);ApplyProfile(store.Load());dirty=false;accountBusy=false;
            ui.CloseModal();ui.Toast("Đã đăng xuất. Bạn đang chơi với hồ sơ khách.");
        }
        void RoomRequest(string method,string path)
        {
            StartCoroutine(LumaReef.Network.DatabaseManager.Request(method,path,"{}",reply=>{
                if(!reply.ok){ui.Toast(LumaReef.Network.DatabaseManager.Explain(reply.error));return;}
                if(reply.room!=null){roomCode=reply.room.code;ShowRoom(reply.room);}else{roomCode=null;OpenPanel("ROOM");}
            }));
        }
        void ShowRoom(LumaReef.Network.RoomInfo room)
        {
            ui.BeginPanel("PHÒNG • "+room.code,"Sảnh 4 người • Chia sẻ mã để mời bạn. Đồng bộ trận đấu đang phát triển.");
            ui.RoomSeats(room);
            ui.Row(5,"Rời phòng hiện tại","RỜI PHÒNG",()=>RoomRequest("POST","/v1/rooms/"+roomCode+"/leave"));
            ui.Row(6,"Cập nhật danh sách người chơi","LÀM MỚI",()=>RoomRequest("GET","/v1/rooms/"+roomCode));
        }
        void BuildEnvironment()
        {
            var go=new GameObject("Ocean backdrop"); go.transform.SetParent(world); var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=Catalog.background; sr.sortingOrder=-20; go.transform.localScale=new Vector3(10.8f,19.2f/Mathf.Max(.01f,Catalog.background.bounds.size.y),1);
            for(int i=0;i<40;i++)
            {
                var bubble=new GameObject("Ambient pearl"); bubble.transform.SetParent(world); bubble.transform.position=new Vector3(UnityEngine.Random.Range(-5.3f,5.3f),UnityEngine.Random.Range(-9f,9f),0);
                var r=bubble.AddComponent<SpriteRenderer>(); r.sprite=Catalog.ring; r.color=new Color(.3f,.85f,.9f,UnityEngine.Random.Range(.04f,.16f)); r.sortingOrder=-10; bubble.transform.localScale=Vector3.one*UnityEngine.Random.Range(.025f,.12f);
            }
        }
        void SeedFish()
        {
            for(int i=0;i<18;i++) { var f=fish.Rent(); if(f!=null)f.Spawn(Catalog.room.fish[i%24],Catalog.room.paths[i%Catalog.room.paths.Length],Vector2.zero,UnityEngine.Random.Range(.12f,.7f)); }
        }
        void Update()
        {
            if(ui==null)return;
            float realDt=Mathf.Min(Time.unscaledDeltaTime,.05f); cameraFX.Tick(realDt); ui.Tick(realDt);
            float aspect=(float)Screen.width/Screen.height; const float design=9f/16f;
            cameraView.rect=aspect>design?new Rect((1-design/aspect)/2,0,design/aspect,1):new Rect(0,(1-aspect/design)/2,1,aspect/design);
            if(ReefInput.DebugPressed&&(Debug.isDebugBuild||Application.isEditor))ui.ToggleDebug();
            if(ReefInput.BackPressed) { if(ui.ModalOpen)ui.CloseModal(); else if(gameManager.State==GameState.Playing)OpenPanel("SETTINGS"); }
            bool playing=gameManager.State==GameState.Playing;
            float dt=realDt*cameraFX.SimulationScale;
            if(gameManager.State==GameState.Menu)
            {
                for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active)fish.Items[i].Tick(dt,false);
                if(FishCount<10)spawner.Spawn(FishCategory.Small,Formation.School);
            }
            if(playing&&!loading)
            {
                Skills.Tick(dt); spawner.Tick(dt);
                for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active)fish.Items[i].Tick(dt,Skills.Active(SkillKind.Freeze));
                bool pointerAllowed=PointerAllowed();
                Vector2 aim=cameraView.ScreenToWorldPoint(ReefInput.ScreenPosition);
                if(Lock||Skills.Active(SkillKind.LockTarget)||Auto&&!ReefInput.Held)
                {
                    if(target==null||!target.Active)target=FindTarget(aim);
                    if(target!=null)aim=target.Position;
                }
                cannon.Tick(dt,aim,Loadout.Current);
                if((Auto||ReefInput.Held&&pointerAllowed)&&cannon.Ready)Fire();
                for(int i=0;i<bullets.Items.Length;i++)if(bullets.Items[i].Active) { var b=bullets.Items[i]; b.Tick(dt,arena); if(b.Active)Combat.Check(b); }
                specials.Tick();
                trailTimer-=dt;
                if(trailTimer<=0){trailTimer=.08f;for(int i=0;i<bullets.Items.Length;i++)if(bullets.Items[i].Active&&bullets.Items[i].Gun.level==5){var b=bullets.Items[i];effects.Lightning(b.Position-b.Direction*.5f,b.Position);}}
                Audio.Boss(Boss!=null); emptyTimer-=dt;
            }
            if(gameManager.State!=GameState.Paused) { effects.Tick(realDt); rewards.Tick(realDt); }
            if(roomCode!=null){roomHeartbeat+=realDt;if(roomHeartbeat>25){roomHeartbeat=0;StartCoroutine(LumaReef.Network.DatabaseManager.Request("POST","/v1/rooms/"+roomCode+"/heartbeat","{}",reply=>{if(!reply.ok)roomCode=null;}));}}
            saveTimer+=realDt; if(saveTimer>10) { saveTimer=0; if(dirty)SaveNow(); }
        }
        bool PointerAllowed()
        {
            if(ui.ModalOpen||ui.DebugOpen||EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return false;
            Vector2 p=cameraView.ScreenToViewportPoint(ReefInput.ScreenPosition); return p.x>=0&&p.x<=1&&p.y>.18f&&p.y<.86f;
        }
        FishActor FindTarget(Vector2 near)
        {
            FishActor best=null; float distance=float.MaxValue;
            for(int i=0;i<fish.Items.Length;i++) { var f=fish.Items[i]; if(!f.Active||f.Position.y< -6.5f||Mathf.Abs(f.Position.x)>5)continue; float d=(f.Position-near).sqrMagnitude; if(d<distance){best=f; distance=d;} } return best;
        }
        public bool Fire()
        {
            var gun=Loadout.Current; int cost=Loadout.Bet;
            if(bullets.Items.Length-bullets.Count<gun.barrels)return false;
            if(!Wallet.Spend(cost)) { Auto=false; if(emptyTimer<=0){ui.Toast("LOW COINS  /  Lower your bet or claim the daily reward."); emptyTimer=3;} return false; }
            for(int i=0;i<gun.barrels;i++)
            {
                float angle=(i-(gun.barrels-1)*.5f)*6;
                Vector2 direction=Quaternion.Euler(0,0,angle)*cannon.Direction;
                // A volley costs one bet. Split capture power to avoid multiplying expected rewards by barrel count.
                var bolt=bullets.Rent();bolt.Launch(cannon.Muzzle,direction,gun,cost,gun.power*Skills.Strength(SkillKind.FirePower)/gun.barrels);
                if((SaveData.selectedCosmetic&2)!=0)bolt.Renderer.color=new Color(1,.4f,.85f);
            }
            cannon.Fired(gun,Skills.Strength(SkillKind.RapidFire)*1.15f);effects.Muzzle(cannon.Muzzle,cannon.Direction,gun.color);Audio.Play(SoundCue.Fire,.9f+Loadout.Selected*.06f);quests.Add(QuestMetric.Shots,gun.barrels);return true;
        }
        void OnKilled(FishData data,Vector2 at,long reward,int bet)
        {
            bool boss=data.category==FishCategory.Boss;
            rewards.Play(at,reward,boss);
            // Hiệu ứng thu cá về HUD
            rewards.Collect(at, data.sprite);
            effects.Death(at,boss?2.2f:data.size*.65f,boss);
            if((SaveData.selectedCosmetic&32)!=0)effects.Burst(at,data.size*.6f,new Color(1,.4f,.85f));
            Audio.Play(boss?SoundCue.BossDeath:SoundCue.Death); Audio.Play(boss?SoundCue.Shower:SoundCue.Coin);
            if(data.category>=FishCategory.Large)cameraFX.Pulse(boss?.28f:data.category==FishCategory.Special?.12f:.045f,boss);
            if(boss)cameraFX.Pulse(.28f,true); // Big win announce handled by OnBigWin
            quests.Add(QuestMetric.Fish,1); quests.Add(QuestMetric.Earnings,reward);
            if(data.category==FishCategory.Small)quests.Add(QuestMetric.Small,1);
            if(data.category==FishCategory.Large)quests.Add(QuestMetric.Large,1);
            if(boss)quests.Add(QuestMetric.Boss,1);
            SaveData.lifetimeKills++; SaveData.exp+=boss?100:5;
            while(SaveData.exp>=SaveData.level*100) { SaveData.exp-=SaveData.level*100; SaveData.level++; Wallet.AddDiamonds(2); }
            dirty=true;
        }
        void OnBigWin(LumaReef.Data.FishData data,Vector2 at,long reward)
        {
            bool isJackpot = data.category==FishCategory.Boss;
            // Hiệu ứng NỔ HŨ sống động bùng nổ
            effects.Burst(at, isJackpot?3.5f:2f, ReefUI.Gold);
            cameraFX.Pulse(isJackpot?1.2f:.18f, isJackpot);
            if(isJackpot)
            {
                Audio.Play(SoundCue.Shower);
                // Mưa vàng toàn màn hình cực khủng
                for(int i=0;i<35;i++) rewards.Play(new Vector2(UnityEngine.Random.Range(-5f,5f),UnityEngine.Random.Range(-7f,7f)),0,false);
                ui.ShowJackpot(data.displayName, reward);
            }
            else
            {
                ui.Announce($"✨ THẮNG LỚN!  {data.displayName.ToUpperInvariant()}  +{reward.ToString("N0")}");
            }
        }
        void OnSkill(SkillData data)
        {
            ui.Toast(data.displayName.ToUpperInvariant()+" ACTIVATED"); Audio.Play(data.kind==SkillKind.Freeze?SoundCue.Freeze:data.kind==SkillKind.Lightning?SoundCue.Lightning:SoundCue.Explosion);
            Vector2 at=target!=null&&target.Active?target.Position:Vector2.zero;
            if(data.kind==SkillKind.Lightning)specials.Chain(at,Loadout.Bet,data.strength);
            if(data.kind==SkillKind.Bomb)Combat.Area(at,data.radius,Loadout.Bet,data.strength,new Color(1,.4f,.15f));
            if(data.kind==SkillKind.Freeze)effects.Freeze();
        }
        public void ToggleAuto() { Auto=!Auto; }
        public void ToggleLock() { Lock=!Lock; target=null; }
        public void UseSkill(int index) { if(gameManager.State==GameState.Playing)Skills.Use(index); }
        public void Play() { if(!loading)StartCoroutine(LoadScene("GameScene",true)); }
        public void Menu() { if(loading)return; SaveNow(); StartCoroutine(LoadScene("MainMenuScene",false)); }
        IEnumerator LoadScene(string name,bool play)
        {
            loading=true; gameManager.SetState(GameState.Loading); ui.Loading(true); yield return null;
            var operation=SceneManager.LoadSceneAsync(name); while(operation!=null&&!operation.isDone)yield return null;
            fish.Clear(); bullets.Clear(); effects.Clear(); rewards.Clear(); cameraFX.Reset(); Auto=Lock=false; target=null; Audio.Boss(false);
            if(play) { spawner=new SpawnDirector(Catalog.room,fish); spawner.Announcement+=OnAnnouncement; }
            SeedFish(); ui.ShowMenu(!play); gameManager.SetState(play?GameState.Playing:GameState.Menu); loading=false; ui.Loading(false);
        }
        void OnAnnouncement(string text) { ui.Announce(text); if(spawner.HasBoss){Audio.Play(SoundCue.Warning); cameraFX.Pulse(.15f);} }
        public void Resume() { if(gameManager.State==GameState.Paused)gameManager.SetState(GameState.Playing); SaveNow(); }
        public void OpenPanel(string name,int category=0)
        {
            if(gameManager.State==GameState.Playing)gameManager.SetState(GameState.Paused);
            if(name=="ACCOUNT"){ui.AccountForm(LumaReef.Network.DatabaseManager.LoggedIn);return;}
            if(name=="ROOM"){
                if(!LumaReef.Network.DatabaseManager.LoggedIn){ui.AccountForm(false);ui.Toast("Đăng nhập để tạo phòng 4 người.");return;}
                if(roomCode!=null){RoomRequest("GET","/v1/rooms/"+roomCode);return;}
                ui.BeginPanel("PHÒNG 4 NGƯỜI","Tạo phòng hoặc nhập mã 8 ký tự. Đây là sảnh chờ; trận online chưa bật.");
                var code=ui.Input(1,"Mã phòng (8 ký tự)",false);code.characterLimit=8;
                ui.Row(3,"Mời bạn vào phòng mới","TẠO PHÒNG",()=>RoomRequest("POST","/v1/rooms"));
                ui.Row(4,"Tham gia bằng mã mời","VÀO PHÒNG",()=>RoomRequest("POST","/v1/rooms/"+code.text.Trim().ToUpperInvariant()+"/join"));return;
            }
            if(name=="SHOP") { Shop(category); return; }
            if(name=="QUEST")
            {
                quests.RefreshDay(); ui.BeginPanel("DAILY MISSIONS","A fresh expedition every UTC day. Rewards use virtual coins.");
                for(int i=0;i<Catalog.quests.Length;i++){int index=i; var q=Catalog.quests[i]; ui.Row(i,q.description+"   "+SaveData.questProgress[i]+" / "+q.target,SaveData.questClaimed[i]?"CLAIMED":"+"+q.reward,()=>{quests.Claim(index); SaveNow(); OpenPanel("QUEST");},!SaveData.questClaimed[i]&&SaveData.questProgress[i]>=q.target);} return;
            }
            if(name=="SETTINGS")
            {
                ui.BeginPanel("SETTINGS","Hold mouse / touch to fire. F3 opens the development console. Escape pauses.");
                ui.Row(0,"MUSIC   "+Mathf.RoundToInt(SaveData.music*100)+"%","CHANGE",()=>{SaveData.music=SaveData.music>=.99f?0:Mathf.Min(1,SaveData.music+.2f); Audio.Volume(SaveData.music,SaveData.sfx); OpenPanel(name);});
                ui.Row(1,"SFX   "+Mathf.RoundToInt(SaveData.sfx*100)+"%","CHANGE",()=>{SaveData.sfx=SaveData.sfx>=.99f?0:Mathf.Min(1,SaveData.sfx+.2f); Audio.Volume(SaveData.music,SaveData.sfx); OpenPanel(name);});
                ui.Row(3,"Progress is saved on this device.","SAVE NOW",()=>{SaveNow();ui.Toast("PROGRESS SAVED");});
                ui.Row(4,"Return to the expedition lobby.","MAIN MENU",Menu); return;
            }
            if(name=="DAILY REWARD") { ui.BeginPanel(name,"Your daily dive supplies. Resets at 00:00 UTC."); bool claimed=string.CompareOrdinal(SaveData.dailyClaim,DateTime.UtcNow.ToString("yyyy-MM-dd"))>=0; ui.Row(1,"1,500 virtual coins",claimed?"CLAIMED":"CLAIM",()=>{quests.Daily();SaveNow();OpenPanel(name);},!claimed); return; }
            if(name=="ACHIEVEMENT") { ui.BeginPanel(name,"Your first chapter beneath the waves."); ui.Row(1,"REEF EXPLORER  /  Catch 100 fish   "+Math.Min(100,SaveData.lifetimeKills)+" / 100",SaveData.achievementClaimed?"CLAIMED":"+5 GEMS",()=>{if(SaveData.lifetimeKills>=100&&!SaveData.achievementClaimed){SaveData.achievementClaimed=true;Wallet.AddDiamonds(5);SaveNow();OpenPanel(name);}},SaveData.lifetimeKills>=100&&!SaveData.achievementClaimed); return; }
            
            if(name=="FRIENDS") { ui.BeginPanel("FRIENDS","You are playing offline. Social connections will require a server."); return; }
            ui.BeginPanel("DIVER MAIL","Welcome to Luma Reef. Your starter cannon and 5,000 coins are ready.");
            ui.Row(1,"Field note: special residents trigger chain effects.","GOT IT",ui.CloseModal);
        }
        void Shop(int category)
        {
            string[] categories={"CANNONS","BOLTS","SKILLS","AVATAR","FRAME","EFFECTS"};
            ui.BeginPanel("REEF OUTFITTERS / "+categories[category],"Virtual currency only. Balance: "+Wallet.Coins.ToString("N0")+" coins / "+Wallet.Diamonds+" gems.");
            if(category==0)for(int i=0;i<Catalog.room.guns.Length;i++) { int index=i; var g=Catalog.room.guns[i]; bool owned=Loadout.Unlocked(i); string label=Loadout.Selected==i?"EQUIPPED":"EQUIP"; ui.Row(i,"CẤP "+(i+1)+"  "+g.displayName+"  |  CƯỢC "+g.bet+" VX",label,()=>{Loadout.Purchase(index,Wallet); SaveNow(); Shop(category);}); }
            else if(category==2)ui.Row(1,"Recharge every skill immediately","5 GEMS",()=>{if(Wallet.SpendDiamonds(5)){Skills.Reset();ui.Toast("SKILLS RECHARGED");SaveNow();}else ui.Toast("Not enough gems.");});
            else
            {
                int flag=1<<category; bool owned=(SaveData.cosmetics&flag)!=0;
                ui.Row(1,"AURORA / "+categories[category]+" cosmetic",owned?"EQUIP":"10 GEMS",()=>{if(owned||Wallet.SpendDiamonds(10)){SaveData.cosmetics|=flag;SaveData.selectedCosmetic|=flag;SaveNow();ui.Toast("AURORA "+categories[category]+" EQUIPPED");Shop(category);}else ui.Toast("Not enough gems.");});
            }
            ui.Tabs(categories,index=>Shop(index));
        }
        public void DebugAction(int index)
        {
            switch(index)
            {
                case 0:Wallet.Credit(10000);break;
                case 1:spawner.Spawn(FishCategory.Small);break;
                case 2:spawner.Spawn(FishCategory.Large);break;
                case 3:spawner.Spawn(FishCategory.Special);break;
                case 4:spawner.Spawn(FishCategory.Boss);break;
                case 5:bool old=Combat.GodMode;Combat.GodMode=true;for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active)Combat.Hit(fish.Items[i],Loadout.Bet);Combat.GodMode=old;break;
                case 6:Wallet.Credit(Catalog.room.guns[(Loadout.Selected+1)%8].unlockCost);Loadout.Purchase((Loadout.Selected+1)%8,Wallet);break;
                case 7:Loadout.ChangeBet(Loadout.BetIndex==2?-2:1);break;
                case 8:Combat.GodMode=!Combat.GodMode;break;
                case 9:showColliders=!showColliders;break;
                case 10:spawner.Wave();break;
            }
        }
        public bool SaveNow()
        {
            if(store==null)return false;
            SaveData.coins=Wallet.Coins;SaveData.diamonds=Wallet.Diamonds;SaveData.selectedGun=Loadout.Selected;SaveData.unlockedGuns=Loadout.UnlockMask;
            bool ok=store.Save(SaveData); if(!accountBusy&&LumaReef.Network.DatabaseManager.LoggedIn)pendingProfile?.Write(LumaReef.Network.DatabaseManager.Revision,SaveData);if(ok)dirty=false; if(!smoke&&!accountBusy&&LumaReef.Network.DatabaseManager.LoggedIn&&!syncBlocked){if(syncBusy)cloudPending=true;else StartCoroutine(SyncProfile());} return ok;
        }
        void OnApplicationPause(bool pause) { if(pause){SaveNow();if(gameManager!=null&&gameManager.State==GameState.Playing)OpenPanel("SETTINGS");} }
        void OnApplicationQuit() { SaveNow(); }
        void OnDrawGizmos()
        { if(!showColliders||fish==null)return; Gizmos.color=Color.green; for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active)Gizmos.DrawWireSphere(fish.Items[i].Position,fish.Items[i].Radius); }
        IEnumerator SmokeTest()
        {
            yield return LoadScene("MainMenuScene",false);yield return new WaitForSeconds(.75f);
            ui.ValidateLayout();
            ReefCapture.Save(cameraView,GetComponentInChildren<Canvas>(),"lobby-smoke.png");yield return new WaitForSeconds(.5f);
            OpenPanel("ACCOUNT");yield return null;ReefCapture.Save(cameraView,GetComponentInChildren<Canvas>(),"login-smoke.png");ui.CloseModal();
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);ui.ValidateLayout();
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--reef-auth-smoke")>=0)yield return AuthSmokeTest();
            yield return LoadScene("GameScene",true); yield return new WaitForSeconds(1);
            long initial=Wallet.Coins; Fire(); Debug.Assert(Wallet.Coins<initial,"Shot must debit wallet");
            var f=fish.Items[0]; if(!f.Active)f.Spawn(Catalog.room.fish[0],Catalog.room.paths[0],Vector2.zero,.4f);
            long before=Wallet.Coins; long expected=(long)Math.Round(Loadout.Bet*(double)f.Data.multiplier);
            Combat.GodMode=true; Combat.Hit(f,Loadout.Bet); Debug.Assert(Wallet.Coins>=before+expected,"Kill reward at least base");
            long credited=Wallet.Coins;Combat.Hit(f,Loadout.Bet); Debug.Assert(Wallet.Coins==credited,"No duplicate kill reward"); Combat.GodMode=false;
            Skills.Use(0); Debug.Assert(Skills.Active(SkillKind.Freeze),"Freeze active"); Debug.Assert(!Skills.Use(0),"Cooldown blocks repeat");
            DebugAction(4); Debug.Assert(Boss!=null,"Boss spawns"); DebugAction(4);
            int bossCount=0;for(int i=0;i<fish.Items.Length;i++)if(fish.Items[i].Active&&fish.Items[i].Data.category==FishCategory.Boss)bossCount++;
            Debug.Assert(bossCount==1,"Single boss cap"); Debug.Assert(SaveNow(),"Save round trip write"); Debug.Assert(store.Load().coins==Wallet.Coins,"Save round trip balance");
            if(Boss!=null)Boss.Spawn(Boss.Data,Catalog.room.paths[1],Vector2.zero,.5f);
            Skills.Tick(6);
            Auto=true; yield return new WaitForSeconds(2); Auto=false;
            ReefCapture.Save(cameraView,GetComponentInChildren<Canvas>(),"gameplay-smoke.png");
            yield return new WaitForSeconds(.6f);OpenPanel("SHOP");yield return new WaitForSeconds(.5f);
            ReefCapture.Save(cameraView,GetComponentInChildren<Canvas>(),"shop-smoke.png");yield return new WaitForSeconds(.5f);ui.CloseModal();
            Debug.Assert(gameManager.State==GameState.Playing,"Closing shop resumes gameplay");
            if(LumaReef.Network.DatabaseManager.LoggedIn){yield return LumaReef.Network.DatabaseManager.Request("POST","/v1/logout","{}",reply=>Debug.Assert(reply.ok,"Logout succeeds"));LumaReef.Network.DatabaseManager.Clear();}
            Debug.Log(smokeFailed?"LUMA_REEF_SMOKE_FAIL":"LUMA_REEF_SMOKE_PASS");Application.Quit(smokeFailed?1:0);
        }
    }
}
