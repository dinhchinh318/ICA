using System;
using System.IO;
using UnityEngine;
namespace LumaReef.Save
{
    [Serializable] public sealed class PendingProfile { public int revision; public PlayerSave profile; }
    // Account-scoped durable outbox. Never silently overwrite a newer server revision.
    public sealed class PendingProfileStore
    {
        readonly string path;
        public PendingProfileStore(string directory){Directory.CreateDirectory(directory);path=System.IO.Path.Combine(directory,"pending-profile.json");}
        public PendingProfile Load()
        {
            try{return File.Exists(path)?JsonUtility.FromJson<PendingProfile>(File.ReadAllText(path)):null;}
            catch(Exception e){Debug.LogWarning("Pending save recovery: "+e.Message);return null;}
        }
        public void Write(int revision,PlayerSave profile)
        {
            try{File.WriteAllText(path+".tmp",JsonUtility.ToJson(new PendingProfile{revision=revision,profile=profile}));if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);}
            catch(Exception e){Debug.LogWarning("Pending save: "+e.Message);}
        }
        public void Clear(){try{if(File.Exists(path))File.Delete(path);}catch(Exception e){Debug.LogWarning("Pending save cleanup: "+e.Message);}}
        public void ArchiveConflict(){try{if(File.Exists(path))File.Copy(path,path+".conflict-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json");}catch(Exception e){Debug.LogWarning("Pending save archive: "+e.Message);}}
    }
}
