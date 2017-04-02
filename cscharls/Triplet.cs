// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Triplet<TSample> : ITriplet<TSample>
        where TSample : struct
    {
        private readonly TSample _r;

        private readonly TSample _g;

        private readonly TSample _b;

        public Triplet(int x1, int x2, int x3)
        {
            _r = (TSample)Convert.ChangeType(x1, typeof(TSample));
            _g = (TSample)Convert.ChangeType(x2, typeof(TSample));
            _b = (TSample)Convert.ChangeType(x3, typeof(TSample));
        }

        public Triplet(TSample x1, TSample x2, TSample x3)
        {
            _r = x1;
            _g = x2;
            _b = x3;
        }

        public int v1 => Convert.ToInt32(_r);

        public int v2 => Convert.ToInt32(_g);

        public int v3 => Convert.ToInt32(_b);

        public TSample R => _r;

        public TSample G => _g;

        public TSample B => _b;

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
