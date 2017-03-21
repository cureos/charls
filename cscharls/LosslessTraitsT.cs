// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

using static CharLS.util;

namespace CharLS
{
    // Optimized trait classes for lossless compression of 8 bit color and 8/16 bit monochrome images.
    // This class assumes MaximumSampleValue correspond to a whole number of bits, and no custom ResetValue is set when encoding.
    // The point of this is to have the most optimized code for the most common and most demanding scenario. 
    public class LosslessTraitsT<SAMPLE>
    {
        private readonly int MAXVAL;
        private readonly int RANGE;
        private readonly int NEAR;
        private readonly int qbpp;
        private readonly int bpp;
        private readonly int LIMIT;
        private readonly int RESET;

        public LosslessTraitsT(int bitsperpixel)
        {
            NEAR = 0;
            bpp = bitsperpixel;
            qbpp = bitsperpixel;
            RANGE = 1 << this.bpp;
            MAXVAL = (1 << bpp) - 1;
            LIMIT = 2 * (bitsperpixel + Math.Max(8, bitsperpixel));
            RESET = BASIC_RESET;
        }

        // TODO Implement remaining methods and inheriting types
    }
}
