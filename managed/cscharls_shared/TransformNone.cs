// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public sealed class TransformNone<TSample> : ColorTransformBase<TSample>
        where TSample : struct
    {
        public override ITriplet<TSample> Transform(int v1, int v2, int v3)
        {
            return new Triplet<TSample>(v1, v2, v3);
        }

        protected override ITriplet<TSample> InverseImpl(int v1, int v2, int v3)
        {
            return Transform(v1, v2, v3);
        }
    }
}
