// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    //
    // This file defines the IProcessLine interface.
    // During coding/decoding, CharLS process one line at a time. The different Processline implementations
    // convert the uncompressed format to and from the internal format for encoding.
    // Conversions include color transforms, line interleaved vs sample interleaved, masking out unused bits,
    // accounting for line padding etc.
    // This mechanism could be used to encode/decode images as they are received.
    //
    public interface IProcessLine<in TPixel>
    {
        void NewLineDecoded(TPixel[] pSrc, int pixelCount, int sourceStride);

        void NewLineRequested(TPixel[] pDest, int pixelCount, int destStride);
    }
}
