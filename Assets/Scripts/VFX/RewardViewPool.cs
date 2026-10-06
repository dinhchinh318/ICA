using UnityEngine;
using LumaReef.Core;
namespace LumaReef.VFX
{
    public sealed class CoinView : IPooled
    {
        public bool Active { get; private set; }
        readonly Transform transform;
        readonly SpriteRenderer renderer;
        Vector2 start,control,target;
        float age,delay;
        public CoinView(Transform root,Sprite sprite)
        {
            var go=new GameObject("Pooled coin"); transform=go.transform; transform.SetParent(root);
            renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=new Color(1,.78f,.23f); renderer.sortingOrder=45;
            transform.localScale=Vector3.one*.15f; go.SetActive(false);
        }
        public void Play(Vector2 origin,Vector2 destination,int index)
        {
            start=origin; target=destination; control=origin+new Vector2(Random.Range(-2f,2f),Random.Range(.7f,2.3f));
            age=0; delay=index*.035f; Active=true; transform.position=start; transform.gameObject.SetActive(true);
        }
        public void Tick(float dt)
        {
            age+=dt; float t=Mathf.Clamp01((age-delay)/.9f),v=1-t;
            transform.position=v*v*start+2*v*t*control+t*t*target;
            transform.localScale=new Vector3(.15f*(.6f+Mathf.Abs(Mathf.Cos(age*16))*.4f),.15f,1);
            if(t>=1)Release();
        }
        public void Release() { Active=false; transform.gameObject.SetActive(false); }
    }
    public sealed class RewardText : IPooled
    {
        public bool Active { get; private set; }
        readonly TextMesh text;
        readonly Transform transform;
        Vector2 origin;
        float age;
        public RewardText(Transform root,Font font)
        {
            var go=new GameObject("Pooled reward text"); transform=go.transform; transform.SetParent(root);
            text=go.AddComponent<TextMesh>(); text.font=font; text.fontSize=64; text.characterSize=.075f; text.anchor=TextAnchor.MiddleCenter; text.fontStyle=FontStyle.Bold;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=font.material; renderer.sortingOrder=55;
            go.SetActive(false);
        }
        public void Play(Vector2 position,long amount,bool big)
        {
            origin=position; age=0; Active=true; text.text="+"+amount.ToString("N0");
            text.characterSize=big?.14f:.075f; transform.position=position; transform.gameObject.SetActive(true);
        }
        public void Tick(float dt)
        {
            age+=dt; transform.position=origin+Vector2.up*(age*.8f);
            transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(age/.25f)*Mathf.PI)*.35f);
            text.color=new Color(1,.88f,.32f,Mathf.Clamp01((1.2f-age)*2)); if(age>1.2f)Release();
        }
        public void Release() { Active=false; transform.gameObject.SetActive(false); }
    }
    public sealed class RewardViewPool
    {
        readonly FixedPool<CoinView> coins;
        readonly FixedPool<RewardText> texts;
        public int ActiveCount => coins.Count+texts.Count;
        public RewardViewPool(Transform root,Sprite coin,Font font,int coinCount,int textCount)
        { coins=new FixedPool<CoinView>(coinCount,i=>new CoinView(root,coin)); texts=new FixedPool<RewardText>(textCount,i=>new RewardText(root,font)); }
        public void Play(Vector2 at,long reward,bool big)
        {
            texts.Rent()?.Play(at,reward,big);
            for(int i=0;i<(big?28:7);i++)coins.Rent()?.Play(at,new Vector2(1.6f,8.75f),i);
        }
        public void Tick(float dt)
        { for(int i=0;i<coins.Items.Length;i++)if(coins.Items[i].Active)coins.Items[i].Tick(dt); for(int i=0;i<texts.Items.Length;i++)if(texts.Items[i].Active)texts.Items[i].Tick(dt); }
        public void Clear() { coins.Clear(); texts.Clear(); }
    }
}
