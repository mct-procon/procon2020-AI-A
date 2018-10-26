using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;
using AngryBee.Boards;

namespace AngryBee.Search
{
    public struct SearchState
    {
		public ColoredBoardSmallBigger MeBoard;
		public ColoredBoardSmallBigger EnemyBoard;
		public Player Me;
		public Player Enemy;
        public ObjectPool<Ways> WaysPool;

		public SearchState(in ColoredBoardSmallBigger MeBoard, in ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy, ObjectPool<Ways> waysPool)
		{
			this.MeBoard = MeBoard;
			this.EnemyBoard = EnemyBoard;
			this.Me = Me;
			this.Enemy = Enemy;
            this.WaysPool = waysPool;
		}

        public Ways GetWays()
        {
            if (WaysPool.Get(out var ret)) return ret;
            else return new Ways();
        }

		//全ての指示可能な方向を求めて, (way1[i], way2[i])に入れる。(Meが動くとする)
		public Ways MakeMoves(VelocityPoint[] WayEnumrator)
		{
            Ways Result = GetWays();
			int n = WayEnumrator.Length;
			uint W = MeBoard.Width;
			uint H = MeBoard.Height;
			int i, j;

			for (i = 0; i < n; i++)
			{
				Point next1 = Me.Agent1 + WayEnumrator[i];
				if (next1.X >= W || next1.Y >= H) continue;
				if (Enemy.Agent1 == next1) continue;
				if (Enemy.Agent2 == next1) continue;
				for (j = 0; j < n; j++)
				{
					Point next2 = Me.Agent2 + WayEnumrator[j];
					if (next2.X >= W || next2.Y >= H) continue;
					if (Enemy.Agent1 == next2) continue;
					if (Enemy.Agent2 == next2) continue;
					if (next1 == next2) continue;
					Result.Add(new Way(WayEnumrator[i], WayEnumrator[j]));
				}
			}
            return Result;
		}

        //タイルスコア最大の手を返す（MakeMoves -> SortMovesで0番目に来る手を返す）探索延長を高速化するために使用。
        public Way MakeGreedyMove(sbyte[,] ScoreBoard, VelocityPoint[] WayEnumrator)
        {
            int i, j;
            int score1 = -100, score2 = -100;
            Way way = new Way();

            //自分2人が被るかのチェックをしないで、最大の組み合わせを探す
            for (i = 0; i < WayEnumrator.Length; i++)
            {
                Point next1 = Me.Agent1 + WayEnumrator[i];
                if (next1.X >= MeBoard.Width || next1.Y >= MeBoard.Height) continue;
                if (next1 == Enemy.Agent1 || next1 == Enemy.Agent2) continue;
                int score = (MeBoard[next1] == true) ? 0 : ScoreBoard[next1.X, next1.Y];
                if (score1 < score) { score1 = score; way.Agent1Way = WayEnumrator[i]; }
            }

            for (i = 0; i < WayEnumrator.Length; i++)
            {
                Point next2 = Me.Agent2 + WayEnumrator[i];
                if (next2.X >= MeBoard.Width || next2.Y >= MeBoard.Height) continue;
                if (next2 == Enemy.Agent1 || next2 == Enemy.Agent2) continue;
                int score = (MeBoard[next2] == true) ? 0 : ScoreBoard[next2.X, next2.Y];
                if (score2 < score) { score2 = score; way.Agent2Way = WayEnumrator[i]; }
            }

            if (score1 > -100 && score2 > -100 && (Me.Agent1 + way.Agent1Way) != (Me.Agent2 + way.Agent2Way)) return way;

            //真面目に探索する
            int maxScore = -100;
            for (i = 0; i < WayEnumrator.Length; i++)
            {
                Point next1 = Me.Agent1 + WayEnumrator[i];
                if (next1.X >= MeBoard.Width || next1.Y >= MeBoard.Height) continue;
                if (Enemy.Agent1 == next1) continue;
                if (Enemy.Agent2 == next1) continue;
                int scoreA = (MeBoard[next1] == true) ? 0 : ScoreBoard[next1.X, next1.Y];
                for (j = 0; j < WayEnumrator.Length; j++)
                {
                    Point next2 = Me.Agent2 + WayEnumrator[j];
                    if (next2.X >= MeBoard.Width || next2.Y >= MeBoard.Height) continue;
                    if (Enemy.Agent1 == next2) continue;
                    if (Enemy.Agent2 == next2) continue;
                    if (next1 == next2) continue;
                    int scoreB = (MeBoard[next2] == true) ? 0 : ScoreBoard[next2.X, next2.Y];
                    if (maxScore < scoreA + scoreB)
                    {
                        maxScore = scoreA + scoreB;
                        way.Agent1Way = WayEnumrator[i];
                        way.Agent2Way = WayEnumrator[j];
                    }
                }
            }
            return way;
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

		//内容が等しいか？
		public bool Equals(SearchState st)
		{
			if (MeBoard.Height != st.MeBoard.Height) return false;
			if (MeBoard.Width != st.MeBoard.Width) return false;
			for (uint i = 0; i < MeBoard.Height; i++) for (uint j = 0; j < MeBoard.Width; j++) if (MeBoard[new Point(j, i)] != st.MeBoard[new Point(j, i)]) return false;

			if (EnemyBoard.Height != st.EnemyBoard.Height) return false;
			if (EnemyBoard.Width != st.EnemyBoard.Width) return false;
			for (uint i = 0; i < EnemyBoard.Height; i++)
				for (uint j = 0; j < EnemyBoard.Width; j++)
					if (EnemyBoard[new Point(j, i)] != st.EnemyBoard[new Point(j, i)]) return false;

			if (Me.Agent1 != st.Me.Agent1) return false;
			if (Me.Agent2 != st.Me.Agent2) return false;
			if (Enemy.Agent1 != st.Enemy.Agent1) return false;
			if (Enemy.Agent2 != st.Enemy.Agent2) return false;
			return true;
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
