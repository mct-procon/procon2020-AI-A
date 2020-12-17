using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.Search
{
    public class SearchState
    {
		public ColoredBoardNormalSmaller MeBoard;
		public ColoredBoardNormalSmaller EnemyBoard;
		public Unsafe16Array<Point> Me;
		public Unsafe16Array<Point> Enemy;
        public ColoredBoardNormalSmaller MeSurroundBoard;
        public ColoredBoardNormalSmaller EnemySurroundBoard;
        public int PointVelocity = 0;
        public int SurroundVelocity = 0;
        private SearchState() { }

        public SearchState(in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Unsafe16Array<Point> Me, in Unsafe16Array<Point> Enemy, in ColoredBoardNormalSmaller MeSurroundBoard, in ColoredBoardNormalSmaller EnemySurroundBoard)
		{
			this.MeBoard = MeBoard;
			this.EnemyBoard = EnemyBoard;
			this.Me = Me;
			this.Enemy = Enemy;
            this.MeSurroundBoard = MeSurroundBoard;
            this.EnemySurroundBoard = EnemySurroundBoard;
        }

        //全ての指示可能な方向を求めて, (way1[i], way2[i])に入れる。(Meが動くとする)
        public MultiAgentWays MakeMoves(int AgentsCount, sbyte[,] ScoreBoard) => new MultiAgentWays(this, AgentsCount, ScoreBoard);
        public SingleAgentWays MakeMovesSingle(int agentsCount, int agentIndex, sbyte[,] scoreBoard) => new SingleAgentWays(this, agentsCount, agentIndex, scoreBoard);

        private static ReadOnlySpan<VelocityPoint> Surrounder => new VelocityPoint[] { new VelocityPoint(1,0), new VelocityPoint(0, 1), new VelocityPoint(-1, 0), new VelocityPoint(0, -1) }; 
        private static ReadOnlySpan<VelocityPoint> Around => new VelocityPoint[] { new VelocityPoint(1,0), new VelocityPoint(1, -1), new VelocityPoint(1, 1), new VelocityPoint(0, 1), new VelocityPoint(-1, 1), new VelocityPoint(-1, 0), new VelocityPoint(-1, -1), new VelocityPoint(0, -1)};

        public void UpdateSurroundedState(Unsafe16Array<Way> agents, int agentsCount, byte width, byte height, int deepness)
        {
            bool lazy = false;
            ColoredBoardNormalSmaller Walls, Checker, BadSpaces;
            Span<Point> myStack = stackalloc Point[24 * 24];
            for (int agentNum = 0; agentNum < agentsCount; ++agentNum)
            {
                var agent = agents[agentNum];
                int fills = 0;
                for (int i = 0; i < Surrounder.Length; ++i)
                {
                    var p = agent.Locate + Surrounder[i];
                    if (p.X < width && p.Y < height && MeBoard[p])
                        fills++;
                }
                if (fills < 2) continue;
                if(!lazy)
                {
                    Walls = MeBoard;
                    Walls.Or(EnemyBoard);
                    Checker = new ColoredBoardNormalSmaller(width, 0);
                    BadSpaces = new ColoredBoardNormalSmaller(width, height);
                    lazy = true;
                }
                Point point;
                int myStackSize = 0;
                for (int a_iter = 0; a_iter < Around.Length; ++a_iter)
                {
                    point = agent.Locate + Around[a_iter];
                    if (point.X >= width || point.Y >= height) continue;
                    Checker.Clear(height);
                    myStack[0] = point;
                    myStackSize = 1;
                    while (myStackSize > 0)
                    {
                        point = myStack[--myStackSize];
                        for (int i = 0; i < 8; i++)
                        {
                            var a = Around[i];
                            var searchTo = point + a;
                            if (searchTo.X >= width || searchTo.Y >= height)
                            {
                                myStackSize++;
                                goto finish;
                            }
                            if (BadSpaces[searchTo])          // reached to bad space.
                            {
                                myStackSize++;
                                goto finish;
                            }
                            if (Walls[searchTo]) continue;    // wall
                            if (Checker[searchTo]) continue;  // needless to search
                            myStack[myStackSize++] = searchTo;
                            Checker[searchTo] = true;
                        }
                    }
                finish:
                    if (myStackSize > 0)
                        BadSpaces.Or(Checker);
                    else
                    {
                        ColoredBoardNormalSmaller diff = MeSurroundBoard;
                        diff.Xor(Checker, height);
                        SurroundVelocity += diff.PopCount(height) * deepness;
                        MeSurroundBoard.Or(Checker);
                        Checker.Invert(height);
                        EnemySurroundBoard.And(Checker, height);
                    }
                }
            }
        }

        public void UpdateSurroundedState(Way agent, byte width, byte height, int deepness)
        {
            int fills = 0;
            for (int i = 0; i < Surrounder.Length; ++i)
            {
                var p = agent.Locate + Surrounder[i];
                if (p.X < width && p.Y < height && MeBoard[p])
                    fills++;
            }
            if (fills < 2) return;
            Span<Point> myStack = stackalloc Point[24 * 24];

            Point point;
            int myStackSize = 0;
            ColoredBoardNormalSmaller Walls = MeBoard;
            Walls.Or(EnemyBoard);
            ColoredBoardNormalSmaller Checker = new ColoredBoardNormalSmaller(width, 0);
            ColoredBoardNormalSmaller BadSpaces = new ColoredBoardNormalSmaller(width, height);
            for (int a_iter = 0; a_iter < Around.Length; ++a_iter)
            {
                point = agent.Locate + Around[a_iter];
                if (point.X >= width || point.Y >= height || Walls[point]) continue;
                Checker.Clear(height);
                myStack[0] = point;
                myStackSize = 1;
                while (myStackSize > 0)
                {
                    point = myStack[--myStackSize];
                    for (int i = 0; i < 8; i++)
                    {
                        var a = Around[i];
                        var searchTo = point + a;
                        if (searchTo.X >= width || searchTo.Y >= height)
                        {
                            myStackSize++;
                            goto finish;
                        }
                        if (BadSpaces[searchTo])          // reached to bad space.
                        {
                            myStackSize++;
                            goto finish;
                        }
                        if (Walls[searchTo]) continue;    // wall
                        if (Checker[searchTo]) continue;  // needless to search
                        myStack[myStackSize++] = searchTo;
                        Checker[searchTo] = true;
                    }
                }
            finish:
                if (myStackSize > 0)
                    BadSpaces.Or(Checker);
                else
                {
                    if (deepness > 0)
                    {
                        ColoredBoardNormalSmaller diff = MeSurroundBoard;
                        diff.Xor(Checker, height);
                        SurroundVelocity += diff.PopCount(height) * deepness;
                    }
                    MeSurroundBoard.Or(Checker, height);
                    Checker.Invert(height);
                    EnemySurroundBoard.And(Checker, height);
                }
            }
        }
        public SearchState GetNextState(int AgentsCount, Unsafe16Array<Way> ways, sbyte[,] scoreBoard, int deepness)
        {
            var ss = new SearchState();
            ss.MeSurroundBoard = this.MeSurroundBoard;
            ss.EnemySurroundBoard = this.EnemySurroundBoard;
            ss.MeBoard = this.MeBoard;
            ss.EnemyBoard = this.EnemyBoard;
            ss.Me = this.Me;
            ss.Enemy = this.Enemy;
            ss.PointVelocity = PointVelocity;
            for (int i = 0; i < AgentsCount; ++i)
            {
                var l = ways[i].Locate;
                if (!ss.MeBoard[l])
                    ss.PointVelocity += scoreBoard[l.X, l.Y];
                if (ss.EnemyBoard[l]) // タイル除去
                    ss.EnemyBoard[l] = false;
                else
                {
                    ss.MeBoard[l] = true;
                    ss.Me[i] = l;
                }
            }
            ss.UpdateSurroundedState(ways, AgentsCount, (byte)scoreBoard.GetLength(0), (byte)scoreBoard.GetLength(1), deepness);
            return ss;
        }

        public SearchState GetNextStateSingle(int agentIndex, Way way, sbyte[,] scoreBoard)
        {
            var ss = new SearchState();
            ss.MeSurroundBoard = this.MeSurroundBoard;
            ss.EnemySurroundBoard = this.EnemySurroundBoard;
            ss.MeBoard = this.MeBoard;
            ss.EnemyBoard = this.EnemyBoard;
            ss.Me = this.Me;
            ss.Enemy = this.Enemy;
            ss.PointVelocity = PointVelocity;
            var l = way.Locate;
            if(!ss.MeBoard[l])
                ss.PointVelocity += scoreBoard[l.X, l.Y];
            if (ss.EnemyBoard[l]) // タイル除去
                ss.EnemyBoard[l] = false;
            else
            {
                ss.MeBoard[l] = true;
                ss.Me[agentIndex] = l;
            }
            ss.UpdateSurroundedState(way, (byte)scoreBoard.GetLength(0), (byte)scoreBoard.GetLength(1), -1);
            return ss;
        }
        public SearchState GetNextStateSingle(int agentIndex, Way way, sbyte[,] scoreBoard, int deepness)
        {
            var ss = new SearchState();
            ss.MeSurroundBoard = this.MeSurroundBoard;
            ss.EnemySurroundBoard = this.EnemySurroundBoard;
            ss.MeBoard = this.MeBoard;
            ss.EnemyBoard = this.EnemyBoard;
            ss.Me = this.Me;
            ss.Enemy = this.Enemy;
            ss.PointVelocity = PointVelocity;
            var l = way.Locate;
            if (!ss.MeBoard[l])
                ss.PointVelocity += scoreBoard[l.X, l.Y] * deepness;
            if (ss.EnemyBoard[l]) // タイル除去
                ss.EnemyBoard[l] = false;
            else
            {
                ss.MeBoard[l] = true;
                ss.Me[agentIndex] = l;
            }
            ss.UpdateSurroundedState(way, (byte)scoreBoard.GetLength(0), (byte)scoreBoard.GetLength(1), deepness);
            return ss;
        }

        public SearchState ChangeTurn()
        {
            var ss = new SearchState();
            ss.MeBoard = this.EnemyBoard;
            ss.EnemyBoard = this.MeBoard;
            ss.Me = this.Enemy;
            ss.Enemy = this.Me;
            ss.MeSurroundBoard = this.EnemySurroundBoard;
            ss.EnemySurroundBoard = this.MeSurroundBoard;
            ss.PointVelocity = -PointVelocity;
            return ss;
        }

        //タイルスコア最大の手を返す（MakeMoves -> SortMovesで0番目に来る手を返す）探索延長を高速化するために使用。
        public Unsafe16Array<VelocityPoint> MakeGreedyMove(sbyte[,] ScoreBoard, VelocityPoint[] WayEnumrator, int AgentsCount)
        {
            byte width = (byte)ScoreBoard.GetLength(0), height = (byte)ScoreBoard.GetLength(1);
            int i, j;
            int[] Score = { -100, -100, -100, -100, -100, -100, -100, -100 };
            Unsafe16Array<VelocityPoint> ways = new Unsafe16Array<VelocityPoint>();

            //自分2人が被るかのチェックをしないで、最大の組み合わせを探す
            for (i = 0; i < AgentsCount; ++i)
            {
                for (j = 0; j < WayEnumrator.Length; j++)
                {
                    Point next = Me[i] + WayEnumrator[j];
                    if (next.X >= width || next.Y >= height) continue;
                    bool b = false;
                    for(int k = 0; k < AgentsCount; ++k)
                    {
                        if (next == Enemy[k])
                        {
                            b = true;
                            break;
                        }
                    }
                    if (b) continue;
                    int score = (MeBoard[next] == true) ? 0 : ScoreBoard[next.X, next.Y];
                    if (Score[i] < score) { Score[i] = score; ways[i] = WayEnumrator[j]; }
                }
            }

            for(i = 0; i < AgentsCount; ++i)
            {
                if (Score[i] <= -100) break;
                for(j = i+1; j < AgentsCount; ++j)
                {
                    if (Me[i] + ways[i] == Me[j] + ways[j]) break;
                }
                if (j != AgentsCount) break;
            }
            if (i == AgentsCount) return ways;

            //真面目に探索する
            int maxScore = -100;
            for (i = 0; i < (WayEnumrator.Length << (AgentsCount * 3)); ++i)
            {
                Unsafe16Array<Point> next = new Unsafe16Array<Point>();
                int score = 0;
                for (j = 0; j < AgentsCount; ++j)
                {
                    int way = (i >> (j * 3)) % WayEnumrator.Length;
                    next[j] = Me[j] + WayEnumrator[way];
                    if (next[j].X >= width || next[j].Y >= height) continue;
                    bool b = false;
                    for(int k = 0; k < AgentsCount; ++k)
                    {
                        if (Enemy[k] == next[j])
                        {
                            b = true;
                            break;
                        }
                    }
                    if (b) continue;
                    score += (MeBoard[next[j]] == true) ? 0 : ScoreBoard[next[j].X, next[j].Y];
                }
                if(maxScore < score)
                {
                    maxScore = score;
                    for (j = 0; j < AgentsCount; ++j)
                    {
                        int way = (i >> (j * 3)) % WayEnumrator.Length;
                        ways[j] = WayEnumrator[way];
                    }
                }
            }
            return ways;
        }

        //Search Stateを更新する (MeとEnemyの入れ替えも忘れずに）（呼び出し時の前提：Validな動きである）
        public void Move(Unsafe16Array<VelocityPoint> way, int AgentsCount)
        {
            Unsafe16Array<Point> next = new Unsafe16Array<Point>();
            for(int i = 0; i < AgentsCount; ++i)
            {
                next[i] = Me[i] + way[i];
            }


            for(int i = 0; i < AgentsCount; ++i)
            {
                if (EnemyBoard[next[i]])  //タイル除去
                {
                    EnemyBoard[next[i]] = false;
                }
                else  //移動
                {
                    MeBoard[next[i]] = true;
                    Me.Agent1 = next[i];
                }
            }

            //MeとEnemyの入れ替え（手番の入れ替え）
            Swap(ref MeBoard, ref EnemyBoard);
            Swap(ref Me, ref Enemy);
        }

        //内容が等しいか？
        public bool Equals(SearchState st, int agentCount)
		{
            // TODO: Restore implementation.
#if false
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

			if (!Unsafe16Array<Point>.Equals(Me, st.Me, agentCount)) return false;
			if (!Unsafe16Array<Point>.Equals(Enemy, st.Enemy, agentCount)) return false;
            return true;
#endif
            throw new NotImplementedException();
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
