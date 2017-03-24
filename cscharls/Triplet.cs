// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public struct Triplet<TSample> : ITriplet<TSample>
        where TSample : struct
    {
        public Triplet(int x1, int x2, int x3)
        {
            V1 = x1;
            V2 = x2;
            V3 = x3;
        }

        public int V1 { get; }

        public int V2 { get; }

        public int V3 { get; }

        public TSample R => (TSample)(object)V1;

        public TSample G => (TSample)(object)V2;

        public TSample B => (TSample)(object)V3;

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
            return V1 == other.V1 && V2 == other.V2 && V3 == other.V3;
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
                var hashCode = V1.GetHashCode();
                hashCode = (hashCode * 397) ^ V2.GetHashCode();
                hashCode = (hashCode * 397) ^ V3.GetHashCode();
                return hashCode;
            }
        }

        public bool IsNear(ITriplet<TSample> other, int tolerance)
        {
            return Math.Abs(other.V1 - V1) <= tolerance && Math.Abs(other.V2 - V2) <= tolerance
                   && Math.Abs(other.V3 - V3) <= tolerance;
        }
    }
}
