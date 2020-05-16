using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    public abstract class Base
    {
        public abstract int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy, ColoredBoardNormalSmaller mySurroundBoard, ColoredBoardNormalSmaller enemySurroundBoard);
    }
}
