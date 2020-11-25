using MCTProcon31Protocol;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AngryBee.Search
{
    public struct SmallWay : IComparable
    {
        public int Point { get; set; }
        public Unsafe16Array<Way> AgentsWay { get; set; }

        public SmallWay(Unsafe16Array<Way> a)
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
