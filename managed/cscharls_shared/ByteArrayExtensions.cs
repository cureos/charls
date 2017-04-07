// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Runtime.InteropServices;

namespace CharLS
{
    internal static class ByteArrayExtensions
    {
        internal static void CopyTo<T>(
            this T[] source,
            byte[] destination,
            int sourceStartIndex,
            int sourceLength,
            int destinationStartIndex)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            Marshal.Copy(
                handle.AddrOfPinnedObject() + sourceStartIndex * size,
                destination,
                destinationStartIndex,
                sourceLength * size);
            handle.Free();
        }

        internal static void CopyTo<T>(
            this byte[] source,
            T[] destination,
            int sourceStartIndex,
            int destinationStartIndex,
            int destinationLength)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            Marshal.Copy(
                source,
                sourceStartIndex,
                handle.AddrOfPinnedObject() + destinationStartIndex * size,
                destinationLength * size);
            handle.Free();
        }
    }
}
