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
    public class AhoAI_8 : MCTProcon31Protocol.AIFramework.AIBase
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

        public AhoAI_8(int startDepth = 1)
        {
            for (int i = 0; i < 50; ++i)
            {
                dp[i] = new DP();
            }
            StartDepth = startDepth;
        }


        protected override void Solve()
        {
            for (int i = 0; i < 50; ++i)
            {
                dp[i].Score = int.MinValue;
                dp[i].Ways = new Unsafe16Array<Way>();
            }

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            //PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            PointEvaluator.Base evaluator = PointEvaluator_Normal;
            SearchState state = new SearchState(MyBoard, EnemyBoard, MyAgents, EnemyAgents);
            var mySurroundBoard = MySurroundedBoard;
            var enemySurroundBoard = EnemySurroundedBoard;

            Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);

            for (int agent = 0; agent < AgentsCount; ++agent)
            {
                Unsafe16Array<Way> nextways = dp[0].Ways;
                NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, nextways, agent, mySurroundBoard, enemySurroundBoard);
            }

            if (CancellationToken.IsCancellationRequested == false)
            {
                SolverResult = new Decision((byte)AgentsCount, Unsafe16Array<VelocityPoint>.Create(dp[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
            }
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, Unsafe16Array<Way> nextways, int nowAgent, ColoredBoardNormalSmaller mySurroundBoard, ColoredBoardNormalSmaller enemySurroundBoard)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, state.EnemyBoard, 0, state.Me, state.Enemy, mySurroundBoard, enemySurroundBoard) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, state.MeBoard, 0, state.Enemy, state.Me, mySurroundBoard, enemySurroundBoard);
            }

            Ways ways = state.MakeMoves(AgentsCount, ScoreBoard);

            int i = 0;
            foreach (var way in ways.Data[nowAgent])
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                if (way.Direction == new VelocityPoint()) continue;
                i++;

                int j = 0;
                for (j = 0; j < nowAgent; ++j)
                {
                    if (dp[0].Ways[j].Locate == way.Locate)
                    {
                        break;
                    }
                }
                if (j != nowAgent) continue;

                Unsafe16Array<Way> newways = new Unsafe16Array<Way>();
                newways[nowAgent] = way;
                SearchState backup = state;
                state = state.GetNextState(AgentsCount, newways);

                int res = NegaMax(deepness - 1, state, alpha, count + 1, evaluator, ngMove, nextways, nowAgent, mySurroundBoard, enemySurroundBoard);
                if (alpha < res)
                {
                    nextways[nowAgent] = way;
                    alpha = res;
                    dp[count].UpdateScore(alpha, nextways);
                }

                state = backup;
            }

            sw.Stop();
            //Log("NODES : {0} nodes, elasped {1} ", i, sw.Elapsed);
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