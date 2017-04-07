// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Diagnostics;

namespace CharLS
{
    // Implements statistical modelling for the run mode context.
    // Computes model dependent parameters like the golomb code lengths
    public class CContextRunMode
    {
        // Note: members are sorted based on their size.
        private int A;

        private byte _nReset;

        private byte N;

        private byte Nn;

        public CContextRunMode(int a, int nRItype, int nReset)
        {
            A = a;
            _nRItype = nRItype;
            _nReset = (byte)nReset;
            N = 1;
            Nn = 0;
        }

        public int _nRItype { get; }

        public int GetGolomb()
        {
            int TEMP = A + (N >> 1) * _nRItype;
            int Ntest = N;
            int k = 0;
            for (; Ntest < TEMP; k++)
            {
                Ntest <<= 1;
                Debug.Assert(k <= 32);
            }
            return k;
        }


        public void UpdateVariables(int Errval, int EMErrval)
        {
            if (Errval < 0)
            {
                Nn = (byte)(Nn + 1);
            }
            A = A + ((EMErrval + 1 - _nRItype) >> 1);
            if (N == _nReset)
            {
                A = A >> 1;
                N = (byte)(N >> 1);
                Nn = (byte)(Nn >> 1);
            }
            N = (byte)(N + 1);
        }


        public int ComputeErrVal(int temp, int k)
        {
            bool map = (temp & 1) != 0;
            int errvalabs = (temp + (map ? 1 : 0)) / 2;

            if ((k != 0 || 2 * Nn >= N) == map)
            {
                Debug.Assert(map == ComputeMap(-errvalabs, k));
                return -errvalabs;
            }

            Debug.Assert(map == ComputeMap(errvalabs, k));
            return errvalabs;
        }


        public bool ComputeMap(int Errval, int k)
        {
            if (k == 0 && Errval > 0 && 2 * Nn < N) return true;

            if (Errval < 0 && 2 * Nn >= N) return true;

            if (Errval < 0 && k != 0) return true;

            return false;
        }


        public bool ComputeMapNegativeE(int k)
        {
            return k != 0 || 2 * Nn >= N;
        }
    }
}
