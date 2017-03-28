// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface IJpegSegment
    {
        void Serialize(JpegStreamWriter streamWriter);
    }
}
