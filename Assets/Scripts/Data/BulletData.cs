using UnityEngine;
namespace LumaReef.Data
{
    [CreateAssetMenu(menuName="Luma Reef/Bullet")]
    public sealed class BulletData : ScriptableObject
    {
        public float speed = 14, lifetime = 5;
        public int bounces = 1;
        public Sprite sprite;
        public GameObject prefab;
        public Vector2 visualScale=new Vector2(.2f,.4f);
    }
}
