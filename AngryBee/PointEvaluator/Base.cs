using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    public abstract class Base
    {
        /// <summary>
        /// This calculation doesn't think about enemy.
        /// </summary>
        /// <param name="ScoreBoard"></param>
        /// <param name="state"></param>
        /// <param name="Turn"></param>
        /// <returns></returns>
        public abstract int Calculate(sbyte[,] ScoreBoard, Search.SearchState state, int Turn);
    }
}
