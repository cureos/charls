// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    internal static class ByteArray
    {
        private static readonly Type ByteType = typeof(byte);

        internal static void Copy<T>(T[] source, int sourceStartIndex, int sourceLength, byte[] destination,
            int destinationStartIndex)
        {
            if (typeof(T) == ByteType)
            {
                Array.Copy(source, sourceStartIndex, destination, destinationStartIndex, sourceLength);
            }
            else
            {
                var size = Marshal.SizeOf(default(T));
                var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
                Marshal.Copy(
#if NET35
                    new IntPtr(handle.AddrOfPinnedObject().ToInt32() + sourceStartIndex * size),
#else
                    handle.AddrOfPinnedObject() + sourceStartIndex * size,
#endif
                    destination,
                    destinationStartIndex,
                    sourceLength * size);
                handle.Free();
            }
        }

        internal static void Copy<T>(byte[] source, int sourceStartIndex, T[] destination, int destinationStartIndex,
            int destinationLength)
        {
            if (typeof(T) == ByteType)
            {
                Array.Copy(source, sourceStartIndex, destination, destinationStartIndex, destinationLength);
            }
            else
            {
                var size = Marshal.SizeOf(default(T));
                var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
                Marshal.Copy(
                    source,
                    sourceStartIndex,
#if NET35
                    new IntPtr(handle.AddrOfPinnedObject().ToInt32() + destinationStartIndex * size),
#else
                    handle.AddrOfPinnedObject() + destinationStartIndex * size,
#endif
                    destinationLength * size);
                handle.Free();
            }
        }
    }
}
