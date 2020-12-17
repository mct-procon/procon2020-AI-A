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
        const float SurroundRate = 1.2f;
        const float FasterSurroundRate = 3.2f;
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
        public override int Calculate(sbyte[,] ScoreBoard, Search.SearchState state, int Turn)
        {
            byte width = (byte)ScoreBoard.GetLength(0), height = (byte)ScoreBoard.GetLength(1);
            int surround = 0;
            for (uint x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (uint y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (state.MeSurroundBoard[x, y])
                        surround += Math.Abs(ScoreBoard[x, y]);
            float rec = 0;
            int checkedCount = 0;
            Point sum = new Point();
            PointFloat average;
            for (byte x = 0; x < width; ++x)
                for (byte y = 0; y < height; ++y)
                {
                    if (state.MeBoard[x, y])
                    {
                        checkedCount++;
                        sum += (x, y);
                    }
                }
            average = new PointFloat((float)sum.X / checkedCount, (float)sum.Y / checkedCount);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (state.MeBoard[x, y])
                    {
                        float tmp = Math.Abs((average.x - x)) + Math.Abs((average.y - y));
                        rec += tmp * tmp;
                    }
                }
            rec /= checkedCount;
            return (int)(state.SurroundVelocity * FasterSurroundRate) + (int)(rec * DispersionRate) + ((int)(surround * SurroundRate) + state.PointVelocity) * width * height / 4;
        }
    }
}
