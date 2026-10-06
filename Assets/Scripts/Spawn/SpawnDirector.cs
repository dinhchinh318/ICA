using System;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Fish;
namespace LumaReef.Spawn
{
    public enum Formation { Single, Line, VShape, Circle, School, Wave }
    public enum WaveType { Chaotic, Beautiful }
    
    public sealed class SpawnDirector
    {
        readonly RoomData room;
        readonly FixedPool<FishActor> pool;
        
        float waveTimer;
        float spawnTimer;
        float bossTimer;
        int waveCount;
        WaveType currentWaveType;
        
        public event Action<string> Announcement;
        
        public SpawnDirector(RoomData room, FixedPool<FishActor> pool) { 
            this.room=room; 
            this.pool=pool; 
            StartNextWave();
        }
        
        public bool HasBoss { get { for(int i=0;i<pool.Items.Length;i++)if(pool.Items[i].Active&&pool.Items[i].Data.category==FishCategory.Boss)return true; return false; } }
        
        public void Tick(float dt)
        {
            waveTimer -= dt;
            spawnTimer -= dt;
            bossTimer -= dt;
            
            if(waveTimer <= 0) { 
                StartNextWave(); 
            }
            
            if(spawnTimer <= 0) { 
                if (currentWaveType == WaveType.Chaotic) {
                    // Hỗn loạn: ra liên tục, rải rác từng con
                    spawnTimer = UnityEngine.Random.Range(0.6f, 1.2f);
                    Spawn(ChooseCategory(), Formation.Single);
                } else {
                    // Đội hình đẹp: ra theo đàn lớn, nghỉ lâu hơn chút
                    spawnTimer = UnityEngine.Random.Range(4.0f, 7.0f);
                    Formation f = (Formation)UnityEngine.Random.Range(1, 6);
                    Spawn(ChooseCategory(), f);
                }
            }
            
            // Đảm bảo đợt nào cũng có boss xuất hiện
            if(bossTimer <= 0 && !HasBoss) { 
                Spawn(FishCategory.Boss); 
                bossTimer = 9999f; // Đã ra boss cho đợt này rồi
            }
        }
        
        void StartNextWave()
        {
            waveCount++;
            waveTimer = UnityEngine.Random.Range(60f, 120f); // 1 đến 2 phút mỗi đợt
            currentWaveType = (WaveType)UnityEngine.Random.Range(0, 2);
            
            // Hẹn giờ Boss ra giữa đợt
            bossTimer = UnityEngine.Random.Range(20f, waveTimer - 20f);
            
            if (currentWaveType == WaveType.Chaotic) {
                Announcement?.Invoke($"ĐỢT {waveCount} : BIỂN ĐỘNG - CÁ XUẤT HIỆN HỖN LOẠN");
            } else {
                Announcement?.Invoke($"ĐỢT {waveCount} : ĐỘI HÌNH TIẾN CÔNG");
            }
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
            
            FishData data=Choose(category); 
            int count=category>=FishCategory.Large?1:formation==Formation.Single?1:UnityEngine.Random.Range(6,12); // Tăng đàn cá lên
            
            var path=room.paths[UnityEngine.Random.Range(0,room.paths.Length)];
            for(int i=0;i<count;i++)
            {
                var fish=pool.Rent(); if(fish==null)break;
                Vector2 offset=Offset(formation,i,count);
                fish.Spawn(data,path,offset,-i*.018f);
            }
            
            if(category==FishCategory.Boss) Announcement?.Invoke($"⚠️ SIÊU BOSS XUẤT HIỆN : {data.displayName.ToUpperInvariant()} ⚠️");
        }
        
        public void Wave()
        {
            // Chỉ bắt đầu wave mới, KHÔNG clear cá cũ nữa để tránh bị đứt quãng giữa chừng
            StartNextWave();
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
