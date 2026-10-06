using UnityEngine;
namespace LumaReef.Data
{
    public enum FishCategory { Small, Medium, Large, Special, Boss }
    public enum SpecialEffect { None, Lantern, Bomb, Treasure, Lightning, Golden }
    [CreateAssetMenu(menuName="Luma Reef/Fish")]
    public sealed class FishData : ScriptableObject
    {
        public string id, displayName;
        public FishCategory category;
        public Sprite sprite;
        public AnimationClip animation;
        public float speed, multiplier, killChance, hitboxScale=.85f, size, spawnWeight=1, bossHP;
        public int coinReward;
        public SpecialEffect special;
        public Color tint=Color.white;
    }
}
