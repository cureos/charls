// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;

using static CharLS.util;

namespace CharLS
{
    //
    // Purpose: a JPEG-LS context with it's current statistics.
    //
    public class JlsContext
    {
        private int A;

        private int B;

        private short C;

        private short N;

        public JlsContext()
            : this(0)
        {
        }


        public JlsContext(int a)
        {
            A = a;
            B = 0;
            C = 0;
            N = 1;
        }


        public int GetErrorCorrection(int k)
        {
            if (k != 0) return 0;

            return BitWiseSign(2 * B + N - 1);
        }


        public void UpdateVariables(int errorValue, int NEAR, int NRESET)
        {
            Debug.Assert(N != 0);

            // For performance work on copies of A,B,N (compiler will use registers).
            int a = A + Math.Abs(errorValue);
            int b = B + errorValue * (2 * NEAR + 1);
            int n = N;

            Debug.Assert(a < 65536 * 256);
            Debug.Assert(Math.Abs(b) < 65536 * 256);

            if (n == NRESET)
            {
                a = a >> 1;
                b = b >> 1;
                n = n >> 1;
            }

            A = a;
            n = n + 1;
            N = (short)n;

            if (b + n <= 0)
            {
                b = b + n;
                if (b <= -n)
                {
                    b = -n + 1;
                }
                C -= (short)(C > -128 ? 1 : 0);
            }
            else if (b > 0)
            {
                b = b - n;
                if (b > 0)
                {
                    b = 0;
                }
                C += (short)(C < 127 ? 1 : 0);
            }
            B = b;

            Debug.Assert(N != 0);
        }

        public int GetGolomb()
        {
            int Ntest = N;
            int Atest = A;

            if (Ntest >= Atest) return 0;
            if (Ntest << 1 >= Atest) return 1;
            if (Ntest << 2 >= Atest) return 2;
            if (Ntest << 3 >= Atest) return 3;
            if (Ntest << 4 >= Atest) return 4;

            int k = 5;
            for (; Ntest << k < Atest; k++)
            {
                Debug.Assert(k <= 32);
            }
            return k;
        }
    }
}
