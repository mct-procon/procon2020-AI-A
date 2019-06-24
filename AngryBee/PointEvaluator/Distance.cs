using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    class Distance : Base
    {
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardSmallBigger Painted, int Turn, Player Me, Player Enemy)
        {
            ColoredBoardSmallBigger checker = new ColoredBoardSmallBigger(Painted.Width, Painted.Height);   //!checker == 領域
            int result = 0;
            uint width = Painted.Width;
            uint height = Painted.Height;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        result += ScoreBoard[x, y];
                        checker[x, y] = true;
                    }
                }

            BadSpaceFill(ref checker, width, height);

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

        //囲いを見つける
        public unsafe void BadSpaceFill(ref ColoredBoardSmallBigger Checker, uint width, uint height)
        {
            unchecked
            {
                Point* myStack = stackalloc Point[12 * 12];

                Point point;
                uint x, y, searchTo = 0, myStackSize = 0;

                searchTo = height - 1;
                for (x = 0; x < width; x++)
                {
                    if (!Checker[x, 0])
                    {
                        myStack[myStackSize++] = new Point(x, 0);
                        Checker[x, 0] = true;
                    }
                    if (!Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }

                searchTo = width - 1;
                for (y = 0; y < height; y++)
                {
                    if (!Checker[0, y])
                    {
                        myStack[myStackSize++] = new Point(0, y);
                        Checker[0, y] = true;
                    }
                    if (!Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }
                }

                while (myStackSize > 0)
                {
                    point = myStack[--myStackSize];
                    x = point.X;
                    y = point.Y;

                    //左方向
                    searchTo = x - 1;
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //下方向
                    searchTo = y + 1;
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }

                    //右方向
                    searchTo = x + 1;
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //上方向
                    searchTo = y - 1;
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }
            }
        }
    }
}
