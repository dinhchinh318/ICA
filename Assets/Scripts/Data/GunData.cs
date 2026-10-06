using UnityEngine;
namespace LumaReef.Data
{
    [CreateAssetMenu(menuName="Luma Reef/Gun")]
    public sealed class GunData : ScriptableObject
    {
        public string id, displayName;
        public int level, bet, barrels = 1, unlockCost;
        public float fireRate, netRadius, power = 1;
        public Color color = Color.cyan;
        public BulletData bullet;
        public Sprite body, barrel, baseSprite;
        public GameObject muzzleFlash, impactEffect, netEffect;
        public AudioClip fireSound;
    }
}
