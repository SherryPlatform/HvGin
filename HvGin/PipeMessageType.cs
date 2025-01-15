namespace HvGin
{
    internal enum PipeMessageType : uint
    {
        Invalid = 0,
        Data = 1,
        Partial = 2,
        SetupGpaDirect = 3,
        TeardownGpaDirect = 4,
        IndicationComplete = 5,
    }
}
