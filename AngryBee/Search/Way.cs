using System;
using MCTProcon31Protocol;
namespace AngryBee.Search
{
    public struct Way : IComparable
    {
        public sbyte Point { get; set; }
        public Point Locate { get; set; }

        public Way(in Point locate, sbyte point)
        {
            Point = point;
            Locate = locate;
        }

        public int CompareTo(object obj) => ((Way)obj).Point - this.Point;
    }
}
