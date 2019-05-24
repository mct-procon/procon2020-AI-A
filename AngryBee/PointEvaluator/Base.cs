using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    public abstract class Base
    {
        public abstract int Calculate(sbyte[,] ScoreBoard, in ColoredBoardSmallBigger Painted, int Turn, Player Me, Player Enemy);
    }
}
