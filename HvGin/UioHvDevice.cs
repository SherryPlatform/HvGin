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

        private byte[] GetPacket()
        {
            RingControlBlock ControlBlock =
                GetRingControlBlock(IncomingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Read;
            if (AvailableSize == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] RawBytes = new byte[AvailableSize];
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
            Accessor.ReadArray(
                IncomingDataOffset + ControlBlock.Out,
                RawBytes,
                0,
                FirstReadSize);
            if (SecondReadSize > 0)
            {
                Accessor.ReadArray(
                    IncomingDataOffset,
                    RawBytes,
                    FirstReadSize,
                    SecondReadSize);
            }
            PacketDescriptor Descriptor =
                Utilities.BytesToStructure<PacketDescriptor>(RawBytes);
            int Size = Descriptor.Length + sizeof(ulong);
            if (RawBytes.Length < Size)
            {
                throw new Exception("Unexpected PacketSize");
            }
            if (Descriptor.Type != PacketType.DataInBand)
            {
                throw new Exception("Unexpected PacketType");
            }
            uint FinalOut = Convert.ToUInt32(ControlBlock.Out + Size);
            if (ControlBlock.In < ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.Out);
                if (FragileSize < Size)
                {
                    FinalOut = Convert.ToUInt32(Size - FragileSize);
                }
            }
            // Write to RingControlBlock's Out field.
            Accessor.Write(IncomingControlOffset + sizeof(uint), FinalOut);
            return RawBytes.Skip(Descriptor.DataOffset).ToArray();
        }

        private void PutPacket(
            byte[] Content)
        {
            PacketDescriptor Descriptor = new PacketDescriptor();
            Descriptor.Type = PacketType.DataInBand;
            Descriptor.DataOffset = Marshal.SizeOf<PacketDescriptor>();
            Descriptor.Length = Descriptor.DataOffset + Content.Length;
            Descriptor.CompletionRequested = false;
            Descriptor.TransactionId = ulong.MaxValue;
            byte[] RawBytes = new byte[Descriptor.Length];
            Utilities.StructureToBytes(Descriptor).CopyTo(RawBytes, 0);
            Content.CopyTo(RawBytes, Descriptor.DataOffset);
            RingControlBlock ControlBlock =
               GetRingControlBlock(OutgoingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Write;
            if (AvailableSize < RawBytes.Length + sizeof(ulong))
            {
                throw new Exception("Insufficient Space");
            }
            int FirstWriteSize = RawBytes.Length;
            int SecondWriteSize = 0;
            if (ControlBlock.In >= ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.In);
                if (FragileSize < RawBytes.Length)
                {
                    FirstWriteSize = FragileSize;
                    SecondWriteSize = RawBytes.Length - FragileSize;
                }
            }
            Accessor.WriteArray(
                OutgoingDataOffset + ControlBlock.In,
                RawBytes,
                0,
                FirstWriteSize);
            if (SecondWriteSize > 0)
            {
                Accessor.WriteArray(
                    OutgoingDataOffset,
                    RawBytes,
                    FirstWriteSize,
                    SecondWriteSize);
            }
            uint FinalIn = Convert.ToUInt32(
                SecondWriteSize > 0
                ? SecondWriteSize
                : ControlBlock.In + RawBytes.Length);
            Accessor.WriteArray(
                OutgoingDataOffset + FinalIn,
                BitConverter.GetBytes(ControlBlock.In),
                0,
                sizeof(uint));
            FinalIn += sizeof(ulong);
            // Write to RingControlBlock's In field.
            Accessor.Write(OutgoingControlOffset, FinalIn);
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

        public int WaitInterrupt()
        {
            byte[] RawBytes = new byte[sizeof(int)];
            int Result = Utilities.PosixRead(
                FileDescriptor,
                RawBytes,
                RawBytes.Length);
            if (Result != RawBytes.Length)
            {
                throw new Exception("WaitInterrupt Failed");
            }
            return BitConverter.ToInt32(RawBytes, 0);
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

                byte[] RawMask = BitConverter.GetBytes(0);
                // Write to RingControlBlock's InterruptMask field.
                Accessor.WriteArray(
                    IncomingControlOffset + (2 * sizeof(uint)),
                    RawMask,
                    0,
                    RawMask.Length);

                return;
            }
            throw new Exception("Failed to create UioHvDevice");
        }

        public void Send(
            byte[] Content)
        {
            int HeaderSize = Marshal.SizeOf<PipeHeader>();
            PipeHeader Header = new PipeHeader();
            Header.Type = PipeMessageType.Data;
            Header.DataSize = Convert.ToUInt32(Content.Length);
            byte[] RawBytes = new byte[HeaderSize + Content.Length];
            Utilities.StructureToBytes(Header).CopyTo(RawBytes, 0);
            Content.CopyTo(RawBytes, HeaderSize);
            PutPacket(RawBytes);
            SignalHost();
        }

        public byte[] Receive()
        {
            byte[] RawBytes = GetPacket();
            if (RawBytes.Length == 0)
            {
                WaitHost();
                return Array.Empty<byte>();
            }
            int HeaderSize = Marshal.SizeOf<PipeHeader>();
            PipeHeader Header = Utilities.BytesToStructure<PipeHeader>(
                RawBytes.Take(HeaderSize).ToArray());
            if (Header.Type != PipeMessageType.Data)
            {
                throw new Exception("Unexpected PipeMessageType");
            }
            int Size = Convert.ToInt32(Header.DataSize);
            if (RawBytes.Length < HeaderSize + Size)
            {
                throw new Exception("Unexpected DataSize");
            }
            byte[] Content = RawBytes.Skip(HeaderSize).Take(Size).ToArray();
            SignalHost();
            return Content;
        }
    }
}
