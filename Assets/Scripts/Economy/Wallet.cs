using System;
namespace LumaReef.Economy
{
    public interface IWallet
    {
        long Coins { get; }
        int Diamonds { get; }
        bool Spend(long amount);
        void Credit(long amount);
    }
    public sealed class Wallet : IWallet
    {
        public long Coins { get; private set; }
        public int Diamonds { get; private set; }
        public event Action Changed;
        public Wallet(long coins,int diamonds) { Coins=Math.Max(0,coins); Diamonds=Math.Max(0,diamonds); }
        public bool Spend(long amount) { if(amount<0||Coins<amount)return false; Coins-=amount; Changed?.Invoke(); return true; }
        public void Credit(long amount) { if(amount<=0)return; Coins=Math.Min(999999999999L,Coins+amount); Changed?.Invoke(); }
        public bool SpendDiamonds(int amount) { if(amount<0||Diamonds<amount)return false; Diamonds-=amount; Changed?.Invoke(); return true; }
        public void AddDiamonds(int amount) { Diamonds=Math.Max(0,Diamonds+amount); Changed?.Invoke(); }
    }
}
