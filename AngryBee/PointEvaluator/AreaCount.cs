using System;
using System.Collections.Generic;
using System.Text;
using AngryBee.Boards;
using System.Runtime.Intrinsics.X86;
using MCTProcon30Protocol;

namespace AngryBee.PointEvaluator
{
	public class AreaCount : Base
	{
		readonly int[] DistanceX = { 0, 1, 0, -1, 1, 1, -1, -1 };
		readonly int[] DistanceY = { 1, 0, -1, 0, -1, 1, 1, -1 };
		public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy)
		{
			ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(Painted.Width, Painted.Height);
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

			BadSpaceFill(ref checker, (byte)width, (byte)height);

			for (uint x = 0; x < width; ++x)
				for (uint y = 0; y < height; ++y)
					if (!checker[x, y])
						result += Math.Abs(ScoreBoard[x, y]);

			//囲った領域の個数を数える(BadSpaceFill(外側の塗りつぶし)が終わったあとの2次元配列checkerを渡す）
			int count = CalcAreaCount(checker);

			return result + count * 4;
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

		private int CalcAreaCount(ColoredBoardNormalSmaller Checker)
		{
			byte i, j;
			byte height = (byte)Checker.Height;
			byte width = (byte)Checker.Width;
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
