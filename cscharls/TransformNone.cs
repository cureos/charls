﻿// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class TransformNone<TSample> : ColorTransformBase<TSample>
    {
        public override Triplet<TSample> ForwardRGB(int v1, int v2, int v3)
        {
            return new Triplet<TSample>(v1, v2, v3);
        }

        public override Triplet<TSample> InverseRGB(int v1, int v2, int v3)
        {
            return ForwardRGB(v1, v2, v3);
        }
    }
}
