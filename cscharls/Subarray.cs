// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class Subarray<T>
    {
        private readonly T[] _array;

        internal Subarray(T[] array, int offset, int count)
        {
            _array = array;
            Offset = offset;
            Count = count;
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
    }
}
