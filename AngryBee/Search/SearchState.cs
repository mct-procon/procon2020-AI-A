using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol.Methods;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.Search
{
    public class SearchState
    {
		public ColoredBoardNormalSmaller MeBoard;
		public ColoredBoardNormalSmaller EnemyBoard;
		public Unsafe8Array<Point> Me;
		public Unsafe8Array<Point> Enemy;

        private SearchState() { }

        public SearchState(in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Unsafe8Array<Point> Me, in Unsafe8Array<Point> Enemy)
		{
			this.MeBoard = MeBoard;
			this.EnemyBoard = EnemyBoard;
			this.Me = Me;
			this.Enemy = Enemy;
        }

        //全ての指示可能な方向を求めて, (way1[i], way2[i])に入れる。(Meが動くとする)
        public Ways MakeMoves(int AgentsCount, sbyte[,] ScoreBoard) => new Ways(this, AgentsCount, ScoreBoard);

        public SearchState GetNextState(int AgentsCount, Unsafe8Array<Way> ways)
        {
            var ss = new SearchState();
            ss.MeBoard = this.EnemyBoard;
            ss.EnemyBoard = this.MeBoard;
            ss.Me = this.Enemy;
            ss.Enemy = this.Me;
            for (int i = 0; i < AgentsCount; ++i)
            {
                var l = ways[i].Locate;
                if (ss.MeBoard[l]) // タイル除去
                    ss.MeBoard[l] = false;
                else
                {
                    ss.EnemyBoard[l] = true;
                    ss.Enemy[i] = l;
                }
            }
            return ss;
        }

		//内容が等しいか？
		public bool Equals(SearchState st, int agentCount)
		{
#if DEBUG
            if (MeBoard.Height != st.MeBoard.Height) return false;
			if (MeBoard.Width != st.MeBoard.Width) return false;
#endif
            for (byte i = 0; i < MeBoard.Height; i++) for (byte j = 0; j < MeBoard.Width; j++) if (MeBoard[new Point(j, i)] != st.MeBoard[new Point(j, i)]) return false;

#if DEBUG
            if (EnemyBoard.Height != st.EnemyBoard.Height) return false;
			if (EnemyBoard.Width != st.EnemyBoard.Width) return false;
#endif
            for (uint i = 0; i < EnemyBoard.Height; i++)
				for (uint j = 0; j < EnemyBoard.Width; j++)
					if (EnemyBoard[new Point((byte)j, (byte)i)] != st.EnemyBoard[new Point((byte)j, (byte)i)]) return false;

			if (!Unsafe8Array<Point>.Equals(Me, st.Me, agentCount)) return false;
			if (!Unsafe8Array<Point>.Equals(Enemy, st.Enemy, agentCount)) return false;
            return true;
		}

		//Swap関数
		//private static void Swap<T>(ref T a, ref T b)
		//{
		//	var t = a;
		//	a = b;
		//	b = t;
		//}
	}
}
