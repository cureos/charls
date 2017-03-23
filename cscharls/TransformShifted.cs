// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class TransformShifted<TSample, TTransform> : ColorTransformBase<TSample>
        where TTransform : IColorTransform<TSample>, new()
    {
        private readonly int _shift;

        private readonly TTransform _colorTransform;

        public TransformShifted(int shift)
        {
            _shift = shift;
            _colorTransform = new TTransform();
        }

        public override Triplet<TSample> ForwardRGB(int v1, int v2, int v3)
        {
            var result = _colorTransform.ForwardRGB(v1 << _shift, v2 << _shift, v3 << _shift);

            return new Triplet<TSample>(
                (int)(object)result.R >> _shift,
                (int)(object)result.G >> _shift,
                (int)(object)result.B >> _shift);
        }

        public override Triplet<TSample> InverseRGB(int v1, int v2, int v3)
        {
            var result = _colorTransform.InverseRGB(v1 << _shift, v2 << _shift, v3 << _shift);

            return new Triplet<TSample>(
                (int)(object)result.R >> _shift,
                (int)(object)result.G >> _shift,
                (int)(object)result.B >> _shift);
        }
    }
}
