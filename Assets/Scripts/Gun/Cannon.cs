using UnityEngine;
using LumaReef.Data;
namespace LumaReef.Gun
{
    public sealed class Cannon
    {
        public readonly Transform Pivot, Barrel;
        public Vector2 Direction { get; private set; } = Vector2.up;
        public Vector2 Muzzle => (Vector2)Pivot.position+Direction*1.05f;
        readonly SpriteRenderer barrelRenderer;
        readonly SpriteRenderer[] sideBarrels=new SpriteRenderer[2];
        readonly SpriteRenderer aiRenderer;
        readonly Sprite[] skins;
        float cooldown, recoil;
        public Cannon(Transform root, Sprite disk, Sprite body,Sprite[] skins=null)
        {
            this.skins=skins;
            var baseGo=new GameObject("Cannon base"); baseGo.transform.SetParent(root); baseGo.transform.position=new Vector3(0,-8.1f,0);
            var sr=baseGo.AddComponent<SpriteRenderer>(); sr.sprite=disk; sr.color=new Color(.04f,.19f,.25f); sr.sortingOrder=30; baseGo.transform.localScale=Vector3.one*1.2f;
            Pivot=new GameObject("Aim pivot").transform; Pivot.SetParent(root); Pivot.position=baseGo.transform.position;
            Barrel=new GameObject("Recoil barrel").transform; Barrel.SetParent(Pivot); Barrel.localPosition=new Vector3(0,.35f,0);
            barrelRenderer=Barrel.gameObject.AddComponent<SpriteRenderer>(); barrelRenderer.sprite=body; barrelRenderer.sortingOrder=31; Barrel.localScale=new Vector3(.43f,.57f,1);
            for(int i=0;i<2;i++){var side=new GameObject("Auxiliary barrel");side.transform.SetParent(Pivot);side.transform.localPosition=new Vector3(i==0?-.25f:.25f,.32f,0);side.transform.localScale=new Vector3(.25f,.49f,1);sideBarrels[i]=side.AddComponent<SpriteRenderer>();sideBarrels[i].sprite=body;sideBarrels[i].sortingOrder=30;side.SetActive(false);}
            var art=new GameObject("AI cannon skin");art.transform.SetParent(Pivot);art.transform.localPosition=Vector3.zero;art.transform.localScale=Vector3.one*1.8f;aiRenderer=art.AddComponent<SpriteRenderer>();aiRenderer.sortingOrder=32;
        }
        public void Tick(float dt, Vector2 target, GunData gun)
        {
            float angle=Mathf.Clamp(Mathf.Atan2(target.y-Pivot.position.y,target.x-Pivot.position.x)*Mathf.Rad2Deg,12,168);
            Direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
            Pivot.rotation=Quaternion.Euler(0,0,angle-90); cooldown-=dt; recoil=Mathf.MoveTowards(recoil,0,dt*2.5f);
            Barrel.localPosition=new Vector3(0,.35f-recoil,0); barrelRenderer.color=gun.color;
            for(int i=0;i<2;i++){sideBarrels[i].gameObject.SetActive(gun.barrels>1);sideBarrels[i].color=gun.color;}
            barrelRenderer.enabled=gun.barrels!=2;
            bool hasSkin=skins!=null&&skins.Length>=gun.level&&skins[gun.level-1]!=null;
            aiRenderer.enabled=hasSkin;
            if(hasSkin){aiRenderer.sprite=skins[gun.level-1];aiRenderer.transform.localPosition=new Vector3(0,.2f-recoil,0);barrelRenderer.enabled=false;for(int i=0;i<2;i++)sideBarrels[i].gameObject.SetActive(false);}
        }
        public bool Ready => cooldown<=0;
        public void Fired(GunData gun, float rate) { cooldown=1f/(gun.fireRate*rate); recoil=.14f; }
    }
}
