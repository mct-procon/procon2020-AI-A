﻿using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol.Methods;
using MCTProcon30Protocol;
using AngryBee.Search;
using System.Linq;

namespace AngryBee.AI
{
    public class NaottiAI : MCTProcon30Protocol.AIFramework.AIBase
    {
        PointEvaluator.Base PointEvaluator_Dispersion = new PointEvaluator.Dispersion();
        //PointEvaluator.Base PointEvaluator_Distance = new PointEvaluator.Distance();
        PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();
        VelocityPoint[] WayEnumerator = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };
        ObjectPool<Ways> WaysPool = new ObjectPool<Ways>();

        private class DP
        {
            public int Score { get; set; } = -10000;
            public Unsafe8Array<Way> Ways { get; set; }

            public void UpdateScore(int score, Unsafe8Array<Way> ways)
            {
                if (Score < score)
                {
                    Ways = ways;
                }
            }
        }

        private DP[] dp1 = new DP[50];	//dp1[i] = 深さi時点での最善手
		private DP[] dp2 = new DP[50];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
		private Decision lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）
        private int greedyMaxDepth = 0;         //探索延長の最大手数
        public int StartDepth { get; set; } = 1;

        public NaottiAI(int startDepth = 1, int greedyMaxDepth = 0)
        {
			for (int i = 0; i < 50; ++i)
			{
				dp1[i] = new DP();
				dp2[i] = new DP();
			}
            StartDepth = startDepth;
            this.greedyMaxDepth = greedyMaxDepth;
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
            int maxDepth = (TurnCount - CurrentTurn) * 2 + 2;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, MyAgents, EnemyAgents);
            int score = PointEvaluator_Normal.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - PointEvaluator_Normal.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);

            Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);
			//if (!(lastTurnDecided is null)) Log("IsAgent1Moved = {0}, IsAgent2Moved = {1}, lastTurnDecided = {2}", IsAgent1Moved, IsAgent2Moved, lastTurnDecided);

            if (!(lastTurnDecided is null) && score > 0)    //勝っている状態で競合していたら
            {
                int i;
                for(i = 0; i < AgentsCount; ++i)
                {
                    if (IsAgentsMoved[i]) break;
                }
                if(i == AgentsCount) SolverResultList.Add(lastTurnDecided);
                return;
            }

            for (; deepness <= maxDepth; deepness++)
            {
				Decided resultList = new Decided();

                int greedyDepth = Math.Min(greedyMaxDepth, maxDepth - deepness);
                if ((deepness + greedyDepth) % 2 == 1 && greedyDepth > 0) greedyDepth--;

				//普通にNegaMaxをして、最善手を探す
                NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator, null, greedyDepth);
				Decision best1 = new Decision(Unsafe8Array<VelocityPoint>.Create(dp1[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
				resultList.Add(best1);

                //競合手.Agent1 == 最善手.Agent1 && 競合手.Agent2 == 最善手.Agent2になった場合、競合手をngMoveとして探索をおこない、最善手を探す
                for(int i = 0; i < AgentsCount; ++i)
                {
                    if(IsAgentsMoved[i] || !lastTurnDecided.Agents[i].Equals(best1.Agents[i]))
                    {
                        break;
                    }
                    if (i < AgentsCount - 1) continue;
                    NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator, best1, greedyDepth);
                    Decision best2 = new Decision(Unsafe8Array<VelocityPoint>.Create(dp2[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
                    resultList.Add(best2);
                }
                
                if (CancellationToken.IsCancellationRequested == false)
				{
					SolverResultList = resultList;
                    if (SolverResultList.Count == 2 && score <= 0)  //現時点で引き分けか負けていたら競合を避けるのを優先してみる（デバッグ用）
                    {
                        var tmp = SolverResultList[0];
                        SolverResultList[0] = SolverResultList[1];
                        SolverResultList[1] = tmp;
                        Log("[SOLVER] Swaped! {0} {1}", SolverResult.Agents[0], SolverResult.Agents[1]);
                    }
                    Log("[SOLVER] SolverResultList.Count = {0}, score = {1}", SolverResultList.Count, score);
                }
				else
					return;
                Log("[SOLVER] deepness = {0}", deepness);
            }
		}

        protected override void EndSolve(object sender, EventArgs e)
        {
            base.EndSolve(sender, e);
            lastTurnDecided = new Decision();  //0番目の手を指したとする。（次善手を人間が選んで競合した～ということがなければOK）
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, PointEvaluator.Base evaluator, Decision ngMove, int greedyDepth)
        {
            if (deepness == 0)
            {
                //for (int j = 0; j < greedyDepth; j++)
                //{
                    //Way move = state.MakeGreedyMove(ScoreBoard, WayEnumerator);
                    //state.Move(move.Agent1Way, move.Agent2Way);
                    //Ways moves = state.MakeMoves(WayEnumerator);
                    //SortMoves(ScoreBoard, state, moves, 49, null);
                    //state.Move(moves[0].Agent1Way, moves[1].Agent2Way);
                //}
                int score = evaluator.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);
                if (greedyDepth % 2 == 1) return -score;
                return score;
            }

            Ways ways = state.MakeMoves(AgentsCount, ScoreBoard);
            //SortMoves(ScoreBoard, state, ways, count, ngMove);

            foreach (var way in ways.GetEnumerator(AgentsCount))
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                //if (count == 0 && !(ngMove is null) && new Decided(ways[i].Agent1Way, ways[i].Agent2Way).Equals(ngMove)) { continue; }	//競合手を避ける場合
                if (count == 0 && !(ngMove is null))    //2人とも競合手とは違う手を指す
                {
                    int i;
                    for(i = 0; i < AgentsCount; ++i)
                    {
                        if (way[i].Equals(ngMove.Agents[i]))
                        {
                            break;
                        }
                    }
                    if (i == AgentsCount) continue;
                }
                
                SearchState nextState = state;
                nextState = nextState.GetNextState(AgentsCount, way);
                int res = -NegaMax(deepness - 1, nextState, -beta, -alpha, count + 1, evaluator, ngMove, greedyDepth);
                if (alpha < res)
                {
                    alpha = res;
					if (ngMove is null) { dp1[count].UpdateScore(alpha, way); }
					else { dp2[count].UpdateScore(alpha, way); }
                    if (alpha >= beta) return beta; //βcut
                }
            }
            //ways.Erase();
            //WaysPool.Return(ways);
            return alpha;
        }

        //TODO

        /*
        //遷移順を決める.  「この関数においては」MeBoard…手番プレイヤのボード, Me…手番プレイヤ、とします。
        //引数: stateは手番プレイヤが手を打つ前の探索状態、(way1[i], way2[i])はi番目の合法手（移動量）です。
        //以下のルールで優先順を決めます.
        //ルール1. Killer手（優先したい手）があれば、それを優先する
        //ルール2. 次のmoveで得られる「タイルポイント」の合計値が大きい移動（の組み合わせ）を優先する。
        //ルール2では, タイル除去によっても「タイルポイント」が得られるとして計算する。
        private void SortMoves(sbyte[,] ScoreBoard, SearchState state, Ways way, int deep, Decision ngMove)
        {
			Player Killer;
			if (ngMove is null)
			{
				Killer = dp1[deep].Score == int.MinValue ? new Player(new Point(114, 191), new Point(114, 191)) : new Player(state.Me.Agent1 + dp1[deep].Agent1Way, state.Me.Agent2 + dp1[deep].Agent2Way);
			}
			else
			{
				Killer = dp2[deep].Score == int.MinValue ? new Player(new Point(114, 191), new Point(114, 191)) : new Player(state.Me.Agent1 + dp2[deep].Agent1Way, state.Me.Agent2 + dp2[deep].Agent2Way);
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
        */
        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 1000;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}