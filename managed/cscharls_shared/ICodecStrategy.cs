// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public interface ICodecStrategy
    {
        void SetPresets(JpegLSPresetCodingParameters presets);

        IProcessLine CreateProcess(ByteStreamInfo info);
    }
}
