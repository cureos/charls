// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public abstract class ColorTransformBase<TSample> : IColorTransform<TSample>
        where TSample : struct
    {
        public abstract ITriplet<TSample> ForwardRGB(int v1, int v2, int v3);

        public abstract ITriplet<TSample> InverseRGB(int v1, int v2, int v3);

        public virtual ITriplet<TSample> ForwardRGBA(int v1, int v2, int v3, int v4)
        {
            return new Quad<TSample>(ForwardRGB(v1, v2, v3), v4);
        }

        public virtual ITriplet<TSample> InverseRGBA(int v1, int v2, int v3, int v4)
        {
            return new Quad<TSample>(InverseRGB(v1, v2, v3), v4);
        }
    }
}
