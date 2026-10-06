using UnityEngine;
using UnityEditor;
using System.IO;

namespace LumaReef.Editor
{
    public class SpriteOptimizer
    {
        [MenuItem("Tools/Luma Reef/Tối Ưu Hóa Ảnh (Nét Hơn)")]
        public static void OptimizeSprites()
        {
            string[] searchFolders = new string[] { "Assets/Art" };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);

            int count = 0;
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer != null)
                    {
                        bool changed = false;

                        if (importer.maxTextureSize < 4096)
                        {
                            importer.maxTextureSize = 4096;
                            changed = true;
                        }

                        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            changed = true;
                        }

                        if (importer.filterMode != FilterMode.Bilinear)
                        {
                            importer.filterMode = FilterMode.Bilinear; // Dùng cho ảnh AI nét (nếu Pixel Art thì chuyển thành Point)
                            changed = true;
                        }

                        if (importer.mipmapEnabled)
                        {
                            importer.mipmapEnabled = false;
                            changed = true;
                        }

                        if (changed)
                        {
                            EditorUtility.DisplayProgressBar("Đang tối ưu ảnh...", path, (float)i / guids.Length);
                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"Đã tối ưu hóa {count} hình ảnh để hiển thị sắc nét hơn!");
            EditorUtility.DisplayDialog("Hoàn tất", $"Đã cấu hình {count} ảnh sắc nét.\nHãy kiểm tra lại trong game!", "OK");
        }
    }
}
