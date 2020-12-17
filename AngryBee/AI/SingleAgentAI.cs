using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using AngryBee.Search;
using System.Linq;

namespace AngryBee.AI
{
    public class SingleAgentAI : MCTProcon31Protocol.AIFramework.AIBase
    {
        PointEvaluator.Base PointEvaluator_Dispersion = new PointEvaluator.Dispersion();
        PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();

        private Unsafe16Array<Way>[] dp1prev = new Unsafe16Array<Way>[102];  //dp1[i] = 深さi時点での最善手
        private Unsafe16Array<Way>[] dp1 = new Unsafe16Array<Way>[102];  //dp1[i] = 深さi時点での最善手
        private Unsafe16Array<Way>[] dp2prev = new Unsafe16Array<Way>[102];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
        private Unsafe16Array<Way>[] dp2 = new Unsafe16Array<Way>[102];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
        private Decision lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）
        public int StartDepth { get; set; } = 1;
        private Unsafe16Array<AgentState> agentStateAry = new Unsafe16Array<AgentState>();

        public SingleAgentAI(int startDepth = 0)
        {
            StartDepth = startDepth;
            for (int i = 0; i < 16; ++i)
                agentStateAry[i] = AgentState.Move;
            Array.Clear(dp1, 0, dp1.Length);
            Array.Clear(dp2, 0, dp2.Length);
            Array.Clear(dp1prev, 0, dp1prev.Length);
            Array.Clear(dp2prev, 0, dp2prev.Length);
        }

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


