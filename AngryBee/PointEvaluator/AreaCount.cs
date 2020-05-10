using System;
using System.Collections.Generic;
using System.Text;
using AngryBee.Boards;
using System.Runtime.Intrinsics.X86;
using MCTProcon31Protocol;

namespace AngryBee.PointEvaluator
{
	public class AreaCount : Base
	{
		readonly int[] DistanceX = { 0, 1, 0, -1, 1, 1, -1, -1 };
		readonly int[] DistanceY = { 1, 0, -1, 0, -1, 1, 1, -1 };
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
						result += Math.Abs(ScoreBoard[x, y]);

			//囲った領域の個数を数える(BadSpaceFill(外側の塗りつぶし)が終わったあとの2次元配列checkerを渡す）
			int count = CalcAreaCount(checker, (byte)width, (byte)height);

			return result + count * 4;
		}

		private int CalcAreaCount(ColoredBoardNormalSmaller Checker, byte width, byte height)
		{
			byte i, j;
			int cnt = 0;

			//ラベリング処理
			for (i = 0; i < height; i++)
			{
				for (j = 0; j < width; j++)
				{
					if (Checker[new Point(j, i)]) continue;

					unsafe
					{
						Point* myStack = stackalloc Point[20 * 20];
						Point point;
						byte x, y, searchToX, searchToY, myStackSize = 0;

						myStack[myStackSize++] = new Point(j, i);
						Checker[new Point(j, i)] = true;

						while (myStackSize > 0)
						{
							point = myStack[--myStackSize];
							x = point.X;
							y = point.Y;

							for (int k = 0; k < 8; k++)
							{
								searchToX = (byte)(x + DistanceX[k]);
								searchToY = (byte)(y + DistanceY[k]);
								if (searchToX < width && searchToY < height && !Checker[searchToX, searchToY])
								{
									myStack[myStackSize++] = new Point(searchToX, searchToY);
									Checker[searchToX, searchToY] = true;
								}
							}
						}
					}
					cnt++;
				}
			}
			return cnt;
		}
	}
}
