using UnityEngine;
using LumaReef.Path;
namespace LumaReef.Data
{
    [CreateAssetMenu(menuName="Luma Reef/Room")]
    public sealed class RoomData : ScriptableObject
    {
        public string displayName;
        public int maxFish=35, waveCount=25, bulletCapacity=96, effectCapacity=128, coinCapacity=96, textCapacity=32;
        public float spawnInterval=1.4f, bossInterval=65, waveInterval=35;
        public float[] categoryWeights={50,30,12,8};
        public int[] betSteps={1,2,5};
        public FishData[] fish;
        public GunData[] guns;
        public BezierPath[] paths;
        public float specialRadius=2.5f, specialPower=2, chainRadius=4;
        public int chainTargets=5, initialCoins=5000, initialDiamonds=25;
    }
}
