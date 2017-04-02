// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Quad<TSample> : ITriplet<TSample> where TSample : struct
    {
        private readonly TSample _r;

        private readonly TSample _g;

        private readonly TSample _b;

        private readonly TSample _a;

        public Quad(ITriplet<TSample> triplet, int alpha)
        {
            _r = triplet.R;
            _g = triplet.G;
            _b = triplet.B;
            _a = (TSample)Convert.ChangeType(alpha, typeof(TSample));
        }

        public int v1 => Convert.ToInt32(_r);

        public int v2 => Convert.ToInt32(_g);

        public int v3 => Convert.ToInt32(_b);

        public int v4 => Convert.ToInt32(_a);

        public TSample R => _r;

        public TSample G => _g;

        public TSample B => _b;

        public TSample A => _a;

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
