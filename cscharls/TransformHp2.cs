﻿// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    public sealed class TransformHp2<TSample> : ColorTransformBase<TSample>
        where TSample : struct
    {
        private readonly int RANGE = 1 << (Marshal.SizeOf(default(TSample)) * 8);

        public override ITriplet<TSample> Transform(int v1, int v2, int v3)
        {
            return new Triplet<TSample>(v1 - v2 + RANGE / 2, v2, v3 - ((v1 + v2) >> 1) - RANGE / 2);
        }

        protected override ITriplet<TSample> InverseImpl(int v1, int v2, int v3)
        {
            var r = v1 + v2 - RANGE / 2;
            var g = v2;
            return new Triplet<TSample>(r, g, v3 + ((r + g) >> 1) - RANGE / 2);
        }
    }
}
