// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Collections.Generic;
using System.IO;

namespace CharLS
{
    public class DecoderStrategy
    {
        internal readonly JlsParameters _params;

        internal IProcessLine _processLine;

        private IList<byte> _buffer;

        private Stream _byteStream;

        // decoding
        private int _readCache;

        private int _validBits;

        private int _position; // TODO e.g. pointer

        private int _nextFFPosition; // TODO e.g. pointer

        private int _endPosition; // TODO e.g. pointer
    }
}