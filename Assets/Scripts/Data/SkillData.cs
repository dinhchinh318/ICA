using UnityEngine;
namespace LumaReef.Data
{
    public enum SkillKind { Freeze, RapidFire, LockTarget, FirePower, Lightning, Bomb }
    [CreateAssetMenu(menuName="Luma Reef/Skill")]
    public sealed class SkillData : ScriptableObject
    {
        public string displayName;
        public SkillKind kind;
        public float cooldown, duration, strength, radius;
        public Sprite icon;
        public AudioClip audio;
        public GameObject vfx;
    }
}
