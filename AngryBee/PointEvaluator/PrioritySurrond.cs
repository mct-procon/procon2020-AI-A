using System;
using System.Collections.Generic;
using System.Text;
using AngryBee.Boards;
using System.Runtime.Intrinsics.X86;
using MCTProcon31Protocol;

namespace AngryBee.PointEvaluator
{

    //通常の計算に加え、自分が囲んでいる囲みの数*50点を加算する。
    public class PrioritySurrond : Base
    {
        readonly int[] DistanceX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        readonly int[] DistanceY = { 1, 0, -1, 0, -1, 1, 1, -1 };
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy)
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

            result += CountSurrounded(checker, width, height) * 50;

            ScoreEvaluation.BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        result += Math.Abs(ScoreBoard[x, y]);

            return result;
        }


        int CountSurrounded(ColoredBoardNormalSmaller Checker, uint width, uint height)
        {
            int count = 0;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    if (!Checker[x, y])
                    {
                        if (FillChecker(ref Checker, x, y, width, height)) count++;
                    }
                }
            }
            Console.WriteLine(count.ToString());
            return count;
        }

        bool FillChecker(ref ColoredBoardNormalSmaller Checker, uint x, uint y, in uint Width, in uint Height)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            if (Checker[x, y]) return true;
            Checker[x, y] = true;
            for (int i = 0; i < 8; i++)
            {
                if (!FillChecker(ref Checker, (uint)(x + DistanceX[i]), (uint)(y + DistanceY[i]), Width, Height)) return false;
            }
            return true;
        }
    }
}