using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SynthRdpVersion
    {
        public uint AsDWORD;

        public ushort MajorVersion
        {
            get
            {
                return Convert.ToUInt16(AsDWORD & 0x0000FFFF);
            }
            set
            {
                AsDWORD |= Convert.ToUInt32(value);
            }
        }

        public ushort MinorVersion
        {
            get
            {
                return Convert.ToUInt16((AsDWORD & 0xFFFF0000) >> 16);
            }
            set
            {
                AsDWORD |= Convert.ToUInt32(value << 16);
            }
        }
    }
}
