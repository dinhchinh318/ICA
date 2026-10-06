using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Economy;
using LumaReef.Fish;
using LumaReef.Bullet;
using LumaReef.Gameplay;
using LumaReef.Gun;
using LumaReef.Quest;
using LumaReef.Save;
using LumaReef.Skill;
using LumaReef.Spawn;
namespace LumaReef.Editor
{
    public static class ReefValidation
    {
        static int checks;
        static void Check(bool condition,string message) { checks++;if(!condition)throw new Exception("Validation failed: "+message); }
        [MenuItem("Luma Reef/Run Domain and Content Checks")]
        public static void Run()
        {
            checks=0;var catalog=AssetDatabase.LoadAssetAtPath<ReefCatalog>("Assets/Resources/ReefCatalog.asset");
            Check(catalog!=null,"catalog exists");var room=catalog.room;
            Check(room.fish.Length>=25&&room.guns.Length==8&&room.paths.Length>=20,"content counts");
            Check(catalog.background!=null&&catalog.audio.mixer!=null&&catalog.particleMaterial!=null,"presentation references");
            Check(catalog.audio.musicGroup!=null&&catalog.audio.sfxGroup!=null&&catalog.audio.musicGroup!=catalog.audio.sfxGroup,"separate audio buses");
            foreach(var f in room.fish)Check(f!=null&&f.sprite!=null&&f.animation!=null&&f.multiplier>0&&f.speed>0,"fish data "+f.name);
            foreach(var g in room.guns)Check(g.bet>0&&g.bullet!=null&&g.bullet.sprite!=null&&g.fireSound!=null&&g.netRadius>0,"gun data "+g.name);
            foreach(var p in room.paths){Check(p.points.Length>=4&&(p.points.Length-1)%3==0&&p.length>0,"valid cubic path");Check(Mathf.Abs(p.Evaluate(0).x)>6&&Mathf.Abs(p.Evaluate(1).x)>6,"offscreen portrait endpoints");for(int i=0;i<=100;i++){var v=p.Evaluate(i/100f);Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y),"finite path sample");}}
            Check(catalog.lobbyBackground!=null&&catalog.cannonSprites.Length==8&&catalog.uiIcons.Length==16,"AI portrait assets");
            Check(catalog.effectSprites.Length==8&&catalog.uiSkins.Length==9,"premium VFX and UI skin references");
            for(int i=0;i<8;i++)for(int j=i+1;j<8;j++)Check(room.guns[i].bullet.sprite!=room.guns[j].bullet.sprite,"distinct AI projectile per cannon");
            var wallet=new Wallet(100,10);Check(!wallet.Spend(-1)&&!wallet.Spend(101)&&wallet.Coins==100,"invalid debits rejected");Check(wallet.Spend(10)&&wallet.Coins==90,"valid debit");wallet.Credit(20);Check(wallet.Coins==110,"credit");
            var loadout=new GunLoadout(room,7,1);Check(loadout.Selected==0,"locked gun rejected");Check(!loadout.Purchase(7,wallet),"unaffordable gun rejected");loadout.ChangeBet(-8);Check(loadout.BetIndex==0,"minimum bet");loadout.ChangeBet(50);Check(loadout.BetIndex==25,"maximum bet");
            var skills=new SkillController(catalog.skills);Check(skills.Use(0)&&skills.Active(SkillKind.Freeze)&&!skills.Use(0),"freeze cooldown");skills.Tick(catalog.skills[0].duration+.1f);Check(!skills.Active(SkillKind.Freeze),"freeze expires");skills.Tick(100);Check(skills.Use(0),"cooldown recovers");
            var save=new PlayerSave();var quests=new QuestController(save,catalog.quests,wallet);quests.Add(QuestMetric.Fish,50);long before=wallet.Coins;Check(quests.Claim(0)&&wallet.Coins==before+catalog.quests[0].reward,"quest payout");Check(!quests.Claim(0),"no duplicate quest payout");Check(quests.Daily()&&!quests.Daily(),"daily reward once");
            string folder=System.IO.Path.Combine(Application.temporaryCachePath,"ReefDomainChecks");Directory.CreateDirectory(folder);var store=new JsonSaveStore(folder);save.coins=4242;Check(store.Save(save)&&store.Load().coins==4242,"save round trip");save.coins=5252;Check(store.Save(save),"atomic replace");File.WriteAllText(System.IO.Path.Combine(folder,"luma-reef-save.json"),"broken");Check(store.Load().coins==4242,"backup recovery");
            var root=new GameObject("Validation objects");
            try
            {
                var pool=new FixedPool<FishActor>(room.maxFish,i=>new FishActor(root.transform));var f=pool.Rent();f.Spawn(room.fish[0],room.paths[0],Vector2.zero,.5f);
                var combat=new CombatResolver(pool,wallet,new OfflineCombatAuthority(17));combat.GodMode=true;before=wallet.Coins;int kills=0;combat.Killed+=(data,position,reward,bet)=>kills++;
                combat.Hit(f,10);combat.Hit(f,10);Check(kills==1&&wallet.Coins>=before+(long)(10*f.Data.multiplier),"one reward per fish");
                f.Spawn(room.fish[0],room.paths[0],Vector2.zero,.5f);var b=new BulletActor(root.transform,catalog.disk);b.Launch(f.Position-Vector2.right,Vector2.right,room.guns[0],17,1);b.Tick(.15f,new Rect(-10,-5,20,10));before=wallet.Coins;combat.Check(b);Check(!f.Active&&!b.Active&&wallet.Coins>=before+(long)(17*f.Data.multiplier),"swept collision and shot bet snapshot");
                var director=new SpawnDirector(room,pool);for(int i=0;i<100;i++)director.Spawn(FishCategory.Small);Check(pool.Count<=room.maxFish,"fish cap");pool.Clear();director.Spawn(FishCategory.Boss);director.Spawn(FishCategory.Boss);Check(pool.Count==1,"boss cap");director.Wave();Check(pool.Count==room.waveCount+1,"wave keeps boss and spawns configured school");
                pool.Clear();Check(pool.Count==0,"pool reset");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            File.WriteAllText("Temp/reef-validation-result.txt","PASS / "+checks+" checks / "+DateTime.UtcNow.ToString("O"));Debug.Log("LUMA_REEF_DOMAIN_PASS / "+checks);
        }
    }
}
