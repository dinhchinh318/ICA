using UnityEngine;
namespace LumaReef.Data
{
    public enum QuestMetric { Fish, Small, Large, Boss, Shots, Earnings }
    [CreateAssetMenu(menuName="Luma Reef/Quest")]
    public sealed class QuestData : ScriptableObject
    {
        public string id,description;
        public QuestMetric metric;
        public int target,reward;
    }
}
