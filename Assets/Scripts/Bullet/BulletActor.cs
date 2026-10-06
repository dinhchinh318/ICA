using UnityEngine;
using LumaReef.Core;
using LumaReef.Data;
namespace LumaReef.Bullet
{
    public sealed class BulletActor : IPooled
    {
        public bool Active { get; private set; }
        public readonly Transform Transform;
        public readonly SpriteRenderer Renderer;
        readonly TrailRenderer trail;
        public Vector2 Position, Previous, Direction;
        public GunData Gun;
        public int Bet;
        public float Power;
        float life;
        int bounces;
        public BulletActor(Transform root, Sprite sprite,Material trailMaterial=null)
        {
            var go=new GameObject("Pooled energy bolt"); Transform=go.transform; Transform.SetParent(root);
            Renderer=go.AddComponent<SpriteRenderer>(); Renderer.sprite=sprite; Renderer.sortingOrder=20;
            if(trailMaterial!=null){trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.14f;trail.minVertexDistance=.08f;trail.startWidth=.11f;trail.endWidth=0;trail.sortingOrder=19;trail.emitting=false;}
            go.SetActive(false);
        }
        public void Launch(Vector2 position, Vector2 direction, GunData gun, int bet, float power)
        {
            Active=true; Position=Previous=position; Direction=direction; Gun=gun; Bet=bet; Power=power;
            life=gun.bullet.lifetime; bounces=gun.bullet.bounces; Renderer.color=gun.color;
            Renderer.sprite=gun.bullet.sprite;Renderer.color=Color.white;Transform.localScale=new Vector3(gun.bullet.visualScale.x,gun.bullet.visualScale.y,1);
            Transform.position=position; Transform.gameObject.SetActive(true);
            if(trail!=null){trail.Clear();trail.startColor=gun.color;trail.endColor=new Color(gun.color.r,gun.color.g,gun.color.b,0);trail.time=gun.level==8?.23f:.14f;trail.emitting=true;}
        }
        public void Tick(float dt, Rect bounds)
        {
            Previous=Position; Position+=Direction*(Gun.bullet.speed*dt); life-=dt;
            bool x=Position.x<bounds.xMin||Position.x>bounds.xMax, y=Position.y<bounds.yMin||Position.y>bounds.yMax;
            if(x||y)
            {
                if(bounces--<=0) { Release(); return; }
                if(x)Direction.x=-Direction.x; if(y)Direction.y=-Direction.y;
                Position=new Vector2(Mathf.Clamp(Position.x,bounds.xMin,bounds.xMax),Mathf.Clamp(Position.y,bounds.yMin,bounds.yMax));
            }
            Transform.position=Position; Transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Direction.y,Direction.x)*Mathf.Rad2Deg-90);
            if(life<=0)Release();
        }
        public void Release() { Active=false;if(trail!=null){trail.emitting=false;trail.Clear();}Transform.gameObject.SetActive(false); }
    }
}
