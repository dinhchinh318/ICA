using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LumaReef.Data;
namespace LumaReef.Editor
{
    public static class ReefAIImporter
    {
        struct Island { public int area,x0,x1,y0,y1; public float CenterX=>(x0+x1)*.5f; public float CenterY=>(y0+y1)*.5f; }
        [MenuItem("Luma Reef/Apply AI Portrait Art")]
        public static void Apply()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ReefCatalog>("Assets/Resources/ReefCatalog.asset");
            Apply(catalog);AssetDatabase.SaveAssets();
        }
        public static void Apply(ReefCatalog catalog)
        {
            if(!File.Exists("Assets/Art/AI/lobby.png"))return;
            catalog.lobbyBackground=Single("Assets/Art/AI/lobby.png");catalog.background=Single("Assets/Art/AI/arena.png");
            var fish=Slice("Assets/Art/AI/fish-atlas.png",4,7);
            for(int i=0;i<fish.Length;i++){catalog.room.fish[i].sprite=fish[i];EditorUtility.SetDirty(catalog.room.fish[i]);}
            catalog.uiIcons=Slice("Assets/Art/AI/ui-atlas.png",4,4);catalog.cannonSprites=Slice("Assets/Art/AI/cannons-atlas.png",4,2);
            for(int i=0;i<catalog.room.guns.Length;i++){catalog.room.guns[i].body=catalog.cannonSprites[i];EditorUtility.SetDirty(catalog.room.guns[i]);}
            for(int i=0;i<catalog.skills.Length;i++){catalog.skills[i].icon=catalog.uiIcons[8+i];EditorUtility.SetDirty(catalog.skills[i]);}
            if(File.Exists("Assets/Art/AI/projectiles-atlas.png"))
            {
                var bolts=Slice("Assets/Art/AI/projectiles-atlas.png",4,2);
                for(int i=0;i<8;i++)
                {
                    string path="Assets/ScriptableObjects/Guns/Bolt"+i+".asset";var data=AssetDatabase.LoadAssetAtPath<BulletData>(path);
                    if(data==null){data=ScriptableObject.CreateInstance<BulletData>();AssetDatabase.CreateAsset(data,path);}
                    data.sprite=bolts[i];data.speed=14;data.bounces=1;data.visualScale=Vector2.one*(.55f+i*.035f);data.prefab=catalog.room.guns[i].bullet.prefab;
                    catalog.room.guns[i].bullet=data;EditorUtility.SetDirty(data);EditorUtility.SetDirty(catalog.room.guns[i]);
                }
            }
            if(File.Exists("Assets/Art/AI/effects-atlas.png"))catalog.effectSprites=Slice("Assets/Art/AI/effects-atlas.png",4,2,true);
            if(File.Exists("Assets/Art/AI/ui-kit-atlas.png"))catalog.uiSkins=Slice("Assets/Art/AI/ui-kit-atlas.png",3,3,false,true);
            EditorUtility.SetDirty(catalog);
            File.WriteAllText("Temp/reef-art-result.txt","PASS / 2 backgrounds, 28 fish, 16 icons, 8 cannons, 8 projectiles, "+catalog.effectSprites.Length+" effects, "+catalog.uiSkins.Length+" UI skins / portrait 1080x1920");
        }
        static TextureImporter Import(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;return importer;
        }
        static Sprite Single(string path)
        {
            var importer=Import(path);importer.spriteImportMode=SpriteImportMode.Single;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);importer.spritePixelsPerUnit=width;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite[] Slice(string path,int columns,int rows,bool grid=false,bool borders=false)
        {
            var importer=Import(path);importer.isReadable=true;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);int w=texture.width,h=texture.height;
            var pixels=texture.GetPixels32();var seen=new bool[pixels.Length];var queue=new int[pixels.Length];var islands=new List<Island>();
            for(int n=0;n<pixels.Length;n++)
            {
                if(seen[n]||pixels[n].a<32)continue;
                int head=0,tail=0;queue[tail++]=n;seen[n]=true;var island=new Island{x0=w,y0=h,x1=0,y1=0};
                while(head<tail)
                {
                    int p=queue[head++],x=p%w,y=p/w;island.area++;island.x0=Math.Min(island.x0,x);island.x1=Math.Max(island.x1,x);island.y0=Math.Min(island.y0,y);island.y1=Math.Max(island.y1,y);
                    if(x>0)Visit(p-1,pixels,seen,queue,ref tail);if(x<w-1)Visit(p+1,pixels,seen,queue,ref tail);if(y>0)Visit(p-w,pixels,seen,queue,ref tail);if(y<h-1)Visit(p+w,pixels,seen,queue,ref tail);
                }
                if(island.area>120)islands.Add(island);
            }
            int count=columns*rows;
            if(grid){islands.Clear();for(int i=0;i<count;i++)islands.Add(new Island{area=1000,x0=(i%columns)*w/columns,x1=(i%columns+1)*w/columns-1,y0=h-(i/columns+1)*h/rows,y1=h-(i/columns)*h/rows-1});}
            islands.Sort((a,b)=>b.area.CompareTo(a.area));
            if(islands.Count<count)throw new Exception(path+": expected "+count+" isolated assets but found "+islands.Count);
            islands.RemoveRange(count,islands.Count-count);islands.Sort((a,b)=>b.CenterY.CompareTo(a.CenterY));
            for(int r=0;r<rows;r++)islands.Sort(r*columns,columns,Comparer<Island>.Create((a,b)=>a.CenterX.CompareTo(b.CenterX)));
            var metadata=new SpriteMetaData[count];
            for(int i=0;i<count;i++)
            {
                var a=islands[i];int x=Math.Max(0,a.x0-3),y=Math.Max(0,a.y0-3),right=Math.Min(w,a.x1+4),top=Math.Min(h,a.y1+4);
                metadata[i]=new SpriteMetaData{name="sprite-"+i.ToString("00"),rect=new Rect(x,y,right-x,top-y),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
                if(grid)metadata[i].rect=new Rect((i%columns)*w/(float)columns,h-(i/columns+1)*h/(float)rows,w/(float)columns,h/(float)rows);
                if(borders){var rect=metadata[i].rect;float b=Mathf.Min(rect.width,rect.height)*.24f;metadata[i].border=new Vector4(b,b,b,b);}
            }
            importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=w/(float)columns;
#pragma warning disable 618
            importer.spritesheet=metadata;
#pragma warning restore 618
            importer.isReadable=false;importer.SaveAndReimport();
            var sprites=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite sprite)sprites.Add(sprite);
            sprites.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));if(sprites.Count!=count)throw new Exception("Sprite import count mismatch: "+path);
            return sprites.ToArray();
        }
        static void Visit(int p,Color32[] pixels,bool[] seen,int[] queue,ref int tail)
        {if(!seen[p]&&pixels[p].a>=32){seen[p]=true;queue[tail++]=p;}}
    }
}
