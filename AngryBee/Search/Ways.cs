using MCTProcon29Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AngryBee.Search
{
    public class ObjectPool<T> where T : class
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

        public int CompareTo(object obj) => ((Way)obj).Point - this.Point;
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
