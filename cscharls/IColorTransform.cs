// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    // This file defines an interface that defines (lossless) color transforms.
    // It is invoked in processline.h to convert between decoded values and the internal line buffers.
    // Color transforms work best for computer generated images, but are outside the official JPEG-LS specifications.
    public interface IColorTransform<TSample>
        where TSample : struct
    {
        ITriplet<TSample> ForwardRGB(int v1, int v2, int v3);

        ITriplet<TSample> InverseRGB(int v1, int v2, int v3);

        ITriplet<TSample> ForwardRGBA(int v1, int v2, int v3, int v4);

        ITriplet<TSample> InverseRGBA(int v1, int v2, int v3, int v4);
    }
}
