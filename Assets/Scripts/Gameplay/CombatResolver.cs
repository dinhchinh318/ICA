using System;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Fish;
using LumaReef.Bullet;
using LumaReef.Economy;
namespace LumaReef.Gameplay
{
    public interface ICombatAuthority { bool ResolveHit(FishActor fish,float power); }
    // A future server owns RNG, HP, wallet mutations and emits the same presentation events.
    public sealed class OfflineCombatAuthority : ICombatAuthority
    {
        readonly System.Random random;
        public OfflineCombatAuthority(int seed) { random=new System.Random(seed); }
        public bool ResolveHit(FishActor fish,float power)
        {
            if(fish.Data.category==FishCategory.Boss) { fish.HP-=power; return fish.HP<=0; }
            double chance=1-Math.Pow(1-Mathf.Clamp01(fish.Data.killChance),Math.Max(.01,power));
            return random.NextDouble()<chance;
        }
    }
    public sealed class CombatResolver
    {
        readonly FixedPool<FishActor> fish;
        readonly IWallet wallet;
        readonly ICombatAuthority authority;
        public event Action<FishData,Vector2,long,int> Killed;
        public event Action<Vector2,float,Color> Impact;
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
            bullet.Release(); Area(position,bullet.Gun.netRadius,bullet.Bet,bullet.Power,bullet.Gun.color);
        }
        public void Area(Vector2 center,float radius,int bet,float power,Color color)
        {
            Impact?.Invoke(center,radius,color);
            for(int i=0;i<fish.Items.Length;i++)
            {
                var f=fish.Items[i]; if(!f.Active||(f.Position-center).sqrMagnitude>(radius+f.Radius)*(radius+f.Radius))continue;
                Hit(f,bet,power);
            }
        }
        public void Hit(FishActor f,int bet,float power)
        {
            if(!f.Active)return;
            if(GodMode||authority.ResolveHit(f,power))
            {
                var data=f.Data; Vector2 position=f.Position;
                long reward=(long)Math.Round(bet*(double)data.multiplier);
                // Retire before callbacks: special chains cannot reward the same fish twice.
                f.Die(); wallet.Credit(reward); Killed?.Invoke(data,position,reward,bet);
            }
            else f.Hit();
        }
    }
}
