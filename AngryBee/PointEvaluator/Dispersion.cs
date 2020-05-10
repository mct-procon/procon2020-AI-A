using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    //塗られているマスの重心を用いて、分散を計算する。
    class Dispersion : Base
    {
        readonly int[] DistanceX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        readonly int[] DistanceY = { 1, 0, -1, 0, -1, 1, 1, -1 };
        const float DispersionRate = 0.8f;
        const float SurroundRate = 0.8f;
        private struct PointFloat
        {
            public float x;
            public float y;
            public PointFloat(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy)
        {
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(Painted.Width, Painted.Height);
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
                        result = result + (int)(Math.Abs(ScoreBoard[x, y]) * SurroundRate);

            float rec = 0;
            int checkedCount = 0;
            Point sum = new Point();
            PointFloat average;
            for (byte x = 0; x < width; ++x)
                for (byte y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        checkedCount++;
                        sum += (x, y);
                    }
                }
            average = new PointFloat((float)sum.X / checkedCount, (float)sum.Y / checkedCount);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        float tmp = Math.Abs((average.x - x)) + Math.Abs((average.y - y));
                        rec += tmp * tmp;
                    }
                }
            rec = rec / checkedCount * DispersionRate;
            return (int)rec + result + checkedCount;
        }

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
