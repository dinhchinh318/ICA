using System;
using System.IO;
using UnityEngine;
namespace LumaReef.Save
{
    [Serializable]
    public sealed class PlayerSave
    {
        public int version=1;
        public long coins=5000,lifetimeKills;
        public int diamonds=25,level=1,exp,unlockedGuns=255,selectedGun,cosmetics,selectedCosmetic;
        public float music=.45f,sfx=.7f;
        public string questDay="",dailyClaim="",username="";
        public long[] questProgress=new long[6];
        public bool[] questClaimed=new bool[6];
        public bool achievementClaimed;
        public int totalSessions,vipGrantVersion;
    }
    public interface ISaveStore { PlayerSave Load(); bool Save(PlayerSave data); }
    public sealed class JsonSaveStore : ISaveStore
    {
        readonly string path;
        public JsonSaveStore(string directory) { Directory.CreateDirectory(directory); path=System.IO.Path.Combine(directory,"luma-reef-save.json"); }
        public PlayerSave Load()
        {
            foreach(string candidate in new[]{path,path+".bak"})
            {
                try
                {
                    if(!File.Exists(candidate))continue;
                    var save=JsonUtility.FromJson<PlayerSave>(File.ReadAllText(candidate));
                    if(save==null||save.version!=1||save.coins<0||save.questProgress==null||save.questClaimed==null||save.questProgress.Length!=6||save.questClaimed.Length!=6)continue;
                    save.level=Math.Max(1,save.level); save.music=Mathf.Clamp01(save.music); save.sfx=Mathf.Clamp01(save.sfx); return save;
                }
                catch(Exception e) { Debug.LogWarning("Save recovery: "+e.Message); }
            }
            return new PlayerSave();
        }
        public bool Save(PlayerSave data)
        {
            try
            {
                string temp=path+".tmp"; File.WriteAllText(temp,JsonUtility.ToJson(data,true));
                if(File.Exists(path))File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
                return true;
            }
            catch(Exception e) { Debug.LogWarning("Could not save: "+e.Message); return false; }
        }
    }
}
