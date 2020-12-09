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
        private Unsafe16Array<AgentState> agentStateAry = new Unsafe16Array<AgentState>();

        Unsafe16Array<Point> SearchFirstPlace()
        {
            int cur = 0;
            for (int i = 0; i < AgentsCount; ++i)
                if (MyAgentsState[i] == AgentState.NonPlaced)
                {
                    cur = i;
                    break;
                }

            if (MyAgentsState[cur] != AgentState.NonPlaced) return MyAgents;
            Random rand = new Random();
            List<Point> recommends = new List<Point>();
            Unsafe16Array<Point> newMyAgents = MyAgents;
            for (int x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (int y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= 10)
                        recommends.Add(new Point((byte)x, (byte)y));

            foreach (var p in recommends.OrderBy(i => rand.Next()))
            {
                bool isSkip = false;
                for (int i = 0; i < AgentsCount; ++i)
                    if ((MyAgentsState[i] != AgentState.NonPlaced && MyAgents[i] == p) ||
                        (EnemyAgentsState[i] != AgentState.NonPlaced && EnemyAgents[i] == p))
                    {
                        isSkip = true;
                        break;
                    }
                if (isSkip) continue;

                newMyAgents[cur] = p;
                for (int i = cur + 1; i < AgentsCount; ++i)
                    if (MyAgentsState[i] == AgentState.NonPlaced)
                    {
                        cur = i;
                        break;
                    }
                if (cur == AgentsCount) break;
            }
            return newMyAgents;
        }

        public AhoAI_8(int startDepth = 1)
        {
            for (int i = 0; i < 50; ++i)
            {
                dp[i] = new DP();
            }
            StartDepth = startDepth;
            for (int i = 0; i < 16; ++i)
                agentStateAry[i] = AgentState.Move;
        }


        protected override void Solve()
        {
            var myAgents = SearchFirstPlace();
            for (int i = 0; i < 50; ++i)
            {
                dp[i].Score = int.MinValue;
                dp[i].Ways = new Unsafe16Array<Way>();
            }

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            //PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            PointEvaluator.Base evaluator = PointEvaluator_Normal;
            SearchState state = new SearchState(MyBoard, EnemyBoard, myAgents, EnemyAgents, MySurroundedBoard, EnemySurroundedBoard);

            Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);

            for (int agent = 0; agent < AgentsCount; ++agent)
            {
                if (MyAgentsState[agent] == AgentState.NonPlaced) continue;
                Unsafe16Array<Way> nextways = dp[0].Ways;
                NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, nextways, agent);
            }

            if (CancellationToken.IsCancellationRequested == false)
            {
                var res = Unsafe16Array.Create(dp[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Locate).ToArray());
                for (int agent = 0; agent < AgentsCount; ++agent)
                    if (MyAgentsState[agent] == AgentState.NonPlaced) res[agent] = myAgents[agent];
                SolverResultList.Add(new Decision((byte)AgentsCount, res, agentStateAry));
            }
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, Unsafe16Array<Way> nextways, int nowAgent)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, state.EnemyBoard, 0, state.Me, state.Enemy, state.MeSurroundBoard, state.EnemySurroundBoard) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, state.MeBoard, 0, state.Enemy, state.Me, state.EnemySurroundBoard, state.MeSurroundBoard);
            }

            MultiAgentWays ways = state.MakeMoves(AgentsCount, ScoreBoard);

            int i = 0;
            foreach (var way in ways.Data[nowAgent])
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
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

                SearchState newState = state.GetNextStateSingle(nowAgent, way);

                int res = NegaMax(deepness - 1, newState, alpha, count + 1, evaluator, ngMove, nextways, nowAgent);
                if (alpha < res)
                {
                    nextways[nowAgent] = way;
                    alpha = res;
                    dp[count].UpdateScore(alpha, nextways);
                }
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