using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol;

namespace AngryBee.Rule
{
    public class MovableChecker
    {
        public MovableResult MovableCheck(in ColoredBoardNormalSmaller MeField, in ColoredBoardNormalSmaller EnemyField, Unsafe8Array<Point> oldMe, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy, int AgentsCount )
        {
            MovableResult result = new MovableResult();

            uint width = MeField.Width, height = MeField.Height;

            bool notMovable = false;
            for(int i = 0; i < AgentsCount; ++i)
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
                for(int j = 0; j < AgentsCount; ++j)
                    notMovable |= Me[i] == Me[j];
            }
            if(notMovable)
            {
                for (int i = 0; i < AgentsCount; ++i)
                    result[i] = MovableResultType.NotMovable;
            }
            else
            {
                for(int i = 0; i < AgentsCount; ++i)
                    result[i] = EnemyField[Me[i]] ? MovableResultType.EraseNeeded : MovableResultType.Ok;
            }
            return result;
        }
    }
}
