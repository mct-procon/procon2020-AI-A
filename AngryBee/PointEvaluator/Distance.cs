using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    class Distance : Base
    {
        readonly int[] DistanceX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        readonly int[] DistanceY = { 1, 0, -1, 0, -1, 1, 1, -1 };
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy)
        {
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(Painted.Width, Painted.Height);   //!checker == 領域
            int result = 0;
            byte width = (byte)Painted.Width;
            byte height = (byte)Painted.Height;
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
        public unsafe void BadSpaceFill(ref ColoredBoardNormalSmaller Checker, byte width, byte height)
        {
            unchecked
            {
                Point* myStack = stackalloc Point[20 * 20];

                Point point;
                byte x, y, searchTo = 0, searchToX, searchToY, myStackSize = 0;

                searchTo = (byte)(height - 1);
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

                searchTo = (byte)(width - 1);
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

                    for (int i = 0; i < 8; i++)
                    {
                        searchToX = (byte)(x + DistanceX[i]);
                        searchToY = (byte)(y + DistanceY[i]);
                        if (searchToX < width && searchToY < height && !Checker[searchToX, searchToY])
                        {
                            myStack[myStackSize++] = new Point(searchToX, searchToY);
                            Checker[searchToX, searchToY] = true;
                        }
                    }
                }
            }
        }
    }
}
