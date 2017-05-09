// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;

namespace CharLS
{
    public static class JpegLs
    {
        public static ApiResult Encode(byte[] destination, byte[] source, JlsParameters parameters,
            out ulong bytesWritten, out string errorMessage)
        {
            if (source == null || destination == null || parameters == null)
            {
                bytesWritten = 0;
                errorMessage = "source, destination and parameters must not be null";
                return ApiResult.InvalidJlsParameters;
            }

            return EncodeStream(new ByteStreamInfo(destination), new ByteStreamInfo(source),
                parameters, out bytesWritten, out errorMessage);
        }

        public static ApiResult Encode(byte[] destination, byte[] source, JlsParameters parameters,
            out ulong bytesWritten)
        {
            string errorMessage;
            return Encode(destination, source, parameters, out bytesWritten, out errorMessage);
        }

        public static ApiResult Encode(Stream destination, Stream source, JlsParameters parameters,
            out ulong bytesWritten, out string errorMessage)
        {
            if (source == null || destination == null || parameters == null)
            {
                bytesWritten = 0;
                errorMessage = "source, destination and parameters must not be null";
                return ApiResult.InvalidJlsParameters;
            }

            return EncodeStream(new ByteStreamInfo(destination), new ByteStreamInfo(source),
                parameters, out bytesWritten, out errorMessage);
        }

        public static ApiResult Decode(byte[] destination, byte[] source, JlsParameters info, out string errorMessage)
        {
            return DecodeStream(new ByteStreamInfo(destination), new ByteStreamInfo(source), info, out errorMessage);
        }

        public static ApiResult Decode(byte[] destination, byte[] source, JlsParameters info = null)
        {
            string errorMessage;
            return DecodeStream(new ByteStreamInfo(destination), new ByteStreamInfo(source), info, out errorMessage);
        }

        public static ApiResult Decode(Stream destination, Stream source, JlsParameters info, out string errorMessage)
        {
            return DecodeStream(new ByteStreamInfo(destination), new ByteStreamInfo(source), info, out errorMessage);
        }

        public static ApiResult DecodeRect(byte[] uncompressedData, byte[] compressedData, JlsRect roi,
            JlsParameters info, out string errorMessage)
        {
            return DecodeRectStream(new ByteStreamInfo(uncompressedData), new ByteStreamInfo(compressedData), roi,
                info, out errorMessage);
        }

        public static ApiResult DecodeRect(byte[] uncompressedData, byte[] compressedData, JlsRect roi,
            JlsParameters info = null)
        {
            string errorMessage;
            return DecodeRectStream(new ByteStreamInfo(uncompressedData), new ByteStreamInfo(compressedData), roi,
                info, out errorMessage);
        }

        public static ApiResult DecodeRect(Stream uncompressedData, Stream compressedData, JlsRect roi,
            JlsParameters info, out string errorMessage)
        {
            return DecodeRectStream(new ByteStreamInfo(uncompressedData), new ByteStreamInfo(compressedData), roi,
                info, out errorMessage);
        }

        public static ApiResult ReadHeader(byte[] compressedData, out JlsParameters parameters, out string errorMessage)
        {
            return ReadHeaderStream(new ByteStreamInfo(compressedData), out parameters, out errorMessage);
        }

        public static ApiResult ReadHeader(byte[] compressedData, out JlsParameters parameters)
        {
            string errorMessage;
            return ReadHeaderStream(new ByteStreamInfo(compressedData), out parameters, out errorMessage);
        }

        public static ApiResult ReadHeader(Stream compressedData, out JlsParameters parameters, out string errorMessage)
        {
            return ReadHeaderStream(new ByteStreamInfo(compressedData), out parameters, out errorMessage);
        }

