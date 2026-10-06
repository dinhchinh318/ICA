using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
using LumaReef.Path;
namespace LumaReef.Fish
{
    public sealed class FishActor : IPooled
    {
        public bool Active { get; private set; }
        public readonly Transform Transform;
        public readonly SpriteRenderer Renderer;
        public readonly CircleCollider2D Collider;
        public FishData Data;
        public float HP;
        public Vector2 Position => Transform.position;
        public float Radius => Data.size*.38f*Data.hitboxScale;
        BezierPath path;
        Vector2 offset;
        float progress, flash, age;
        public FishActor(Transform root)
        {
            var go=new GameObject("Pooled reef resident"); Transform=go.transform; Transform.SetParent(root);
            Renderer=go.AddComponent<SpriteRenderer>(); Renderer.sortingOrder=5;
            Collider=go.AddComponent<CircleCollider2D>(); Collider.isTrigger=true;
            go.SetActive(false);
        }
        public void Spawn(FishData data, BezierPath route, Vector2 formationOffset, float start=0)
        {
            Data=data; path=route; offset=formationOffset; progress=start; age=flash=0; HP=data.bossHP;
            Renderer.sprite=data.sprite; Renderer.color=Color.white; Renderer.sortingOrder=5+(int)data.category;
            Collider.radius=.38f*data.hitboxScale; Transform.localScale=Vector3.one*data.size;
            Transform.position=path.Evaluate(progress)+offset; Active=true; Transform.gameObject.SetActive(true);
        }
        public void Tick(float dt, bool frozen)
        {
            age+=dt; flash=Mathf.Max(0,flash-dt);
            if(!frozen)progress+=dt*Data.speed/Mathf.Max(1,path.length);
            if(progress>=1) { Release(); return; }
            Transform.position=path.Evaluate(progress)+offset;
            Vector2 tangent=path.Tangent(progress);
            Transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg);
            Renderer.flipY=tangent.x<0;
            Transform.localScale=new Vector3(Data.size,Data.size*(1+Mathf.Sin(age*8)*.035f),1);
            Renderer.color=flash>0?new Color(2,2,2):frozen?new Color(.55f,.85f,1):Color.white;
        }
        public void Hit() { flash=.10f; }
        public void Die() { Release(); }
        public void Release() { Active=false; Transform.gameObject.SetActive(false); }
    }
}
