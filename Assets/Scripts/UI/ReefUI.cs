using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using LumaReef.Core;
using LumaReef.Data;
namespace LumaReef.UI
{
    // Presentation authored on a 1080 x 1920 canvas; the simulation uses world coordinates.
    public sealed class ReefUI
    {
        public static readonly Color Ink=new Color(.025f,.105f,.20f,.96f),Aqua=new Color(.27f,.96f,1),Gold=new Color(1,.80f,.28f),Muted=new Color(.64f,.88f,.94f);
        readonly ReefApp app;
        readonly Font font;
        readonly RectTransform root,modalContent;
        readonly GameObject menu,game,modal,loading,debug,bossRoot;
        readonly Text balance,menuBalance,bet,eventText,gunLabel,bossLabel,fpsText,toast,profile,autoLabel,lockLabel;
        readonly Text gemBalance,menuGems;
        readonly Text[] skillLabels=new Text[6];
        readonly Button[] skillButtons=new Button[6];
        readonly Image bossFill,avatar,avatarFrame;
        double displayedCoins;
        float eventTime,toastTime,hudTimer,jackpotTime;
        GameObject jackpotOverlay;
        Text jackpotTitle,jackpotAmount;
        public bool ModalOpen=>modal.activeSelf;
        public bool DebugOpen=>debug.activeSelf;
        public ReefUI(ReefApp app,Font font)
        {
            this.app=app;this.font=font;
            var go=new GameObject("Portrait Canvas 1080x1920",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(app.transform);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=(RectTransform)go.transform;
            new GameObject("Input UI",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(app.transform);
            menu=Panel(root,"Lobby",0,0,1080,1920,new Color(.035f,.16f,.27f)).gameObject;
            if(app.Catalog.lobbyBackground!=null)Art(menu.transform,app.Catalog.lobbyBackground,0,0,1080,1920,false);
            Panel(menu.transform,"Header",0,884,1080,152,new Color(.02f,.07f,.17f,.74f));
            Button(menu.transform,"☰",-474,880,86,88,new Color(.04f,.32f,.53f),()=>app.OpenPanel("SETTINGS"),40);
            Plate(menu.transform,-156,880,477,80);Icon(menu.transform,0,-355,884,77);
            Plate(menu.transform,319,880,395,80);Icon(menu.transform,1,172,884,77);
            menuBalance=Label(menu.transform,"",-116,880,332,72,31,Color.white);menuGems=Label(menu.transform,"",329,880,220,72,31,Color.white);
            Plate(menu.transform,-163,746,708,156);
            avatarFrame=Art(menu.transform,app.Catalog.disk,-419,750,146,146);avatarFrame.color=Gold;
            if(app.Catalog.uiSkins.Length>3){avatarFrame.sprite=app.Catalog.uiSkins[3];avatarFrame.color=Color.white;}
            avatar=Art(menu.transform,app.Catalog.room.fish[0].sprite,-419,750,125,108);
            Label(menu.transform,"THUYỀN TRƯỞNG",-82,785,465,42,29,Color.white);
            profile=Label(menu.transform,"",-82,738,465,44,24,Aqua);
            Label(menu.transform,"◆ NHÀ THÁM HIỂM ĐẠI DƯƠNG",-56,695,514,35,19,Gold);
            Label(menu.transform,"LUMA REEF",0,553,850,103,70,Gold);
            Label(menu.transform,"HUYỀN THOẠI BIỂN SÂU",0,487,700,44,25,Color.white);
            Badge(menu.transform,6,"ĐIỂM DANH",-434,351,()=>app.OpenPanel("DAILY REWARD"));
            Badge(menu.transform,5,"THÀNH TÍCH",-434,164,()=>app.OpenPanel("ACHIEVEMENT"));
            Badge(menu.transform,4,"NHIỆM VỤ",-434,-23,()=>app.OpenPanel("QUEST"));
            Badge(menu.transform,14,"PHÒNG CHƠI",434,351,()=>app.OpenPanel("ROOM"));
            Badge(menu.transform,3,"TRANG BỊ",434,164,()=>app.OpenPanel("SHOP"));
            Badge(menu.transform,2,"THƯ MỚI",434,-23,()=>app.OpenPanel("MAIL"));
            Plate(menu.transform,0,-414,440,59);Label(menu.transform,"✦ THÁM HIỂM MIỄN PHÍ ✦",0,-412,426,55,24,Gold);
            RoomBubble(menu.transform,-320,-611,220,8,"VƯỜN SAN HÔ",()=>app.OpenPanel("ROOM"));
            RoomBubble(menu.transform,320,-611,220,15,"VỰC ÁNH TRĂNG",()=>app.OpenPanel("ROOM"));
            RoomBubble(menu.transform,0,-575,306,25,"ĐIỆN NGỌC TRIỀU",()=>app.Play());
            Button(menu.transform,"CHƠI NGAY",0,-771,453,97,new Color(1,.55f,.07f),()=>app.Play(),40);
            Panel(menu.transform,"Navigation",0,-909,1080,142,new Color(.01f,.12f,.23f,.95f));
            string[] tabs={"Hộp thư","Cửa hàng","Nhiệm vụ","Bộ sưu tập","Bạn bè","Cài đặt"};string[] actions={"MAIL","SHOP","QUEST","ACHIEVEMENT","FRIENDS","SETTINGS"};int[] icons={2,3,4,5,15,7};
            for(int i=0;i<tabs.Length;i++){string action=actions[i];Badge(menu.transform,icons[i],tabs[i],-450+i*180,-891,()=>app.OpenPanel(action),.95f);}
            game=Panel(root,"Portrait gameplay HUD",0,0,1080,1920,Color.clear).gameObject;
            Panel(game.transform,"Header shade",0,889,1080,142,new Color(.01f,.08f,.14f,.88f));
            Button(game.transform,"‹",-474,881,90,85,new Color(.06f,.33f,.48f),()=>app.Menu(),53);
            Icon(game.transform,0,-350,882,73);Icon(game.transform,1,150,882,70);
            balance=Label(game.transform,"",-119,880,313,76,30,Gold);gemBalance=Label(game.transform,"",280,880,234,76,30,Gold);
            Button(game.transform,"II",474,881,85,85,new Color(.06f,.33f,.48f),()=>app.OpenPanel("SETTINGS"),32);
            Label(game.transform,"ĐIỆN NGỌC TRIỀU",0,797,580,44,27,Gold);
            for(int i=0;i<6;i++)
            {
                int index=i;float x=-415+i*166;skillButtons[i]=Button(game.transform,"",x,687,143,140,new Color(.04f,.23f,.33f,.9f),()=>app.UseSkill(index),19);
                Icon(skillButtons[i].transform,8+i,0,16,90);
                skillLabels[i]=Label(skillButtons[i].transform,"",0,-48,140,38,20,Color.white);
            }
            eventText=Label(game.transform,"",0,466,995,82,30,Gold);
            bossRoot=Panel(game.transform,"Boss",0,561,958,77,new Color(.08f,.02f,.16f,.9f)).gameObject;
            bossLabel=Label(bossRoot.transform,"",0,14,904,37,25,Gold);
            var track=Panel(bossRoot.transform,"HP track",0,-22,902,12,new Color(.16f,.12f,.24f));bossFill=Panel(track,"HP",0,0,902,12,new Color(.95f,.26f,.49f)).GetComponent<Image>();
            Badge(game.transform,3,"ĐỔI SÚNG",437,-592,()=>app.OpenPanel("SHOP"),.8f);
            Badge(game.transform,4,"NHIỆM VỤ",-437,-592,()=>app.OpenPanel("QUEST"),.8f);
            autoLabel=Button(game.transform,"TỰ ĐỘNG",-370,-774,262,88,new Color(.035f,.28f,.39f,.95f),()=>app.ToggleAuto(),26).GetComponentInChildren<Text>();
            lockLabel=Button(game.transform,"KHÓA CÁ",370,-774,262,88,new Color(.035f,.28f,.39f,.95f),()=>app.ToggleLock(),26).GetComponentInChildren<Text>();
            Plate(game.transform,0,-918,1018,78);
            Button(game.transform,"−",-404,-919,78,64,new Color(.05f,.34f,.48f),()=>app.Loadout.ChangeBet(-1),41);
            bet=Label(game.transform,"",-146,-918,407,66,29,Gold);
            Button(game.transform,"+",140,-919,78,64,new Color(.05f,.34f,.48f),()=>app.Loadout.ChangeBet(1),41);
            gunLabel=Label(game.transform,"",347,-919,314,64,23,Color.white);
            Label(game.transform,"GIỮ ĐỂ BẮN • CHẠM ĐỂ NGẮM",0,-693,700,39,21,Muted);
            modal=Panel(root,"Modal shade",0,0,1080,1920,new Color(0,.025f,.07f,.87f),true).gameObject;
            modalContent=Panel(modal.transform,"Modal edge",0,0,1016,1450,Gold);Panel(modalContent,"Modal inner",0,0,1004,1438,Ink);
            if(app.Catalog.uiSkins.Length>4){Skin(modalContent.GetComponent<Image>(),4);modalContent.GetChild(0).GetComponent<Image>().color=Color.clear;}
            debug=Panel(root,"Debug",0,0,972,1470,Ink,true).gameObject;
            Label(debug.transform,"DEVELOPMENT CONSOLE",0,669,900,68,33,Aqua);
            string[] commands={"+10,000 COINS","SPAWN SMALL","SPAWN LARGE","SPAWN SPECIAL","SPAWN BOSS","KILL ALL","NEXT GUN","CHANGE BET","GOD MODE","COLLIDERS","FISH WAVE"};
            for(int i=0;i<commands.Length;i++){int index=i;Button(debug.transform,commands[i],0,562-i*96,880,78,new Color(.07f,.26f,.37f),()=>app.DebugAction(index),27);}
            fpsText=Label(debug.transform,"",0,-570,900,99,27,Muted);Button(debug.transform,"ĐÓNG",0,-665,380,65,new Color(.13f,.33f,.4f),ToggleDebug,27);
            toast=Label(root,"",0,323,990,110,30,Color.white);
            loading=Panel(root,"Loading",0,0,1080,1920,Ink,true).gameObject;Icon(loading.transform,14,0,110,280);
            Label(loading.transform,"ĐANG KHÁM PHÁ ĐẠI DƯƠNG…",0,-127,970,130,34,Aqua);
            modal.SetActive(false);debug.SetActive(false);loading.SetActive(false);bossRoot.SetActive(false);ShowMenu(true);
            // Jackpot overlay toàn màn hình
            jackpotOverlay=Panel(root,"Jackpot overlay",0,0,1080,1920,new Color(0,.01f,.06f,.92f),false).gameObject;
            var glow=Panel(jackpotOverlay.transform,"Glow",0,120,920,440,new Color(.7f,.5f,.05f,.18f)).gameObject;
            Label(jackpotOverlay.transform,"🎊 NỔ HŨ! 🎊",0,380,960,140,72,new Color(1,.92f,.1f));
            jackpotTitle=Label(jackpotOverlay.transform,"",0,190,960,110,46,Color.white);
            Label(jackpotOverlay.transform,"PHẦN THƯỞNG",0,90,700,72,32,new Color(.8f,.85f,1f));
            jackpotAmount=Label(jackpotOverlay.transform,"",0,-50,960,130,66,new Color(1,.92f,.1f));
            Label(jackpotOverlay.transform,"CHÚC MỪNG!",0,-200,700,80,34,Gold);
            jackpotOverlay.SetActive(false);
        }
        RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color color,bool blocks=false)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=blocks;return rt;}
        Image Art(Transform parent,Sprite sprite,float x,float y,float w,float h,bool preserve=true)
        {var rt=Panel(parent,"Artwork",x,y,w,h,Color.white);var image=rt.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=preserve;return image;}
        void Icon(Transform parent,int index,float x,float y,float size)
        {if(app.Catalog.uiIcons!=null&&app.Catalog.uiIcons.Length>index)Art(parent,app.Catalog.uiIcons[index],x,y,size,size);else Art(parent,app.Catalog.disk,x,y,size,size).color=Gold;}
        void Plate(Transform parent,float x,float y,float w,float h)
        {if(app.Catalog.uiSkins.Length>2)Skin(Panel(parent,"Currency plate",x,y,w,h,Color.white).GetComponent<Image>(),2);else{Panel(parent,"Gold trim",x,y,w,h,new Color(.87f,.7f,.29f,.95f));Panel(parent,"Blue glass",x,y,w-5,h-5,new Color(.02f,.12f,.24f,.92f));}}
        void Skin(Image image,int index)
        {image.sprite=app.Catalog.uiSkins[index];image.type=Image.Type.Sliced;image.color=Color.white;image.pixelsPerUnitMultiplier=.6f;}
        Text Label(Transform parent,string value,float x,float y,float w,float h,int size,Color color)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(Text));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.005f,.035f,.10f,.9f);outline.effectDistance=new Vector2(1.4f,-1.4f);return text;
        }
        Button Button(Transform parent,string title,float x,float y,float w,float h,Color color,Action callback,int fontSize=27)
        {
            var rt=Panel(parent,title,x,y,w,h,Gold,true);var face=Panel(rt,"Button face",0,0,w-5,h-5,color,true);var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=face.GetComponent<Image>();
            if(app.Catalog.uiSkins.Length>1){rt.GetComponent<Image>().color=Color.clear;Skin(face.GetComponent<Image>(),title=="CHƠI NGAY"?0:1);}
            var colors=button.colors;colors.highlightedColor=new Color(.8f,1,1);colors.pressedColor=new Color(.6f,.8f,.9f);colors.disabledColor=new Color(.4f,.5f,.6f);button.colors=colors;
            Label(rt,title,0,0,w-8,h-8,fontSize,Color.white);button.onClick.AddListener(()=>{app.Audio.Play(SoundCue.Button);callback();});rt.gameObject.AddComponent<ButtonFeedback>();return button;
        }
        void Badge(Transform parent,int icon,string title,float x,float y,Action callback,float scale=1)
        {
            var go=new GameObject(title,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(152,160);rt.localScale=Vector3.one*scale;
            var image=Art(rt,app.Catalog.disk,0,13,131,131);image.color=new Color(.11f,.5f,.73f,.8f);image.raycastTarget=true;Art(rt,app.Catalog.ring,0,13,139,139).color=Gold;
            if(app.Catalog.uiSkins.Length>5){image.sprite=app.Catalog.uiSkins[5];image.color=Color.white;}
            Icon(rt,icon,0,17,120);Label(rt,title,0,-66,182,43,21,Gold);var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{app.Audio.Play(SoundCue.Button);callback();});image.gameObject.AddComponent<ButtonFeedback>();
        }
        void RoomBubble(Transform parent,float x,float y,float size,int fishIndex,string title,Action callback)
        {
            var image=Art(parent,app.Catalog.disk,x,y,size,size);image.color=new Color(.15f,.65f,1,.85f);image.raycastTarget=true;Art(parent,app.Catalog.ring,x,y,size+15,size+15).color=Gold;
            if(app.Catalog.uiSkins.Length>5){image.sprite=app.Catalog.uiSkins[5];image.color=Color.white;}
            Art(parent,app.Catalog.room.fish[fishIndex].sprite,x,y+8,size*.81f,size*.77f);Label(parent,title,x,y-size*.53f,size*1.3f,58,size>250?28:20,Color.white);
            var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{app.Audio.Play(SoundCue.Button);callback();});
        }
        public void ShowMenu(bool value){menu.SetActive(value);game.SetActive(!value);modal.SetActive(false);debug.SetActive(false);}
        public void Loading(bool value)=>loading.SetActive(value);
        public void Announce(string value){eventText.text=value;eventTime=5;}
        public void Toast(string value){toast.text=value;toastTime=3;}
        public void ShowJackpot(string fishName,long reward)
        {
            jackpotTitle.text=fishName.ToUpperInvariant();
            jackpotAmount.text="+ "+reward.ToString("N0")+" VÀNG";
            jackpotOverlay.SetActive(true); jackpotTime=3.5f;
        }
        public void ToggleDebug()=>debug.SetActive(!debug.activeSelf);
        public void CloseModal(){modal.SetActive(false);app.Resume();}
        public void BeginPanel(string title,string subtitle)
        {
            for(int i=modalContent.childCount-1;i>=1;i--)UnityEngine.Object.Destroy(modalContent.GetChild(i).gameObject);
            modal.SetActive(true);Label(modalContent,title,-32,623,850,86,38,Gold);Label(modalContent,subtitle,0,518,921,117,26,Muted);
            Button(modalContent,"X",433,640,72,72,new Color(.16f,.31f,.39f),CloseModal,30);
        }
        public void Row(int index,string description,string action,Action callback,bool enabled=true)
        {
            float y=372-index*120;Panel(modalContent,"Row",0,y,930,106,new Color(.04f,.20f,.30f));Label(modalContent,description,-147,y,602,96,25,Color.white);
            var button=Button(modalContent,action,332,y,247,82,enabled?new Color(.04f,.40f,.47f):Ink,callback,23);button.interactable=enabled;
        }
        public void Tabs(string[] names,Action<int> callback)
        {for(int i=0;i<names.Length;i++){int index=i;Button(modalContent,names[i],-322+(i%3)*322,-589-(i/3)*80,305,67,new Color(.05f,.27f,.38f),()=>callback(index),22);}}
        public void Tick(float dt)
        {
            if(eventTime>0){eventTime-=dt;eventText.transform.localScale=Vector3.one*(1+Mathf.Sin(eventTime*12)*.05f);if(eventTime<=0)eventText.text="";}
            if(toastTime>0&&(toastTime-=dt)<=0)toast.text="";
            // Auto-dismiss jackpot
            if(jackpotTime>0){jackpotTime-=dt;if(jackpotTime<=0)jackpotOverlay.SetActive(false);}
            hudTimer-=dt;if(hudTimer>0)return;hudTimer=.1f;
            double prev=displayedCoins;
            displayedCoins=Math.Abs(displayedCoins-app.Wallet.Coins)<2?app.Wallet.Coins:displayedCoins+(app.Wallet.Coins-displayedCoins)*.35;
            string value=((long)displayedCoins).ToString("N0");balance.text=value;menuBalance.text=value;gemBalance.text=menuGems.text=app.Wallet.Diamonds.ToString("N0");
            // Animation số dư nảy lên khi nhận tiền
            float scale = 1f + Mathf.Clamp01((float)(displayedCoins - prev) / 1000f) * 0.4f;
            balance.transform.localScale = Vector3.Lerp(balance.transform.localScale, Vector3.one * scale, 0.4f);
            if (Mathf.Abs(balance.transform.localScale.x - 1f) < 0.02f) balance.transform.localScale = Vector3.one;
            profile.text="ID: LUMA-0001       CẤP "+app.SaveData.level;avatar.sprite=app.Catalog.room.fish[(app.SaveData.selectedCosmetic&8)!=0?23:0].sprite;avatarFrame.color=(app.SaveData.selectedCosmetic&16)!=0?new Color(1,.4f,.85f):Color.white;
            bet.text="CƯỢC  "+app.Loadout.Bet;gunLabel.text="SÚNG CẤP "+(app.Loadout.Selected+1);autoLabel.color=app.Auto?Aqua:Color.white;lockLabel.color=app.Lock?Aqua:Color.white;
            for(int i=0;i<6;i++){float cd=app.Skills.Cooldown(i);skillButtons[i].interactable=cd<=0;skillLabels[i].text=cd>0?Mathf.CeilToInt(cd)+"s":app.Catalog.skills[i].displayName;}
            var boss=app.Boss;bossRoot.SetActive(boss!=null);if(boss!=null){bossLabel.text=boss.Data.displayName.ToUpperInvariant()+"  /  "+Mathf.CeilToInt(boss.HP)+" HP";bossFill.rectTransform.localScale=new Vector3(Mathf.Clamp01(boss.HP/boss.Data.bossHP),1,1);}
            if(debug.activeSelf)fpsText.text="FPS "+Mathf.RoundToInt(1/Mathf.Max(.001f,dt))+" / FISH "+app.FishCount+"\nBOLTS "+app.BulletCount+" / VFX "+app.EffectCount+" / GOD "+app.Combat.GodMode;
        }
    }
}
