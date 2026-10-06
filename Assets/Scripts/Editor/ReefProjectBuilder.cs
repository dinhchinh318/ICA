using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using LumaReef.Data;
using LumaReef.Path;
namespace LumaReef.Editor
{
    [InitializeOnLoad]
    public static class ReefProjectBuilder
    {
        static readonly string[] FishNames={"Dewdrop Dart","Citrine Sprat","Ribbon Jester","Prism Guppy","Moonseed Sardine","Petalfin","Pompon Puffer","Crownfin","Mossback Glider","Velvet Kite","Curltail","Bell Jelly","Needle Comet","Dusk Pup","Cobalt Runner","Night Sail","Cloudback","Anvilfin","Boulder Gulp","Lumen Angler","Ember Crab","Cofferfin","Arc Ribbon","Gilded Mossback","Solstice Shark","Mythroot Wyrm","Umbra Bloom","Opal Leviathan"};
        static readonly string[] GunNames={"Sprout","Tideglass","Prism","Twin Current","Stormcoil","Ember Bloom","Trinity","Solstice Beam"};
        static readonly string[] Scenes={"BootScene","MainMenuScene","GameScene"};
        static double nextPoll;
        static ReefProjectBuilder() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(File.Exists("Temp/reef-stop-play.request")){File.Delete("Temp/reef-stop-play.request");EditorApplication.isPlaying=false;return;}
            if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            if(File.Exists("Temp/reef-generate.request")) { File.Delete("Temp/reef-generate.request"); Generate(); }
            if(File.Exists("Temp/reef-build.request")) { File.Delete("Temp/reef-build.request"); BuildWindows(); }
        }
        [MenuItem("Luma Reef/Generate or Repair Project")]
        public static void Generate()
        {
            try
            {
                string[] folders={"Art/Fish","Art/Boss","Art/Guns","Art/Bullets","Art/Coins","Art/UI","Art/Background","Art/VFX","Audio/Music","Audio/SFX","Prefabs/Fish","Prefabs/Boss","Prefabs/Guns","Prefabs/Bullets","Prefabs/UI","Prefabs/VFX","Scenes","ScriptableObjects/Fish","ScriptableObjects/Guns","ScriptableObjects/Paths","ScriptableObjects/Skills","ScriptableObjects/Quests","Resources"};
                foreach(string f in folders)Directory.CreateDirectory("Assets/"+f);
                var catalog=Asset<ReefCatalog>("Assets/Resources/ReefCatalog.asset");
                catalog.disk=SpriteAsset("Assets/Art/Coins/Pearl.png",64,64,(x,y)=>{float d=new Vector2(x,y).magnitude; return d<.82f?(d>.65f?new Color(1,.73f,.2f):new Color(1,.95f,.55f)):Color.clear;});
                catalog.ring=SpriteAsset("Assets/Art/VFX/Ring.png",64,64,(x,y)=>{float d=new Vector2(x,y).magnitude;return d>.74f&&d<.87f?Color.white:Color.clear;});
                catalog.barrel=SpriteAsset("Assets/Art/Guns/Barrel.png",64,128,(x,y)=>Mathf.Abs(x)<.72f&&Mathf.Abs(y)<.9f?new Color(.68f+(.5f-x)*.2f,.92f,1):Color.clear);
                catalog.background=SpriteAsset("Assets/Art/Background/LanternShoals.png",960,540,OceanPixel);
                catalog.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/ReefParticles.mat");
                if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material,"Assets/Art/VFX/ReefParticles.mat"); }
                material.SetFloat("_Surface",1); material.SetFloat("_Blend",0); material.SetFloat("_SrcBlend",5); material.SetFloat("_DstBlend",10); material.SetFloat("_ZWrite",0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000; material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Coins/Pearl.png")); catalog.particleMaterial=material; EditorUtility.SetDirty(material);
                catalog.audio=BuildAudio();
                var room=Asset<RoomData>("Assets/ScriptableObjects/LanternShoals.asset"); room.displayName="Lantern Shoals"; catalog.room=room;
                room.fish=new FishData[28];
                float[] multipliers={2,2,3,3,4,4,6,7,8,8,9,10,12,16,18,20,24,28,30,15,18,25,22,35,120,160,200,240};
                for(int i=0;i<28;i++)
                {
                    int index=i; var f=Asset<FishData>("Assets/ScriptableObjects/Fish/"+FishNames[i].Replace(" ","")+".asset");
                    f.id="resident-"+i; f.displayName=FishNames[i]; f.category=i<6?FishCategory.Small:i<13?FishCategory.Medium:i<19?FishCategory.Large:i<24?FishCategory.Special:FishCategory.Boss;
                    f.tint=Color.HSVToRGB((i*.137f+.08f)%1,.62f,.95f); f.multiplier=multipliers[i]; f.coinReward=Mathf.RoundToInt(multipliers[i]);
                    f.speed=i<6?1.3f:i<13?1.05f:i<24?.75f:.46f;
                    // Cá to hơn rõ ràng — nhìn ngầu hơn như iCá
                    if(f.category==FishCategory.Small)        f.size=1.2f;
                    else if(f.category==FishCategory.Medium)  f.size=1.85f;
                    else if(f.category==FishCategory.Large)   f.size=2.8f;
                    else if(f.category==FishCategory.Special) f.size=2.6f;
                    else                                       f.size=4.5f; // Boss rất to
                    // killChance chuẩn casino: cá nhỏ dễ ăn, cá lớn cực khó, boss như nổ hũ
                    if(f.category==FishCategory.Small)        f.killChance=0.25f;  // ~25% / viên đạn
                    else if(f.category==FishCategory.Medium)  f.killChance=0.10f;  // ~10%
                    else if(f.category==FishCategory.Large)   f.killChance=0.03f;  // ~3%
                    else if(f.category==FishCategory.Special) f.killChance=0.02f;  // ~2%
                    else                                       f.killChance=Mathf.Max(0.004f, 0.8f/multipliers[i]); // Boss: 0.4–0.8%
                    f.bossHP=0; // Không dùng HP — dùng tỉ lệ RNG thuần
                    f.hitboxScale=.85f; f.spawnWeight=1;
                    f.special=i==19?SpecialEffect.Lantern:i==20?SpecialEffect.Bomb:i==21?SpecialEffect.Treasure:i==22?SpecialEffect.Lightning:i==23?SpecialEffect.Golden:SpecialEffect.None;
                    string art="Assets/Art/"+(i>=24?"Boss":"Fish")+"/"+f.id+".png";
                    // Độ phân giải cao hơn → ảnh nét hơn
                    f.sprite=SpriteAsset(art,384,256,(x,y)=>FishPixel(index,x,y,f.tint));
                    f.animation=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Fish/Swim.asset");
                    if(f.animation==null){var clip=new AnimationClip();clip.name="Swim";clip.SetCurve("",typeof(Transform),"localScale.y",AnimationCurve.EaseInOut(0,1,.4f,1.035f));AssetDatabase.CreateAsset(clip,"Assets/Art/Fish/Swim.asset");f.animation=clip;}
                    FishPrefab(f); room.fish[i]=f; EditorUtility.SetDirty(f);
                }
                var bullet=Asset<BulletData>("Assets/ScriptableObjects/EnergyBolt.asset"); bullet.speed=14;bullet.bounces=1;bullet.sprite=catalog.disk;bullet.prefab=SimplePrefab("Assets/Prefabs/Bullets/EnergyBolt.prefab",catalog.disk);
                room.guns=new GunData[8]; int[] bets={5,10,20,50,100,200,500,1000};
                for(int i=0;i<8;i++)
                {
                    var g=Asset<GunData>("Assets/ScriptableObjects/Guns/"+GunNames[i].Replace(" ","")+".asset");g.id="cannon-"+i;g.displayName=GunNames[i];g.level=i+1;g.bet=bets[i];
                    // Tất cả súng power = 1 (BÌNH ĐẲNG). Chỉ khác bet (tiền cược = tiền thắng)
                    g.fireRate=5f;g.netRadius=0.75f;g.power=1;g.barrels=1;g.unlockCost=0;g.color=Color.HSVToRGB((.46f+i*.11f)%1,.72f,1);g.bullet=bullet;
                    g.body=catalog.disk;g.barrel=catalog.barrel;g.baseSprite=catalog.disk;g.fireSound=catalog.audio.cues[0];
                    g.muzzleFlash=SimplePrefab("Assets/Prefabs/VFX/Muzzle.prefab",catalog.disk);g.impactEffect=SimplePrefab("Assets/Prefabs/VFX/Impact.prefab",catalog.ring);g.netEffect=SimplePrefab("Assets/Prefabs/VFX/Net.prefab",catalog.ring);
                    SimplePrefab("Assets/Prefabs/Guns/"+g.id+".prefab",catalog.barrel);room.guns[i]=g;EditorUtility.SetDirty(g);
                }
                room.paths=new BezierPath[21]; for(int i=0;i<21;i++)room.paths[i]=BuildPath(i);
                catalog.skills=new SkillData[6];string[] skillNames={"FREEZE","RAPID","LOCK","POWER","ARC","BOMB"};float[] cooldowns={24,20,14,25,30,35};float[] durations={5,7,8,7,0,0};
                for(int i=0;i<6;i++){var s=Asset<SkillData>("Assets/ScriptableObjects/Skills/"+skillNames[i]+".asset");s.kind=(SkillKind)i;s.displayName=skillNames[i];s.cooldown=cooldowns[i];s.duration=durations[i];s.strength=i>=4?6:2;s.radius=3.5f;s.icon=catalog.ring;s.audio=catalog.audio.cues[i==0?8:10];s.vfx=room.guns[0].impactEffect;catalog.skills[i]=s;EditorUtility.SetDirty(s);}
                catalog.quests=new QuestData[6];string[] descriptions={"Catch 50 fish","Catch 100 small fish","Catch 5 large fish","Catch a boss","Fire 500 bolts","Earn 10,000 coins"};int[] targets={50,100,5,1,500,10000};
                for(int i=0;i<6;i++){var q=Asset<QuestData>("Assets/ScriptableObjects/Quests/Mission"+i+".asset");q.id="daily-"+i;q.description=descriptions[i];q.metric=(QuestMetric)i;q.target=targets[i];q.reward=300+i*200;catalog.quests[i]=q;EditorUtility.SetDirty(q);}
                EditorUtility.SetDirty(room);EditorUtility.SetDirty(bullet);EditorUtility.SetDirty(catalog);
                BuildAtlas();
                var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                ReefAIImporter.Apply(catalog);
                PlayerSettings.companyName="Lantern Workshop";PlayerSettings.productName="Luma Reef";PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.lanternworkshop.lumareef");PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
                var builds=new EditorBuildSettingsScene[3];
                for(int i=0;i<3;i++) { string path="Assets/Scenes/"+Scenes[i]+".unity"; if(!File.Exists(path)){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var marker=new GameObject(Scenes[i]+" / runtime composition via ReefApp");SceneManager.MoveGameObjectToScene(marker,scene);EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);}builds[i]=new EditorBuildSettingsScene(path,true); }
                EditorBuildSettings.scenes=builds;AssetDatabase.SaveAssets();AssetDatabase.Refresh();
                File.WriteAllText("Temp/reef-generation-result.txt","SUCCESS: 28 fish, 8 cannons, 21 paths, 6 skills, 6 quests, 3 scenes.");
                Debug.Log("LUMA_REEF_GENERATION_PASS");
                if(!Application.isBatchMode)EditorSceneManager.OpenScene("Assets/Scenes/BootScene.unity");
            }
            catch(Exception e){File.WriteAllText("Temp/reef-generation-result.txt",e.ToString());Debug.LogException(e);throw;}
        }
        static T Asset<T>(string path) where T:ScriptableObject
        {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
        static Sprite SpriteAsset(string path,int width,int height,Func<float,float,Color> pixel)
        {
            if(!File.Exists(path))
            {
                var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var colors=new Color[width*height];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)colors[y*width+x]=pixel((x+.5f)/width*2-1,(y+.5f)/height*2-1);
                texture.SetPixels(colors);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=width;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            // Chất lượng ảnh tối đa để game NHÌN NÉT
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=4096;
            var platformSettings=importer.GetDefaultPlatformTextureSettings();
            platformSettings.maxTextureSize=4096;platformSettings.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platformSettings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Color FishPixel(int i,float x,float y,Color tint)
        {
            y*=.78f;
            bool ray=i==9||i==15, jelly=i==11||i==26, turtle=i==8||i==23, eel=i==22||i==25, crab=i==20;
            float bodyX=eel?.78f:ray?.52f:.6f,bodyY=eel?.19f:ray?.60f:.37f;
            bool body=x*x/(bodyX*bodyX)+y*y/(bodyY*bodyY)<1;
            bool tail=x<-.43f&&x>-.94f&&Mathf.Abs(y)<(-x-.4f)*.63f;
            bool fin=x>-.34f&&x<.18f&&Mathf.Abs(y)<.57f-Mathf.Abs(x+.06f)*.9f;
            if(jelly){body=x*x/.32f+(y-.15f)*(y-.15f)/.17f<1&&y>-.05f;tail=false;fin=y<0&&y>-.55f&&Mathf.Abs(x)<.45f&&Mathf.Abs(Mathf.Sin(x*25+y*9+i))>.79f;}
            if(crab){body=x*x/.38f+y*y/.08f<1;tail=false;fin=Mathf.Abs(x)<.91f&&Mathf.Abs(y)<.46f&&Mathf.Abs(Mathf.Sin(y*21+x*6))>.83f;}
            if(turtle){body=x*x/.31f+y*y/.18f<1;fin=(Mathf.Abs(x)-.25f)*(Mathf.Abs(x)-.25f)/.06f+(Mathf.Abs(y)-.38f)*(Mathf.Abs(y)-.38f)/.018f<1||((x-.65f)*(x-.65f)+y*y<.045f);}
            if(!body&&!tail&&!fin)return Color.clear;
            Color c=body?tint:Color.Lerp(tint,new Color(.04f,.17f,.25f),.35f);
            if(body)
            {
                float stripe=Mathf.Sin(x*(i%3==0?26:17)+y*4+i);
                if(stripe>.77f)c=Color.Lerp(c,Color.white,.37f);
                float shade=Mathf.Clamp01((y+.4f)/.8f);c*=.62f+shade*.45f;c.a=1;
                if(y<-.16f)c=Color.Lerp(c,new Color(.85f,.97f,.85f),.40f);
                if(turtle&&Mathf.Abs(Mathf.Sin(x*17)*Mathf.Cos(y*19))<.22f)c=Color.Lerp(c,new Color(.02f,.15f,.17f),.6f);
                if((i==6||i==18)&&Mathf.Sin(x*39)*Mathf.Cos(y*34)>.75f)c=Color.Lerp(c,Color.white,.6f);
            }
            float eyeX=jelly?.22f:turtle?.66f:.38f,eyeY=jelly?.14f:.12f;
            float eye=(x-eyeX)*(x-eyeX)+(y-eyeY)*(y-eyeY);
            if(eye<.011f)c=new Color(.95f,1,1);if(eye<.004f)c=new Color(.02f,.07f,.12f);if((x-eyeX-.018f)*(x-eyeX-.018f)+(y-eyeY-.024f)*(y-eyeY-.024f)<.0007f)c=Color.white;
            c.a=1;return c;
        }
        static Color OceanPixel(float x,float y)
        {
            float glow=Mathf.Exp(-((x-.4f)*(x-.4f)*2+(y-.4f)*(y-.4f)*1.2f));
            Color c=Color.Lerp(new Color(.018f,.06f,.14f),new Color(.045f,.27f,.35f),glow);
            float rays=Mathf.Pow(Mathf.Max(0,Mathf.Sin((x+y*.26f)*23)),15)*Mathf.Clamp01(y+.6f)*.06f;c+=new Color(rays,rays*1.5f,rays*1.4f);
            float floor=-.80f+.06f*Mathf.Sin(x*8)+.025f*Mathf.Cos(x*19);
            if(y<floor)c=Color.Lerp(new Color(.025f,.16f,.20f),new Color(.018f,.075f,.14f),(floor-y)*4);
            for(int k=0;k<9;k++)
            {
                float cx=-.94f+k*.245f,cy=-.89f;float h=.09f+(k%3)*.055f;
                float dx=x-cx-.017f*Mathf.Sin((y+1)*45+k);
                if(y>cy&&y<cy+h&&Mathf.Abs(dx)<.007f)c=new Color(.06f,.33f,.31f);
                if((x-cx)*(x-cx)*500+(y-cy-h)*(y-cy-h)*700<1)c=k%2==0?new Color(.14f,.38f,.38f):new Color(.29f,.20f,.37f);
            }
            c.a=1;return c;
        }
        static void FishPrefab(FishData data)
        {
            string path="Assets/Prefabs/"+(data.category==FishCategory.Boss?"Boss/":"Fish/")+data.id+".prefab";
            if(File.Exists(path))return;var go=new GameObject(data.displayName);go.AddComponent<SpriteRenderer>().sprite=data.sprite;var c=go.AddComponent<CircleCollider2D>();c.radius=.38f*data.hitboxScale;c.isTrigger=true;go.transform.localScale=Vector3.one*data.size;PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
        }
        static GameObject SimplePrefab(string path,Sprite sprite)
        {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab!=null)return prefab;var go=new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));go.AddComponent<SpriteRenderer>().sprite=sprite;prefab=PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);return prefab;}
        static BezierPath BuildPath(int index)
        {
            var path=Asset<BezierPath>("Assets/ScriptableObjects/Paths/Route"+index.ToString("00")+".asset");path.kind=(PathKind)(index%7);
            int anchors=9;var p=new Vector2[anchors];float lane=(index/7-1)*1.8f;
            for(int i=0;i<anchors;i++)
            {
                float t=i/(float)(anchors-1);float x=Mathf.Lerp(-7.5f,7.5f,t),y=lane;
                switch(path.kind)
                {
                    case PathKind.Curve:y+=Mathf.Sin(t*Mathf.PI)*2.3f;break;
                    case PathKind.SCurve:y+=Mathf.Sin(t*Mathf.PI*2)*1.7f;break;
                    case PathKind.Wave:y+=Mathf.Sin(t*Mathf.PI*4)*.9f;break;
                    case PathKind.Zigzag:y+=(i%2==0?-1:1)*1.1f;break;
                    case PathKind.Circle: if(i>1&&i<7){float a=(i-2)/4f*Mathf.PI*2;p[i]=new Vector2(Mathf.Cos(a)*4,Mathf.Sin(a)*2.4f);continue;}break;
                    case PathKind.Spiral:if(i>1&&i<7){float a=(i-2)/4f*Mathf.PI*3,r=3.4f-(i-2)*.45f;p[i]=new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.65f);continue;}break;
                }
                p[i]=new Vector2(index%2==0?x:-x,Mathf.Clamp(y,-3,3.5f)*1.7f);
            }
            path.points=new Vector2[(anchors-1)*3+1];path.points[0]=p[0];
            for(int i=0;i<anchors-1;i++){Vector2 prev=p[Mathf.Max(0,i-1)],next=p[Mathf.Min(anchors-1,i+2)];path.points[i*3+1]=p[i]+(p[i+1]-prev)/6;path.points[i*3+2]=p[i+1]-(next-p[i])/6;path.points[i*3+3]=p[i+1];}
            path.length=0;Vector2 last=path.Evaluate(0);for(int i=1;i<=160;i++){Vector2 current=path.Evaluate(i/160f);path.length+=Vector2.Distance(last,current);last=current;}
            EditorUtility.SetDirty(path);return path;
        }
        static AudioLibrary BuildAudio()
        {
            var lib=Asset<AudioLibrary>("Assets/ScriptableObjects/ReefAudio.asset");lib.cues=new AudioClip[11];
            for(int i=0;i<11;i++){string path="Assets/Audio/SFX/"+((SoundCue)i)+".wav";WriteTone(path,i,false);lib.cues[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);}
            WriteTone("Assets/Audio/Music/Reef.wav",0,true);WriteTone("Assets/Audio/Music/Boss.wav",1,true);lib.reefMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Reef.wav");lib.bossMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Boss.wav");
            string mixerPath="Assets/Audio/Reef.mixer";
            if(!File.Exists(mixerPath))
            {
                Type mixerType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                if(mixerType==null)foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){mixerType=assembly.GetType("UnityEditor.Audio.AudioMixerController");if(mixerType!=null)break;}
                var create=mixerType?.GetMethod("CreateMixerControllerAtPath",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);create?.Invoke(null,new object[]{mixerPath});
            }
            lib.mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if(lib.mixer!=null)
            {
                var master=lib.mixer.FindMatchingGroups("Master");
                if(master.Length>0)
                {
                    var type=lib.mixer.GetType();var createGroup=type.GetMethod("CreateNewGroup");var parent=type.GetMethod("AddChildToParent");
                    foreach(string bus in new[]{"Music","SFX"})
                    {
                        if(lib.mixer.FindMatchingGroups(bus).Length==0)
                        {
                            var group=createGroup.Invoke(lib.mixer,new object[]{bus,false});parent.Invoke(lib.mixer,new[]{group,(object)master[0]});
                        }
                    }
                    lib.musicGroup=lib.mixer.FindMatchingGroups("Music")[0];lib.sfxGroup=lib.mixer.FindMatchingGroups("SFX")[0];EditorUtility.SetDirty(lib.mixer);
                }
            }
            EditorUtility.SetDirty(lib);return lib;
        }
        static void WriteTone(string path,int cue,bool music)
        {
            if(!File.Exists(path))
            {
                const int rate=22050;int count=(int)(rate*(music?8:cue==7?1.1:.19+cue*.025));var rng=new System.Random(cue+31);
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                    int[] notes={0,7,12,16,12,7,4,7,0,7,11,14,11,7,2,7};
                    for(int i=0;i<count;i++)
                    {
                        double t=i/(double)rate,value;
                        if(music){int step=(int)(t*2)%16;double local=t%.5,f=(cue==0?130.8128:110)*Math.Pow(2,notes[step]/12.0);value=(Math.Sin(t*f*Math.PI*2)+.32*Math.Sin(t*f*Math.PI*4))*Math.Exp(-local*5)*.16;value+=Math.Sin(t*(cue==0?65.4064:55)*Math.PI*2)*.065;value*=Math.Min(1,t*8)*Math.Min(1,(8-t)*8);}
                        else {double f=cue==0?650-1800*t:cue==3?1000+1400*t:cue==9?100:220+cue*80;double noise=(rng.NextDouble()*2-1)*(cue==9||cue==7?.65:.07);value=(Math.Sin(t*f*Math.PI*2)+noise)*Math.Exp(-t*(cue==7?4:13))*.36;}
                        writer.Write((short)(Math.Max(-1,Math.Min(1,value))*32767));
                    }
                }
            }
            AssetDatabase.ImportAsset(path);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        static void BuildAtlas()
        {
            const string path="Assets/Art/ReefAtlas.spriteatlas";if(File.Exists(path))return;
            var atlas=new SpriteAtlas();atlas.SetPackingSettings(new SpriteAtlasPackingSettings{enableRotation=false,enableTightPacking=false,padding=4});
            atlas.Add(new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/Fish"),AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/Boss"),AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/Coins"),AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/Guns")});AssetDatabase.CreateAsset(atlas,path);
        }
        [MenuItem("Luma Reef/Build Windows Development")]
        public static void BuildWindows()
        {
            ReefValidation.Run();
            Directory.CreateDirectory("Builds/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Array.ConvertAll(Scenes,s=>"Assets/Scenes/"+s+".unity"),locationPathName="Builds/Windows/LumaReef.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Temp/reef-build-result.txt",report.summary.result+" / errors="+report.summary.totalErrors+" / bytes="+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
        }
        public static void BatchBuild() { Generate();BuildWindows(); }
    }
}
