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

        public NaottiAI(int startDepth = 1)
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
            SearchState state = new SearchState(MyBoard, EnemyBoard, new Player(MyAgent1, MyAgent2), new Player(EnemyAgent1, EnemyAgent2));

            for (; deepness < maxDepth; deepness++)
            {
                NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator);
                if (CancellationToken.IsCancellationRequested == false)
                    SolverResult = new Decided(dp[0].Agent1Way, dp[0].Agent2Way);
                else
                    break;
                Log("[SOLVER] deepness = {0}", deepness);
            }
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, PointEvaluator.Base evaluator)
        {
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, 0) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0);
            }

            List<VelocityPoint> way1 = new List<VelocityPoint>();
            List<VelocityPoint> way2 = new List<VelocityPoint>();

            state.MakeMoves(WayEnumerator, way1, way2);
            List<KeyValuePair<int, (VelocityPoint Agent1, VelocityPoint Agent2)>> moves = SortMoves(ScoreBoard, state, way1, way2, count);

            for (int i = 0; i < moves.Count; i++)
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                SearchState backup = state;
                state.Move(moves[i].Value.Agent1, moves[i].Value.Agent2);
                int res = -NegaMax(deepness - 1, state, -beta, -alpha, count + 1, evaluator);
                if (alpha < res)
                {
                    alpha = res;
                    dp[count].UpdateScore(alpha, moves[i].Value.Agent1, moves[i].Value.Agent2);
                    if (alpha >= beta) return beta; //βcut
                }
                state = backup;
            }
            return alpha;
        }

        //遷移順を決める.  「この関数においては」MeBoard…手番プレイヤのボード, Me…手番プレイヤ、とします。
        //引数: stateは手番プレイヤが手を打つ前の探索状態、(way1[i], way2[i])はi番目の合法手（移動量）です。
        //以下のルールで優先順を決めます.
        //ルール1. Killer手（優先したい手）があれば、それを優先する
        //ルール2. 次のmoveで得られる「タイルポイント」の合計値が大きい移動（の組み合わせ）を優先する。
        //ルール2では, タイル除去によっても「タイルポイント」が得られるとして計算する。
        private List<KeyValuePair<int, (VelocityPoint Agent1, VelocityPoint Agent2)>> SortMoves(sbyte[,] ScoreBoard, SearchState state, List<VelocityPoint> way1, List<VelocityPoint> way2, int deep)
        {
            int n = way1.Count;
            List<KeyValuePair<int, (VelocityPoint, VelocityPoint)>> orderling = new List<KeyValuePair<int, (VelocityPoint, VelocityPoint)>>();
            var Killer = dp[deep].Score == int.MinValue ? new Player(new Point(114, 514), new Point(114, 514)) : new Player(state.Me.Agent1 + dp[deep].Agent1Way, state.Me.Agent2 + dp[deep].Agent2Way);

            for (int i = 0; i < n; i++)
            {
                int score = 0;
                Point next1 = state.Me.Agent1 + way1[i];
                Point next2 = state.Me.Agent2 + way2[i];

                if (Killer.Agent1 == next1 && Killer.Agent2 == next2) { score = 100; }

                if (state.EnemyBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }     //タイル除去によって有利になる
                else if (!state.MeBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }  //移動でMeの陣地が増えて有利になる
                if (state.EnemyBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                else if (!state.MeBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                orderling.Add(new KeyValuePair<int, (VelocityPoint, VelocityPoint)>(-score, (way1[i], way2[i])));   //スコア降順にソートするために-scoreを入れておく
            }
            orderling.Sort(impl_sorter);
            return orderling;
        }

        private int impl_sorter(KeyValuePair<int, (VelocityPoint Agent1, VelocityPoint Agent2)> a, KeyValuePair<int, (VelocityPoint Agent1, VelocityPoint Agent2)> b) => a.Key - b.Key;

        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 1000;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}
