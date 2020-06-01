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
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, in ColoredBoardNormalSmaller enemyPainted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy, ColoredBoardNormalSmaller mySurroundBoard, ColoredBoardNormalSmaller enemySurroundBoard) => ScoreEvaluation.EvaluateGameScore(Painted, enemyPainted, ScoreBoard);
    }
}