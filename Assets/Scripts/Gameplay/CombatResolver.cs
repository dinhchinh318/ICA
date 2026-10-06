using System;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Fish;
using LumaReef.Bullet;
using LumaReef.Economy;
namespace LumaReef.Gameplay
{
    public interface ICombatAuthority { bool ResolveHit(FishData data); }
    // A future server owns RNG, HP, wallet mutations and emits the same presentation events.
    public sealed class OfflineCombatAuthority : ICombatAuthority
    {
        readonly System.Random random;
        public OfflineCombatAuthority(int seed) { random=new System.Random(seed); }
        
        public bool ResolveHit(FishData data)
        {
            // Công thức casino: tỉ lệ bắt = killChance gốc, giảm dần theo hệ số cá
            // Cá nhỏ (x2-x4): ~20-35% / viên đạn
            // Cá vừa (x6-x12): ~5-15% / viên đạn  
            // Cá lớn (x15-x35): ~1-5% / viên đạn
            // Boss/Đặc biệt (x100+): ~0.1-0.8% / viên đạn (như nổ hũ thật)
            double chance = data.killChance;
            return random.NextDouble() < chance;
        }
    }
    
    public sealed class CombatResolver
    {
        readonly FixedPool<FishActor> fish;
        readonly IWallet wallet;
        readonly ICombatAuthority authority;
        readonly System.Random gacha = new System.Random();
        
        public event Action<FishData,Vector2,long,int> Killed;
        public event Action<Vector2,float,Color> Impact;
        public event Action<FishData,Vector2,long> BigWin; // Thắng lớn: cá boss/special
        public bool GodMode;
        
        public CombatResolver(FixedPool<FishActor> fish,IWallet wallet,ICombatAuthority authority) { this.fish=fish; this.wallet=wallet; this.authority=authority; }
        
        public void Check(BulletActor bullet)
        {
            Vector2 delta=bullet.Position-bullet.Previous; float distance=delta.sqrMagnitude;
            FishActor first=null; float nearest=2;
            for(int i=0;i<fish.Items.Length;i++)
            {
                var f=fish.Items[i]; if(!f.Active)continue;
                float t=distance>0?Mathf.Clamp01(Vector2.Dot(f.Position-bullet.Previous,delta)/distance):0;
                if(t<nearest&&(f.Position-(bullet.Previous+delta*t)).sqrMagnitude<Mathf.Pow(f.Radius+.09f,2)) { nearest=t; first=f; }
            }
            if(first==null)return;
            Vector2 position=bullet.Previous+delta*nearest;
            bullet.Release();
            // Chỉ dính đúng 1 con cá (first) — không AOE
            Impact?.Invoke(position,bullet.Gun.netRadius,bullet.Gun.color);
            Hit(first, bullet.Bet);
        }
        
        public void Area(Vector2 center,float radius,int bet,float power,Color color)
        {
            Impact?.Invoke(center,radius,color);
            for(int i=0;i<fish.Items.Length;i++)
            {
                var f=fish.Items[i]; if(!f.Active||(f.Position-center).sqrMagnitude>(radius+f.Radius)*(radius+f.Radius))continue;
                Hit(f,bet);
            }
        }
        
        public void Hit(FishActor f,int bet)
        {
            if(!f.Active)return;
            if(GodMode||authority.ResolveHit(f.Data))
            {
                var data=f.Data; Vector2 position=f.Position;
                
                // Phần thưởng có GACHA:
                // - Cá nhỏ/vừa: reward cố định = bet * multiplier
                // - Cá lớn/special/boss: có xác suất GACHA tăng thêm từ 1.5x → 5x (như nổ hũ nhiều mức)
                long baseReward = (long)Math.Round(bet * (double)data.multiplier);
                long reward = baseReward;
                
                if(data.category >= FishCategory.Large)
                {
                    // Gacha cấp độ thưởng: xác suất nhận được thưởng lớn hơn mức base
                    double roll = gacha.NextDouble();
                    if(data.category == FishCategory.Boss || data.category == FishCategory.Special)
                    {
                        // Boss/Special: gacha 3 mức
                        // Boss/Special: gacha 3 mức, tỉ lệ cực thấp
                        if(roll < 0.01)       reward = (long)(baseReward * 5.0); // 1%: Jackpot x5
                        else if(roll < 0.05)  reward = (long)(baseReward * 2.5); // 4%: Mega x2.5
                        else if(roll < 0.15)  reward = (long)(baseReward * 1.5); // 10%: Bonus x1.5
                        // 85%: mức base bình thường
                    }
                    else
                    {
                        // Cá lớn thường: gacha 2 mức, tỉ lệ thấp
                        if(roll < 0.03)       reward = (long)(baseReward * 2.0); // 3%: Bonus x2
                        else if(roll < 0.10)  reward = (long)(baseReward * 1.3); // 7%: Bonus x1.3
                        // 90%: mức base
                    }
                }
                
                // Retire before callbacks: special chains cannot reward the same fish twice.
                f.Die(); wallet.Credit(reward); Killed?.Invoke(data,position,reward,bet);
                
                // Phát sự kiện BigWin nếu thắng lớn
                bool isBigWin = data.category >= FishCategory.Boss || 
                                data.category >= FishCategory.Special ||
                                (data.category == FishCategory.Large && reward > baseReward * 1.5);
                if(isBigWin) BigWin?.Invoke(data,position,reward);
            }
            else f.Hit();
        }
    }
}
