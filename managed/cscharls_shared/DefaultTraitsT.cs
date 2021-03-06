﻿// Copyright (c) 2017 cscharls contributors.
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
    public class DefaultTraitsT<TSample, TPixel> : TraitsBase<TSample, TPixel>
        where TSample : struct
    {
        public DefaultTraitsT(int max, int near, int reset = BASIC_RESET)
            : base(max, near, reset)
        {
        }

        public DefaultTraitsT(ITraits<TSample, TPixel> other)
            : base(other)
        {
        }

        public override int ComputeErrVal(int e)
        {
            return ModuloRange(Quantize(e));
        }

        public override TSample ComputeReconstructedSample(int Px, int ErrVal)
        {
            return FixReconstructedValue(Px + DeQuantize(ErrVal));
        }

        public override bool IsNear(int lhs, int rhs)
        {
            return Math.Abs(lhs - rhs) <= NEAR;
        }

        public override bool IsNear(TPixel lhs, TPixel rhs)
        {
            var lhs_ = lhs as ITriplet<TSample>;
            var rhs_ = rhs as ITriplet<TSample>;

            if (lhs_ != null && rhs_ != null)
            {
                return rhs_.IsNear(lhs_, NEAR);
            }

            return IsNear(Convert.ToInt32(lhs_), Convert.ToInt32(rhs_));
        }

        public override int CorrectPrediction(int Pxc)
        {
            if ((Pxc & MAXVAL) == Pxc) return Pxc;

            return ~(Pxc >> (INT32_BITCOUNT - 1)) & MAXVAL;
        }

        /// <summary>
        /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
        /// </summary>
        public override int ModuloRange(int errorValue)
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
            if (Errval > 0) return (Errval + NEAR) / (2 * NEAR + 1);

            return -(NEAR - Errval) / (2 * NEAR + 1);
        }

        private int DeQuantize(int Errval)
        {
            return Errval * (2 * NEAR + 1);
        }

        private TSample FixReconstructedValue(int val)
        {
            if (val < -NEAR)
            {
                val = val + RANGE * (2 * NEAR + 1);
            }
            else if (val > MAXVAL + NEAR)
            {
                val = val - RANGE * (2 * NEAR + 1);
            }

            return (TSample)Convert.ChangeType(CorrectPrediction(val), typeof(TSample));
        }
    }
}
