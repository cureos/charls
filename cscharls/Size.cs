// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public struct Size
    {
        public Size(int width, int height)
        {
            cx = width;
            cy = height;
        }

        public int cx { get; }

        public int cy { get; }
    }
}
