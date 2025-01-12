using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct PacketDescriptor
    {
        public ushort Type;

        public ushort DataOffset8;

        public ushort Length8;

        public ushort Flags;

        public ulong TransactionId;
    };
}
