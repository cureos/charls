﻿// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    // This file defines an interface that defines (lossless) color transforms.
    // It is invoked in processline.h to convert between decoded values and the internal line buffers.
    // Color transforms work best for computer generated images, but are outside the official JPEG-LS specifications.
    public interface IColorTransform<TSample>
        where TSample : struct
    {
        IColorTransform<TSample> Inverse { get; }

        ITriplet<TSample> Transform(int v1, int v2, int v3);

        ITriplet<TSample> Transform(int v1, int v2, int v3, int v4);
    }
}
