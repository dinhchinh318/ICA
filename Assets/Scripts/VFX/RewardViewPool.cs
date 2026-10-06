using UnityEngine;
using LumaReef.Core;
namespace LumaReef.VFX
{
    // Coin bay về góc HUD (vị trí balance)
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

    // Cá chết → thu về góc dưới trái (hiệu ứng "lưới kéo cá về")
    public sealed class CollectEffect : IPooled
    {
        public bool Active { get; private set; }
        readonly SpriteRenderer renderer;
        readonly Transform transform;
        Vector2 start, target;
        float age;
        public CollectEffect(Transform root)
        {
            var go=new GameObject("Pooled collect"); transform=go.transform; transform.SetParent(root);
            renderer=go.AddComponent<SpriteRenderer>(); renderer.sortingOrder=50;
            transform.localScale=Vector3.one*.6f; go.SetActive(false);
        }
        public void Play(Vector2 at, Sprite sprite, Vector2 dest)
        {
            start=at; target=dest; age=0; Active=true;
            renderer.sprite=sprite; renderer.color=Color.white;
            transform.position=at; transform.localScale=Vector3.one*.6f;
            transform.gameObject.SetActive(true);
        }
        public void Tick(float dt)
        {
            age+=dt;
            float t=Mathf.Clamp01(age/.55f);
            float ease=1f-(1f-t)*(1f-t)*(1f-t); // ease out cubic
            transform.position=Vector2.Lerp(start,target,ease);
            float scale=Mathf.Lerp(.6f,.1f,ease);
            transform.localScale=Vector3.one*scale;
            renderer.color=new Color(1,1,1,Mathf.Lerp(1f,.0f,ease*ease));
            if(t>=1) Release();
        }
        public void Release() { Active=false; transform.gameObject.SetActive(false); }
    }

    // Text thưởng nổi lên
    public sealed class RewardText : IPooled
    {
        public bool Active { get; private set; }
        readonly TextMesh text;
        readonly Transform transform;
        Vector2 origin;
        float age;
        bool jackpot;
        public RewardText(Transform root,Font font)
        {
            var go=new GameObject("Pooled reward text"); transform=go.transform; transform.SetParent(root);
            text=go.AddComponent<TextMesh>(); text.font=font; text.fontSize=64; text.characterSize=.075f; text.anchor=TextAnchor.MiddleCenter; text.fontStyle=FontStyle.Bold;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=font.material; renderer.sortingOrder=55;
            go.SetActive(false);
        }
        public void Play(Vector2 position,long amount,bool big)
        {
            origin=position; age=0; Active=true; jackpot=big;
            text.text=(big?"🎊 ":"+")+amount.ToString("N0")+(big?" 🎊":"");
            text.characterSize=big?.18f:.085f;
            text.color=big?new Color(1,.92f,.1f):new Color(1,.88f,.32f);
            transform.position=position; transform.gameObject.SetActive(true);
        }
        public void Tick(float dt)
        {
            age+=dt;
            float rise=jackpot?1.5f:.8f;
            transform.position=origin+Vector2.up*(age*rise);
            float pop=1+Mathf.Sin(Mathf.Clamp01(age/.25f)*Mathf.PI)*(jackpot?.65f:.35f);
            transform.localScale=Vector3.one*pop;
            float alpha=Mathf.Clamp01((1.4f-age)*2);
            text.color=jackpot?new Color(1,.92f,.1f,alpha):new Color(1,.88f,.32f,alpha);
            if(age>1.4f)Release();
        }
        public void Release() { Active=false; transform.gameObject.SetActive(false); }
    }

    public sealed class RewardViewPool
    {
        readonly FixedPool<CoinView> coins;
        readonly FixedPool<RewardText> texts;
        readonly FixedPool<CollectEffect> collects;
        public int ActiveCount => coins.Count+texts.Count+collects.Count;
        public RewardViewPool(Transform root,Sprite coin,Font font,int coinCount,int textCount)
        {
            coins=new FixedPool<CoinView>(coinCount,i=>new CoinView(root,coin));
            texts=new FixedPool<RewardText>(textCount,i=>new RewardText(root,font));
            collects=new FixedPool<CollectEffect>(32,i=>new CollectEffect(root));
        }
        public void Play(Vector2 at,long reward,bool big)
        {
            texts.Rent()?.Play(at,reward,big);
            int coinCount=big?40:8;
            for(int i=0;i<coinCount;i++) coins.Rent()?.Play(at,new Vector2(1.6f,8.75f),i);
        }
        // Hiệu ứng thu cá về góc dưới trái HUD
        public void Collect(Vector2 at, Sprite sprite)
        {
            // Target: vị trí "balance" coin HUD (góc dưới trái màn hình world coords ≈ (-4.5f, -8.8f))
            collects.Rent()?.Play(at, sprite, new Vector2(-4.5f,-8.8f));
        }
        public void Tick(float dt)
        {
            for(int i=0;i<coins.Items.Length;i++)if(coins.Items[i].Active)coins.Items[i].Tick(dt);
            for(int i=0;i<texts.Items.Length;i++)if(texts.Items[i].Active)texts.Items[i].Tick(dt);
            for(int i=0;i<collects.Items.Length;i++)if(collects.Items[i].Active)collects.Items[i].Tick(dt);
        }
        public void Clear() { coins.Clear(); texts.Clear(); collects.Clear(); }
    }
}
