// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class Quad<TSample> : Triplet<TSample>
    {
        public Quad()
        {
            v4 = default(TSample);
        }

        public Quad(Triplet<TSample> triplet, int alpha)
            : base(triplet)
        {
            v4 = (TSample)(object)alpha;
        }

        public TSample v4 { get; }

        public TSample A => v4;
    }
}
