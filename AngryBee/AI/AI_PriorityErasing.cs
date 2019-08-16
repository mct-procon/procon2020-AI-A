using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol.Methods;
using MCTProcon30Protocol;

namespace AngryBee.AI
{
    /// <summary>
    /// 深さを制限した深さ優先探索を用いて、最高得点をとれるパターンを計算するAI。
    /// タイルを除去するときの加点のみを3倍にして計算しているので、近くに敵のタイルがあれば、妨害を優先して動く。
    /// </summary>
    public class AI_PriorityErasing : MCTProcon30Protocol.AIFramework.AIBase
    {
        Rule.MovableChecker Checker = new Rule.MovableChecker();
        PointEvaluator.Normal PointEvaluator = new PointEvaluator.Normal();

        VelocityPoint[] WayEnumerator = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };

        bool[] isDesidedNextErase = new bool[2];

        public  int MaxDepth { get; set; }

        void SearchPriorityErase(int MaxDepth, in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Player Me, in Player Enemy, in sbyte[,] ScoreBoard)
        {
            SolverResult = new Decision();
            int maxScore = 0;
            for (int i = 0; i < WayEnumerator.Length; ++i)
                for (int m = 0; m < WayEnumerator.Length; ++m)
                {
                    
                    Player newMe = Me;
                    newMe.Agent1 += WayEnumerator[i];
                    newMe.Agent2 += WayEnumerator[m];

                    var movable = Checker.MovableCheck(MeBoard, EnemyBoard, Me, newMe, Enemy);

                    if (!movable.IsMovable) continue;

                    var newMeBoard = MeBoard;
                    var newEnBoard = EnemyBoard;
                    int score = 0;
                    if (movable.IsEraseNeeded)
                    {
                        if (movable.Me1 == Rule.MovableResultType.EraseNeeded)
                        {
                            newEnBoard[newMe.Agent1] = false;
                            score += ScoreBoard[newMe.Agent1.Y, newMe.Agent1.X] * 2;
                            //Console.WriteLine("yobareta");
                            newMe.Agent1 = Me.Agent1;
                        }
                        else
                            newMeBoard[newMe.Agent1] = true;

                        if (movable.Me2 == Rule.MovableResultType.EraseNeeded)
                        {
                            newEnBoard[newMe.Agent2] = false;
                            score += ScoreBoard[newMe.Agent2.Y, newMe.Agent2.X] * 2;
                            //Console.WriteLine("yobareta");
                            newMe.Agent2 = Me.Agent2;
                        }
                        else
                            newMeBoard[newMe.Agent2] = true;
                    }
                    else
                    {
                        newMeBoard[newMe.Agent1] = true;
                        newMeBoard[newMe.Agent2] = true;
                    }
                    score += Max(MaxDepth - 1, newMeBoard, newEnBoard, newMe, Enemy, ScoreBoard);
                    if (CancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("[SOLVER] Canceled");
                        return;
                    }
                    if (score > maxScore)
                    {
                        maxScore = score;
                        //Console.WriteLine("Max" + maxScore.ToString());
                        SolverResult.MeAgent1 = WayEnumerator[i];
                        SolverResult.MeAgent2 = WayEnumerator[m];
                    }
                }
        }

