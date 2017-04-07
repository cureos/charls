// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    public sealed class TransformHp3<TSample> : ColorTransformBase<TSample>
        where TSample : struct
    {
        private readonly int RANGE  = 1 << (Marshal.SizeOf(default(TSample)) * 8);

        public override ITriplet<TSample> Transform(int v1, int v2, int v3)
        {
            var g = v3 - v2 + RANGE / 2;
            var b = v1 - v2 + RANGE / 2;
            var r = v2 + ((g + b) >> 2) - RANGE / 4;

            return new Triplet<TSample>(r, g, b);
        }

        protected override ITriplet<TSample> InverseImpl(int v1, int v2, int v3)
        {
            var g = v1 - ((v3 + v2) >> 2) + RANGE / 4;
            var r = v3 + g - RANGE / 2;
            var b = v2 + g - RANGE / 2;
            return new Triplet<TSample>(r, g, b);
        }
    }
}
