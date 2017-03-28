// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    public static class ByteArraySegmentExtensions
    {
        internal static void CopyTo<T>(
            this T[] source,
            ArraySegment<byte> destination,
            int sourceStartIndex,
            int sourceLength)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            Marshal.Copy(
                handle.AddrOfPinnedObject() + sourceStartIndex * size,
                destination.Array,
                destination.Offset,
                sourceLength * size);
            handle.Free();
        }

        internal static void CopyTo<T>(
            this ArraySegment<byte> source,
            T[] destination,
            int destinationStartIndex,
            int destinationLength)
        {
            var size = Marshal.SizeOf(default(T));
            var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            Marshal.Copy(
                source.Array,
                source.Offset,
                handle.AddrOfPinnedObject() + destinationStartIndex * size,
                destinationLength * size);
            handle.Free();
        }
    }
}
