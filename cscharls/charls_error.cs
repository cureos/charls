// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class charls_error : Exception
    {
        public charls_error(ApiResult errorCode) : base($"Error code: {errorCode}")
        {
        }

        public charls_error(ApiResult errorCode, string message) : base($"Error code: {errorCode}, message: {message}")
        {
        }
    }
}
