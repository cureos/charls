// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface ITraits<out TSample, in TPixel>
        where TSample : struct
    {
        int MAXVAL { get; }

        int RANGE { get; }

        int NEAR { get; }

        int qbpp { get; }

        int bpp { get; }

        int LIMIT { get; }

        int RESET { get; }

        int ComputeErrVal(int e);

        TSample ComputeReconstructedSample(int Px, int ErrVal);

        bool IsNear(int lhs, int rhs);

        bool IsNear(TPixel lhs, TPixel rhs);

        int CorrectPrediction(int Pxc);

        /// <summary>
        /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
        /// </summary>
        int ModuloRange(int errorValue);
    }
}
