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
    public class AokiAI_8 : MCTProcon31Protocol.AIFramework.AIBase
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
        private DP[] dp1 = new DP[50];  //dp1[i] = 深さi時点での最善手
        private DP[] dp2 = new DP[50];  //dp2[i] = 競合手を指さないとしたときの, 深さi時点での最善手
        private Decision lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）
        public int StartDepth { get; set; } = 1;

        public AokiAI_8(int startDepth = 0)
        {
            for (int i = 0; i < 50; ++i)
            {
                dp1[i] = new DP();
                dp2[i] = new DP();
            }
            StartDepth = startDepth;
        }


        protected override void Solve()
        {
            for (int i = 0; i < 50; ++i)
            {
                dp1[i].Score = int.MinValue;
                dp2[i].Score = int.MinValue;
                dp1[i].Ways = new Unsafe16Array<Way>();
                dp2[i].Ways = new Unsafe16Array<Way>();
            }

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, MyAgents, EnemyAgents);
            int score = PointEvaluator_Normal.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - PointEvaluator_Normal.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);

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
                    Unsafe16Array<Way> nextways = dp1[0].Ways;
                    NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, null, nextways, agent);
                }
                Decision best1 = new Decision((byte)AgentsCount, Unsafe16Array<VelocityPoint>.Create(dp1[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
                resultList.Add(best1);
                //競合手.Agent1 == 最善手.Agent1 && 競合手.Agent2 == 最善手.Agent2になった場合、競合手をngMoveとして探索をおこない、最善手を探す
                for (int i = 0; i < AgentsCount; ++i)
                {
                    if (IsAgentsMoved[i] || !lastTurnDecided.Agents[i].Equals(best1.Agents[i]))
                    {
                        break;
                    }
                    if (i < AgentsCount - 1) continue;

                    for (int agent = 0; agent < AgentsCount; ++agent)
                    {
                        Unsafe16Array<Way> nextways = dp2[0].Ways;
                        NegaMax(deepness, state, int.MinValue + 1, 0, evaluator, best1, nextways, agent);
                    }
                    Decision best2 = new Decision((byte)AgentsCount, Unsafe16Array<VelocityPoint>.Create(dp2[0].Ways.GetEnumerable(AgentsCount).Select(x => x.Direction).ToArray()));
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
            lastTurnDecided = SolverResultList[0];  //0番目の手を指したとする。（次善手を人間が選んで競合した～ということがなければOK）
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        //NegaMaxではない
        private int NegaMax(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, Decision ngMove, Unsafe16Array<Way> nextways, int nowAgent)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (deepness == 0)
            {
                return evaluator.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);
            }

            Ways ways = state.MakeMoves(AgentsCount, ScoreBoard);

            int i = 0;
            foreach (var way in ways.Data[nowAgent])
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                if (way.Direction == new VelocityPoint()) continue;
                i++;
                if (count == 0 && !(ngMove is null))    //競合手とは違う手を指す
                {
                    if (!way.Equals(ngMove.Agents[nowAgent])) continue;
                }
                //自エージェントとの衝突を防ぐ（後を行くのも防ぐ）
                if (count == 0) {
                    int j = 0;
                    for (j = 0; j < nowAgent; ++j)
                    {
                        if (ngMove is null)
                        {
                            if (dp1[0].Ways[j].Locate == way.Locate)
                                break;
                        }
                        else
                        {
                            if(dp2[0].Ways[j].Locate == way.Locate)
                                break;
                        }
                    }
                    if (j != nowAgent) continue;

                    for(j = 0; j < AgentsCount; ++j)
                    {
                        if (j == nowAgent) continue;
                        if (way.Locate == state.Me[j]) break;
                    }
                    if (j != AgentsCount) continue;
                }

                Unsafe16Array<Way> newways = new Unsafe16Array<Way>();
                newways[nowAgent] = way;
                SearchState backup = state;
                state = state.GetNextState(AgentsCount, newways);
                
                int res = NegaMax(deepness - 1, state, alpha, count + 1, evaluator, ngMove, nextways, nowAgent);
                if (alpha < res)
                {
                    nextways[nowAgent] = way;
                    alpha = res;
                    if (ngMove is null) { dp1[count].UpdateScore(alpha, nextways); }
                    else { dp2[count].UpdateScore(alpha, nextways); }
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
            return miliseconds - 1000;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}