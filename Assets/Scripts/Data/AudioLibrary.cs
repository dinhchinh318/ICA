using UnityEngine;
using UnityEngine.Audio;
namespace LumaReef.Data
{
    public enum SoundCue { Fire, Hit, Death, Coin, Shower, Button, Warning, BossDeath, Freeze, Explosion, Lightning }
    [CreateAssetMenu(menuName="Luma Reef/Audio library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public AudioClip[] cues;
        public AudioClip reefMusic,bossMusic;
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup,sfxGroup;
    }
}
