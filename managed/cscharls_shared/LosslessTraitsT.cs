// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

using static CharLS.util;

namespace CharLS
{
    // Optimized trait classes for lossless compression of 8 bit color and 8/16 bit monochrome images.
    // This class assumes MaximumSampleValue correspond to a whole number of bits, and no custom ResetValue is set when encoding.
    // The point of this is to have the most optimized code for the most common and most demanding scenario. 
    public abstract class LosslessTraitsImplT<TSample, TPixel> : TraitsBase<TSample, TPixel>
        where TSample : struct
    {
        protected LosslessTraitsImplT(int bitsperpixel)
            : base(bitsperpixel)
        {
        }

        public override int ComputeErrVal(int d)
        {
            return ModuloRange(d);
        }

        public override bool IsNear(int lhs, int rhs)
        {
            return lhs == rhs;
        }

        public override bool IsNear(TPixel lhs, TPixel rhs)
        {
            return IsNear(Convert.ToInt32(lhs), Convert.ToInt32(rhs));
        }

        public override int ModuloRange(int errorValue)
        {
            return (errorValue << (INT32_BITCOUNT - bpp)) >> (INT32_BITCOUNT - bpp);
        }

        public override TSample ComputeReconstructedSample(int Px, int ErrVal)
        {
            return (TSample)Convert.ChangeType(MAXVAL & (Px + ErrVal), typeof(TSample));
        }

        public override int CorrectPrediction(int Pxc)
        {
            if ((Pxc & MAXVAL) == Pxc) return Pxc;

            return ~(Pxc >> (INT32_BITCOUNT - 1)) & MAXVAL;
        }
    }

    public class LosslessTraitsT<TSample> : LosslessTraitsImplT<TSample, TSample>
        where TSample : struct
    {
        public LosslessTraitsT(int bitsperpixel)
            : base(bitsperpixel)
        {
        }
    }

    public class LosslessTraits8 : LosslessTraitsImplT<byte, byte>
    {
        public LosslessTraits8()
            : base(8)
        {
        }

        public sbyte ModRange(int Errval)
        {
            return (sbyte)Errval;
        }

        public override int ComputeErrVal(int d)
        {
            return (sbyte)d;
        }

        public override byte ComputeReconstructedSample(int Px, int ErrVal)
        {
            return (byte)(Px + ErrVal);
        }
    }

    public class LosslessTraits16 : LosslessTraitsImplT<ushort, ushort>
    {
        public LosslessTraits16()
            : base(16)
        {
        }

        public short ModRange(int Errval)
        {
            return (short)Errval;
        }

        public override int ComputeErrVal(int d)
        {
            return (short)d;
        }

        public override ushort ComputeReconstructedSample(int Px, int ErrVal)
        {
            return (ushort)(Px + ErrVal);
        }
    }

    public class LosslessTraitsTriplet<TSample> : LosslessTraitsImplT<TSample, Triplet<TSample>>
        where TSample : struct
    {
        public LosslessTraitsTriplet(int bitsperpixel)
            : base(bitsperpixel)
        {
        }

        public override bool IsNear(Triplet<TSample> lhs, Triplet<TSample> rhs)
        {
            return lhs.Equals(rhs);
        }

        public override TSample ComputeReconstructedSample(int Px, int ErrVal)
        {
            return Delimit<TSample>(Px + ErrVal);
        }
    }
}
