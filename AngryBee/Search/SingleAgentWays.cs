using MCTProcon31Protocol;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AngryBee.Search
{
    public class SingleAgentWays
    {
        // This is fastest.
        public static ReadOnlySpan<VelocityPoint> WayEnumerator => new VelocityPoint[] { new VelocityPoint(0, -1), new VelocityPoint(1, -1), new VelocityPoint(1, 0), new VelocityPoint(1, 1), new VelocityPoint(0, 1), new VelocityPoint(-1, 1), new VelocityPoint(-1, 0), new VelocityPoint(-1, -1) };
        public Point[] Data { get; private set; }
        public int Count { get; private set; }

        public SingleAgentWays(in SearchState searchState, int sgentsCount, int agentIndex, sbyte[,] ScoreBoard)
        {
            uint W = (uint)ScoreBoard.GetLength(0);
            uint H = (uint)ScoreBoard.GetLength(1);
            Data = ArrayPool<Point>.Shared.Rent(WayEnumerator.Length);
            int actualItr = 0;
            //for (int itr = 0; itr < WayEnumerator.Length; ++itr)
            int itr = 0;
        loop_start:
            if (itr < WayEnumerator.Length)
            {
                Point next = searchState.Me[agentIndex] + WayEnumerator[itr];
                // Is Agent is out of bounds?
                if (next.X >= W || next.Y >= H) goto loop_end; // continue;
                // Is hit each side agents?
                for (int enemy = 0; enemy < sgentsCount; ++enemy)
                    if (searchState.Enemy[enemy] == next) goto loop_end; // continue; on outer for loop.
                Data[actualItr] = next;
                actualItr++;
            loop_end:
                ++itr;
                goto loop_start;
            }
            Count = actualItr;
        }

        public void End()
        {
            if (Data != null)
                ArrayPool<Point>.Shared.Return(Data);
            Data = null;
        }
    }
}
