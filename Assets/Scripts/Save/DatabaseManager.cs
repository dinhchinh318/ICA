using System;
using System.Threading.Tasks;
using UnityEngine;
using LumaReef.Save;

namespace LumaReef.Network
{
    // Mô phỏng hệ thống Database Online/Firebase để chuẩn bị cho Multi-player 4 người
    public static class DatabaseManager
    {
        public static string CurrentUsername { get; private set; }
        public static string CurrentToken { get; private set; }
        
        public static async Task<bool> AuthenticateAsync(string username, string password)
        {
            Debug.Log("[Database] Connecting to auth server...");
            await Task.Delay(800); // Fake network delay
            
            CurrentUsername = username;
            CurrentToken = Guid.NewGuid().ToString();
            Debug.Log($"[Database] Authenticated as {username}, Token: {CurrentToken}");
            
            return true;
        }

        public static async Task SaveProfileAsync(PlayerSave data)
        {
            if (string.IsNullOrEmpty(CurrentToken)) return;
            Debug.Log("[Database] Syncing profile to cloud...");
            await Task.Delay(300);
            PlayerPrefs.SetString("cloud_profile_" + CurrentUsername, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Debug.Log("[Database] Sync complete.");
        }
        
        public static async Task<PlayerSave> LoadProfileAsync(string username)
        {
            Debug.Log("[Database] Loading profile from cloud...");
            await Task.Delay(500);
            string json = PlayerPrefs.GetString("cloud_profile_" + username, "");
            if (!string.IsNullOrEmpty(json)) {
                return JsonUtility.FromJson<PlayerSave>(json);
            }
            return null; // Trả về null nếu chưa có acc
        }
    }
}
