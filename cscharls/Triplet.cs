// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;

namespace CharLS
{
    public class Triplet<SAMPLE> : IEquatable<Triplet<SAMPLE>>
    {
        public Triplet() : this(0, 0, 0)
        {
        }

        public Triplet(int x1, int x2, int x3)
        {
            v1 = (SAMPLE) (object) x1;
            v2 = (SAMPLE) (object) x2;
            v3 = (SAMPLE) (object) x3;
        }

        protected Triplet(Triplet<SAMPLE> triplet)
        {
            v1 = triplet.v1;
            v2 = triplet.v2;
            v3 = triplet.v3;
        }

        public SAMPLE v1 { get; }

        public SAMPLE v2 { get; }

        public SAMPLE v3 { get; }

        public SAMPLE R => v1;

        public SAMPLE G => v2;

        public SAMPLE B => v3;

        public bool Equals(Triplet<SAMPLE> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<SAMPLE>.Default.Equals(v1, other.v1) &&
                   EqualityComparer<SAMPLE>.Default.Equals(v2, other.v2) &&
                   EqualityComparer<SAMPLE>.Default.Equals(v3, other.v3);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Triplet<SAMPLE>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<SAMPLE>.Default.GetHashCode(v1);
                hashCode = (hashCode * 397) ^ EqualityComparer<SAMPLE>.Default.GetHashCode(v2);
                hashCode = (hashCode * 397) ^ EqualityComparer<SAMPLE>.Default.GetHashCode(v3);
                return hashCode;
            }
        }

        public static bool operator ==(Triplet<SAMPLE> left, Triplet<SAMPLE> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Triplet<SAMPLE> left, Triplet<SAMPLE> right)
        {
            return !Equals(left, right);
        }
    }
}
