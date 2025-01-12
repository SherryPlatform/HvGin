using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RingControlBlock
    {
        public uint In;

        public uint Out;

        public uint InterruptMask;

        public uint PendingSendSize;

        public unsafe fixed uint Reserved[12];

        public uint FeatureBitsValue;

        public bool SupportsPendingSendSize
        {
            get
            {
                return (FeatureBitsValue & 1) != 0;
            }
            set
            {
                if (value)
                {
                    FeatureBitsValue |= 1;
                }
                else
                {
                    FeatureBitsValue &= ~Convert.ToUInt32(1);
                }
            }
        }
    };
}
