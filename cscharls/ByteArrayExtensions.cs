// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    internal static class ByteArrayExtensions
    {
        internal static void CopyTo<T>(this T[] source, byte[] destination, int destinationStartIndex, int sourceLength)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), destination, destinationStartIndex, sourceLength * size);
            handle.Free();
        }

        internal static void CopyTo<T>(this byte[] source, T[] destination, int sourceStartIndex, int destinationLength)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            Marshal.Copy(source, sourceStartIndex, handle.AddrOfPinnedObject(), destinationLength * size);
            handle.Free();
        }
    }
}
