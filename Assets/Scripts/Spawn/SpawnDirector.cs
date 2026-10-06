using System;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Fish;
namespace LumaReef.Spawn
{
    public enum Formation { Single, Line, VShape, Circle, School, Wave }
    public sealed class SpawnDirector
    {
        readonly RoomData room;
        readonly FixedPool<FishActor> pool;
        float spawnTimer, bossTimer, waveTimer;
        public event Action<string> Announcement;
        public SpawnDirector(RoomData room, FixedPool<FishActor> pool) { this.room=room; this.pool=pool; bossTimer=room.bossInterval; waveTimer=room.waveInterval; }
        public bool HasBoss { get { for(int i=0;i<pool.Items.Length;i++)if(pool.Items[i].Active&&pool.Items[i].Data.category==FishCategory.Boss)return true; return false; } }
        public void Tick(float dt)
        {
            spawnTimer-=dt; bossTimer-=dt; waveTimer-=dt;
            if(spawnTimer<=0) { spawnTimer=room.spawnInterval; Spawn(ChooseCategory(),(Formation)UnityEngine.Random.Range(0,6)); }
            if(bossTimer<=0&&!HasBoss) { Spawn(FishCategory.Boss); bossTimer=room.bossInterval; }
            if(waveTimer<=0) { Wave(); waveTimer=room.waveInterval; }
        }
        FishCategory ChooseCategory()
        {
            float total=0; for(int i=0;i<4;i++)total+=room.categoryWeights[i];
            float r=UnityEngine.Random.value*total;
            for(int i=0;i<4;i++) { r-=room.categoryWeights[i]; if(r<=0)return (FishCategory)i; } return FishCategory.Small;
        }
        FishData Choose(FishCategory category)
        {
            float total=0; for(int i=0;i<room.fish.Length;i++)if(room.fish[i].category==category)total+=room.fish[i].spawnWeight;
            float r=UnityEngine.Random.value*total;
            for(int i=0;i<room.fish.Length;i++)if(room.fish[i].category==category) { r-=room.fish[i].spawnWeight; if(r<=0)return room.fish[i]; }
            return room.fish[0];
        }
        public void Spawn(FishCategory category, Formation formation=Formation.Single)
        {
            if(category==FishCategory.Boss&&HasBoss)return;
            FishData data=Choose(category); int count=category>=FishCategory.Large?1:formation==Formation.Single?1:UnityEngine.Random.Range(3,7);
            var path=room.paths[UnityEngine.Random.Range(0,room.paths.Length)];
            for(int i=0;i<count;i++)
            {
                var fish=pool.Rent(); if(fish==null)break;
                Vector2 offset=Offset(formation,i,count);
                fish.Spawn(data,path,offset,-i*.018f);
            }
            if(category==FishCategory.Boss)Announcement?.Invoke("BOSS INCOMING  /  "+data.displayName.ToUpperInvariant());
        }
        public void Wave()
        {
            // Reserve the arena for the wave, retaining an existing boss.
            for(int i=0;i<pool.Items.Length;i++)if(pool.Items[i].Active&&pool.Items[i].Data.category!=FishCategory.Boss)pool.Items[i].Release();
            var data=Choose(FishCategory.Small); var path=room.paths[0];
            for(int i=0;i<room.waveCount;i++) { var f=pool.Rent(); if(f==null)break; f.Spawn(data,path,new Vector2(-(i%5)*.7f,(i/5-2)*.6f)); }
            Announcement?.Invoke("REEF RUSH  /  A SCHOOL OF OPPORTUNITY");
        }
        static Vector2 Offset(Formation f,int i,int count)
        {
            switch(f)
            {
                case Formation.Line: return new Vector2(-i*.65f,0);
                case Formation.VShape: return new Vector2(-i*.5f,(i%2==0?1:-1)*i*.25f);
                case Formation.Circle: float a=i*Mathf.PI*2/count; return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.7f;
                case Formation.School: return new Vector2(-(i%3)*.7f,(i/3)*.6f);
                case Formation.Wave: return new Vector2(-i*.65f,Mathf.Sin(i)*.7f);
                default:return Vector2.zero;
            }
        }
    }
}
