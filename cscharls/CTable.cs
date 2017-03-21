// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Diagnostics;

namespace CharLS
{
    public class CTable
    {
        internal const int cbit = 8;

        private readonly Code[] _rgtype;

        public CTable()
        {
            _rgtype = new Code[1 << cbit];
        }

        public void AddEntry(byte bvalue, Code c)
        {
            int length = c.GetLength();
            Debug.Assert(length <= cbit);

            for (var i = 0; i < 1 << (cbit - length); ++i)
            {
                Debug.Assert(_rgtype[(bvalue << (cbit - length)) + i].GetLength() == 0);
                _rgtype[(bvalue << (cbit - length)) + i] = c;
            }
        }

        public Code Get(int value)
        {
            return _rgtype[value];
        }
    }
}