        int Max(int depth, in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Player Me, in Player Enemy, in sbyte[,] ScoreBoard)
        {
            if (depth == 0)
            {
                return PointEvaluator.Calculate(ScoreBoard, MeBoard, 0, Me, Enemy);
            }

            int result = 0;
            for (int i = 0; i < WayEnumerator.Length; ++i)
                for (int m = 0; m < WayEnumerator.Length; ++m)
                {
                    if (CancellationToken.IsCancellationRequested)
                        return -1000;
                    int score = 0;
                    Player newMe = Me;
                    newMe.Agent1 += WayEnumerator[i];
                    newMe.Agent2 += WayEnumerator[m];

                    var movable = Checker.MovableCheck(MeBoard, EnemyBoard, Me, newMe, Enemy);

                    if (!movable.IsMovable) continue;

                    var newMeBoard = MeBoard;
                    var newEnBoard = EnemyBoard;

                    if (movable.IsEraseNeeded)
                    {
                        if (movable.Me1 == Rule.MovableResultType.EraseNeeded)
                        {
                            newEnBoard[newMe.Agent1] = false;
                            score += ScoreBoard[newMe.Agent1.Y, newMe.Agent1.X] * 2;
                            //Console.WriteLine("yobareta");
                            newMe.Agent1 = Me.Agent1;
                        }
                        else
                            newMeBoard[newMe.Agent1] = true;

                        if (movable.Me2 == Rule.MovableResultType.EraseNeeded)
                        {
                            newEnBoard[newMe.Agent2] = false;
                            score += ScoreBoard[newMe.Agent2.Y, newMe.Agent2.X] * 2;
                            //Console.WriteLine("yobareta");
                            newMe.Agent2 = Me.Agent2;
                        }
                        else
                            newMeBoard[newMe.Agent2] = true;
                    }
                    else
                    {
                        newMeBoard[newMe.Agent1] = true;
                        newMeBoard[newMe.Agent2] = true;
                    }
                    score += Max(depth - 1, newMeBoard, newEnBoard, newMe, Enemy, ScoreBoard);
                    result = Math.Max(score, result);
                }
            return result;
        }

        //Decided Deside(in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Player Me, in Player Enemy)
        //{
        //    var decided = new Decided();
        //    //Agent1
        //    VelocityPoint agent1 = new VelocityPoint((int)erasePoint.Agent1.X - (int)Me.Agent1.X, (int)erasePoint.Agent1.Y - (int)Me.Agent1.Y);
        //    VelocityPoint agent2 = new VelocityPoint((int)erasePoint.Agent2.X - (int)Me.Agent2.X, (int)erasePoint.Agent2.Y - (int)Me.Agent2.Y);
        //    decided.MeAgent1 = CompressToLegal(agent1);
        //    decided.MeAgent1 = CompressToLegal(agent2);
        //    return decided;
        //}

        //Point SearchNextErase(in int AgentNum, in BoardSetting Setting, in int ErasingMinScore, in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Player Me, in Player Enemy, in sbyte[,] ScoreBoard)
        //{
        //    Point agent;
        //    if (AgentNum == 0)
        //    {
        //        agent = Me.Agent1;
        //    }
        //    else
        //    {
        //        agent = Me.Agent2;
        //    }
        //    var pointQueue = new Queue<Point>();
        //    var isVisited = new bool[Setting.Height, Setting.Width];
        //    pointQueue.Enqueue(agent);
        //    isVisited[agent.Y, agent.Y] = true;
        //    while (pointQueue.Count > 0)
        //    {
        //        var nowAgent = pointQueue.Dequeue();
        //        for (int i = 0; i < WayEnumerator.Length; i++)
        //        {
        //            var movable = Checker.MovableCheck(MeBoard, EnemyBoard, , Enemy);

        //            if (!movable.IsMovable) continue;
        //            var newAgent = nowAgent;
        //            newAgent += WayEnumerator[i];
        //            if (isVisited[newAgent.Y, newAgent.X]) continue;
        //            isVisited[newAgent.Y, newAgent.X] = true;
        //            if (EnemyBoard[newAgent] && ScoreBoard[newAgent.Y, newAgent.X] >= ErasingMinScore)
        //            {
        //                return newAgent;
        //            }
        //            pointQueue.Enqueue(newAgent);
        //        }
        //    }
        //    return new Point(114, 514);
        //}

        VelocityPoint CompressToLegal(VelocityPoint velocityPoint)
        {
            velocityPoint.X = 0.CompareTo(velocityPoint.X);
            velocityPoint.Y = 0.CompareTo(velocityPoint.Y);
            return velocityPoint;
        }

        protected override void EndGame(GameEnd end)
        {
        }

        protected override void Solve()
        {
            SearchPriorityErase(MaxDepth, MyBoard, EnemyBoard, new Player(MyAgent1, MyAgent2), new Player(EnemyAgent1, EnemyAgent2), ScoreBoard);
        }

    }
}
