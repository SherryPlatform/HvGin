using System.Runtime.InteropServices;

namespace HvGin
{
    internal class UioHv
    {
        public static readonly int PageSize = Environment.SystemPageSize;

        public static UioDeviceInformation GetDeviceInformation(
            string InstanceId)
        {
            foreach (DirectoryInfo Instance in new DirectoryInfo(string.Format(
                "/sys/bus/vmbus/devices/{0}/uio",
                InstanceId)).GetDirectories())
            {
                string InstanceName = string.Format("/dev/{0}", Instance.Name);
                if (File.Exists(InstanceName))
                {
                    UioDeviceInformation Result = new UioDeviceInformation();
                    Result.DeviceObjectPath = InstanceName;
                    Result.MemoryMap = new List<UioDeviceMemoryMapItem>();
                    for (int i = 0; ; ++i)
                    {
                        string CurrentPath = string.Format(
                            "{0}/maps/map{1}",
                            Instance.FullName,
                            i);
                        if (!Directory.Exists(CurrentPath))
                        {
                            break;
                        }
                        UioDeviceMemoryMapItem Item = new UioDeviceMemoryMapItem();
                        Item.Name = File.ReadAllLines(
                            CurrentPath + "/name")[0];
                        // The parameter offset of the mmap() call has a special
                        // meaning for UIO devices: It is used to select which
                        // mapping of your device you want to map. To map the
                        // memory of mapping N, you have to use N times the page
                        // size as your offset.
                        Item.Offset = i * PageSize;
                        Item.Size = Convert.ToInt64(File.ReadAllLines(
                            CurrentPath + "/size")[0], 16);
                        Result.MemoryMap.Add(Item);
                    }
                    return Result;
                }
            }
            throw new Exception("Hyper-V VMBus device not found.");
        }

        public static void RegisterDevice(
            string ClassId)
        {
            try
            {
                File.WriteAllText(
                    "/sys/bus/vmbus/drivers/uio_hv_generic/new_id",
                    ClassId);
            }
            catch (Exception e)
            {
                const int EEXIST = 17;
                if (e.HResult != EEXIST)
                {
                    // Rethrow if not EEXIST a.k.a. already registered.
                    throw;
                }
            }
        }

        public static readonly int PacketDescriptorSize =
            Marshal.SizeOf<PacketDescriptor>();

        public static readonly int PipeHeaderSize =
            Marshal.SizeOf<PipeHeader>();

        public static readonly int PipePacketHeaderSize =
            PacketDescriptorSize + PipeHeaderSize;
    }
}
