using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AngryBee.Rule
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MovableResult
    {
        public MovableResultType Me1, Me2, Me3, Me4, Me5, Me6, Me7, Me8;
        public unsafe MovableResultType this[int index] {
            get {
#if DEBUG
                if (index < 0 || index >= 8) throw new IndexOutOfRangeException();
#endif
                MovableResultType * tp = (MovableResultType *)Unsafe.AsPointer(ref this);
                return tp[index];
            }
            set {
#if DEBUG
                if (index < 0 || index >= 8) throw new IndexOutOfRangeException();
#endif
                MovableResultType* tp = (MovableResultType*)Unsafe.AsPointer(ref this);
                tp[index] = value;
            }
        }

        public bool IsMovable =>
            ((Me1 | Me2 | Me3 | Me4 | Me5 | Me6 | Me7 | Me8) & MovableResultType.NotMovable) == 0;

        public bool IsEraseNeeded =>
            ((Me1 | Me2 | Me3 | Me4 | Me5 | Me6 | Me7 | Me8) & MovableResultType.EraseNeeded) == MovableResultType.EraseNeeded;
    }

    public enum MovableResultType : byte
    {
        Ok = 0,
        OutOfField = 0b1,
        EnemyIsHere = 0b10,
        EraseNeeded = 0b100,
        NotMovable = 0b11
    }
}
