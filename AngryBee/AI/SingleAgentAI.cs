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

        private Unsafe16Array<Way>[] dp1 = new Unsafe16Array<Way>[50];  //dp1[i] = 深さi時点での最善手
        private Unsafe16Array<Way>[] dp2 = new Unsafe16Array<Way>[50];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
        private Decision lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）
        public int StartDepth { get; set; } = 1;
        private Unsafe16Array<AgentState> agentStateAry = new Unsafe16Array<AgentState>();

        public SingleAgentAI(int startDepth = 0, int greedyMaxDepth = 0)
        {
            for (int i = 0; i < 50; ++i)
            {
                dp1[i] = new Unsafe16Array<Way>();
                dp2[i] = new Unsafe16Array<Way>();
            }
            StartDepth = startDepth;
            for (int i = 0; i < 16; ++i)
                agentStateAry[i] = AgentState.Move;
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
            for (int x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (int y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= 10)
                        recommends.Add(new Point((byte)x,(byte)y));

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
            for (int i = 0; i < 50; ++i)
            {
                dp1[i] = new Unsafe16Array<Way>();
                dp2[i] = new Unsafe16Array<Way>();
            }

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, myAgents, EnemyAgents, MySurroundedBoard, EnemySurroundedBoard);
            int score = PointEvaluator_Normal.Calculate(ScoreBoard, state.MeBoard, state.EnemyBoard, 0, state.Me, state.Enemy, state.MeSurroundBoard, state.EnemySurroundBoard) - PointEvaluator_Normal.Calculate(ScoreBoard, state.EnemyBoard, state.MeBoard, 0, state.Enemy, state.Me, state.EnemySurroundBoard, state.MeSurroundBoard);

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
                    Unsafe16Array<Way> nextways = dp1[0];
                    NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, nextways, agent, deepness);
                }
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
                        Unsafe16Array<Way> nextways = dp2[0];
                        NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, best1, nextways, agent, deepness);
                    }
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
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, Unsafe16Array<Way> nextways, int nowAgent, int watch_deepness)
        {
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, state.EnemyBoard, 0, state.Me, state.Enemy, state.MeSurroundBoard, state.EnemySurroundBoard) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, state.MeBoard, 0, state.Enemy, state.Me, state.EnemySurroundBoard, state.MeSurroundBoard);
            }

            SingleAgentWays ways = state.MakeMovesSingle(AgentsCount, nowAgent, ScoreBoard);

            int i = 0;
            foreach (var way in ways)
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                i++;
                if (count == 0 && !(ngMove is null))    //競合手とは違う手を指す
                {
                    if (!way.Equals(ngMove.Agents[nowAgent])) continue;
                }
                //自エージェントとの衝突を防ぐ
                int j = 0;
                for (j = 0; j < nowAgent; ++j)
                {
                    int k;
                    for (k = 0; k < watch_deepness; ++k)
                    {
                        if (ngMove is null && dp1[k][j].Locate == way.Locate)
                            break;
                        if (!(ngMove is null) && dp2[k][j].Locate == way.Locate)
                            break;
                    }
                    if (k != watch_deepness) break;
                }
                if (j != nowAgent) continue;
                for (j = 0; j < AgentsCount; ++j)
                {
                    if (j == nowAgent) continue;
                    if (way.Locate == state.Me[j]) break;
                }
                if (j != AgentsCount) continue;
                

                SearchState newState = state.GetNextStateSingle(nowAgent, way);

                int res = NegaMax(deepness - 1, newState, alpha, count + 1, evaluator, ngMove, nextways, nowAgent, watch_deepness);
                if (alpha < res)
                {
                    nextways[nowAgent] = way;
                    alpha = res;
                    if (ngMove is null) { dp1[count] = nextways; }
                    else { dp2[count] = nextways; }
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