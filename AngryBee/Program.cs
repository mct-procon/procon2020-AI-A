﻿using System;
using MCTProcon31Protocol;
using MCTProcon31Protocol.Methods;

namespace AngryBee
{
    public static class Program
    {
        public static void Main(string[] args)
		{
			int aiType = 0;
			Console.WriteLine("AIの種類を入力(3:Aho_8, 4:SA_AI, 5:SA_AI(MT)");
			aiType = int.Parse(Console.ReadLine());
			MCTProcon31Protocol.AIFramework.AIBase AI = null;

			//if (aiType == 1) {
   //             int greedyMaxDepth = 4;
   ////             Console.WriteLine("探索延長の最大手数(推奨値 = 4)");
   ////             greedyMaxDepth = int.Parse(Console.ReadLine());
   //             AI = new AI.NaottiAI(1, greedyMaxDepth);
   //         }
            //if (aiType == 2)
            //{
            //    AI = new AI.AokiAI_8(1);
            //}
            if(aiType == 3)
            {
                AI = new AI.AhoAI_8();
            }
            if(aiType == 4)
            {
                AI = new AI.SingleAgentAI(1);
            }
            if (aiType == 5)
            {
                Console.WriteLine("Type threads count.(0 or empty = Processor Count).");
                var str = Console.ReadLine();
                var threads = string.IsNullOrWhiteSpace(str) || int.Parse(str) == 0 ? Environment.ProcessorCount : int.Parse(str);
                AI = new AI.SingleAgentMTAI(threads);
            }

            int portId;

            Console.CancelKeyPress +=
                (o, e) =>
                {
                    AI.End();
                    Environment.Exit(0);
                };

            Console.WriteLine("ポート番号を入力（先手15000, 後手15001)＞");
            portId = int.Parse(Console.ReadLine());
            if (portId == 1) portId = 15000;
            //Console.WriteLine("探索の深さの上限を入力（深さ = ターン数 * 2, 5以下が目安）");
            //Ai_PriorityErasing.MaxDepth = int.Parse(Console.ReadLine());
            Console.WriteLine("Hostname＞");
            string hostname = Console.ReadLine();
            AI.StartSync(portId, hostname, true);

            /*byte width = 12;
            byte height = 12;

            var ai = new AI.AI();
            var game = Boards.BoardSetting.Generate(height, width);

            var meBoard = new Boards.ColoredBoardNormalSmaller(height, width);
            var enemyBoard = new Boards.ColoredBoardNormalSmaller(height, width);

            meBoard[game.me.Agent1] = true;
            meBoard[game.me.Agent2] = true;

            enemyBoard[game.enemy.Agent1] = true;
            enemyBoard[game.enemy.Agent2] = true;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var res = ai.Begin(3, game.setting, meBoard, enemyBoard, game.me, game.enemy);

            sw.Stop();
			Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < game.setting.ScoreBoard.GetLength(0); ++i)
            {
                for (int m = 0; m < game.setting.ScoreBoard.GetLength(1); ++m)
                {
                    string strr = game.setting.ScoreBoard[m, i].ToString();
                    int hoge = 4 - strr.Length;

                    if (meBoard[(uint)m, (uint)i])
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                    else if (enemyBoard[(uint)m, (uint)i])
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                    else
                        Console.BackgroundColor = ConsoleColor.Black;

                    for (int n = 0; n < hoge; ++n)
                        Console.Write(" ");
                    Console.Write(strr);
                }
                Console.WriteLine();
            }

            Console.WriteLine();

            //Console.WriteLine(res);

            Console.WriteLine(res.Item1);

            for (int i = 0; i < game.setting.ScoreBoard.GetLength(1); ++i)
            {
                for (int m = 0; m < game.setting.ScoreBoard.GetLength(0); ++m)
                {
                    string strr = game.setting.ScoreBoard[m, i].ToString();
                    int hoge = 4 - strr.Length;

                    if (res.Item2[(uint)m, (uint)i])
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                    else if (res.Item3[(uint)m, (uint)i])
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                    else
                        Console.BackgroundColor = ConsoleColor.Black;

                    for (int n = 0; n < hoge; ++n)
                        Console.Write(" ");
                    Console.Write(strr);
                }
                Console.WriteLine();
            }

            Console.WriteLine("End Nodes:{0}[nodes]", ai.ends);
            Console.WriteLine("Time Elasped:{0}[ms]", sw.ElapsedMilliseconds);*/
        }
    }
}
