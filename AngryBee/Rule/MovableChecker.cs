using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon31Protocol;

namespace AngryBee.Rule
{
    public class MovableChecker
    {
        public MovableResult MovableCheck(uint width, uint height, in ColoredBoardNormalSmaller MeField, in ColoredBoardNormalSmaller EnemyField, Unsafe16Array<Point> oldMe, Unsafe16Array<Point> Me, Unsafe16Array<Point> Enemy, int MyAgentsCount )
        {
            MovableResult result = new MovableResult();

            bool notMovable = false;
            for(int i = 0; i < MyAgentsCount; ++i)
            {
                if (Me[i].X >= width || Me[i].Y >= height)
                {
                    result[i] = MovableResultType.OutOfField;
                    return result;
                }
                if (Me[i] == Enemy[i] || Me[i] == Enemy[i])
                {
                    result[i] = MovableResultType.EnemyIsHere;
                    return result;
                }
                for(int j = 0; j < MyAgentsCount; ++j)
                    notMovable |= Me[i] == Me[j];
            }
            if(notMovable)
            {
                for (int i = 0; i < MyAgentsCount; ++i)
                    result[i] = MovableResultType.NotMovable;
            }
            else
            {
                for(int i = 0; i < MyAgentsCount; ++i)
                    result[i] = EnemyField[Me[i]] ? MovableResultType.EraseNeeded : MovableResultType.Ok;
            }
            return result;
        }
    }
}
