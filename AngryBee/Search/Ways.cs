using MCTProcon31Protocol;
using System;
using System.Buffers;
using System.Collections;
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
        public sbyte Point { get; set; }
        public VelocityPoint Direction { get; set; }
        public Point Locate { get; set; }
        
        public Way(in VelocityPoint direction, in Point locate)
        {
            Point = 0;
            Direction = direction;
            Locate = locate;
        }

        public int CompareTo(object obj) => ((Way)obj).Point - this.Point;
    }

    public unsafe class Ways
    {
        // This is fastest.
        public static ReadOnlySpan<VelocityPoint> WayEnumerator => new VelocityPoint[] { new VelocityPoint(0, -1), new VelocityPoint(1, -1), new VelocityPoint(1, 0), new VelocityPoint(1, 1), new VelocityPoint(0, 1), new VelocityPoint(-1, 1), new VelocityPoint(-1, 0), new VelocityPoint(-1, -1) };
        public Way[][] Data { get; private set; }
        public int[] ActualCount { get; private set; }

        public int Count { get; private set; }

        public Ways(in SearchState searchState, int MyAgentsCount, int EnemyAgentsCount, sbyte[,] ScoreBoard)
        {
            uint W = (uint)ScoreBoard.GetLength(0);
            uint H = (uint)ScoreBoard.GetLength(1);
            Data = ArrayPool<Way[]>.Shared.Rent(MyAgentsCount);
            ActualCount = ArrayPool<int>.Shared.Rent(MyAgentsCount);
            for (int agent = 0; agent < MyAgentsCount; ++agent)
            {
                Data[agent] = ArrayPool<Way>.Shared.Rent(WayEnumerator.Length);
                int actualItr = 0;
                //for (int itr = 0; itr < WayEnumerator.Length; ++itr)
                int itr = 0;
                loop_start:
                if(itr < WayEnumerator.Length)
                {
                    Point next = searchState.Me[agent] + WayEnumerator[itr];
                    if (next.X >= W || next.Y >= H) goto loop_end;
                    for (int enemy = 0; enemy < EnemyAgentsCount; ++enemy)
                        if (searchState.Enemy[enemy] == next) goto loop_end;
                    Data[agent][actualItr] = new Way(WayEnumerator[itr], next) { Point = ScoreBoard[next.X, next.Y]};
                    actualItr++;
                    loop_end:
                    ++itr;
                    goto loop_start;
                }
                Array.Sort(Data[agent], 0, actualItr);
                ActualCount[agent] = actualItr;
            }
        }

        public void End()
        {
            if (Data != null)
                ArrayPool<Way[]>.Shared.Return(Data);
        }

        //public ref Way this[int index] {
        //    get => ref data[index];
        //}

        public WayEnumerator GetEnumerator(int myAgentsCount) => new WayEnumerator(this, myAgentsCount);
    }

    public class WayEnumerator : IEnumerator<Unsafe16Array<Way>>
    {
        public Ways Parent { get; set; }
        public int MyAgentsCount { get; set; }
        private ulong Iterator = 0;
        private bool isHead = true;
        public unsafe Unsafe16Array<Way> Current {
            get {
                fixed (ulong* ll = &Iterator)
                {
                    byte* itrs = (byte*)ll;
                    Unsafe16Array<Way> ret = new Unsafe16Array<Way>();
                    for (int i = 0; i < MyAgentsCount; ++i)
                        ret[i] = Parent.Data[i][itrs[i]];
                    return ret;
                }
            }
        }

        object IEnumerator.Current => Current;

        public WayEnumerator(Ways parent, int myAgentsCount)
        {
            Parent = parent;
            MyAgentsCount = myAgentsCount;
        }

        public void Dispose()
        {
        }

        public unsafe bool IncreaseIterator(byte * itrs)
        {
            itrs[0]++;
            for (int i = 0; i < MyAgentsCount; ++i)
            {
                if (itrs[i] < Parent.ActualCount[i])
                    continue;
                if (i == MyAgentsCount - 1)
                    return false;
                itrs[i + 1]++;
                itrs[i] = 0;
            }
            return true;
        }

        public unsafe bool MoveNext()
        {
            fixed (ulong* ll = &Iterator) {
                byte* itrs = (byte*)ll;
            err:
                if (!isHead)
                    if (!IncreaseIterator(itrs)) return false;
                isHead = false;
                // Check weather each agents hits an another.
                for (int a = 0; a < MyAgentsCount; ++a)
                    for (int b = a + 1; b < MyAgentsCount; ++b)
                        if (Parent.Data[a][itrs[a]].Locate == Parent.Data[b][itrs[b]].Locate) goto err;
                return true;
            }
        }

        public void Reset()
        {
            isHead = true;
            Iterator = 0;
        }

        public WayEnumerator GetEnumerator() => this;
    }
}
