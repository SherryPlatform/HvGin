using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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

        public static T BytesToStructure<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors |
                DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(
            byte[] Bytes) where T : struct
        {
            GCHandle Handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            T Result = Marshal.PtrToStructure<T>(Handle.AddrOfPinnedObject());
            Handle.Free();
            return Result;
        }

        public static byte[] StructureToBytes<T>(
            T Structure) where T : struct
        {
            byte[] Result = new byte[Marshal.SizeOf<T>()];
            GCHandle Handle = GCHandle.Alloc(Result, GCHandleType.Pinned);
            Marshal.StructureToPtr(
                Structure,
                Handle.AddrOfPinnedObject(),
                false);
            Handle.Free();
            return Result;
        }

        public static void PrintBytes(
            string Tag,
            byte[] Bytes)
        {
            string UpperTag = string.Format("======== {0} ========", Tag);
            string LowerTag = string.Empty;
            for (int i = 0; i < UpperTag.Length; ++i)
            {
                LowerTag += "=";
            }
            Console.WriteLine(UpperTag);
            Console.WriteLine(BitConverter.ToString(Bytes).Replace("-", " "));
            Console.WriteLine(LowerTag);
        }
    }
}
