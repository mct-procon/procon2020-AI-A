using System;
using System.Collections.Generic;
using System.Text;
using AngryBee.Boards;
using System.Runtime.Intrinsics.X86;
using MCTProcon31Protocol;

namespace AngryBee.PointEvaluator
{
    public class Normal : Base
    {
        public override int Calculate(sbyte[,] ScoreBoard, Search.SearchState state, int Turn)
        {
            int retVal = 0;
            for (uint x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (uint y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (state.MeSurroundBoard[x, y])
                        retVal += Math.Abs(ScoreBoard[x, y]);
            return retVal + state.PointVelocity;
        }
    }
}