// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

using static CharLS.util;

namespace CharLS
{
    public abstract class TraitsBase<TSample, TPixel> : ITraits<TSample, TPixel>
        where TSample : struct
    {
        protected TraitsBase(int max, int near, int reset = BASIC_RESET)
        {
            MAXVAL = max;
            RANGE = (max + 2 * near) / (2 * near + 1) + 1;
            NEAR = near;
            qbpp = Log2(RANGE);
            bpp = Log2(max);
            LIMIT = 2 * (bpp + Math.Max(8, bpp));
            RESET = reset;
        }

        protected TraitsBase(ITraits<TSample, TPixel> other)
        {
            MAXVAL = other.MAXVAL;
            RANGE = other.RANGE;
            NEAR = other.NEAR;
            qbpp = other.qbpp;
            bpp = other.bpp;
            LIMIT = other.LIMIT;
            RESET = other.RESET;
        }

        protected TraitsBase(int bitsperpixel)
        {
            NEAR = 0;
            bpp = bitsperpixel;
            qbpp = bitsperpixel;
            RANGE = 1 << bpp;
            MAXVAL = (1 << bpp) - 1;
            LIMIT = 2 * (bitsperpixel + Math.Max(8, bitsperpixel));
            RESET = BASIC_RESET;
        }

        public int MAXVAL { get; set; }

        public int RANGE { get; }

        public int NEAR { get; }

        public int qbpp { get; }

        public int bpp { get; }

        public int LIMIT { get; }

        public int RESET { get; }

        public abstract int ComputeErrVal(int e);

        public abstract TSample ComputeReconstructedSample(int Px, int ErrVal);

        public abstract bool IsNear(int lhs, int rhs);

        public abstract bool IsNear(TPixel lhs, TPixel rhs);

        public abstract int CorrectPrediction(int Pxc);

        /// <summary>
        /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
        /// </summary>
        public abstract int ModuloRange(int errorValue);

        private static int Log2(int n)
        {
            var x = 0;
            while (n > 1 << x)
            {
                ++x;
            }

            return x;
        }
    }
}
