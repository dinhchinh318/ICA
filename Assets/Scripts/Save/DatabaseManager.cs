using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using LumaReef.Save;
namespace LumaReef.Network
{
    [Serializable] public class RoomSeat { public int seat; public string username,userId; }
    [Serializable] public class RoomInfo { public string code,mode; public int capacity; public RoomSeat[] seats; }
    [Serializable] public class ApiReply { public bool ok; public string error,token,userId,username; public int revision; public PlayerSave profile; public RoomInfo room; public RoomInfo[] rooms; }
    [Serializable] class Credentials { public string username,password; public PlayerSave profile; }
    [Serializable] class ProfileUpdate { public int revision; public PlayerSave profile; }
    [Serializable] class EndpointConfig { public string baseUrl="http://127.0.0.1:8787"; }
    public static class DatabaseManager
    {
        static string token,endpoint;
        public static string CurrentUsername { get; private set; }="Khách";
        public static string UserId { get; private set; }
        public static int Revision { get; private set; }
        public static bool LoggedIn=>!string.IsNullOrEmpty(token);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear(){token=null;UserId=null;CurrentUsername="Khách";Revision=0;endpoint=null;}
        public static IEnumerator Request(string method,string path,string json,Action<ApiReply> done)
        {
            if(endpoint==null){
                var config=Resources.Load<TextAsset>("ServerConfig");endpoint=(config==null?new EndpointConfig():JsonUtility.FromJson<EndpointConfig>(config.text)).baseUrl.TrimEnd('/');
                if(Debug.isDebugBuild||Application.isEditor)foreach(string arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--reef-api=",StringComparison.Ordinal)&&Uri.TryCreate(arg.Substring(11),UriKind.Absolute,out var uri)&&uri.IsLoopback)endpoint=uri.AbsoluteUri.TrimEnd('/');
            }
            using(var request=new UnityWebRequest(endpoint+path,method))
            {
                request.timeout=5;request.downloadHandler=new DownloadHandlerBuffer();
                if(json!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));request.SetRequestHeader("Content-Type","application/json");}
                if(LoggedIn)request.SetRequestHeader("Authorization","Bearer "+token);
                yield return request.SendWebRequest();ApiReply reply=null;
                try{reply=JsonUtility.FromJson<ApiReply>(request.downloadHandler.text);}catch(ArgumentException){}
                if(reply==null)reply=new ApiReply{error="SERVER_UNREACHABLE"};
                if(request.result!=UnityWebRequest.Result.Success)reply.ok=false;
                done(reply);
            }
        }
        public static IEnumerator Authenticate(string username,string password,bool register,PlayerSave seed,Action<ApiReply> done)
        {
            yield return Request("POST",register?"/v1/register":"/v1/login",JsonUtility.ToJson(new Credentials{username=username,password=password,profile=seed}),reply=>{
                if(reply.ok&&reply.profile!=null){token=reply.token;UserId=reply.userId;CurrentUsername=reply.username;Revision=reply.revision;}done(reply);
            });
        }
        public static IEnumerator SaveProfile(PlayerSave profile,Action<ApiReply> done)
        {
            yield return Request("PUT","/v1/profile",JsonUtility.ToJson(new ProfileUpdate{revision=Revision,profile=profile}),reply=>{if(reply.ok)Revision=reply.revision;done(reply);});
        }
        public static string Explain(string error)
        {
            switch(error){
                case "CREDENTIALS_FORMAT":return "Tên 3–24 ký tự a-z, 0-9, _. Mật khẩu 8–128 ký tự.";
                case "INVALID_CREDENTIALS":return "Sai tên đăng nhập hoặc mật khẩu.";
                case "USERNAME_TAKEN":return "Tên tài khoản đã được sử dụng.";
                case "PROFILE_CONFLICT":return "Dữ liệu đã đổi trên thiết bị khác. Đăng nhập lại để tải bản mới.";
                case "ROOM_FULL":return "Phòng đã đủ 4 người.";
                case "ALREADY_IN_ROOM":return "Hãy rời phòng hiện tại trước.";
                case "ROOM_NOT_FOUND":return "Không tìm thấy phòng.";
                case "LOGIN_REQUIRED":return "Phiên hết hạn. Vui lòng đăng nhập lại.";
                case "TRY_LATER":return "Thử lại sau một phút.";
                default:return "Chưa kết nối được server. Tiến trình vẫn lưu trên máy.";
            }
        }
    }
}
