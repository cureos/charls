// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class Code
    {
        public Code() : this(0, 0)
        {
        }

        public Code(int value, int length)
        {
            _value = value;
            _length = length;
        }

        public int GetValue()
        {
            return _value;
        }

        public int GetLength()
        {
            return _length;
        }

        private readonly int _value;
        private readonly int _length;
    }
}
