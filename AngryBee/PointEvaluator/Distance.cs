using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    class Distance : Base
    {
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy)
        {
            byte width = (byte)ScoreBoard.GetLength(0);
            byte height = (byte)ScoreBoard.GetLength(1);
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(width, height);   //!checker == 領域
            int result = 0;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        result += ScoreBoard[x, y];
                        checker[x, y] = true;
                    }
                }

            ScoreEvaluation.BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        result += Math.Abs(ScoreBoard[x, y]);

            //差分計算
            int DistanceScore = 0;
            //(取ったマスから敵エージェントの最短距離) * (取ったマスの点数)
            int DistanceScoreX1 = (int)Math.Min(Math.Abs(Me.Agent1.X - Enemy.Agent1.X), Math.Abs(Me.Agent1.X - Enemy.Agent2.X));
            int DistanceScoreY1 = (int)Math.Min(Math.Abs(Me.Agent1.Y - Enemy.Agent1.Y), Math.Abs(Me.Agent1.Y - Enemy.Agent2.Y));
            int DistanceScoreX2 = (int)Math.Min(Math.Abs(Me.Agent2.X - Enemy.Agent1.X), Math.Abs(Me.Agent2.X - Enemy.Agent2.X));
            int DistanceScoreY2 = (int)Math.Min(Math.Abs(Me.Agent2.Y - Enemy.Agent1.Y), Math.Abs(Me.Agent2.Y - Enemy.Agent2.Y));
            if(!Painted[Me.Agent1.X, Me.Agent1.Y])
            {
                if (checker[Me.Agent1.X, Me.Agent1.Y])
                    DistanceScore += Math.Min(DistanceScoreX1, DistanceScoreY1) * ScoreBoard[Me.Agent1.X, Me.Agent1.Y];
                else
                    DistanceScore += Math.Min(DistanceScoreX1, DistanceScoreY1) * ScoreBoard[Me.Agent1.X, Me.Agent1.Y] / 2;
            }
            if(!Painted[Me.Agent2.X, Me.Agent2.Y])
            {
                if (checker[Me.Agent2.X, Me.Agent2.Y])
                    DistanceScore += Math.Min(DistanceScoreX2, DistanceScoreY2) * ScoreBoard[Me.Agent2.X, Me.Agent2.Y];
                else
                    DistanceScore += Math.Min(DistanceScoreX2, DistanceScoreY2) * ScoreBoard[Me.Agent2.X, Me.Agent2.Y] / 2;
            }

            //(距離)×(盤面の点数)で、(敵エージェント) - (自エージェント)
            /*
            for (uint x = 0; x < width; ++x)
                for(uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y]) continue;

                    int ma1 = (int)Math.Max(Math.Abs(x - Me.Agent1.X), Math.Abs(y - Me.Agent1.Y));
                    int ma2 = (int)Math.Max(Math.Abs(x - Me.Agent2.X), Math.Abs(y - Me.Agent2.Y));
                    int en1 = (int)Math.Max(Math.Abs(x - Enemy.Agent1.X), Math.Abs(y - Enemy.Agent1.Y));
                    int en2 = (int)Math.Max(Math.Abs(x - Enemy.Agent2.X), Math.Abs(y - Enemy.Agent2.Y));

                    DistanceScore += ScoreBoard[x, y] * (en1 + en2) - (ma1 + ma2);
                }
            */

            return result + DistanceScore;
        }
    }
}
