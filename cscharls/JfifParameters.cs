// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    /// <summary>
    /// Defines the parameters for the JPEG File Interchange Format.
    /// The format is defined in the JPEG File Interchange Format v1.02 document by Eric Hamilton.
    /// </summary>
    /// <remarks>
    /// The JPEG File Interchange Format is the de-facto standard JPEG interchange format.
    /// </remarks>
    public class JfifParameters
    {
        /// <summary>
        /// Version of the JPEG File Interchange Format.
        /// Should be set to zero to not write a JFIF header or to 1.02, encoded as: (1 * 256) + 2. 
        /// </summary>
        public int version { get; set; }

        /// <summary>
        /// Defines the units for the X and Y densities.
        /// 0: no units, X and Y specify the pixel aspect ratio.
        /// 1: X and Y are dots per inch.
        /// 2: X and Y are dots per cm.
        /// </summary>
        public int units { get; set; }

        /// <summary>
        /// Horizontal pixel density
        /// </summary>
        public int Xdensity { get; set; }

        /// <summary>
        /// Vertical pixel density
        /// </summary>
        public int Ydensity { get; set; }

        /// <summary>
        /// Thumbnail horizontal pixel count.
        /// </summary>
        public int Xthumbnail { get; set; }

        /// <summary>
        /// Thumbnail vertical pixel count.
        /// </summary>
        public int Ythumbnail { get; set; }

        /// <summary>
        /// Reference to a buffer with thumbnail pixels of size Xthumbnail * Ythumbnail * 3(RGB).
        /// This parameter is only used when creating JPEG-LS encoded images.
        /// </summary>
        public byte[] thumbnail { get; set; }
    }
}
