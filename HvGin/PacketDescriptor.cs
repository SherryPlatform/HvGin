using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct PacketDescriptor
    {
        private ushort RawType;

        public ushort DataOffset8;

        public ushort Length8;

        public ushort Flags;

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
    };
}
