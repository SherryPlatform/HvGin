using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace HvGin
{
    public class UioHvDevice
    {
        private UioDeviceInformation DeviceInformation;

        private int RingBufferSize;
        private int ControlMaximumSize;
        private int DataMaximumSize;

        private int OutgoingControlOffset;
        private int OutgoingDataOffset;
        private int IncomingControlOffset;
        private int IncomingDataOffset;

        private FileStream DeviceFileStream;
        private MemoryMappedFile DeviceMappedFile;

        private MemoryMappedViewAccessor OutgoingControlAccessor;
        private MemoryMappedViewAccessor OutgoingDataAccessor;
        private MemoryMappedViewAccessor IncomingControlAccessor;
        private MemoryMappedViewAccessor IncomingDataAccessor;

        private RingControlBlock GetRingControlBlock(
            MemoryMappedViewAccessor ControlAccessor)
        {
            if (ControlAccessor != OutgoingControlAccessor &&
                ControlAccessor != IncomingControlAccessor)
            {
                throw new ArgumentException();
            }
            RingControlBlock Result;
            ControlAccessor.Read(0, out Result);
            return Result;
        }

        private void UpdateReadOffset(
            MemoryMappedViewAccessor ControlAccessor,
            uint Offset)
        {
            if (ControlAccessor != OutgoingControlAccessor &&
                ControlAccessor != IncomingControlAccessor)
            {
                throw new ArgumentException();
            }
            // Write to RingControlBlock's Out field.
            ControlAccessor.Write(sizeof(uint), Offset);
        }

        private void UpdateWriteOffset(
            MemoryMappedViewAccessor ControlAccessor,
            uint Offset)
        {
            if (ControlAccessor != OutgoingControlAccessor &&
                ControlAccessor != IncomingControlAccessor)
            {
                throw new ArgumentException();
            }
            // Write to RingControlBlock's In field.
            ControlAccessor.Write(0, Offset);
        }

        private (int Read, int Write) GetAvailableSizeInformation(
            RingControlBlock ControlBlock)
        {
            if (ControlBlock.In >= ControlBlock.Out)
            {
                int ReadableSize = Convert.ToInt32(
                    ControlBlock.In - ControlBlock.Out);
                return (ReadableSize, DataMaximumSize - ReadableSize);
            }
            int WritableSize = Convert.ToInt32(
                ControlBlock.Out - ControlBlock.In);
            return (DataMaximumSize - WritableSize, WritableSize);
        }

        private byte[] ReadAvailableBytes(
            int Size)
        {
            RingControlBlock ControlBlock =
                GetRingControlBlock(IncomingControlAccessor);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Read;
            int ReadSize = Math.Min(Size, AvailableSize);
            byte[] Data = new byte[ReadSize];
            int FirstReadSize = ReadSize;
            int SecondReadSize = 0;
            if (ControlBlock.In < ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.Out);
                if (FragileSize < ReadSize)
                {
                    FirstReadSize = FragileSize;
                    SecondReadSize = ReadSize - FragileSize;
                }
            }
            IncomingDataAccessor.ReadArray(
                ControlBlock.Out,
                Data,
                0,
                FirstReadSize);
            if (SecondReadSize > 0)
            {
                IncomingDataAccessor.ReadArray(
                    0,
                    Data,
                    FirstReadSize,
                    SecondReadSize);
            }
            UpdateReadOffset(
                IncomingControlAccessor,
                Convert.ToUInt32(
                    SecondReadSize > 0
                    ? SecondReadSize
                    : ControlBlock.Out + ReadSize));
            return Data;
        }

        private int WriteAvailableBytes(
            byte[] Content)
        {
            RingControlBlock ControlBlock =
               GetRingControlBlock(OutgoingControlAccessor);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Write;
            int WriteSize = Math.Min(
                Convert.ToInt32(Content.Length),
                AvailableSize);
            int FirstWriteSize = WriteSize;
            int SecondWriteSize = 0;
            if (ControlBlock.In >= ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.In);
                if (FragileSize < WriteSize)
                {
                    FirstWriteSize = FragileSize;
                    SecondWriteSize = WriteSize - FragileSize;
                }
            }
            OutgoingDataAccessor.WriteArray(
                ControlBlock.In,
                Content,
                0,
                FirstWriteSize);
            if (SecondWriteSize > 0)
            {
                OutgoingDataAccessor.WriteArray(
                    0,
                    Content,
                    FirstWriteSize,
                    SecondWriteSize);
            }
            UpdateWriteOffset(
                OutgoingControlAccessor,
                Convert.ToUInt32(
                    SecondWriteSize > 0
                    ? SecondWriteSize
                    : ControlBlock.In + WriteSize));
            return WriteSize;
        }

        private byte[] ReadBytes(
            int Size)
        {
            byte[] Data = new byte[Size];
            int ProcessedSize = 0;
            int UnprocessedSize = Size;
            while (UnprocessedSize != 0)
            {
                byte[] Current = ReadAvailableBytes(UnprocessedSize);
                if (Current.Length > 0)
                {
                    Current.CopyTo(Data, ProcessedSize);
                    ProcessedSize += Current.Length;
                    UnprocessedSize -= Current.Length;
                }
            }
            return Data;
        }

        private void WriteBytes(
            byte[] Content)
        {
            int ProcessedSize = 0;
            int UnprocessedSize = Content.Length;
            byte[] Current = Content;
            while (UnprocessedSize != 0)
            {
                int CurrentSize = WriteAvailableBytes(Current);
                if (CurrentSize > 0)
                {
                    ProcessedSize += CurrentSize;
                    UnprocessedSize -= CurrentSize;
                    Current = new byte[UnprocessedSize];
                    Array.Copy(
                        Content,
                        ProcessedSize,
                        Current,
                        0,
                        UnprocessedSize);
                }
            }
        }

        private void SignalHost()
        {
            byte[] Bytes = BitConverter.GetBytes(1);
            DeviceFileStream.Write(Bytes, 0, Bytes.Length);
        }

        private void WaitHost()
        {
            byte[] Bytes = BitConverter.GetBytes(1);
            DeviceFileStream.Read(Bytes, 0, Bytes.Length);
        }

        public UioHvDevice(
            string InstanceId)
        {
            DeviceInformation = UioHv.GetDeviceInformation(InstanceId);
            foreach (var MapItem in DeviceInformation.MemoryMap)
            {
                if (MapItem.Name != "txrx_rings" || MapItem.Offset != 0)
                {
                    continue;
                }

                RingBufferSize = Convert.ToInt32(MapItem.Size / 2);
                ControlMaximumSize = UioHv.PageSize;
                DataMaximumSize = RingBufferSize - ControlMaximumSize;

                OutgoingControlOffset = 0;
                OutgoingDataOffset = OutgoingControlOffset + ControlMaximumSize;
                IncomingControlOffset = OutgoingControlOffset + RingBufferSize;
                IncomingDataOffset = IncomingControlOffset + ControlMaximumSize;

                DeviceFileStream = new FileStream(
                    DeviceInformation.DeviceObjectPath,
                    FileMode.Open,
                    FileAccess.ReadWrite);
                DeviceMappedFile = MemoryMappedFile.CreateFromFile(
                    DeviceFileStream,
                    null,
                    MapItem.Size,
                    MemoryMappedFileAccess.ReadWrite,
                    HandleInheritability.None,
                    false);

                OutgoingControlAccessor = DeviceMappedFile.CreateViewAccessor(
                    OutgoingControlOffset,
                    ControlMaximumSize);
                OutgoingDataAccessor = DeviceMappedFile.CreateViewAccessor(
                    OutgoingDataOffset,
                    DataMaximumSize);
                IncomingControlAccessor = DeviceMappedFile.CreateViewAccessor(
                    IncomingControlOffset,
                    ControlMaximumSize);
                IncomingDataAccessor = DeviceMappedFile.CreateViewAccessor(
                    IncomingDataOffset,
                    DataMaximumSize);

                return;
            }
            throw new Exception("Failed to create UioHvDevice");
        }

        public void Send(
            byte[] Content)
        {
            PacketDescriptor Descriptor = new PacketDescriptor();
            Descriptor.Type = PacketType.DataInBand;
            Descriptor.DataOffset = Marshal.SizeOf<PacketDescriptor>();
            Descriptor.Length = Descriptor.DataOffset + Content.Length;
            Descriptor.CompletionRequested = false;
            Descriptor.TransactionId = ulong.MaxValue;
            byte[] Current = new byte[Descriptor.Length];
            GCHandle DescriptorHandle = GCHandle.Alloc(
                Descriptor,
                GCHandleType.Pinned);
            Marshal.Copy(
                DescriptorHandle.AddrOfPinnedObject(),
                Current,
                0,
                Descriptor.DataOffset);
            DescriptorHandle.Free();
            Content.CopyTo(Current, Descriptor.DataOffset);
            WriteBytes(Current);
            SignalHost();
        }

        public byte[] Receive()
        {
            WaitHost();
            PacketDescriptor Descriptor = new PacketDescriptor();
            byte[] Current = ReadBytes(Marshal.SizeOf<PacketDescriptor>());
            GCHandle DescriptorHandle = GCHandle.Alloc(
                Descriptor,
                GCHandleType.Pinned);
            Marshal.Copy(
                Current,
                0,
                DescriptorHandle.AddrOfPinnedObject(),
                Current.Length);
            DescriptorHandle.Free();
            if (Descriptor.Type != PacketType.DataInBand)
            {
                throw new Exception("Unexpected packet type");
            }
            return ReadBytes(Descriptor.Length - Descriptor.DataOffset);
        }
    }
}
