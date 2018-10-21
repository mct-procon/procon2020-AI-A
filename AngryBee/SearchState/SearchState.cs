using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;
using AngryBee.Boards;

namespace AngryBee.SearchState
{
    class SearchState
    {
		public ColoredBoardSmallBigger MeBoard;
		public ColoredBoardSmallBigger EnemyBoard;
		public Player Me;
		public Player Enemy;

		public SearchState() { }
		public SearchState(in ColoredBoardSmallBigger MeBoard, in ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy)
		{
			this.MeBoard = MeBoard;
			this.EnemyBoard = EnemyBoard;
			this.Me = Me;
			this.Enemy = Enemy;
		}

		//全ての指示可能な方向(0～8)を求めて, (way1[i], way2[i])に入れる。(Meが動くとする)
		public void MakeMoves(VelocityPoint[] WayEmnurator, List<VelocityPoint> way1, List<VelocityPoint> way2)
		{
			int n = WayEmnurator.Length;
			uint W = MeBoard.Width;
			uint H = MeBoard.Height;
			int i, j;

			for (i = 0; i < n; i++)
			{
				Point next1 = Me.Agent1 + WayEmnurator[i];
				if (next1.X >= W || next1.Y >= H) continue;
				if (Enemy.Agent1 == next1) continue;
				if (Enemy.Agent2 == next1) continue;
				for (j = 0; j < n; j++)
				{
					Point next2 = Me.Agent2 + WayEmnurator[j];
					if (next2.X >= W || next2.Y >= H) continue;
					if (Enemy.Agent1 == next2) continue;
					if (Enemy.Agent2 == next2) continue;
					if (next1 == next2) continue;
					way1.Add(WayEmnurator[i]);
					way2.Add(WayEmnurator[j]);
				}
			}
		}

		//Search Stateを更新する (MeとEnemyの入れ替えも忘れずに）（呼び出し時の前提：Validな動きである）
		public void Move(VelocityPoint way1, VelocityPoint way2)
		{
			Point next1 = Me.Agent1 + way1;
			Point next2 = Me.Agent2 + way2;

			//エージェント1
			if (EnemyBoard[next1])	//タイル除去
			{
				EnemyBoard[next1] = false;
			}
			else  //移動
			{
				MeBoard[next1] = true;
				Me.Agent1 = next1;
			}

			//エージェント2
			if (EnemyBoard[next2])
			{
				EnemyBoard[next2] = false;
			}
			else
			{
				MeBoard[next2] = true;
				Me.Agent2 = next2;
			}

			//MeとEnemyの入れ替え（手番の入れ替え）
			Swap(ref MeBoard, ref EnemyBoard);
			Swap(ref Me, ref Enemy);
		}

		//Swap関数
		private static void Swap<T>(ref T a, ref T b)
		{
			var t = a;
			a = b;
			b = t;
		}
	}
}
