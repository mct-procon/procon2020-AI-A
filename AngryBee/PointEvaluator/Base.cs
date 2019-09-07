using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    public abstract class Base
    {
        public abstract int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy);
    }
}
