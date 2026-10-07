using System;
using System.Collections;
using UnityEngine;
using LumaReef.Network;
namespace LumaReef.Core
{
    public sealed partial class ReefApp
    {
        IEnumerator AuthSmokeTest()
        {
            string username="smoke_"+Guid.NewGuid().ToString("N").Substring(0,12);
            accountBusy=true;yield return Login(username,"smoke-only-password",true);
            Debug.Assert(DatabaseManager.LoggedIn&&SaveData.username==username,"Unity registration loads account");
            if(!DatabaseManager.LoggedIn)yield break;
            Wallet.Credit(1234);SaveNow();yield return SyncProfile();long expected=Wallet.Coins;
            yield return DatabaseManager.Request("GET","/v1/profile",null,reply=>Debug.Assert(reply.ok&&reply.profile.coins==expected,"Unity profile write persists"));
            // Simulate a shutdown after local save but before upload, then log in again.
            SaveData.coins+=4321;pendingProfile.Write(DatabaseManager.Revision,SaveData);
            DatabaseManager.Clear();accountBusy=true;yield return Login(username,"smoke-only-password",false);
            while(syncBusy)yield return null;
            Debug.Assert(Wallet.Coins==expected+4321,"Pending save restored on login");
            yield return DatabaseManager.Request("GET","/v1/profile",null,reply=>Debug.Assert(reply.ok&&reply.profile.coins==expected+4321,"Pending save uploaded"));
            yield return DatabaseManager.Request("POST","/v1/rooms","{}",reply=>{
                Debug.Assert(reply.ok&&reply.room.capacity==4&&reply.room.seats.Length==1,"Unity creates a four-seat room");
                if(reply.ok){roomCode=reply.room.code;ShowRoom(reply.room);}
            });
            yield return new WaitForSeconds(.5f);ReefCapture.Save(cameraView,GetComponentInChildren<Canvas>(),"room-smoke.png");ui.CloseModal();
            if(roomCode!=null){yield return DatabaseManager.Request("POST","/v1/rooms/"+roomCode+"/leave","{}",reply=>Debug.Assert(reply.ok,"Unity leaves room"));roomCode=null;}
        }
    }
}
