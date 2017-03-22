// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;

namespace CharLS
{
    public class Triplet<TSample> : IEquatable<Triplet<TSample>>
    {
        public Triplet() : this(0, 0, 0)
        {
        }

        public Triplet(int x1, int x2, int x3)
        {
            v1 = (TSample) (object) x1;
            v2 = (TSample) (object) x2;
            v3 = (TSample) (object) x3;
        }

        protected Triplet(Triplet<TSample> triplet)
        {
            v1 = triplet.v1;
            v2 = triplet.v2;
            v3 = triplet.v3;
        }

        public TSample v1 { get; }

        public TSample v2 { get; }

        public TSample v3 { get; }

        public TSample R => v1;

        public TSample G => v2;

        public TSample B => v3;

        public bool Equals(Triplet<TSample> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TSample>.Default.Equals(v1, other.v1) &&
                   EqualityComparer<TSample>.Default.Equals(v2, other.v2) &&
                   EqualityComparer<TSample>.Default.Equals(v3, other.v3);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Triplet<TSample>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TSample>.Default.GetHashCode(v1);
                hashCode = (hashCode * 397) ^ EqualityComparer<TSample>.Default.GetHashCode(v2);
                hashCode = (hashCode * 397) ^ EqualityComparer<TSample>.Default.GetHashCode(v3);
                return hashCode;
            }
        }

        public static bool operator ==(Triplet<TSample> left, Triplet<TSample> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Triplet<TSample> left, Triplet<TSample> right)
        {
            return !Equals(left, right);
        }
    }
}
