using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;
using AngryBee.Search;

namespace AngryBee.AI
{
    public class NaottiAI : MCTProcon29Protocol.AIFramework.AIBase
    {
        PointEvaluator.Base PointEvaluator_Dispersion = new PointEvaluator.Dispersion();
        PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();
        VelocityPoint[] WayEnumerator = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };
        ObjectPool<Ways> WaysPool = new ObjectPool<Ways>();

        private struct DP
        {
            public int Score;
            public VelocityPoint Agent1Way;
            public VelocityPoint Agent2Way;

            public void UpdateScore(int score, VelocityPoint a1, VelocityPoint a2)
            {
                if (Score < score)
                {
                    Agent1Way = a1;
                    Agent2Way = a2;
                }
            }
        }

		private DP[] dp1 = new DP[50];	//dp1[i] = 深さi時点での最善手
		private DP[] dp2 = new DP[50];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
		private Decided lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）

        public int StartDepth { get; set; } = 1;

        public NaottiAI(int startDepth = 1)
        {
			for (int i = 0; i < 50; ++i)
			{
				dp1[i] = new DP();
				dp2[i] = new DP();
			}
            StartDepth = startDepth;
        }

        //1ターン = 深さ2
        protected override void Solve()
        {
			for (int i = 0; i < 50; ++i)
			{
				dp1[i].Score = int.MinValue;
				dp2[i].Score = int.MinValue;
			}

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) * 2 + 4;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, new Player(MyAgent1, MyAgent2), new Player(EnemyAgent1, EnemyAgent2), WaysPool);

			//Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);
			if (!(lastTurnDecided is null)) Log("IsAgent1Moved = {0}, IsAgent2Moved = {1}, lastTurnDecided = {2}", IsAgent1Moved, IsAgent2Moved, lastTurnDecided);

            for (; deepness <= maxDepth; deepness++)
            {
				DecidedEx resultList = new DecidedEx();

				//普通にNegaMaxをして、最善手を探す
                NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator, null);
				Decided best1 = new Decided(dp1[0].Agent1Way, dp1[0].Agent2Way);
				resultList.Add(best1);
				
				//競合手 == 最善手になった場合、競合手をngMoveとして探索をおこない、最善手を探す
				if (IsAgent1Moved == false && IsAgent2Moved == false && lastTurnDecided.Equals(best1))
				{
					NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator, best1);
					Decided best2 = new Decided(dp2[0].Agent1Way, dp2[0].Agent2Way);
					resultList.Add(best2);
				}

				if (CancellationToken.IsCancellationRequested == false)
				{
					SolverResultList = resultList;
				}
				else
					break;
                Log("[SOLVER] deepness = {0}", deepness);
            }

			if (SolverResultList.Count == 2)	//競合を避けるのを優先してみる（デバッグ用）
			{
				var tmp = SolverResultList[0];
				SolverResultList[0] = SolverResultList[1];
				SolverResultList[1] = tmp;
			}
			lastTurnDecided = SolverResultList[0];	//0番目の手を指したとする。（次善手を人間が選んで競合した～ということがなければOK）
			Log("[SOLVER] SolverResultList.Count = {0}", SolverResultList.Count);
		}

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, PointEvaluator.Base evaluator, Decided ngMove)
        {
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, 0) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0);
            }

            Ways ways = state.MakeMoves(WayEnumerator);
            SortMoves(ScoreBoard, state, ways, count, ngMove);

            for (int i = 0; i < ways.Count; i++)
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
				if (count == 0 && !(ngMove is null) && new Decided(ways[i].Agent1Way, ways[i].Agent2Way).Equals(ngMove)) { continue; }	//競合手を避ける場合

                SearchState nextState = state;
                nextState.Move(ways[i].Agent1Way, ways[i].Agent2Way);
                int res = -NegaMax(deepness - 1, nextState, -beta, -alpha, count + 1, evaluator, ngMove);
                if (alpha < res)
                {
                    alpha = res;
					if (ngMove is null) { dp1[count].UpdateScore(alpha, ways[i].Agent1Way, ways[i].Agent2Way); }
					else { dp2[count].UpdateScore(alpha, ways[i].Agent1Way, ways[i].Agent2Way); }
                    if (alpha >= beta) return beta; //βcut
                }
            }
            ways.Erase();
            WaysPool.Return(ways);
            return alpha;
        }

        //遷移順を決める.  「この関数においては」MeBoard…手番プレイヤのボード, Me…手番プレイヤ、とします。
        //引数: stateは手番プレイヤが手を打つ前の探索状態、(way1[i], way2[i])はi番目の合法手（移動量）です。
        //以下のルールで優先順を決めます.
        //ルール1. Killer手（優先したい手）があれば、それを優先する
        //ルール2. 次のmoveで得られる「タイルポイント」の合計値が大きい移動（の組み合わせ）を優先する。
        //ルール2では, タイル除去によっても「タイルポイント」が得られるとして計算する。
        private void SortMoves(sbyte[,] ScoreBoard, SearchState state, Ways way, int deep, Decided ngMove)
        {
			Player Killer;
			if (ngMove is null)
			{
				Killer = dp1[deep].Score == int.MinValue ? new Player(new Point(114, 514), new Point(114, 514)) : new Player(state.Me.Agent1 + dp1[deep].Agent1Way, state.Me.Agent2 + dp1[deep].Agent2Way);
			}
			else
			{
				Killer = dp2[deep].Score == int.MinValue ? new Player(new Point(114, 514), new Point(114, 514)) : new Player(state.Me.Agent1 + dp2[deep].Agent1Way, state.Me.Agent2 + dp2[deep].Agent2Way);
			}

            for (int i = 0; i < way.Count; i++)
            {
                int score = 0;
                Point next1 = state.Me.Agent1 + way[i].Agent1Way;
                Point next2 = state.Me.Agent2 + way[i].Agent2Way;

                if (Killer.Agent1 == next1 && Killer.Agent2 == next2) { score = 100; }

                if (state.EnemyBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }     //タイル除去によって有利になる
                else if (!state.MeBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }  //移動でMeの陣地が増えて有利になる
                if (state.EnemyBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                else if (!state.MeBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                way[i].Point = score;
            }
            way.Sort();
        }

        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 1000;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}