        private static ApiResult DecodeStream(ByteStreamInfo rawStream, ByteStreamInfo compressedStream,
            JlsParameters info, out string errorMessage)
        {
            try
            {
                var reader = new JpegStreamReader(compressedStream);

                if (info != null)
                {
                    reader.SetInfo(info);
                }

                reader.Read(rawStream);

                errorMessage = null;
                return ApiResult.OK;
            }
            catch (charls_error e)
            {
                errorMessage = e.Message;
                return e.Code;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }

        private static ApiResult EncodeStream(ByteStreamInfo compressedStreamInfo, ByteStreamInfo rawStreamInfo,
            JlsParameters parameters, out ulong pcbyteWritten, out string errorMessage)
        {
            try
            {
                VerifyInput(rawStreamInfo, parameters);

                JlsParameters info = parameters;
                if (info.stride == 0)
                {
                    info.stride = info.width * ((info.bitsPerSample + 7) / 8);
                    if (info.interleaveMode != InterleaveMode.None)
                    {
                        info.stride *= info.components;
                    }
                }

                JpegStreamWriter writer = new JpegStreamWriter();
                if (info.jfif.version != 0)
                {
                    writer.AddSegment(JpegMarkerSegment.CreateJpegFileInterchangeFormatSegment(info.jfif));
                }

                writer.AddSegment(
                    JpegMarkerSegment.CreateStartOfFrameSegment(
                        info.width,
                        info.height,
                        info.bitsPerSample,
                        info.components));

                if (info.colorTransformation != ColorTransformation.None)
                {
                    writer.AddColorTransform(info.colorTransformation);
                }

                if (info.interleaveMode == InterleaveMode.None)
                {
                    int cbyteComp = info.width * info.height * ((info.bitsPerSample + 7) / 8);
                    for (int component = 0; component < info.components; ++component)
                    {
                        writer.AddScan(rawStreamInfo, info);
                        rawStreamInfo.Skip(cbyteComp);
                    }
                }
                else
                {
                    writer.AddScan(rawStreamInfo, info);
                }

                writer.Write(compressedStreamInfo);
                pcbyteWritten = writer.GetBytesWritten();

                errorMessage = null;
                return ApiResult.OK;
            }
            catch (charls_error e)
            {
                pcbyteWritten = 0;
                errorMessage = e.Message;
                return e.Code;
            }
            catch (Exception e)
            {
                pcbyteWritten = 0;
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }

        private static ApiResult DecodeRectStream(ByteStreamInfo rawStreamInfo, ByteStreamInfo compressedStream,
            JlsRect roi, JlsParameters info, out string errorMessage)
        {
            try
            {
                var reader = new JpegStreamReader(compressedStream);

                if (info != null)
                {
                    reader.SetInfo(info);
                }

                reader.SetRect(roi);
                reader.Read(rawStreamInfo);

                errorMessage = null;
                return ApiResult.OK;
            }
            catch (charls_error e)
            {
                errorMessage = e.Message;
                return e.Code;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }

        private static ApiResult ReadHeaderStream(ByteStreamInfo rawStreamInfo, out JlsParameters parameters,
            out string errorMessage)
        {
            try
            {
                var reader = new JpegStreamReader(rawStreamInfo);
                reader.ReadHeader();
                reader.ReadStartOfScan(true);
                parameters = reader.GetMetadata();

                errorMessage = null;
                return ApiResult.OK;
            }
            catch (charls_error e)
            {
                parameters = null;
                errorMessage = e.Message;
                return e.Code;
            }
            catch (Exception e)
            {
                parameters = null;
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }

        private static void VerifyInput(ByteStreamInfo uncompressedStream, JlsParameters parameters)
        {
            if (uncompressedStream == null)
                throw new charls_error(
                    ApiResult.InvalidJlsParameters,
                    "rawStream or rawData needs to reference to something");

            if (parameters.width < 1 || parameters.width > 65535)
                throw new charls_error(ApiResult.InvalidJlsParameters, "width needs to be in the range [1, 65535]");

            if (parameters.height < 1 || parameters.height > 65535)
                throw new charls_error(ApiResult.InvalidJlsParameters, "height needs to be in the range [1, 65535]");

            if (parameters.bitsPerSample < 2 || parameters.bitsPerSample > 16)
                throw new charls_error(ApiResult.InvalidJlsParameters,
                    "bitspersample needs to be in the range [2, 16]");

            if (
                !(parameters.interleaveMode == InterleaveMode.None || parameters.interleaveMode == InterleaveMode.Sample
                  || parameters.interleaveMode == InterleaveMode.Line))
                throw new charls_error(
                    ApiResult.InvalidJlsParameters,
                    "interleaveMode needs to be set to a value of {None, Sample, Line}");

            if (parameters.components < 1 || parameters.components > 255)
                throw new charls_error(ApiResult.InvalidJlsParameters, "components needs to be in the range [1, 255]");

            if (
                !uncompressedStream.Require(
                    false,
                    parameters.height * parameters.width * parameters.components
                    * (parameters.bitsPerSample > 8 ? 2 : 1)))
            {
                throw new charls_error(
                    ApiResult.InvalidJlsParameters,
                    "uncompressed size does not match with the other parameters");
            }

            switch (parameters.components)
            {
                case 3:
                    break;
                case 4:
                    if (parameters.interleaveMode == InterleaveMode.Sample)
                        throw new charls_error(
                            ApiResult.InvalidJlsParameters,
                            "interleaveMode cannot be set to Sample in combination with components = 4");
                    break;
                default:
                    if (parameters.interleaveMode != InterleaveMode.None)
                        throw new charls_error(
                            ApiResult.InvalidJlsParameters,
                            "interleaveMode can only be set to None in combination with components = 1");
                    break;
            }
        }
    }
}
