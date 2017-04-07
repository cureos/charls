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
            SetDelimited(x1, x2, x3, out _r, out _g, out _b);
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

        private static void SetDelimited(int x1, int x2, int x3, out TSample v1, out TSample v2, out TSample v3)
        {
            int min = int.MinValue, max = int.MaxValue;

            var type = typeof(TSample);
            if (type == typeof(byte))
            {
                min = byte.MinValue;
                max = byte.MaxValue;
            }
            else if (type == typeof(sbyte))
            {
                min = sbyte.MinValue;
                max = sbyte.MaxValue;
            }
            else if (type == typeof(ushort))
            {
                min = ushort.MinValue;
                max = ushort.MaxValue;
            }
            else if (type == typeof(short))
            {
                min = short.MinValue;
                max = short.MaxValue;
            }
            else if (type == typeof(uint))
            {
                min = (int)uint.MinValue;
            }

            v1 = (TSample)Convert.ChangeType(Math.Min(Math.Max(x1, min), max), type);
            v2 = (TSample)Convert.ChangeType(Math.Min(Math.Max(x2, min), max), type);
            v3 = (TSample)Convert.ChangeType(Math.Min(Math.Max(x3, min), max), type);
        }
    }
}
