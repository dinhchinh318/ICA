using LumaReef.Data;
using LumaReef.Economy;
namespace LumaReef.Gun
{
    public sealed class GunLoadout
    {
        readonly RoomData room;
        public int Selected { get; private set; }
        public int BetIndex { get; private set; }
        public int UnlockMask { get; private set; }
        public GunData Current => room.guns[Selected];
        
        static readonly int[] vals = { 5,10,20,50,100,200,500,1000,2000,5000,10000,20000,50000,100000,200000,500000,1000000,2000000,5000000,10000000,20000000,50000000,100000000,200000000,500000000,1000000000 };
        public int Bet {
            get {
                if (BetIndex < vals.Length) return vals[BetIndex];
                return 1000000000;
            }
        }
        
        public GunLoadout(RoomData room,int selected,int mask) { this.room=room; UnlockMask=mask|1; Selected=selected>=0&&selected<room.guns.Length&&Unlocked(selected)?selected:0; }
        public bool Unlocked(int index) => (UnlockMask&(1<<index))!=0;
        public bool Select(int index) { if(index<0||index>=room.guns.Length||!Unlocked(index))return false; Selected=index; return true; }
        public bool Purchase(int index,IWallet wallet)
        {
            if(index<0||index>=room.guns.Length)return false;
            if(!Unlocked(index)) { if(!wallet.Spend(room.guns[index].unlockCost))return false; UnlockMask|=1<<index; }
            return Select(index);
        }
        public void ChangeBet(int delta) { BetIndex=UnityEngine.Mathf.Clamp(BetIndex+delta,0,25); } // max index 25 for 1 billion
    }
}
