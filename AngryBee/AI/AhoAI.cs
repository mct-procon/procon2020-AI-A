using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using AngryBee.Search;
using System.Linq;

namespace AngryBee.AI
{
	public class AhoAI : MCTProcon31Protocol.AIFramework.AIBase
	{
		PointEvaluator.Base PointEvaluator_Dispersion = new PointEvaluator.Dispersion();
		PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();

		private class DP
		{
            public int Score { get; set; } = -10000;
            public Unsafe16Array<Way> Ways { get; set; }

			public void UpdateScore(int score, Unsafe16Array<Way> ways)
			{
				if (Score < score)
				{
                    Ways = ways;
				}
			}
		}
		private DP[] dp = new DP[50];

		public int StartDepth { get; set; } = 1;

		public AhoAI(int startDepth = 1)
		{
			for (int i = 0; i < 50; ++i)
				dp[i] = new DP();
			StartDepth = startDepth;
		}

		//1ターン = 深さ2
		protected override void Solve()
		{
			for (int i = 0; i < 50; ++i)
				dp[i].Score = int.MinValue;

			int deepness = StartDepth;
			int maxDepth = (TurnCount - CurrentTurn) * 2;
			PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
			SearchState state = new SearchState(MyBoard, EnemyBoard, MyAgents, EnemyAgents);

			for (; deepness <= 1; deepness++)
			{
				NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator);
				if (CancellationToken.IsCancellationRequested == false)
					SolverResult = new Decision((byte)AgentsCount, Unsafe16Array<VelocityPoint>.Create(dp[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
				else
					break;
				Log("[SOLVER] deepness = {0}", deepness);
			}
		}

		//Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
		private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, PointEvaluator.Base evaluator)
		{
            var sw = System.Diagnostics.Stopwatch.StartNew();
			if (deepness == 0)
			{
				return evaluator.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);
			}

			Ways ways = state.MakeMoves(AgentsCount, ScoreBoard);

            int i = 0;
			foreach(var way in ways.GetEnumerator(AgentsCount))
			{
                i++;
				if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
				SearchState backup = state;
				state = state.GetNextState(AgentsCount, way);
                state = state.ChangeTurn();
                int res = -NegaMax(deepness - 1, state, -beta, -alpha, count + 1, evaluator);
				if (alpha < res)
				{
					alpha = res;
					dp[count].UpdateScore(alpha, way);
					if (alpha >= beta) return beta; //βcut
				}
				state = backup;
            }
            sw.Stop();
            Log("NODES : {0} nodes, elasped {1} ", i,sw.Elapsed);
            ways.End();
            return alpha;
		}

		protected override int CalculateTimerMiliSconds(int miliseconds)
		{
			return int.MaxValue;
		}

		protected override void EndGame(GameEnd end)
		{
		}
	}
}
