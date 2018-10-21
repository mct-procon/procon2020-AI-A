using MCTProcon29Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngryBee.Search
{
    public struct Way : IComparable
    {
        public int Point { get; set; }
        public VelocityPoint Agent1Way { get; set; }
        public VelocityPoint Agent2Way { get; set; }

        public Way(VelocityPoint a1, VelocityPoint a2)
        {
            Agent1Way = a1;
            Agent2Way = a2;
            Point = 0;
        }

        public int CompareTo(object obj) => this.Point - ((Way)obj).Point;
    }

    public unsafe class Ways
    {
        private Way[] data = new Way[8 * 8];

        private int Back = 0;

        public void Add(in Way w)
        {
            data[Back] = w;
            Back++;
        }
        
        public void Erase()
        {
            Back = 0;
        }

        public int Count => Back;

        public ref Way this[int index] {
            get => ref data[index];
        }

        public void Sort()
        {
            if (Back <= 1) return;
            Array.Sort(data, 0, Back);
        }
    }
}
