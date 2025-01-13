namespace HvGin
{
    internal class Utilities
    {
        public static int GetAlignedSize(
            int Size,
            int Alignment)
        {
            return (Size + Alignment - 1) & ~(Alignment - 1);
        }
    }
}
