using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using AngryBee.Search;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace AngryBee.AI
{
    public class SingleAgentMTAI : MCTProcon31Protocol.AIFramework.AIBase
    {
        PointEvaluator.Base PointEvaluator_Dispersion = new PointEvaluator.Dispersion();
        PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();
        ResultComparator1 resultComparator = new ResultComparator1();

        private Decision lastTurnDecided = null;		//1ターン前に「実際に」打った手（競合していた場合, 競合手==lastTurnDecidedとなる。競合していない場合は, この変数は探索に使用されない）
        public int StartDepth { get; set; } = 1;
        private Unsafe16Array<AgentState> agentStateAry = new Unsafe16Array<AgentState>();

        private int DispatchedThreads = 0;
        private int CreationThreads = 0;

        private static Random rng = new Random();

        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public SingleAgentMTAI(int creationThreads, int startDepth = 1)
        {
            StartDepth = startDepth;
            for (int i = 0; i < 16; ++i)
                agentStateAry[i] = AgentState.Move;
            CreationThreads = creationThreads;
        }

        Unsafe16Array<Point> SearchFirstPlace()
        {
            Unsafe16Array<Point> newMyAgents = MyAgents;

            //均等に配置する
            //平方根を用いていろいろする
            uint xn = (uint)Math.Sqrt(AgentsCount);
            uint yn = (uint)AgentsCount / xn;
            uint ratio = (uint)(ScoreBoard.GetLength(0) / ScoreBoard.GetLength(1)); //縦横の比率
            if (ScoreBoard.GetLength(0) > ScoreBoard.GetLength(1))
            {
                uint tmp = xn;
                xn = yn;
                yn = tmp;
            }
            uint left = (uint)(AgentsCount - (xn * yn)); //AgentsCountが7の時など、エージェントが余ってしまうときに
            uint spaceX = (uint)ScoreBoard.GetLength(0) / (xn * ratio);
            uint spaceY = (uint)ScoreBoard.GetLength(1) / yn;
            for (byte i = 0; i < AgentsCount - left;)
            {
                uint baseX = spaceX * (i % xn) + spaceX / 2, baseY = spaceY * (i / xn) + spaceY / 2;
                if (MyAgentsState[i] == AgentState.Move) goto next;
                for(byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[(baseX + j) * ratio, baseY] >= 0 && !EnemyBoard[(baseX + j) * ratio, baseY])
                    {
                        newMyAgents[i] = new Point((byte)((baseX + j) * ratio), (byte)baseY);
                        goto next;
                    }
                }
                for (byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[(baseX - j) * ratio, baseY] >= 0 && !EnemyBoard[(baseX - j) * ratio, baseY])
                    {
                        newMyAgents[i] = new Point((byte)((baseX - j) * ratio), (byte)baseY);
                        goto next;
                    }
                }
                for (byte j = 0; j < spaceY / 2; j++)
                {
                    if (ScoreBoard[baseX * ratio, baseY + j] >= 0 && !EnemyBoard[baseX * ratio, baseY + j])
                    {
                        newMyAgents[i] = new Point((byte)(baseX * ratio), (byte)(baseY + j));
                        goto next;
                    }
                }
                for (byte j = 0; j < spaceX / 2; j++)
                {
                    if (ScoreBoard[baseX * ratio, baseY - j] >= 0 && !EnemyBoard[baseX * ratio, baseY - j])
                    {
                        newMyAgents[i] = new Point((byte)(baseX * ratio), (byte)(baseY - j));
                        goto next;
                    }
                }
                newMyAgents[i] = new Point((byte)(baseX * ratio), (byte)baseY);
                next:
                ++i;
            }

            if (left == 0) return newMyAgents;
            int cur = AgentsCount - (int)left - 1;
            for(;; ++cur)
            {
                if (cur >= AgentsCount) return newMyAgents;
                if (MyAgentsState[cur] == AgentState.Move) continue;
                break;
            }
            Random rand = new Random();
            List<Point> recommends = new List<Point>();
            //余ったエージェントの配置
            // 10点より高いところに配置する
            for (byte x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (byte y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= 10 && !EnemyBoard[x, y])
                        recommends.Add(new Point(x, y));

            foreach (var p in recommends.OrderBy(i => rand.Next()))
            {
                do
                {
                    cur++;
                    if (cur >= AgentsCount) return newMyAgents;
                } while (MyAgentsState[cur] == AgentState.Move);
                newMyAgents[cur] = p;
            }

            //まだ余っていたら
            for (byte x = 0; x < ScoreBoard.GetLength(0); ++x)
                for (byte y = 0; y < ScoreBoard.GetLength(1); ++y)
                    if (ScoreBoard[x, y] >= -1 && !EnemyBoard[x, y])
                        recommends.Add(new Point(x, y));

            foreach (var p in recommends.OrderBy(i => rand.Next()))
            {
                do
                {
                    cur++;
                    if (cur >= AgentsCount) return newMyAgents;
                } while (MyAgentsState[cur] == AgentState.Move);
                newMyAgents[cur] = p;
            }
            return newMyAgents;
        }

        protected Unsafe16Array<Point> GetResult((int, CalculationNode)[] calculationResult, Unsafe16Array<Point> res, Decision lastTurnDecided)
        {
            Array.Sort(calculationResult, resultComparator);
            uint flag = 0;
            for (int i = 0; i < calculationResult.Length; ++i)
            {
            skipstart:
                int agent = calculationResult[i].Item1;
                if (calculationResult[i].Item2 == null || calculationResult[i].Item2.Children.Count == 0)
                    goto success;
                var dest = calculationResult[i].Item2.Children[0].nextWay;
                if (!(lastTurnDecided is null) && !IsAgentsMoved[agent] && dest == lastTurnDecided.Agents[agent])
                    goto restart;
                for (int p = 0; p < AgentsCount; ++p)
                    if ((flag & (1 << p)) != 0 && res[p] == dest)
                        goto restart;
                res[agent] = dest;
                goto success;
            restart:
                var children = calculationResult[i].Item2.Children;
                if (children.Count == 1 || children[1].Result == int.MinValue)
                    goto success;
                calculationResult[i].Item2.Result = children[1].Result;
                children[0].Result = int.MinValue;
                for (int j = 1; j < children.Count; ++j)
                    children[j - 1] = children[j];
                for (int j = i + 1; j < calculationResult.Length; ++j)
                {
                    if (calculationResult[j - 1].Item2.Result > calculationResult[j].Item2.Result) break;
                    var tmp = calculationResult[j - 1];
                    calculationResult[j - 1] = calculationResult[j];
                    calculationResult[j] = tmp;
                }
                goto skipstart;
            success:
                flag |= 1u << agent;
            }
            if (this.lastTurnDecided is null) return res;
            for (int i = 0; i < AgentsCount; ++i)
                Log($"[GetResult]{i} : {MyAgents[i]} -> {res[i]}");
            return res;
        }
        protected override void Solve()
        {
            int Wait(CalculationNode node)
            {
                if (node == null) return -100000000;
                if (node.Children == null)
                {
                    node.CalculationTask.Wait();
                    return node.Result;
                }
                int res = 0;
                foreach (var n in node.Children)
                    res = Math.Max(res, Wait(n));
                return res;
            };
            var myAgents = SearchFirstPlace();

            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) + 1;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_Dispersion;
            SearchState state = new SearchState(MyBoard, EnemyBoard, myAgents, EnemyAgents, MySurroundedBoard, EnemySurroundedBoard);
            int score = 0;
            for (uint x = 0; x < ScoreBoard.GetLength(0); ++x)
                for(uint y = 0; y < ScoreBoard.GetLength(1); ++y)
                {
                    if (MyBoard[x, y])
                        score += ScoreBoard[x, y];
                    else if (MySurroundedBoard[x, y])
                        score += Math.Abs(ScoreBoard[x, y]);
                    else if (EnemyBoard[x, y])
                        score -= ScoreBoard[x, y];
                    else if (EnemySurroundedBoard[x, y])
                        score -= Math.Abs(ScoreBoard[x, y]);
                }

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

                (int, CalculationNode)[] result = new (int, CalculationNode)[AgentsCount];
                DispatchedThreads = 0;
                //普通にNegaMaxをして、最善手を探す
                for(int agent = 0; agent < AgentsCount; ++agent)
                {
                    if (MyAgentsState[agent] == AgentState.NonPlaced)
                        result[agent] = (agent, new CalculationNode() { Result = int.MaxValue, Children = new List<CalculationNode>()});
                    else
                    {
                        result[agent] = (agent, new CalculationNode());
                        CalcStart(deepness, state, int.MinValue + 1, 0, evaluator, agent, AgentsCount * 8, result[agent].Item2);
                    }
                }
                Log("[SOLVER/MT]Dispatched {0} Thread(s).", DispatchedThreads);
                if (CancellationToken.IsCancellationRequested) return;
                for (int i = 0; i < result.Length; ++i)
                {
                    Wait(result[i].Item2);
                    var children = result[i].Item2.Children;
                    if ((children is null) || children.Count == 0)
                    {
                        result[i].Item2.Result = int.MaxValue;
                        continue;
                    }
                    Shuffle(children);
                    children.Sort(resultComparator);
                    result[i].Item2.Result = children[0].Result;
                }
                if (CancellationToken.IsCancellationRequested) return;
                resultList.Add(new Decision((byte)AgentsCount, GetResult(result, myAgents, score <= 0 ? lastTurnDecided : null), agentStateAry));
                SolverResultList = resultList;
                Log("[SOLVER] SolverResultList.Count = {0}, score = {1}", SolverResultList.Count, score);
                if (CancellationToken.IsCancellationRequested) return;
                Log("[SOLVER] deepness = {0}", deepness);
            }
        }

        protected override void EndSolve()
        {
            base.EndSolve();
            if (SolverResultList.Count == 0) return;
            lastTurnDecided = SolverResultList[0];  //0番目の手を指したとする。（次善手を人間が選んで競合した～ということがなければOK）
            //for (int agent = 0; agent < AgentsCount; ++agent)
            //{
            //    StringBuilder sb = new StringBuilder();
            //    sb.Append($"AGENT {agent}: {MyAgents[agent]} ->");
            //    for (int i = 0; i < 10; ++i)
            //    {
            //        sb.Append("  ");
            //        sb.Append(dp1[i][agent]);
            //    }
            //    Log(sb.ToString());
            //}
        }
        private void CalcStart(int deepness, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, int nowAgent, int parentThreads, CalculationNode parent)
        {
            if (CancellationToken.IsCancellationRequested == true) return;    //何を返しても良いのでとにかく返す
            
            SingleAgentWays ways = state.MakeMovesSingle(AgentsCount, nowAgent, ScoreBoard);
            for(int i = 0; i < ways.Count; ++i)
            {
                var way = ways.Data[i];
                SearchState newState = state.GetNextStateSingle(nowAgent, way, ScoreBoard, deepness);
                if (parentThreads * 8 / 2 > CreationThreads || deepness == 1)
                {
                    var copied_scoreBoard = new sbyte[ScoreBoard.GetLength(0), ScoreBoard.GetLength(1)];
                    Array.Copy(ScoreBoard, copied_scoreBoard, ScoreBoard.Length);
                    CalculationNode currentNode = new CalculationNode(deepness - 1, way, newState, alpha, count + 1, evaluator, nowAgent, CancellationToken, AgentsCount, copied_scoreBoard);
                    parent.Children.Add(currentNode);
                    DispatchedThreads++;
                }
                else
                {
                    CalculationNode currentNode = new CalculationNode(way, newState);
                    parent.Children.Add(currentNode);
                    CalcStart(deepness - 1, newState, alpha, count + 1, evaluator, nowAgent, parentThreads * 8, currentNode);
                }
            }
            //Log("NODES : {0} nodes, elasped {1} ", i, sw.Elapsed);
            ways.End();
        }

        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 500;
        }

        protected override void EndGame(GameEnd end)
        {
        }

        public class ResultComparator1 : IComparer<(int, CalculationNode)>, IComparer<CalculationNode>
        {
            int IComparer<(int, CalculationNode)>.Compare((int, CalculationNode) x, (int, CalculationNode) y)
                => ((IComparer<CalculationNode>)this).Compare(x.Item2, y.Item2);

            int IComparer<CalculationNode>.Compare(CalculationNode x, CalculationNode y)
            {
                if (x is null) return 1;
                if (y is null) return -1;
                return y.Result - x.Result;
            }
        }

        public class CalculationNode
        {
            public List<CalculationNode> Children;
            public Task CalculationTask;
            public int deepness;
            public SearchState state;
            public int alpha;
            public int count;
            public PointEvaluator.Base evaluator;
            public int nowAgent;
            public CancellationToken cancellationToken;
            public int agentsCount;
            public sbyte[,] scoreBoard;
            public Point nextWay;

            public int Result;

            public CalculationNode()
            {
                Children = new List<CalculationNode>();
            }

            public CalculationNode(Point nextWay, SearchState state)
            {
                this.nextWay = nextWay;
                this.state = state;
                Children = new List<CalculationNode>();
            }

            public CalculationNode(int deepness, Point nextWay, SearchState state, int alpha, int count, PointEvaluator.Base evaluator, int nowAgent, CancellationToken cancellationToken, int agentsCount, sbyte[,] scoreBoard)
            {
                this.deepness = deepness;
                this.nextWay = nextWay;
                this.state = state;
                this.alpha = alpha;
                this.count = count;
                this.evaluator = evaluator;
                this.nowAgent = nowAgent;
                this.cancellationToken = cancellationToken;
                this.agentsCount = agentsCount;
                this.scoreBoard = scoreBoard;
                CalculationTask = Task.Run(this.Calc);
            }


            public int Calc(int deepness, SearchState state, int alpha, int count)
            {
                if (deepness == 0)
                    return evaluator.Calculate(scoreBoard, state, count);
                if (cancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
                SingleAgentWays ways = state.MakeMovesSingle(agentsCount, nowAgent, scoreBoard);

                for (int i = 0; i < ways.Count; ++i)
                {
                    var way = ways.Data[i];

                    SearchState newState = state.GetNextStateSingle(nowAgent, way, scoreBoard, deepness);

                    int res = Calc(deepness - 1, newState, alpha, count + 1);
                    if (alpha < res)
                        alpha = res;
                }

                //Log("NODES : {0} nodes, elasped {1} ", i, sw.Elapsed);
                ways.End();
                return alpha;
            }

            public void Calc()
            {
                if (deepness == 0)
                {
                    Result = evaluator.Calculate(scoreBoard, state, count);
                    return;
                }
                if (cancellationToken.IsCancellationRequested == true) { Result = alpha; return; }    //何を返しても良いのでとにかく返す
                SingleAgentWays ways = state.MakeMovesSingle(agentsCount, nowAgent, scoreBoard);

                for (int i = 0; i < ways.Count; ++i)
                {
                    var way = ways.Data[i];

                    SearchState newState = state.GetNextStateSingle(nowAgent, way, scoreBoard, deepness);

                    int res = Calc(deepness - 1, newState, alpha, count + 1);
                    if (alpha < res)
                        alpha = res;
                }

                //Log("NODES : {0} nodes, elasped {1} ", i, sw.Elapsed);
                ways.End();
                Result = alpha;
            }
        }
    }
}