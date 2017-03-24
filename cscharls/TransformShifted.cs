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

        public override ITriplet<TSample> ForwardRGB(int v1, int v2, int v3)
        {
            var result = _colorTransform.ForwardRGB(v1 << _shift, v2 << _shift, v3 << _shift);
            return new Triplet<TSample>(result.V1 >> _shift, result.V2 >> _shift, result.V3 >> _shift);
        }

        public override ITriplet<TSample> InverseRGB(int v1, int v2, int v3)
        {
            var result = _colorTransform.InverseRGB(v1 << _shift, v2 << _shift, v3 << _shift);
            return new Triplet<TSample>(result.V1 >> _shift, result.V2 >> _shift, result.V3 >> _shift);
        }
    }
}
