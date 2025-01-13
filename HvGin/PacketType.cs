namespace HvGin
{
    internal enum PacketType : ushort
    {
        Invalid = 0x0,

        // 1 through 5 are reserved.

        DataInBand = 0x6,
        DataUsingTransferPages = 0x7,

        // 8 is reserved.

        DataUsingGpaDirect = 0x9,
        CancelRequest = 0xa,
        Completion = 0xb,
    }
}
