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

        private Unsafe16Array<Point>[] dp = new Unsafe16Array<Point>[100];
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
            Array.Clear(dp, 0, dp.Length);
            StartDepth = startDepth;
            for (int i = 0; i < 16; ++i)
                agentStateAry[i] = AgentState.Move;
        }


        protected override void Solve()
        {
            var myAgents = SearchFirstPlace();
            Array.Clear(dp, 0, dp.Length);

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            //PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            PointEvaluator.Base evaluator = PointEvaluator_Normal;
            SearchState state = new SearchState(MyBoard, EnemyBoard, myAgents, EnemyAgents, MySurroundedBoard, EnemySurroundedBoard);

            Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);

            for (int agent = 0; agent < AgentsCount; ++agent)
            {
                if (MyAgentsState[agent] == AgentState.NonPlaced) continue;
                Unsafe16Array<Point> nextways = dp[0];
                NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, nextways, agent);
            }

            if (CancellationToken.IsCancellationRequested == false)
            {
                var res = dp[0];
                for (int agent = 0; agent < AgentsCount; ++agent)
                    if (MyAgentsState[agent] == AgentState.NonPlaced) res[agent] = myAgents[agent];
                SolverResultList.Add(new Decision((byte)AgentsCount, res, agentStateAry));
            }
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, Unsafe16Array<Point> nextways, int nowAgent)
        {
            if (deepness == 0)
                return evaluator.Calculate(ScoreBoard, state, 0);

            SingleAgentWays ways = state.MakeMovesSingle(AgentsCount, nowAgent, ScoreBoard);

            for(int i = 0; i < ways.Count; ++i)
            {
                Point way = ways.Data[i];
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                i++;

                int j = 0;
                for (j = 0; j < nowAgent; ++j)
                {
                    if (dp[0][j] == way)
                    {
                        break;
                    }
                }
                if (j != nowAgent) continue;

                SearchState newState = state.GetNextStateSingle(nowAgent, way, ScoreBoard);

                int res = NegaMax(deepness - 1, newState, alpha, count + 1, evaluator, ngMove, nextways, nowAgent);
                if (alpha < res)
                {
                    nextways[nowAgent] = way;
                    alpha = res;
                    dp[count] = nextways;
                }
            }

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