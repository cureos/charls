// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    /// <summary>
    /// Defines the interleave mode for multi-component (color) pixel data.
    /// </summary>
    public enum InterleaveMode
    {
        /// <summary>
        /// The data is encoded and stored as component for component: RRRGGGBBB.
        /// </summary>
        None = 0,

        /// <summary>
        /// The interleave mode is by line. A full line of each component is encoded before moving to the next line.
        /// </summary>
        Line = 1,

        /// <summary>
        /// The data is encoded and stored by sample. For color images this is the format like RGBRGBRGB.
        /// </summary>
        Sample = 2
    }
}
