// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    /// <summary>
    /// Defines the JPEG-LS preset coding parameters as defined in ISO/IEC 14495-1, C.2.4.1.1.
    /// JPEG-LS defines a default set of parameters, but custom parameters can be used.
    /// When used these parameters are written into the encoded bit stream as they are needed for the decoding process.
    /// </summary>
    public class JpegLSPresetCodingParameters
    {
        /// <summary>
        /// Maximum possible value for any image sample in a scan.
        /// This must be greater than or equal to the actual maximum value for the components in a scan.
        /// </summary>
        public int MaximumSampleValue { get; set; }

        /// <summary>
        /// First quantization threshold value for the local gradients.
        /// </summary>
        public int Threshold1 { get; set; }

        /// <summary>
        /// Second quantization threshold value for the local gradients.
        /// </summary>
        public int Threshold2 { get; set; }

        /// <summary>
        /// Third quantization threshold value for the local gradients.
        /// </summary>
        public int Threshold3 { get; set; }

        /// <summary>
        /// Value at which the counters A, B, and N are halved.
        /// </summary>
        public int ResetValue { get; set; }
    }
}
