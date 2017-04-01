// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public static class JpegLS
    {
        public static ApiResult EncodeStream(
            ByteStreamInfo compressedStreamInfo,
            ref int pcbyteWritten,
            ByteStreamInfo rawStreamInfo,
            JlsParameters parameters,
            ref string errorMessage)
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

                return ResultAndErrorMessage(ApiResult.OK, ref errorMessage);
            }
            catch (charls_error e)
            {
                return ResultAndErrorMessageFromException(e, out errorMessage);
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }


        public static ApiResult DecodeStream(
            ByteStreamInfo rawStream,
            ByteStreamInfo compressedStream,
            JlsParameters info,
            ref string errorMessage)
        {
            try
            {
                var reader = new JpegStreamReader(compressedStream);

                if (info != null)
                {
                    reader.SetInfo(info);
                }

                reader.Read(rawStream);

                return ResultAndErrorMessage(ApiResult.OK, ref errorMessage);
            }
            catch (charls_error e)
            {
                return ResultAndErrorMessageFromException(e, out errorMessage);
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return ApiResult.UnexpectedFailure;
            }
        }


        public static ApiResult ReadHeaderStream(
            ByteStreamInfo rawStreamInfo,
            out JlsParameters parameters,
            ref string errorMessage)
        {
            try
            {
                var reader = new JpegStreamReader(rawStreamInfo);
                reader.ReadHeader();
                reader.ReadStartOfScan(true);
                parameters = reader.GetMetadata();

                return ResultAndErrorMessage(ApiResult.OK, ref errorMessage);
            }
            catch (charls_error e)
            {
                parameters = null;
                return ResultAndErrorMessageFromException(e, out errorMessage);
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

            if (parameters.width < 1 || parameters.width > 65535) throw new charls_error(ApiResult.InvalidJlsParameters, "width needs to be in the range [1, 65535]");

            if (parameters.height < 1 || parameters.height > 65535) throw new charls_error(ApiResult.InvalidJlsParameters, "height needs to be in the range [1, 65535]");

            if (parameters.bitsPerSample < 2 || parameters.bitsPerSample > 16) throw new charls_error(ApiResult.InvalidJlsParameters, "bitspersample needs to be in the range [2, 16]");

            if (
                !(parameters.interleaveMode == InterleaveMode.None || parameters.interleaveMode == InterleaveMode.Sample
                  || parameters.interleaveMode == InterleaveMode.Line))
                throw new charls_error(
                    ApiResult.InvalidJlsParameters,
                    "interleaveMode needs to be set to a value of {None, Sample, Line}");

            if (parameters.components < 1 || parameters.components > 255) throw new charls_error(ApiResult.InvalidJlsParameters, "components needs to be in the range [1, 255]");

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


        private static ApiResult ResultAndErrorMessage(ApiResult result, ref string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = string.Empty;
            }

            return result;
        }


        private static ApiResult ResultAndErrorMessageFromException(charls_error e, out string errorMessage)
        {
            errorMessage = e.Message;
            return e.Code;
        }
    }
}
