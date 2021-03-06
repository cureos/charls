﻿// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public interface ITriplet<TSample> : IEquatable<ITriplet<TSample>>
        where TSample : struct
    {
        int v1 { get; }

        int v2 { get; }

        int v3 { get; }

        TSample R { get; }

        TSample G { get; }

        TSample B { get; }

        bool IsNear(ITriplet<TSample> other, int tolerance);
    }
}
