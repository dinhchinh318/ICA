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
        readonly RectTransform bossBar;
        double displayedCoins;
        float eventTime,toastTime,hudTimer,jackpotTime;
        float toastSlide; // -1 = hidden, 0..1 = visible
        float bossBarPulse;
        float jackpotShake;

        float jackpotCoinTimer;
        readonly Text[] jackpotCoins=new Text[18];
        readonly RectTransform[] jackpotCoinRT=new RectTransform[18];
        GameObject jackpotOverlay;
        Text jackpotTitle,jackpotAmount,jackpotMult;
        RectTransform jackpotPanel;
        RectTransform toastRT;

        public bool ModalOpen=>modal.activeSelf;
        public bool DebugOpen=>debug.activeSelf;
        public ReefUI(ReefApp app,Font font)
        {
            this.app=app;this.font=font;
            var go=new GameObject("Portrait Canvas 1080x1920",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(app.transform);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            // Fit the entire 9:16 design inside any window, including a wide Editor Game View.
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=0.5f;
            root=(RectTransform)go.transform;
            new GameObject("Input UI",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(app.transform);
            // ─── LOBBY / MENU ───────────────────────────────────────────────
            menu=Panel(root,"Lobby",0,0,1080,1920,new Color(.035f,.16f,.27f)).gameObject;
            if(app.Catalog.lobbyBackground!=null)Art(menu.transform,app.Catalog.lobbyBackground,0,0,1080,1920,false);
            Panel(menu.transform,"Header",0,884,1080,152,new Color(.02f,.07f,.17f,.82f));
            Button(menu.transform,"☰",-474,880,86,88,new Color(.04f,.32f,.53f),()=>app.OpenPanel("SETTINGS"),40);
            Plate(menu.transform,-156,880,477,80);Icon(menu.transform,0,-355,884,77);
            Plate(menu.transform,319,880,395,80);Icon(menu.transform,1,172,884,77);
            menuBalance=Label(menu.transform,"",-116,880,332,72,31,Color.white);menuGems=Label(menu.transform,"",329,880,220,72,31,Color.white);
            // Profile card với gradient đẹp
            Plate(menu.transform,-163,746,708,156);
            avatarFrame=Art(menu.transform,app.Catalog.disk,-419,750,146,146);avatarFrame.color=Gold;
            if(app.Catalog.uiSkins.Length>3){avatarFrame.sprite=app.Catalog.uiSkins[3];avatarFrame.color=Color.white;}
            avatar=Art(menu.transform,app.Catalog.room.fish[0].sprite,-419,750,125,108);
            Label(menu.transform,"THUYỀN TRƯỞNG",-82,785,465,42,29,Color.white);
            profile=Label(menu.transform,"",-82,738,465,44,24,Aqua);
            Label(menu.transform,"◆ NHÀ THÁM HIỂM ĐẠI DƯƠNG",-56,695,514,35,19,Gold);
            Button(menu.transform,"TÀI KHOẢN",365,746,286,92,Ink,app.Account,25);
            // Logo to bự, chữ cực đẹp
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
            Panel(menu.transform,"Navigation",0,-889,1080,142,new Color(.01f,.12f,.23f,.95f));
            string[] tabs={"Hộp thư","Cửa hàng","Nhiệm vụ","Bộ sưu tập","Bạn bè","Cài đặt"};string[] actions={"MAIL","SHOP","QUEST","ACHIEVEMENT","FRIENDS","SETTINGS"};int[] icons={2,3,4,5,15,7};
            for(int i=0;i<tabs.Length;i++){string action=actions[i];Badge(menu.transform,icons[i],tabs[i],-450+i*180,-867,()=>app.OpenPanel(action),.95f);}
            // ─── GAMEPLAY HUD ───────────────────────────────────────────────
            game=Panel(root,"Portrait gameplay HUD",0,0,1080,1920,Color.clear).gameObject;
            Panel(game.transform,"Header shade",0,889,1080,142,new Color(.01f,.08f,.14f,.92f));
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
            // Boss bar xịn hơn với border
            var bossContainer=Panel(game.transform,"Boss Container",0,561,980,86,new Color(.02f,.005f,.04f,.0f));
            bossRoot=bossContainer.gameObject;
            Panel(bossContainer,"Boss bg",0,0,970,80,new Color(.08f,.02f,.16f,.95f));
            bossLabel=Label(bossContainer,"",0,16,920,40,25,new Color(1,.55f,.75f));
            var track=Panel(bossContainer,"HP track",0,-18,930,16,new Color(.16f,.12f,.24f));
            bossFill=Panel(track,"HP",0,0,930,16,new Color(.95f,.26f,.49f)).GetComponent<Image>();
            bossBar=track;
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
            // ─── MODALS ─────────────────────────────────────────────────────
            modal=Panel(root,"Modal shade",0,0,1080,1920,new Color(0,.025f,.07f,.87f),true).gameObject;
            modalContent=Panel(modal.transform,"Modal edge",0,0,1016,1450,Gold);Panel(modalContent,"Modal inner",0,0,1004,1438,Ink);
            if(app.Catalog.uiSkins.Length>4){Skin(modalContent.GetComponent<Image>(),4);modalContent.GetChild(0).GetComponent<Image>().color=Color.clear;}
            debug=Panel(root,"Debug",0,0,972,1470,Ink,true).gameObject;
            Label(debug.transform,"DEVELOPMENT CONSOLE",0,669,900,68,33,Aqua);
            string[] commands={"+10,000 COINS","SPAWN SMALL","SPAWN LARGE","SPAWN SPECIAL","SPAWN BOSS","KILL ALL","NEXT GUN","CHANGE BET","GOD MODE","COLLIDERS","FISH WAVE"};
            for(int i=0;i<commands.Length;i++){int index=i;Button(debug.transform,commands[i],0,562-i*96,880,78,new Color(.07f,.26f,.37f),()=>app.DebugAction(index),27);}
            fpsText=Label(debug.transform,"",0,-570,900,99,27,Muted);Button(debug.transform,"ĐÓNG",0,-665,380,65,new Color(.13f,.33f,.4f),ToggleDebug,27);
            // ─── TOAST (slide-in animation) ─────────────────────────────────
            var toastGo=new GameObject("Toast",typeof(RectTransform),typeof(Image));toastGo.transform.SetParent(root,false);
            toastRT=(RectTransform)toastGo.transform;toastRT.anchoredPosition=new Vector2(0,280);toastRT.sizeDelta=new Vector2(900,80);
            var toastBG=toastGo.GetComponent<Image>();toastBG.color=new Color(.04f,.28f,.45f,.92f);toastBG.raycastTarget=false;
            if(app.Catalog.uiSkins.Length>2)Skin(toastBG,2);
            toast=Label(toastRT,"",0,0,880,72,28,Color.white);
            toastRT.anchoredPosition=new Vector2(0,1100); // Start off screen
            toastSlide=-1;toastRT.gameObject.SetActive(false);
            // Loading screen
            loading=Panel(root,"Loading",0,0,1080,1920,Ink,true).gameObject;Icon(loading.transform,14,0,110,280);
            Label(loading.transform,"ĐANG KHÁM PHÁ ĐẠI DƯƠNG…",0,-127,970,130,34,Aqua);
            // ─── JACKPOT OVERLAY siêu bùng nổ ──────────────────────────────
            jackpotOverlay=new GameObject("Jackpot Overlay",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            jackpotOverlay.transform.SetParent(root,false);
            jackpotOverlay.GetComponent<Canvas>().overrideSorting=true;
            jackpotOverlay.GetComponent<Canvas>().sortingOrder=200;
            var jpRT=(RectTransform)jackpotOverlay.transform;jpRT.anchoredPosition=Vector2.zero;jpRT.sizeDelta=new Vector2(1080,1920);
            // Full screen dark flash
            Panel(jpRT,"JP bg",0,0,1080,1920,new Color(0,.01f,.06f,.93f));
            // Animated glow panel
            jackpotPanel=Panel(jpRT,"JP panel",0,80,1000,700,new Color(.12f,.07f,.01f,.0f));
            Panel(jackpotPanel,"Glow 1",0,0,940,620,new Color(.6f,.42f,.02f,.22f));
            Panel(jackpotPanel,"Glow 2",0,0,860,540,new Color(.9f,.65f,.04f,.12f));
            // Labels
            Icon(jpRT,5,0,525,180);
            Label(jpRT,"NỔ HŨ !!",0,380,960,140,76,new Color(1,.92f,.1f));
            jackpotTitle=Label(jpRT,"",0,230,960,110,44,Color.white);
            Label(jpRT,"PHẦN THƯỞNG KHỔNG LỒ",0,145,900,75,28,new Color(.8f,.85f,1f));
            jackpotAmount=Label(jpRT,"",0,20,960,130,72,new Color(1,.92f,.1f));
            jackpotMult=Label(jpRT,"",0,-90,700,65,30,new Color(.6f,.95f,.7f));
            Label(jpRT,"CHÚC MỪNG!",0,-190,880,80,34,Gold);
            Label(jpRT,"CHẠM ĐỂ TIẾP TỤC",0,-310,700,55,22,Muted);
            // Coin rain sprites
            for(int i=0;i<18;i++)
            {
                var crt=Panel(jpRT,"Coin"+i,UnityEngine.Random.Range(-520f,520f),UnityEngine.Random.Range(-1000f,-500f),55,55,Gold);
                var cImg=crt.GetComponent<Image>();cImg.sprite=app.Catalog.disk;cImg.color=new Color(1,.85f,.15f,0);
                jackpotCoinRT[i]=crt;jackpotCoins[i]=Label(crt,"✦",0,0,55,55,22,new Color(1,.6f,.1f));
            }
            // Tap to dismiss
            var dismissBtn=jackpotOverlay.AddComponent<Button>();dismissBtn.onClick.AddListener(()=>{jackpotOverlay.SetActive(false);jackpotTime=0;});
            var dismissImg=jackpotOverlay.AddComponent<Image>();dismissImg.color=Color.clear;dismissImg.raycastTarget=true;
            jackpotOverlay.SetActive(false);
            modal.SetActive(false);debug.SetActive(false);loading.SetActive(false);bossRoot.SetActive(false);ShowMenu(true);
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
            var image=Art(rt,app.Catalog.disk,0,13,131,131);image.color=new Color(.11f,.5f,.73f,.8f);image.raycastTarget=true;if(app.Catalog.uiSkins.Length<=5)Art(rt,app.Catalog.ring,0,13,139,139).color=Gold;
            if(app.Catalog.uiSkins.Length>5){image.sprite=app.Catalog.uiSkins[5];image.color=Color.white;}
            Icon(rt,icon,0,17,120);Label(rt,title,0,-66,182,43,21,Gold);var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{app.Audio.Play(SoundCue.Button);callback();});image.gameObject.AddComponent<ButtonFeedback>();
        }
        void RoomBubble(Transform parent,float x,float y,float size,int fishIndex,string title,Action callback)
        {
            var image=Art(parent,app.Catalog.disk,x,y,size,size);image.color=new Color(.15f,.65f,1,.85f);image.raycastTarget=true;if(app.Catalog.uiSkins.Length<=5)Art(parent,app.Catalog.ring,x,y,size+15,size+15).color=Gold;
            if(app.Catalog.uiSkins.Length>5){image.sprite=app.Catalog.uiSkins[5];image.color=Color.white;}
            Art(parent,app.Catalog.room.fish[fishIndex].sprite,x,y+8,size*.81f,size*.77f);Label(parent,title,x,y-size*.48f,size*1.3f,58,size>250?28:20,Color.white);
            var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{app.Audio.Play(SoundCue.Button);callback();});
        }
        public void ShowMenu(bool value){menu.SetActive(value);game.SetActive(!value);modal.SetActive(false);debug.SetActive(false);}
        public void ValidateLayout()
        {
            Canvas.ForceUpdateCanvases();
            Debug.Assert(root.rect.width>=1079&&root.rect.height>=1919,"Canvas must fit full portrait design at any screen aspect");
            Debug.Assert(!loading.activeSelf,"Loading overlay must be dismissed after scene load");
            var corners=new Vector3[4];
            foreach(var button in menu.GetComponentsInChildren<Button>()){
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(var corner in corners){var p=root.InverseTransformPoint(corner);Debug.Assert(root.rect.Contains(new Vector2(p.x,p.y)),"Lobby button must fit visible canvas: "+button.name);}
            }
        }
        public void Loading(bool value)=>loading.SetActive(value);
        public void Announce(string value){eventText.text=value;eventTime=5;}
        public void Toast(string value)
        {
            toastRT.gameObject.SetActive(true);toast.text=value;toastTime=3.2f;toastSlide=0;
            // Slide in from top
            if(toastRT!=null)toastRT.anchoredPosition=new Vector2(0,1100);
        }
        public void ShowJackpot(string fishName,long reward)
        {
            jackpotTitle.text=fishName.ToUpperInvariant();
            jackpotAmount.text="+ "+reward.ToString("N0")+" VÀNG";
            jackpotMult.text="JACKPOT x"+(reward/(Mathf.Max(1,app.Loadout.Bet))).ToString("N0");
            jackpotOverlay.SetActive(true); jackpotTime=5f; jackpotShake=0.5f;
            // Reset coins
            for(int i=0;i<18;i++)
            {
                float rx=UnityEngine.Random.Range(-510f,510f);
                jackpotCoinRT[i].anchoredPosition=new Vector2(rx,-900);
                var img=jackpotCoinRT[i].GetComponent<Image>();img.color=new Color(1,.85f,.15f,0);
            }
        }
        public void ToggleDebug()=>debug.SetActive(!debug.activeSelf);
        public void CloseModal(){modal.SetActive(false);app.Resume();}
        public void BeginPanel(string title,string subtitle)
        {
            for(int i=modalContent.childCount-1;i>=1;i--)UnityEngine.Object.Destroy(modalContent.GetChild(i).gameObject);
            modal.SetActive(true);Label(modalContent,title,-32,623,850,86,38,Gold);Label(modalContent,subtitle,0,518,921,117,26,Muted);
            Button(modalContent,"X",433,640,72,72,new Color(.16f,.31f,.39f),CloseModal,30);
        }
        public InputField Input(int index,string hint,bool password)
        {
            float y=372-index*120;var rt=Panel(modalContent,"Input",0,y,910,96,new Color(.025f,.12f,.20f),true);
            var field=rt.gameObject.AddComponent<InputField>();field.targetGraphic=rt.GetComponent<Image>();
            var text=Label(rt,"",0,0,850,86,30,Color.white);text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;
            var placeholder=Label(rt,hint,0,0,850,86,27,Muted);placeholder.alignment=TextAnchor.MiddleLeft;
            field.textComponent=text;field.placeholder=placeholder;field.characterLimit=password?128:24;
            field.contentType=password?InputField.ContentType.Password:InputField.ContentType.Standard;return field;
        }
        public void RoomSeats(LumaReef.Network.RoomInfo room)
        {
            string[] corners={"DƯỚI TRÁI","DƯỚI PHẢI","TRÊN TRÁI","TRÊN PHẢI"};
            for(int i=0;i<4;i++){
                float x=i%2==0?-235:235,y=i<2?-5:260;string player="Đang chờ...";bool occupied=false;
                foreach(var seat in room.seats)if(seat.seat==i){player=seat.username;occupied=true;}
                var panel=Panel(modalContent,"Seat "+i,x,y,430,230,new Color(.025f,.16f,.25f));
                if(app.Catalog.cannonSprites.Length>i){var art=Art(panel,app.Catalog.cannonSprites[i],0,20,110,115);art.color=occupied?Color.white:new Color(1,1,1,.25f);}
                Label(panel,corners[i],0,87,405,38,22,Gold);Label(panel,player,0,-73,405,46,24,occupied?Aqua:Muted);
            }
        }
        public void AccountForm(bool loggedIn)
        {
            BeginPanel("TÀI KHOẢN",loggedIn?"Đã đăng nhập: "+LumaReef.Network.DatabaseManager.CurrentUsername+" • Lưu SQLite trên server.":"Tên 3–24 ký tự a-z, 0-9, _. Mật khẩu tối thiểu 8 ký tự. Có thể đóng để chơi khách.");
            if(loggedIn){Row(1,"Lưu tiến trình lên database","LƯU NGAY",()=>app.SaveNow());Row(3,"Phòng chờ dành cho 4 người","PHÒNG CHƠI",()=>app.OpenPanel("ROOM"));Row(5,"Trở lại hồ sơ khách trên máy","ĐĂNG XUẤT",app.Logout);return;}
            var username=Input(0,"Tên đăng nhập",false);var password=Input(1,"Mật khẩu",true);
            Row(3,"Tải tiến trình đã lưu trên server","ĐĂNG NHẬP",()=>app.Authenticate(username.text.Trim(),password.text,false));
            Row(4,"Tạo tài khoản từ tiến trình khách","ĐĂNG KÝ",()=>app.Authenticate(username.text.Trim(),password.text,true));
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
            // ── Toast slide animation ──────────────────────────────────────
            if(toastSlide>=0 && toastRT!=null)
            {
                toastSlide=Mathf.MoveTowards(toastSlide,1,dt*5);
                float targetY=toastTime>0.3f?490:1100;
                float curY=toastRT.anchoredPosition.y;
                toastRT.anchoredPosition=new Vector2(0,Mathf.Lerp(curY,targetY,dt*8));
                if(toastTime>0)toastTime-=dt;
                if(toastTime<=0&&Mathf.Abs(curY-1100)<2){toastRT.anchoredPosition=new Vector2(0,1100);toastSlide=-1;toastRT.gameObject.SetActive(false);toast.text="";}
            }
            // ── Event text bounce ─────────────────────────────────────────
            if(eventTime>0){eventTime-=dt;eventText.transform.localScale=Vector3.one*(1+Mathf.Sin(eventTime*12)*.05f);if(eventTime<=0)eventText.text="";}
            // ── Boss bar pulse ────────────────────────────────────────────
            bossBarPulse+=dt*4;
            if(bossRoot.activeSelf && bossFill!=null)
            {
                float pulse=1+Mathf.Sin(bossBarPulse)*.06f;
                bossBar.localScale=new Vector3(1,pulse,1);
                bossFill.color=Color.Lerp(new Color(.95f,.26f,.49f),new Color(1,.55f,.75f),Mathf.Sin(bossBarPulse)*.5f+.5f);
            }
            // ── Jackpot coin rain ─────────────────────────────────────────
            if(jackpotTime>0)
            {
                jackpotTime-=dt;
                // Shake the panel
                if(jackpotShake>0){ jackpotShake-=dt; if(jackpotPanel!=null)jackpotPanel.anchoredPosition=new Vector2(Mathf.Sin(jackpotShake*60)*8*(jackpotShake/.5f),80); }
                else if(jackpotPanel!=null)jackpotPanel.anchoredPosition=new Vector2(0,80);
                // Animate coins raining down and fading in
                jackpotCoinTimer-=dt;
                for(int i=0;i<18;i++)
                {
                    var pos=jackpotCoinRT[i].anchoredPosition;
                    pos.y+=dt*600;
                    var col=jackpotCoinRT[i].GetComponent<Image>().color;
                    if(pos.y<400)col.a=Mathf.MoveTowards(col.a,1,dt*3);
                    if(pos.y>600)col.a=Mathf.MoveTowards(col.a,0,dt*4);
                    if(pos.y>650){pos.y=-900;pos.x=UnityEngine.Random.Range(-510f,510f);col.a=0;}
                    jackpotCoinRT[i].anchoredPosition=pos;
                    jackpotCoinRT[i].GetComponent<Image>().color=col;
                    // Spin
                    jackpotCoinRT[i].localRotation=Quaternion.Euler(0,0,jackpotCoinRT[i].localRotation.eulerAngles.z+dt*360*(i%2==0?1:-1));
                }
                if(jackpotTime<=0)jackpotOverlay.SetActive(false);
            }
            hudTimer-=dt;if(hudTimer>0)return;hudTimer=.1f;
            double prev=displayedCoins;
            displayedCoins=Math.Abs(displayedCoins-app.Wallet.Coins)<2?app.Wallet.Coins:displayedCoins+(app.Wallet.Coins-displayedCoins)*.35;
            string value=((long)displayedCoins).ToString("N0");balance.text=value;menuBalance.text=value;gemBalance.text=menuGems.text=app.Wallet.Diamonds.ToString("N0");
            // Animation số dư nảy lên khi nhận tiền
            float scale = 1f + Mathf.Clamp01((float)(displayedCoins - prev) / 1000f) * 0.4f;
            balance.transform.localScale = Vector3.Lerp(balance.transform.localScale, Vector3.one * scale, 0.4f);
            if (Mathf.Abs(balance.transform.localScale.x - 1f) < 0.02f) balance.transform.localScale = Vector3.one;
            profile.text="ID: "+LumaReef.Network.DatabaseManager.CurrentUsername+"  |  CẤP "+app.SaveData.level;
            avatar.sprite=app.Catalog.room.fish[(app.SaveData.selectedCosmetic&8)!=0?23:0].sprite;avatarFrame.color=(app.SaveData.selectedCosmetic&16)!=0?new Color(1,.4f,.85f):Color.white;
            bet.text="CƯỢC  "+app.Loadout.Bet;gunLabel.text="SÚNG CẤP "+(app.Loadout.Selected+1);autoLabel.color=app.Auto?Aqua:Color.white;lockLabel.color=app.Lock?Aqua:Color.white;
            for(int i=0;i<6;i++){float cd=app.Skills.Cooldown(i);skillButtons[i].interactable=cd<=0;skillLabels[i].text=cd>0?Mathf.CeilToInt(cd)+"s":app.Catalog.skills[i].displayName;}
            var boss=app.Boss;bossRoot.SetActive(boss!=null);
            if(boss!=null){bossLabel.text=boss.Data.displayName.ToUpperInvariant()+"  ⚔  CẤP NGUY HIỂM";bossFill.rectTransform.localScale=new Vector3(boss.Data.bossHP>0?Mathf.Clamp01(boss.HP/boss.Data.bossHP):1,1,1);}
            if(debug.activeSelf)fpsText.text="FPS "+Mathf.RoundToInt(1/Mathf.Max(.001f,dt))+" / FISH "+app.FishCount+"\nBOLTS "+app.BulletCount+" / VFX "+app.EffectCount+" / GOD "+app.Combat.GodMode;
        }
    }
}
