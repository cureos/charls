// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class JlsParameters
    {
        /// <summary>
        /// Width of the image in pixels.
        /// </summary>
        public int width { get; set; }

        /// <summary>
        /// Height of the image in pixels.
        /// </summary>
        public int height { get; set; }

        /// <summary>
        /// The number of valid bits per sample to encode.
        /// Valid range 2 - 16. When greater than 8, pixels are assumed to stored as two bytes per sampe, otherwise one byte per sample is assumed.
        /// </summary>
        public int bitsPerSample { get; set; }

        /// <summary>
        /// The stride is the number of bytes from one row of pixels in memory to the next row of pixels in memory.
        /// Stride is sometimes called pitch. If padding bytes are present, the stride is wider than the width of the image.
        /// </summary>
        public int stride { get; set; }

        /// <summary>
        /// The number of components.
        /// Typical 1 for monochrome images and 3 for color images or 4 if alpha channel is present.
        /// </summary>
        public int components { get; set; }

        /// <summary>
        /// Defines the allowed lossy error. Value 0 defines lossless.
        /// </summary>
        public int allowedLossyError { get; set; }

        /// <summary>
        /// Determines the order of the color components in the compressed stream.
        /// </summary>
        public InterleaveMode interleaveMode { get; set; }

        /// <summary>
        /// Color transformation used in the compressed stream. The color transformations are all lossless and 
        /// are an HP proprietary extension of the standard. Do not use the color transformations unless 
        /// you know the decoder is capable of decoding it. Color transform typically improve compression ratios only 
        /// for sythetic images (non - photorealistic computer generated images).
        /// </summary>
        public ColorTransformation colorTransformation { get; set; }

        /// <summary>
        /// If set to true RGB images will be decoded to BGR. BGR is the standard ordering in MS Windows bitmaps.
        /// </summary>
        public bool outputBgr { get; set; }

        public JpegLSPresetCodingParameters custom { get; set; } = new JpegLSPresetCodingParameters();

        public JfifParameters jfif { get; set; }
    }
}
