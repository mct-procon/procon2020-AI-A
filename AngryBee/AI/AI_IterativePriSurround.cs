using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;

namespace AngryBee.AI
{
    /// <summary>
    /// 反復深化法とminimax法を用いて、最高得点をとれるパターンを計算するAI。
    /// 評価関数の計算に[自軍の囲んでいる陣地数*50]を加えているので、ひたすら囲みを増やそうと動く。(はず)
    /// </summary>
    public class AI_IterativePriSurround
    {
        Rule.MovableChecker Checker = new Rule.MovableChecker();
        PointEvaluator.Normal PointEvaluator = new PointEvaluator.Normal();
        PointEvaluator.PrioritySurrond PointEvaluatorPriS = new PointEvaluator.PrioritySurrond();

        public int ends = 0;
        private System.Diagnostics.Stopwatch SearchTime;

        private class DP
        {
            public int score = int.MinValue;
            public VelocityPoint Ag1Way = (0, 0);
            public VelocityPoint Ag2Way = (0, 0);
        }
        private DP[] dp = new DP[100];
        private int cnt;

        public Decided Begin(int time, BoardSetting setting, ColoredBoardSmallBigger MeBoard, ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy)
        {
            VelocityPoint[] WayEnumerator = { (1, 1), (1, -1), (-1, 1), (-1, -1), (0, 1), (-1, 0), (1, 0), (0, -1) };

            for (int i = 1; i < 100; ++i)
            {
                dp[i] = new DP();
            }
            cnt = 1;

            return Search(time, WayEnumerator, MeBoard, EnemyBoard, Me, Enemy, setting.ScoreBoard);
        }


        Decided Search(int time, in VelocityPoint[] WayEnumerator, in ColoredBoardSmallBigger MeBoard, in ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy, in sbyte[,] ScoreBoard)
        {
            Decided BestWay = new Decided();
            SearchTime = System.Diagnostics.Stopwatch.StartNew();
            while (cnt<100)
            {
                Max(cnt, WayEnumerator, MeBoard, EnemyBoard, Me, Enemy, int.MinValue, int.MaxValue, ScoreBoard, time);
                if (time <= SearchTime.ElapsedMilliseconds) break;
                BestWay.MeAgent1 = dp[cnt].Ag1Way;
                BestWay.MeAgent2 = dp[cnt].Ag2Way;
                cnt++;
            }
            SearchTime.Stop();

            return BestWay;
        }

        int Max(int deepness, in VelocityPoint[] WayEnumerator, in ColoredBoardSmallBigger MeBoard, in ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy, int alpha, int beta, in sbyte[,] ScoreBoard, int time)
        {
            if (deepness == 0)
            {
                ends++;
                return PointEvaluator.Calculate(ScoreBoard, MeBoard, 0) - PointEvaluator.Calculate(ScoreBoard, EnemyBoard, 0);
            }
            if (SearchTime.ElapsedMilliseconds > time)
            {
                return 0;
            }

            int result = alpha;
            if (alpha == int.MinValue && dp[deepness].score != int.MinValue)
            {
                Player newMe = Me;
                newMe.Agent1 += dp[deepness].Ag1Way;
                newMe.Agent2 += dp[deepness].Ag2Way;
                var moveResult = Move(MeBoard, EnemyBoard, newMe, Enemy);

                if (moveResult != null)
                {
                    var newMeBoard = moveResult.Item1;
                    var newEnBoard = moveResult.Item2;
                    newMe = moveResult.Item3;
                    var newEnemy = moveResult.Item4;
                    result = Mini(deepness, WayEnumerator, newMeBoard, EnemyBoard, newMe, Enemy, result, beta, ScoreBoard, time);
                }

            }
            for (int i = 0; i < WayEnumerator.Length; ++i)
                for (int m = 0; m < WayEnumerator.Length; ++m)
                {

                    Player newMe = Me;
                    newMe.Agent1 += WayEnumerator[i];
                    newMe.Agent2 += WayEnumerator[m];

                    var moveResult = Move(MeBoard, EnemyBoard, newMe, Enemy);

                    if (moveResult == null) continue;

                    int cache = 0;
                    var newMeBoard = moveResult.Item1;
                    var newEnBoard = moveResult.Item2;
                    newMe = moveResult.Item3;
                    var newEnemy = moveResult.Item4;

                    cache = Mini(deepness, WayEnumerator, newMeBoard, newEnBoard, newMe, newEnemy, result, beta, ScoreBoard, time);

                    if (result < cache)
                    {
                        result = Math.Max(result, cache);
                        if (deepness == cnt)
                        {
                            Console.WriteLine("i=" + i.ToString() + ",m=" + m.ToString());
                            dp[deepness].score = result;
                            dp[deepness].Ag1Way = WayEnumerator[i];
                            dp[deepness].Ag2Way = WayEnumerator[m];
                        }
                    }

                    if (result >= beta)
                    {
                        return result;
                    }

                }
            return result;
        }

        int Mini(int deepness, in VelocityPoint[] WayEnumerator, in ColoredBoardSmallBigger MeBoard, in ColoredBoardSmallBigger EnemyBoard, in Player Me, in Player Enemy, int alpha, int beta, in sbyte[,] ScoreBoard, int time)
        {
            deepness--;
            if (SearchTime.ElapsedMilliseconds >= time)
            {
                return 0;
            }

            int result = beta;
            for (int i = 0; i < WayEnumerator.Length; ++i)
                for (int m = 0; m < WayEnumerator.Length; ++m)
                {
                    if (WayEnumerator[i] == WayEnumerator[m])
                        continue;

                    Player newEnemy = Enemy;
                    newEnemy.Agent1 += WayEnumerator[i];
                    newEnemy.Agent2 += WayEnumerator[m];

                    var moveResult = Move(EnemyBoard, MeBoard, newEnemy, Me);

                    if (moveResult == null) continue;

                    int cache = 0;

                    var newEnBoard = moveResult.Item1;
                    var newMeBoard = moveResult.Item2;
                    newEnemy = moveResult.Item3;
                    var newMe = moveResult.Item4;

                    cache = Max(deepness, WayEnumerator, newMeBoard, newEnBoard, Me, newEnemy, alpha, result, ScoreBoard, time);

                    result = Math.Min(result, cache);

                    if (result <= alpha)
                    {
                        return result;
                    }

                }

            return result;
        }

        Tuple<ColoredBoardSmallBigger, ColoredBoardSmallBigger, Player, Player> Move(ColoredBoardSmallBigger meBoard, ColoredBoardSmallBigger enemyBoard, Player me, Player enemy)
        {
            var movable = Checker.MovableCheck(meBoard, enemyBoard, me, enemy);

            if (!movable.IsMovable) return null;

            if (movable.IsEraseNeeded)
            {

                if (movable.Me1 == Rule.MovableResultType.EraseNeeded)
                {
                    enemyBoard[me.Agent1] = false;
                    me.Agent1 = me.Agent1;
                }
                else
                    meBoard[me.Agent1] = true;

                if (movable.Me2 == Rule.MovableResultType.EraseNeeded)
                {
                    enemyBoard[me.Agent2] = false;
                    me.Agent2 = me.Agent2;
                }
                else
                    meBoard[me.Agent2] = true;

            }
            else
            {
                meBoard[me.Agent1] = true;
                meBoard[me.Agent2] = true;
            }
            return new Tuple<ColoredBoardSmallBigger, ColoredBoardSmallBigger, Player, Player>(meBoard, enemyBoard, me, enemy);
        }
    }
}