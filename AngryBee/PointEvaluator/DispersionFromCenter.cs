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
            PointFloat center = new PointFloat((width - 1) / 2f, (height - 1) / 2f);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (state.MeBoard[x, y])
                    {
                        float tmp = Math.Abs((center.x - x)) + Math.Abs((center.y - y));
                        rec += tmp * tmp;
                    }
                }
            rec /= checkedCount;
            return (int)(state.SurroundVelocity * FasterSurroundRate) + (int)(rec * DispersionRate) + ((int)(surround * SurroundRate) + state.PointVelocity) * width * height / 4;
        }
    }
}
