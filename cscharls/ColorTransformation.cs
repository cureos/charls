// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    /// <summary>
    /// Defines color space transformations as defined and implemented by the JPEG-LS library of HP Labs.
    /// These color space transformation decrease the correlation between the 3 color components, resulting in better encoding ratio.
    /// These options are only implemented for backwards compatibility and NOT part of the JPEG-LS standard.
    /// The JPEG-LS ISO/IEC 14495-1:1999 standard provides no capabilities to transport which color space transformation was used.
    /// </summary>
    public enum ColorTransformation
    {
        /// <summary>
        /// No color space transformation has been applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Defines the reversible lossless color transformation:
        /// G = G
        /// R = R - G
        /// B = B - G
        /// </summary>
        HP1 = 1,

        /// <summary>
        /// Defines the reversible lossless color transformation:
        /// G = G
        /// B = B - (R + G) / 2
        /// R = R - G
        /// </summary>
        HP2 = 2,

        /// <summary>
        /// Defines the reversible lossless color transformation of Y-Cb-Cr):
        /// R = R - G
        /// B = B - G
        /// G = G + (R + B) / 4
        /// </summary>
        HP3 = 3
    }
}
