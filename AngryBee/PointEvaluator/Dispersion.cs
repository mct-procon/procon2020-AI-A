using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    //塗られているマスの重心を用いて、分散を計算する。
    class Dispersion : Base
    {
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
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy)
        {
            uint width = (uint)ScoreBoard.GetLength(0);
            uint height = (uint)ScoreBoard.GetLength(1);
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(width, height);
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

            ScoreEvaluation.BadSpaceFill(ref checker, (byte)width, (byte)height);

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
    }
}
