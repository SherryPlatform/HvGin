using Microsoft.Win32.SafeHandles;
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

        private MemoryMappedFile DeviceMappedFile;
        private MemoryMappedViewAccessor Accessor;

        private int FileDescriptor
        {
            get
            {
                SafeMemoryMappedFileHandle SafeHandle =
                DeviceMappedFile.SafeMemoryMappedFileHandle;
                return SafeHandle.DangerousGetHandle().ToInt32();
            }
        }

        private RingControlBlock GetRingControlBlock(
            int AccessorOffset)
        {
            if (AccessorOffset != OutgoingControlOffset &&
                AccessorOffset != IncomingControlOffset)
            {
                throw new ArgumentException();
            }
            RingControlBlock Result;
            Accessor.Read(AccessorOffset, out Result);
            return Result;
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

        public byte[] PopPipePacket()
        {
            RingControlBlock ControlBlock =
                GetRingControlBlock(IncomingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Read;
            if (AvailableSize < UioHv.PipePacketHeaderSize)
            {
                return Array.Empty<byte>();
            }
            int FirstReadSize = AvailableSize;
            int SecondReadSize = 0;
            if (ControlBlock.In < ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.Out);
                if (FragileSize < AvailableSize)
                {
                    FirstReadSize = FragileSize;
                    SecondReadSize = AvailableSize - FragileSize;
                }
            }
            byte[] AvailableBytes = new byte[AvailableSize];
            Accessor.ReadArray(
                IncomingDataOffset + ControlBlock.Out,
                AvailableBytes,
                0,
                FirstReadSize);
            if (SecondReadSize > 0)
            {
                Accessor.ReadArray(
                    IncomingDataOffset,
                    AvailableBytes,
                    FirstReadSize,
                    SecondReadSize);
            }
            PacketDescriptor Descriptor =
                Utilities.BytesToStructure<PacketDescriptor>(AvailableBytes);
            if (Descriptor.Type != PacketType.DataInBand)
            {
                throw new Exception("Unexpected PacketType");
            }
            if (Descriptor.DataOffset < UioHv.PacketDescriptorSize ||
                Descriptor.DataOffset > AvailableBytes.Length ||
                Descriptor.DataOffset > Descriptor.Length)
            {
                throw new Exception("Unexpected DataOffset");
            }
            int ActualPacketSize = Descriptor.Length + sizeof(ulong);
            if (AvailableBytes.Length < ActualPacketSize)
            {
                throw new Exception("Unexpected PacketSize");
            }
            AvailableBytes = AvailableBytes.Skip(
                Descriptor.DataOffset).ToArray();
            PipeHeader Header =
                Utilities.BytesToStructure<PipeHeader>(AvailableBytes);
            if (Header.Type != PipeMessageType.Data)
            {
                throw new Exception("Unexpected PipeMessageType");
            }
            int PipeDataSize = Convert.ToInt32(Header.DataSize);
            if (AvailableBytes.Length < UioHv.PipeHeaderSize + PipeDataSize)
            {
                throw new Exception("Unexpected DataSize");
            }
            byte[] Content = AvailableBytes.Skip(
                UioHv.PipeHeaderSize).Take(PipeDataSize).ToArray();
            uint FinalOffset = Convert.ToUInt32(
                ControlBlock.Out + ActualPacketSize);
            if (ControlBlock.In < ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.Out);
                if (FragileSize < ActualPacketSize)
                {
                    FinalOffset = Convert.ToUInt32(
                        ActualPacketSize - FragileSize);
                }
            }
            // Write to RingControlBlock's Out field.
            Accessor.Write(IncomingControlOffset + sizeof(uint), FinalOffset);
            return Content;
        }

        public int GetMaximumPushSize()
        {
            const int MaximumPushSize = 16384;
            RingControlBlock ControlBlock =
               GetRingControlBlock(OutgoingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Write;
            AvailableSize -= UioHv.PipePacketHeaderSize + sizeof(ulong);
            AvailableSize -= AvailableSize % sizeof(ulong);
            return AvailableSize < 0
                ? 0
                : Math.Min(AvailableSize, MaximumPushSize);
        }

        public void PushPipePacket(
            byte[] Content)
        {
            PacketDescriptor Descriptor = new PacketDescriptor();
            Descriptor.Type = PacketType.DataInBand;
            Descriptor.DataOffset = UioHv.PacketDescriptorSize;
            Descriptor.Length = UioHv.PipePacketHeaderSize + Content.Length;
            Descriptor.CompletionRequested = false;
            Descriptor.TransactionId = ulong.MaxValue;
            PipeHeader Header = new PipeHeader();
            Header.Type = PipeMessageType.Data;
            Header.DataSize = Convert.ToUInt32(Content.Length);
            int PacketSize = Descriptor.Length + sizeof(ulong);
            RingControlBlock ControlBlock =
               GetRingControlBlock(OutgoingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Write;
            if (AvailableSize < PacketSize)
            {
                throw new Exception("Insufficient Space");
            }
            byte[] PacketBytes = new byte[PacketSize];
            Utilities.StructureToBytes(Descriptor).CopyTo(
                PacketBytes,
                0);
            Utilities.StructureToBytes(Header).CopyTo(
                PacketBytes,
                UioHv.PacketDescriptorSize);
            Content.CopyTo(
                PacketBytes,
                UioHv.PipePacketHeaderSize);
            BitConverter.GetBytes(ControlBlock.In).CopyTo(
                PacketBytes,
                PacketBytes.Length - sizeof(ulong));
            int FirstWriteSize = PacketBytes.Length;
            int SecondWriteSize = 0;
            if (ControlBlock.In >= ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.In);
                if (FragileSize < PacketBytes.Length)
                {
                    FirstWriteSize = FragileSize;
                    SecondWriteSize = PacketBytes.Length - FragileSize;
                }
            }
            Accessor.WriteArray(
                OutgoingDataOffset + ControlBlock.In,
                PacketBytes,
                0,
                FirstWriteSize);
            if (SecondWriteSize > 0)
            {
                Accessor.WriteArray(
                    OutgoingDataOffset,
                    PacketBytes,
                    FirstWriteSize,
                    SecondWriteSize);
            }
            uint FinalOffset = Convert.ToUInt32(
                SecondWriteSize > 0
                ? SecondWriteSize
                : ControlBlock.In + PacketBytes.Length);
            // Write to RingControlBlock's In field.
            Accessor.Write(OutgoingControlOffset, FinalOffset);
        }

        public void InterruptControl(
            bool Enable)
        {
            byte[] RawBytes = BitConverter.GetBytes(Enable ? 1 : 0);
            int Result = Utilities.PosixWrite(
                FileDescriptor,
                RawBytes,
                RawBytes.Length);
            if (Result != RawBytes.Length)
            {
                throw new Exception("InterruptControl Failed");
            }
        }

        public bool WaitInterrupt()
        {
            byte[] RawBytes = new byte[sizeof(int)];
            int Result = Utilities.PosixRead(
                FileDescriptor,
                RawBytes,
                RawBytes.Length);
            if (Result < 0)
            {
                const int EINTR = 4;
                const int EAGAIN = 11;
                int ErrorCode = Marshal.GetLastWin32Error();
                if (ErrorCode != EINTR && ErrorCode != EAGAIN)
                {
                    throw new Exception("WaitInterrupt Failed");
                }
                return false;
            }
            return true;
        }

        public void SignalHost()
        {
            InterruptControl(true);
        }

        public void WaitHost()
        {
            WaitInterrupt();
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
                DeviceMappedFile = MemoryMappedFile.CreateFromFile(
                    DeviceInformation.DeviceObjectPath,
                    FileMode.Open,
                    null,
                    MapItem.Size,
                    MemoryMappedFileAccess.ReadWrite);
                Accessor = DeviceMappedFile.CreateViewAccessor(
                    MapItem.Offset,
                    MapItem.Size);

                // Write to RingControlBlock's InterruptMask field.
                Accessor.Write(
                    IncomingControlOffset + (2 * sizeof(uint)),
                    0);

                return;
            }
            throw new Exception("Failed to create UioHvDevice");
        }

        public void Send(
            byte[] Content)
        {
            PushPipePacket(Content);
            SignalHost();
        }

        public byte[] Receive()
        {
            byte[] Content = PopPipePacket();
            if (Content.Length == 0)
            {
                WaitHost();
                return Array.Empty<byte>();
            }
            SignalHost();
            return Content;
        }
    }
}
