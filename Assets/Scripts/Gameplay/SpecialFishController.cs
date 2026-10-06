using System;
using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Fish;
using LumaReef.Economy;
namespace LumaReef.Gameplay
{
    public sealed class SpecialFishController
    {
        struct Pending { public SpecialEffect effect; public Vector2 at; public int bet; }
        readonly Pending[] pending=new Pending[64];
        int read,write,count;
        readonly RoomData room;
        readonly CombatResolver combat;
        readonly FixedPool<FishActor> pool;
        readonly IWallet wallet;
        public event Action<Vector2,Vector2> Lightning;
        public event Action<Vector2,long> Treasure;
        public SpecialFishController(RoomData room,CombatResolver combat,FixedPool<FishActor> pool,IWallet wallet)
        { this.room=room; this.combat=combat; this.pool=pool; this.wallet=wallet; combat.Killed+=Enqueue; }
        void Enqueue(FishData data,Vector2 at,long reward,int bet)
        {
            if(data.special==SpecialEffect.None||count==pending.Length)return;
            pending[write]=new Pending { effect=data.special,at=at,bet=bet }; write=(write+1)%pending.Length; count++;
        }
        public void Tick()
        {
            // Iterative bounded queue avoids recursive chain explosions and stack growth.
            int budget=pending.Length;
            while(count>0&&budget-->0)
            {
                var p=pending[read]; read=(read+1)%pending.Length; count--;
                if(p.effect==SpecialEffect.Lightning)Chain(p.at,p.bet,room.specialPower);
                else if(p.effect==SpecialEffect.Bomb||p.effect==SpecialEffect.Lantern)combat.Area(p.at,room.specialRadius,p.bet,room.specialPower,Color.cyan);
                else { long bonus=p.bet*(p.effect==SpecialEffect.Golden?12:6); wallet.Credit(bonus); Treasure?.Invoke(p.at,bonus); }
            }
        }
        public void Chain(Vector2 at,int bet,float power)
        {
            int hits=0;
            for(int i=0;i<pool.Items.Length&&hits<room.chainTargets;i++)
            {
                var f=pool.Items[i]; if(!f.Active||(f.Position-at).sqrMagnitude>room.chainRadius*room.chainRadius)continue;
                Lightning?.Invoke(at,f.Position); combat.Hit(f,bet); hits++;
            }
        }
    }
}
