// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Quad<TSample> : ITriplet<TSample> where TSample : struct
    {
        private readonly int _v1;

        private readonly int _v2;

        private readonly int _v3;

        private readonly int _v4;

        public Quad(ITriplet<TSample> triplet, int alpha)
        {
            _v1 = triplet.v1;
            _v2 = triplet.v2;
            _v3 = triplet.v3;
            _v4 = alpha;
        }

        public int v1 => _v1;

        public int v2 => _v2;

        public int v3 => _v3;

        public int v4 => _v4;

        public TSample R => (TSample)(object)v1;

        public TSample G => (TSample)(object)v2;

        public TSample B => (TSample)(object)v3;

        public TSample A => (TSample)(object)v4;

        public static bool operator ==(Quad<TSample> left, ITriplet<TSample> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Quad<TSample> left, ITriplet<TSample> right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ITriplet<TSample> left, Quad<TSample> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ITriplet<TSample> left, Quad<TSample> right)
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
