using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct PacketDescriptor
    {
        private ushort RawType;

        private ushort RawDataOffset8;

        private ushort RawLength8;

        private ushort RawFlags;

        public ulong TransactionId;

        public PacketType Type
        {
            get
            {
                return Enum.IsDefined(typeof(PacketType), RawType)
                    ? (PacketType)RawType
                    : PacketType.Invalid;
            }
            set
            {
                RawType = Convert.ToUInt16(value);
            }
        }

        public int DataOffset
        {
            get
            {
                return Convert.ToInt32(RawDataOffset8 << 3);
            }
            set
            {
                RawDataOffset8 = Math.Min(
                    Convert.ToUInt16(Utilities.GetAlignedSize(value, 8) >> 3),
                    ushort.MaxValue);
            }
        }

        public int Length
        {
            get
            {
                return Convert.ToInt32(RawLength8 << 3);
            }
            set
            {
                RawLength8 = Math.Min(
                    Convert.ToUInt16(Utilities.GetAlignedSize(value, 8) >> 3),
                    ushort.MaxValue);
            }
        }

        public bool CompletionRequested
        {
            get
            {
                return (RawFlags & 1) != 0;
            }
            set
            {
                if (value)
                {
                    RawFlags |= 1;
                }
                else
                {
                    RawFlags &= 0xFFFE;
                }
            }
        }
    };
}
