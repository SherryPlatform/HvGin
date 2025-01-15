using System.Runtime.InteropServices;

namespace HvGin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SynthRdpVersionResponse
    {
        public SynthRdpHeader Header;

        public SynthRdpVersion Version;

        public readonly uint Reserved;

        public byte IsAccepted;

        public bool IsAcceptedWithVersionExchange()
        {
            return IsAccepted == 2;
        }

        public void SetAcceptedWithVersionExchange()
        {
            IsAccepted = 2;
        }
    }
}
