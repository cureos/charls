// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class Quad<SAMPLE> : Triplet<SAMPLE>
    {
        public Quad()
        {
            v4 = default(SAMPLE);
        }

        public Quad(Triplet<SAMPLE> triplet, int alpha) : base(triplet)
        {
            v4 = (SAMPLE) (object) alpha;
        }

        public SAMPLE v4 { get; }

        public SAMPLE A => v4;
    }
}
