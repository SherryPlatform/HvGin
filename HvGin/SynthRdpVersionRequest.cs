﻿using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SynthRdpVersionRequest
    {
        public SynthRdpHeader Header;

        public SynthRdpVersion Version;

        public readonly uint Reserved;
    }
}