            //均等に配置する
            //平方根を用いていろいろする
            int xn = (int)Math.Sqrt(AgentsCount);
            int yn = AgentsCount / xn;
            int ratio = ScoreBoard.GetLength(0) / ScoreBoard.GetLength(1); //縦横の比率
            if (ScoreBoard.GetLength(0) > ScoreBoard.GetLength(1))
            {
                int tmp = xn;
                xn = yn;
                yn = tmp;
            }
            int left = AgentsCount - (xn * yn); //AgentsCountが7の時など、エージェントが余ってしまうときに
            int spaceX = ScoreBoard.GetLength(0) / (xn * ratio);
            int spaceY = ScoreBoard.GetLength(1) / yn;
            int flag = 0;
            for (byte i = 0; i < AgentsCount - left; i++)
            {
                for(byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[(spaceX * (i % xn) + spaceX / 2 + j) * ratio, spaceY * (i / xn) + spaceY / 2] >= 0)
                    {
                        recommends.Add(new Point((byte)((spaceX * (i % xn) + spaceX / 2 + j)*ratio), (byte)(spaceY * (i / xn) + spaceY / 2)));
                        flag = 1;
                        break;
                    }
                }
                for (byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[(spaceX * (i % xn) + spaceX / 2 - j) * ratio, spaceY * (i / xn) + spaceY / 2] >= 0 && flag == 0)
                    {
                        recommends.Add(new Point((byte)((spaceX * (i % xn) + spaceX / 2 - j) * ratio), (byte)(spaceY * (i / xn) + spaceY / 2)));
                        flag = 1;
                        break;
                    }
                }
                for (byte j = 0; j < spaceY / 2; j++)
                {
                    if (ScoreBoard[(spaceX * (i % xn) + spaceX / 2) * ratio, spaceY * (i / xn) + spaceY / 2 + j] >= 0 && flag == 0)
                    {
                        recommends.Add(new Point((byte)((spaceX * (i % xn) + spaceX / 2) * ratio), (byte)(spaceY * (i / xn) + spaceY / 2 + j)));
                        flag = 1;
                        break;
                    }
                }
                for (byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[(spaceX * (i % xn) + spaceX / 2) * ratio, spaceY * (i / xn) + spaceY / 2 - j] >= 0 && flag == 0)
                    {
                        recommends.Add(new Point((byte)((spaceX * (i % xn) + spaceX / 2) * ratio), (byte)(spaceY * (i / xn) + spaceY / 2 - j)));
                        flag = 1;
                        break;
                    }
                }
                if (flag == 0)
                    recommends.Add(new Point((byte)((spaceX * (i % xn) + spaceX / 2) * ratio), (byte)(spaceY * (i / xn) + spaceY / 2)));
            }

            //余ったエージェントの配置
            // 10点より高いところに配置する
            byte num = 0;
            for (int x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (int y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= 0 && num < left)
                    {
                        recommends.Add(new Point((byte)x, (byte)y));
                        num++;
                    }

            //まだ余っていたら
            for (int x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (int y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= -1 && num < left)
                    {
                        recommends.Add(new Point((byte)x, (byte)y));
                        num++;
                    }

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

        protected override void Solve()
        {
            var myAgents = SearchFirstPlace();
            {
                var tmp = dp1;
                dp1 = dp1prev;
                dp1prev = tmp;
            }
            {
                var tmp = dp2;
                dp2 = dp2prev;
                dp2prev = tmp;
            }
            Array.Clear(dp1, 0, dp1.Length);
            Array.Clear(dp2, 0, dp2.Length);

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, myAgents, EnemyAgents, MySurroundedBoard, EnemySurroundedBoard);
            int score = PointEvaluator_Normal.Calculate(ScoreBoard, state, 0);

            Log("TurnCount = {0}, CurrentTurn = {1}", TurnCount, CurrentTurn);
            //if (!(lastTurnDecided is null)) Log("IsAgent1Moved = {0}, IsAgent2Moved = {1}, lastTurnDecided = {2}", IsAgent1Moved, IsAgent2Moved, lastTurnDecided);

            if (!(lastTurnDecided is null) && score > 0)    //勝っている状態で競合していたら
            {
                int i;
                for (i = 0; i < AgentsCount; ++i)
                {
                    if (IsAgentsMoved[i]) break;
                }
                if (i == AgentsCount)
                {
                    SolverResultList.Add(lastTurnDecided);
                    return;
                }
            }
            for (; deepness <= maxDepth; deepness++)
            {
                Decided resultList = new Decided();

                //普通にNegaMaxをして、最善手を探す
                for (int agent = 0; agent < AgentsCount; ++agent)
                {
                    if (MyAgentsState[agent] == AgentState.NonPlaced) continue;
                    NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, agent);
                }
                for (int agent = 0; agent < AgentsCount; ++agent)
                    if(dp1[1][agent].Locate == dp1prev[1][agent].Locate)
                        for(int i = 1; i <= deepness; ++i)
                            dp1[i - 1][agent] = dp1[i][agent];
                var res = Unsafe16Array.Create(dp1[0].GetEnumerable(AgentsCount).Select(x => x.Locate).ToArray());
                for (int agent = 0; agent < AgentsCount; ++agent)
                    if (MyAgentsState[agent] == AgentState.NonPlaced) res[agent] = myAgents[agent];
                Decision best1 = new Decision((byte)AgentsCount, res, agentStateAry);
                resultList.Add(best1);
                //競合手.Agent1 == 最善手.Agent1 && 競合手.Agent2 == 最善手.Agent2になった場合、競合手をngMoveとして探索をおこない、最善手を探す
                for (int i = 0; i < AgentsCount; ++i)
                {
                    if (IsAgentsMoved[i] || (!(lastTurnDecided is null) && lastTurnDecided.Agents[i] != best1.Agents[i]))
                        break;
                    if (i < AgentsCount - 1) continue;

                    for (int agent = 0; agent < AgentsCount; ++agent)
                    {
                        if (MyAgentsState[agent] == AgentState.NonPlaced) continue;
                        NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, best1, agent);
                    }
                    for (int agent = 0; agent < AgentsCount; ++agent)
                        if (dp2[1][agent].Locate == dp2prev[1][agent].Locate)
                            for (int iii = 1; iii <= deepness; ++iii)
                                dp2[iii - 1][agent] = dp2[iii][agent];
                    res = Unsafe16Array.Create(dp2[0].GetEnumerable(AgentsCount).Select(x => x.Locate).ToArray());
                    for (int agent = 0; agent < AgentsCount; ++agent)
                        if (MyAgentsState[agent] == AgentState.NonPlaced) res[agent] = myAgents[agent];
                    Decision best2 = new Decision((byte)AgentsCount, res, agentStateAry);
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

        protected override void EndSolve()
        {
            base.EndSolve();
            if (SolverResultList.Count == 0) return;
            lastTurnDecided = SolverResultList[0];  //0番目の手を指したとする。（次善手を人間が選んで競合した～ということがなければOK）
            for (int agent = 0; agent < AgentsCount; ++agent)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"AGENT {agent}:");
                for (int i = 0; i < 10; ++i)
                {
                    sb.Append("  ");
                    sb.Append(dp1[i][agent].Locate);
                }
                Log(sb.ToString());
            }
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, int nowAgent)
        {
            if (deepness == 0)
                return evaluator.Calculate(ScoreBoard, state, 0);
            if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
            SingleAgentWays ways = state.MakeMovesSingle(AgentsCount, nowAgent, ScoreBoard);

            for(int i = 0; i < ways.ActualCount; ++i)
            {
                var way = ways.Data[i];
                if (count == 0 && !(ngMove is null))    //競合手とは違う手を指す
                    if (way.Locate == ngMove.Agents[nowAgent]) continue;
              
                SearchState newState = state.GetNextStateSingle(nowAgent, way, ScoreBoard, deepness);

                //自エージェントとの衝突を防ぐ
                if (count == 0)
                {
                    int j = 0;
                    for (j = 0; j < AgentsCount; ++j)
                    {
                        if (j == nowAgent) continue;
                        if (ngMove is null && dp1[count][j].Locate == way.Locate)
                            break;
                        if (!(ngMove is null) && dp2[count][j].Locate == way.Locate)
                            break;
                    }
                    if (j != AgentsCount) continue;
                }
                int res = NegaMax(deepness - 1, newState, alpha, count + 1, evaluator, ngMove, nowAgent);
                if (alpha < res)
                {
                    alpha = res;
                    if (ngMove is null) { dp1[count][nowAgent] = way; }
                    else { dp2[count][nowAgent] = way; }
                }
            }

            //Log("NODES : {0} nodes, elasped {1} ", i, sw.Elapsed);
            ways.End();
            return alpha;
        }

        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 500;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}