// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Triplet<TSample> : ITriplet<TSample>
        where TSample : struct
    {
        [FieldOffset(0)]
        private readonly int _v1;

        [FieldOffset(4)]
        private readonly int _v2;

        [FieldOffset(8)]
        private readonly int _v3;

        public Triplet(int x1, int x2, int x3)
        {
            _v1 = x1;
            _v2 = x2;
            _v3 = x3;
        }

        public Triplet(TSample x1, TSample x2, TSample x3)
        {
            _v1 = (int)(object)x1;
            _v2 = (int)(object)x2;
            _v3 = (int)(object)x3;
        }

        public int v1 => _v1;

        public int v2 => _v2;

        public int v3 => _v3;

        public TSample R => (TSample)(object)v1;

        public TSample G => (TSample)(object)v2;

        public TSample B => (TSample)(object)v3;

        public static bool operator ==(Triplet<TSample> left, ITriplet<TSample> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Triplet<TSample> left, ITriplet<TSample> right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ITriplet<TSample> left, Triplet<TSample> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ITriplet<TSample> left, Triplet<TSample> right)
        {
            return !Equals(left, right);
        }

        public bool Equals(ITriplet<TSample> other)
        {
            return v1 == other.v1 && v2 == other.v2 && v3 == other.v3;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return Equals((ITriplet<TSample>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = v1.GetHashCode();
                hashCode = (hashCode * 397) ^ v2.GetHashCode();
                hashCode = (hashCode * 397) ^ v3.GetHashCode();
                return hashCode;
            }
        }

        public bool IsNear(ITriplet<TSample> other, int tolerance)
        {
            return Math.Abs(other.v1 - v1) <= tolerance && Math.Abs(other.v2 - v2) <= tolerance
                   && Math.Abs(other.v3 - v3) <= tolerance;
        }
    }
}
