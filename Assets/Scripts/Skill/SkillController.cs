using System;
using LumaReef.Data;
namespace LumaReef.Skill
{
    public sealed class SkillController
    {
        public readonly SkillData[] Data;
        readonly float[] cooldown,remaining;
        public event Action<SkillData> Activated;
        public SkillController(SkillData[] data) { Data=data; cooldown=new float[data.Length]; remaining=new float[data.Length]; }
        public float Cooldown(int index) => cooldown[index];
        public bool Active(SkillKind kind) => remaining[(int)kind]>0;
        public float Strength(SkillKind kind) => Active(kind)?Data[(int)kind].strength:1;
        public bool Use(int index)
        {
            if(index<0||index>=Data.Length||cooldown[index]>0)return false;
            cooldown[index]=Data[index].cooldown; remaining[index]=Data[index].duration; Activated?.Invoke(Data[index]); return true;
        }
        public void Tick(float dt) { for(int i=0;i<Data.Length;i++) { cooldown[i]=Math.Max(0,cooldown[i]-dt); remaining[i]=Math.Max(0,remaining[i]-dt); } }
        public void Reset() { Array.Clear(cooldown,0,cooldown.Length); Array.Clear(remaining,0,remaining.Length); }
    }
}
