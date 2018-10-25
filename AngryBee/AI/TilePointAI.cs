using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;
using AngryBee.Search;

namespace AngryBee.AI
{
	public class TilePointAI : MCTProcon29Protocol.AIFramework.AIBase
	{
		VelocityPoint[] WayEnumerator1 = { (1, -1), (1, 1), (-1, 1), (-1, -1) };
		VelocityPoint[] WayEnumerator2 = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };
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
		private DP[] dp = new DP[50];

		//public int ends = 0;

		public int StartDepth { get; set; } = 1;

		public TilePointAI(int startDepth = 1)
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
			SearchState state = new SearchState(MyBoard, EnemyBoard, new Player(MyAgent1, MyAgent2), new Player(EnemyAgent1, EnemyAgent2), WaysPool);

			for (; deepness <= maxDepth; deepness++)
			{
				NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, 0);
				if (CancellationToken.IsCancellationRequested == false)
					SolverResult = new Decided(dp[0].Agent1Way, dp[0].Agent2Way);
				else
					break;
				Log("[SOLVER] deepness = {0}", deepness);
			}
		}

		//Meが動くとする。「Meのタイルポイント - Enemyのタイルポイント」の最大値を返す。
		private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, int tilePoint)
		{
			if (deepness == 0)
			{
				return tilePoint;
			}

			VelocityPoint[] WayEnumerator = CurrentTurn * 2 + deepness <= 50 ? WayEnumerator1 : WayEnumerator2;
			Ways ways = state.MakeMoves(WayEnumerator);
			SortMoves(ScoreBoard, state, ways, count);

			for (int i = 0; i < ways.Count; i++)
			{
				if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
				SearchState backup = state;
				state.Move(ways[i].Agent1Way, ways[i].Agent2Way);
				int res = -NegaMax(deepness - 1, state, -beta, -alpha, count + 1, -(tilePoint + ways[i].Point));
				if (alpha < res)
				{
					alpha = res;
					dp[count].UpdateScore(alpha, ways[i].Agent1Way, ways[i].Agent2Way);
					if (alpha >= beta) return beta; //βcut
				}
				state = backup;
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
		private void SortMoves(sbyte[,] ScoreBoard, SearchState state, Ways way, int deep)
		{
			var Killer = dp[deep].Score == int.MinValue ? new Player(new Point(114, 514), new Point(114, 514)) : new Player(state.Me.Agent1 + dp[deep].Agent1Way, state.Me.Agent2 + dp[deep].Agent2Way);

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
