// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class JpegImageDataSegment : IJpegSegment
    {
        private int _componentCount;

        private readonly ByteStreamInfo _rawStreamInfo;

        private readonly JlsParameters _params;

        public JpegImageDataSegment(ByteStreamInfo rawStream, JlsParameters parameters, int componentCount)
        {
            _componentCount = componentCount;
            _rawStreamInfo = rawStream;
            _params = parameters;
        }

        public void Serialize(JpegStreamWriter streamWriter)
        {
            /*JlsParameters info = _params;
            info.components = _componentCount;
            var codec = JlsCodecFactory<EncoderStrategy>().GetCodec(info, _params.custom);
            var processLine = codec.CreateProcess(_rawStreamInfo);
            ByteStreamInfo compressedData = streamWriter.OutputStream();
            var cbyteWritten = codec.EncodeScan(processLine, compressedData);
            streamWriter.Seek(cbyteWritten);*/
        }
    }
}
