// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public abstract class ColorTransformBase<TSample> : IColorTransform<TSample>
        where TSample : struct
    {
        protected ColorTransformBase()
        {
            Inverse = new InverseColorTransform(this);
        }

        public IColorTransform<TSample> Inverse { get; }

        public abstract ITriplet<TSample> Transform(int v1, int v2, int v3);

        public virtual ITriplet<TSample> Transform(int v1, int v2, int v3, int v4)
        {
            return new Quad<TSample>(Transform(v1, v2, v3), v4);
        }

        protected abstract ITriplet<TSample> InverseImpl(int v1, int v2, int v3);

        private class InverseColorTransform : IColorTransform<TSample>
        {
            private readonly ColorTransformBase<TSample> _forward;

            internal InverseColorTransform(ColorTransformBase<TSample> forward)
            {
                _forward = forward;
            }

            public IColorTransform<TSample> Inverse => _forward;

            public ITriplet<TSample> Transform(int v1, int v2, int v3)
            {
                return _forward.InverseImpl(v1, v2, v3);
            }

            public ITriplet<TSample> Transform(int v1, int v2, int v3, int v4)
            {
                return new Quad<TSample>(Transform(v1, v2, v3), v4);
            }
        }
    }
}
