// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    public class TransformHp1<TSample> : ColorTransformBase<TSample>
        where TSample : struct
    {
        private readonly int RANGE;

        public TransformHp1()
        {
            RANGE = 1 << (Marshal.SizeOf(default(TSample)) * 8);
        }

        public override ITriplet<TSample> ForwardRGB(int v1, int v2, int v3)
        {
            return new Triplet<TSample>(v1 - v2 + RANGE / 2, v2, v3 - v2 + RANGE / 2);
        }

        public override ITriplet<TSample> InverseRGB(int v1, int v2, int v3)
        {
            return new Triplet<TSample>(v1 + v2 - RANGE / 2, v2, v3 + v2 - RANGE / 2);
        }
    }
}
