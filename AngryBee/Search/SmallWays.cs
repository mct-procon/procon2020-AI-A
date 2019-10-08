using MCTProcon30Protocol;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AngryBee.Search
{
    public class SmallObjectPool<T> where T : class
    {
        ConcurrentBag<T> bag = new ConcurrentBag<T>();

        public void Return(T obj)
        {
            bag.Add(obj);
        }
        public bool Get(out T obj)
        {
            return bag.TryTake(out obj);
        }
    }

    public struct SmallWay : IComparable
    {
        public int Point { get; set; }
        public Unsafe8Array<Way> AgentsWay { get; set; }

        public SmallWay(Unsafe8Array<Way> a)
        {
            AgentsWay = a;
            Point = 0;
        }

        public int CompareTo(object obj) => ((SmallWay)obj).Point - this.Point;
    }

    public unsafe class SmallWays
    {
        private SmallWay[] data { get; set; }

        private int Back = 0;

        public SmallWays(int AgentsCount)
        {
            data = ArrayPool<SmallWay>.Shared.Rent((1 << (AgentsCount * 3)));
        }

        public void Add(in SmallWay w)
        {
            data[Back] = w;
            Back++;
        }

        public void Erase()
        {
            Back = 0;
        }

        public int Count => Back;

        public ref SmallWay this[int index]
        {
            get => ref data[index];
        }

        public void Sort()
        {
            if (Back <= 1) return;
            Array.Sort(data, 0, Back);
        }
    }
}
