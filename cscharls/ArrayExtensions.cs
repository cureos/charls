// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    public static class ArrayExtensions
    {
        internal static void CopyTo<T>(this T[] source, ArraySegment<byte> destination, int startIndex, int length)
        {
            var handle = GCHandle.Alloc(source[startIndex], GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), destination.Array, destination.Offset, length);
            handle.Free();
        }

        internal static void CopyTo<T>(this ArraySegment<byte> source, T[] destination, int startIndex, int length)
        {
            var handle = GCHandle.Alloc(destination[startIndex], GCHandleType.Pinned);
            Marshal.Copy(source.Array, source.Offset, handle.AddrOfPinnedObject(), length);
            handle.Free();
        }
    }
}
