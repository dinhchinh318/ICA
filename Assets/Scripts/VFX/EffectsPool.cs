using UnityEngine;
using LumaReef.Core;
namespace LumaReef.VFX
{
    public sealed class BurstEffect : IPooled
    {
        public bool Active { get; private set; }
        readonly ParticleSystem particles;
        readonly LineRenderer ring;
        readonly Transform transform;
        readonly SpriteRenderer artwork;
        readonly Sprite[] sprites;
        int artIndex;
        float age,duration,radius;
        Color color;
        bool beam;
        public BurstEffect(Transform root,Material material,Sprite[] sprites)
        {
            this.sprites=sprites;
            var go=new GameObject("Pooled impact / net / explosion"); transform=go.transform; transform.SetParent(root);
            particles=go.AddComponent<ParticleSystem>(); particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main; main.playOnAwake=false; main.loop=false; main.duration=.6f; main.startLifetime=.45f; main.startSpeed=3.0f; main.startSize=.18f; main.maxParticles=100; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission; emission.enabled=false;
            var shape=particles.shape; shape.shapeType=ParticleSystemShapeType.Circle; shape.radius=.1f;
            var pr=go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial=material; pr.sortingOrder=35;
            ring=go.AddComponent<LineRenderer>(); ring.sharedMaterial=material; ring.sortingOrder=34; ring.useWorldSpace=true; ring.widthMultiplier=.025f; ring.positionCount=33;
            var art=new GameObject("AI VFX artwork");art.transform.SetParent(transform,false);artwork=art.AddComponent<SpriteRenderer>();artwork.sortingOrder=36;
            go.SetActive(false);
        }
        public void Play(Vector2 at,float size,Color tint,bool lightning=false,Vector2 end=default,int spriteIndex=2,float rotation=0)
        {
            Active=true;age=0;artIndex=spriteIndex;duration=lightning?.22f:spriteIndex<2?.6f:.4f;radius=size;color=tint;beam=lightning;transform.position=at;transform.gameObject.SetActive(true);
            var main=particles.main; main.startColor=tint; main.startSpeed=Mathf.Max(1.0f,size*3f); particles.Emit(size>.2f?35:15);
            ring.positionCount=33;ring.enabled=lightning||(sprites==null||sprites.Length==0)&&size>.2f;
            artwork.enabled=!lightning&&sprites!=null&&spriteIndex<sprites.Length;
            if(artwork.enabled){artwork.sprite=sprites[spriteIndex];artwork.transform.localRotation=Quaternion.Euler(0,0,rotation);artwork.transform.localScale=Vector3.one*(radius*2);artwork.color=Color.white;}
            if(beam)for(int i=0;i<33;i++)ring.SetPosition(i,Vector2.Lerp(at,end,i/32f)+Vector2.up*(i==0||i==32?0:Random.Range(-.12f,.12f)));
            RenderRing();
        }
        void RenderRing()
        {
            float t=age/duration;
            if(artwork.enabled){artwork.color=new Color(1,1,1,1-t*t);artwork.transform.localScale=Vector3.one*radius*2*Mathf.Lerp(artIndex<2?.65f:.35f,1,t);}
            ring.startColor=ring.endColor=new Color(color.r,color.g,color.b,1-t);
            if(!beam)for(int i=0;i<33;i++) { float a=i*Mathf.PI/16; ring.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius*Mathf.Lerp(.3f,1,t)); }
        }
        public void Tick(float dt) { age+=dt; RenderRing(); if(age>=duration)Release(); }
        public void Release() { Active=false; particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); transform.gameObject.SetActive(false); }
    }
    public sealed class EffectsPool
    {
        readonly FixedPool<BurstEffect> pool;
        public int ActiveCount=>pool.Count;
        public EffectsPool(Transform root,Material material,int capacity,Sprite[] sprites=null) { pool=new FixedPool<BurstEffect>(capacity,i=>new BurstEffect(root,material,sprites)); }
        public void Burst(Vector2 at,float radius,Color color) { pool.Rent()?.Play(at,radius,color); }
        public void Lightning(Vector2 from,Vector2 to) { pool.Rent()?.Play(from,1,Color.cyan,true,to); }
        public void Net(Vector2 at,float radius,Color color,int level){pool.Rent()?.Play(at,radius,color,false,default,level>=4?1:0);}
        public void Muzzle(Vector2 at,Vector2 direction,Color color){pool.Rent()?.Play(at,.35f,color,false,default,7,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);}
        public void Death(Vector2 at,float radius,bool boss){pool.Rent()?.Play(at,radius,Color.yellow,false,default,boss?4:6);}
        public void Freeze(){pool.Rent()?.Play(Vector2.zero,5,Color.cyan,false,default,5);}
        public void Tick(float dt) { for(int i=0;i<pool.Items.Length;i++)if(pool.Items[i].Active)pool.Items[i].Tick(dt); }
        public void Clear()=>pool.Clear();
    }
}
