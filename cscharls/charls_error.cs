// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class charls_error : Exception
    {
        public charls_error(ApiResult errorCode) : this(errorCode, null)
        {
        }

        public charls_error(ApiResult errorCode, string message)
            : base(message)
        {
            Code = errorCode;
        }

        public ApiResult Code { get; }
    }
}
