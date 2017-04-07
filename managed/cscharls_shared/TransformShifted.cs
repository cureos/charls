// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class TransformShifted<TSample, TTransform> : ColorTransformBase<TSample>
        where TSample : struct where TTransform : IColorTransform<TSample>, new()
    {
        private readonly int _shift;

        private readonly TTransform _colorTransform;

        public TransformShifted(int shift)
        {
            _shift = shift;
            _colorTransform = new TTransform();
        }

        public override ITriplet<TSample> Transform(int v1, int v2, int v3)
        {
            var result = _colorTransform.Transform(v1 << _shift, v2 << _shift, v3 << _shift);
            return new Triplet<TSample>(result.v1 >> _shift, result.v2 >> _shift, result.v3 >> _shift);
        }

        protected override ITriplet<TSample> InverseImpl(int v1, int v2, int v3)
        {
            var result = _colorTransform.Inverse.Transform(v1 << _shift, v2 << _shift, v3 << _shift);
            return new Triplet<TSample>(result.v1 >> _shift, result.v2 >> _shift, result.v3 >> _shift);
        }
    }
}
