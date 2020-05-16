using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    //フィールドの中心を重心とし、分散を計算する。
    class DispersionFromCenter : Base
    {
        const float DispersionRate = 0.5f;
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
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy, ColoredBoardNormalSmaller mySurroundBoard, ColoredBoardNormalSmaller enemySurroundBoard)
        {
            byte width = (byte)ScoreBoard.GetLength(0);
            byte height = (byte)ScoreBoard.GetLength(1);
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

            ScoreEvaluation.BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        result += Math.Abs(ScoreBoard[x, y]);

            float rec = 0;
            int checkedCount = 0;
            Point sum = new Point();
            PointFloat center = new PointFloat((width - 1) / 2f, (height - 1) / 2f);
            for (byte x = 0; x < width; ++x)
                for (byte y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        checkedCount++;
                        sum += (x, y);
                    }
                }

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        float tmp = Math.Abs((center.x - x)) + Math.Abs((center.y - y));
                        rec += tmp * tmp;
                    }
                }
            rec /= checkedCount * DispersionRate;
            return (int)rec + result;
        }
    }
}
