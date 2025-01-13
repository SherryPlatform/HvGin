namespace HvGin
{
    internal class UioHv
    {
        public static UioDeviceInformation? GetDeviceInformation(
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
                    foreach (DirectoryInfo MapInstance in new DirectoryInfo(
                        Instance.FullName + "/maps").GetDirectories())
                    {
                        UioDeviceMemoryMapItem Item = new UioDeviceMemoryMapItem();
                        Item.Name = File.ReadAllLines(
                            MapInstance.FullName + "/name")[0];
                        Item.Size = long.Parse(File.ReadAllLines(
                            MapInstance.FullName + "/size")[0]);
                        Item.Offset = long.Parse(File.ReadAllLines(
                            MapInstance.FullName + "/offset")[0]);
                        Result.MemoryMap.Add(Item);
                    }
                    return Result;
                }
            }

            return null;
        }
    }
}
