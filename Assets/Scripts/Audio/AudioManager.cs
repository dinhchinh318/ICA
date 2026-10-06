using UnityEngine;
using LumaReef.Data;
namespace LumaReef.Audio
{
    public sealed class AudioManager
    {
        readonly AudioSource music;
        readonly AudioSource[] voices=new AudioSource[12];
        readonly AudioLibrary library;
        int voice;
        float sfxVolume;
        bool boss;
        public AudioManager(Transform root,AudioLibrary library,float musicVolume,float sfxVolume)
        {
            this.library=library;
            var go=new GameObject("Music bus"); go.transform.SetParent(root); music=go.AddComponent<AudioSource>(); music.loop=true; music.playOnAwake=false; music.outputAudioMixerGroup=library.musicGroup;
            for(int i=0;i<voices.Length;i++) { var v=new GameObject("SFX voice "+i); v.transform.SetParent(root); voices[i]=v.AddComponent<AudioSource>(); voices[i].playOnAwake=false; voices[i].outputAudioMixerGroup=library.sfxGroup; }
            Volume(musicVolume,sfxVolume); music.clip=library.reefMusic; music.Play();
        }
        public void Volume(float musicValue,float sfxValue)
        {
            sfxVolume=Mathf.Clamp01(sfxValue); music.volume=Mathf.Clamp01(musicValue);
            // Sources are routed to separate mixer buses; source gain is independent per category.
        }
        public void Boss(bool active) { if(boss==active)return; boss=active; music.clip=active?library.bossMusic:library.reefMusic; music.Play(); }
        public void Play(SoundCue cue,float pitch=1)
        {
            int index=(int)cue; if(index>=library.cues.Length||library.cues[index]==null)return;
            var source=voices[voice++%voices.Length]; source.pitch=pitch; source.volume=sfxVolume*.5f; source.clip=library.cues[index]; source.Play();
        }
    }
}
