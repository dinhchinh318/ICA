using System;
using UnityEngine;
namespace LumaReef.Core
{
    public interface IPooled { bool Active { get; } void Release(); }
    // Pools are fully warmed during loading. Exhaustion skips a cosmetic/shot, never allocates.
    public sealed class FixedPool<T> where T : class, IPooled
    {
        public readonly T[] Items;
        public int Count { get { int n=0; for(int i=0;i<Items.Length;i++) if(Items[i].Active)n++; return n; } }
        public FixedPool(int capacity, Func<int,T> factory)
        { Items=new T[capacity]; for(int i=0;i<capacity;i++)Items[i]=factory(i); }
        public T Rent() { for(int i=0;i<Items.Length;i++)if(!Items[i].Active)return Items[i]; return null; }
        public void Clear() { for(int i=0;i<Items.Length;i++)Items[i].Release(); }
    }
}
