using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct PipeHeader
    {
        private uint RawPacketType;

        private uint RawDataSize;

        private void ThrowArgumentException(
            string PropertyName)
        {
            throw new ArgumentException(string.Format(
                "{0} is not available for {1} messages.",
                PropertyName,
                Type == PipeMessageType.Partial ? "partial" : "non-partial"));
        }

        public PipeMessageType Type
        {
            get
            {
                return Enum.IsDefined(typeof(PipeMessageType), RawPacketType)
                    ? (PipeMessageType)RawPacketType
                    : PipeMessageType.Invalid;
            }
            set
            {
                RawPacketType = Convert.ToUInt32(value);
            }
        }

        public uint DataSize
        {
            get
            {
                if (Type == PipeMessageType.Partial)
                {
                    ThrowArgumentException("DataSize");
                }
                return RawDataSize;
            }
            set
            {
                if (Type == PipeMessageType.Partial)
                {
                    ThrowArgumentException("DataSize");
                }
                RawDataSize = value;
            }
        }

        public ushort PartialDataSize
        {
            get
            {
                if (Type != PipeMessageType.Partial)
                {
                    ThrowArgumentException("PartialDataSize");
                }
                return Convert.ToUInt16(RawDataSize);
            }
            set
            {
                if (Type != PipeMessageType.Partial)
                {
                    ThrowArgumentException("PartialDataSize");
                }
                RawDataSize = Convert.ToUInt32(value);
            }
        }

        public ushort PartialOffset
        {
            get
            {
                if (Type != PipeMessageType.Partial)
                {
                    ThrowArgumentException("PartialOffset");
                }
                return Convert.ToUInt16(RawDataSize >> 16);
            }
            set
            {
                if (Type != PipeMessageType.Partial)
                {
                    ThrowArgumentException("PartialOffset");
                }
                RawDataSize |= Convert.ToUInt32(value) << 16;
            }
        }
    }
}
