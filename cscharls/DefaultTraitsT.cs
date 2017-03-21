// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;

using static CharLS.util;

namespace CharLS
{
    // Default traits that support all JPEG LS parameters: custom limit, near, maxval (not power of 2)

    // This traits class is used to initialize a coder/decoder.
    // The coder/decoder also delegates some functions to the traits class.
    // This is to allow the traits class to replace the default implementation here with optimized specific implementations.
    // This is done for lossless coding/decoding: see losslesstraits.h 
    public class DefaultTraitsT<SAMPLE, PIXEL>
    {
        private readonly int MAXVAL;
        private readonly int RANGE;
        private readonly int NEAR;
        private readonly int qbpp;
        private readonly int bpp;
        private readonly int LIMIT;
        private readonly int RESET;

        public DefaultTraitsT(int max, int near, int reset = BASIC_RESET)
        {
            MAXVAL = max;
            RANGE = (max + 2 * near) / (2 * near + 1) + 1;
            NEAR = near;
            qbpp = log_2(RANGE);
            bpp = log_2(max);
            LIMIT = 2 * (bpp + Math.Max(8, bpp));
            RESET = reset;
        }

        public DefaultTraitsT(DefaultTraitsT<SAMPLE, PIXEL> other)
        {
            MAXVAL = other.MAXVAL;
            RANGE = other.RANGE;
            NEAR = other.NEAR;
            qbpp = other.qbpp;
            bpp = other.bpp;
            LIMIT = other.LIMIT;
            RESET = other.RESET;
        }

        public int ComputeErrVal(int e)
        {
            return ModuloRange(Quantize(e));
        }

        public SAMPLE ComputeReconstructedSample(int Px, int ErrVal)
        {
            return FixReconstructedValue(Px + DeQuantize(ErrVal));
        }

        public bool IsNear(int lhs, int rhs)
        {
            return Math.Abs(lhs - rhs) <= NEAR;
        }

        public bool IsNear(Triplet<SAMPLE> lhs, Triplet<SAMPLE> rhs)
        {
            return Math.Abs((int)(object)lhs.v1 - (int)(object)rhs.v1) <= NEAR &&
                   Math.Abs((int)(object)lhs.v2 - (int)(object)rhs.v2) <= NEAR &&
                   Math.Abs((int)(object)lhs.v3 - (int)(object)rhs.v3) <= NEAR;
        }

        public int CorrectPrediction(int Pxc)
        {
            if ((Pxc & MAXVAL) == Pxc)
                return Pxc;

            return (~(Pxc >> (INT32_BITCOUNT - 1))) & MAXVAL;
        }

        /// <summary>
        /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
        /// </summary>
        public int ModuloRange(int errorValue)
        {
            Debug.Assert(Math.Abs(errorValue) <= RANGE);

            if (errorValue < 0)
            {
                errorValue += RANGE;
            }
            if (errorValue >= (RANGE + 1) / 2)
            {
                errorValue -= RANGE;
            }

            Debug.Assert(-RANGE / 2 <= errorValue && errorValue <= (RANGE / 2) - 1);
            return errorValue;
        }

        private int Quantize(int Errval)
        {
            if (Errval > 0)
                return (Errval + NEAR) / (2 * NEAR + 1);

            return -(NEAR - Errval) / (2 * NEAR + 1);
        }

        private int DeQuantize(int Errval)
        {
            return Errval * (2 * NEAR + 1);
        }

        private SAMPLE FixReconstructedValue(int val)
        {
            if (val < -NEAR)
            {
                val = val + RANGE * (2 * NEAR + 1);
            }
            else if (val > MAXVAL + NEAR)
            {
                val = val - RANGE * (2 * NEAR + 1);
            }

            return (SAMPLE)(object)(CorrectPrediction(val));
        }
    }
}
