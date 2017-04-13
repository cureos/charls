// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    public class Subarray<T>
    {
        private readonly int _typeSize = Marshal.SizeOf(default(T));

        private readonly T[] _array;

        internal Subarray(T[] array, int offset, int count = -1)
        {
            _array = array;
            Offset = offset;
            Count = count < 0 ? array.Length - offset : count;
        }

        internal Subarray(T[] array)
            : this(array, 0, array.Length)
        {
        }

        public int Offset { get; set; }

        public int Count { get; }

        public T this[int index]
        {
            get
            {
                return _array[Offset + index];
            }

            set
            {
                _array[Offset + index] = value;
            }
        }

        public Subarray<T> Copy(int offset)
        {
            return new Subarray<T>(_array, Offset + offset, Count);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Count * _typeSize];
            ByteArray.Copy(_array, Offset, Count, bytes, 0);
            return bytes;
        }

        public void FromBytes(byte[] bytes)
        {
            ByteArray.Copy(bytes, 0, _array, Offset, bytes.Length / _typeSize);
        }
    }
}
