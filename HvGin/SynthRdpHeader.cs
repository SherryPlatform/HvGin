using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SynthRdpHeader
    {
        private uint RawType;

        public int Size;

        public SynthRdpMessageType Type
        {
            get
            {
                return Enum.IsDefined(typeof(SynthRdpMessageType), RawType)
                    ? (SynthRdpMessageType)RawType
                    : SynthRdpMessageType.Error;
            }
            set
            {
                RawType = Convert.ToUInt32(value);
            }
        }
    }
}
