using System;
using System.Collections.Generic;
using Chromacolony.Core;

namespace Chromacolony.Tests.EditMode
{
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> _draws;

        public ScriptedRandom(params int[] draws)
        {
            _draws = new Queue<int>(draws);
        }

        public uint NextUInt()
        {
            throw new InvalidOperationException("only ranges are scripted");
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            int draw = _draws.Dequeue();
            if (draw < minInclusive || draw >= maxExclusive)
            {
                throw new InvalidOperationException($"the scripted draw {draw} is outside [{minInclusive}, {maxExclusive})");
            }

            return draw;
        }

        public float Value()
        {
            throw new InvalidOperationException("only ranges are scripted");
        }
    }
}
