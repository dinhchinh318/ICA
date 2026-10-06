using System;
using LumaReef.Data;
using LumaReef.Save;
using LumaReef.Economy;
namespace LumaReef.Quest
{
    public sealed class QuestController
    {
        readonly PlayerSave save;
        readonly QuestData[] data;
        readonly IWallet wallet;
        long checkedDay;
        public QuestController(PlayerSave save,QuestData[] data,IWallet wallet) { this.save=save; this.data=data; this.wallet=wallet; RefreshDay(); }
        public void RefreshDay()
        {
            var now=DateTime.UtcNow;long day=now.Date.Ticks;if(day==checkedDay)return;checkedDay=day;
            string today=now.ToString("yyyy-MM-dd");
            if(string.CompareOrdinal(today,save.questDay)<=0)return;
            save.questDay=today; Array.Clear(save.questProgress,0,save.questProgress.Length); Array.Clear(save.questClaimed,0,save.questClaimed.Length);
        }
        public void Add(QuestMetric metric,long value)
        { RefreshDay(); for(int i=0;i<data.Length;i++)if(data[i].metric==metric)save.questProgress[i]=Math.Min(data[i].target,save.questProgress[i]+value); }
        public bool Claim(int index)
        {
            RefreshDay(); if(index<0||index>=data.Length||save.questClaimed[index]||save.questProgress[index]<data[index].target)return false;
            save.questClaimed[index]=true; wallet.Credit(data[index].reward); return true;
        }
        public bool Daily()
        {
            string today=DateTime.UtcNow.ToString("yyyy-MM-dd"); if(string.CompareOrdinal(today,save.dailyClaim)<=0)return false;
            save.dailyClaim=today; wallet.Credit(1500); return true;
        }
    }
}
