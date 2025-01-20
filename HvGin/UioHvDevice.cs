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

        private void UpdateWriteOffset(
            int AccessorOffset,
            uint Offset)
        {
            if (AccessorOffset != OutgoingControlOffset &&
                AccessorOffset != IncomingControlOffset)
            {
                throw new ArgumentException();
            }
            // Write to RingControlBlock's In field.
            Accessor.Write(AccessorOffset, Offset);
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

        private byte[] PeekAvailableBytes()
        {
            RingControlBlock ControlBlock =
                GetRingControlBlock(IncomingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Read;
            if (AvailableSize == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] Data = new byte[AvailableSize];
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
                Data,
                0,
                FirstReadSize);
            if (SecondReadSize > 0)
            {
                Accessor.ReadArray(
                    IncomingDataOffset,
                    Data,
                    FirstReadSize,
                    SecondReadSize);
            }
            return Data;
        }

        private void CommitReadOperation(
            int ProcessedSize)
        {
            RingControlBlock ControlBlock =
                GetRingControlBlock(IncomingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Read;
            if (ProcessedSize > AvailableSize)
            {
                throw new ArgumentException();
            }
            uint FinalOut = Convert.ToUInt32(ControlBlock.Out + ProcessedSize);
            if (ControlBlock.In < ControlBlock.Out)
            {
                int FragileSize =
                    DataMaximumSize - Convert.ToInt32(ControlBlock.Out);
                if (FragileSize < ProcessedSize)
                {
                    FinalOut = Convert.ToUInt32(ProcessedSize - FragileSize);
                }
            }
            // Write to RingControlBlock's Out field.
            Accessor.Write(IncomingControlOffset + sizeof(uint), FinalOut);
            // Signal to Host
            SignalHost();
        }

        private int WriteAvailableBytes(
            byte[] Content)
        {
            RingControlBlock ControlBlock =
               GetRingControlBlock(OutgoingControlOffset);
            int AvailableSize =
                GetAvailableSizeInformation(ControlBlock).Write - sizeof(ulong);
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
            Accessor.WriteArray(
                OutgoingDataOffset + ControlBlock.In,
                Content,
                0,
                FirstWriteSize);
            if (SecondWriteSize > 0)
            {
                Accessor.WriteArray(
                    OutgoingDataOffset,
                    Content,
                    FirstWriteSize,
                    SecondWriteSize);
            }
            uint FinalWriteOffset = Convert.ToUInt32(
                SecondWriteSize > 0
                ? SecondWriteSize
                : ControlBlock.In + WriteSize);
            Accessor.WriteArray(
                OutgoingDataOffset + FinalWriteOffset,
                BitConverter.GetBytes(ControlBlock.In),
                0,
                sizeof(uint));
            FinalWriteOffset += sizeof(ulong);
            UpdateWriteOffset(
                OutgoingControlOffset,
                FinalWriteOffset);
            return WriteSize;
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

        [DllImport("libc", SetLastError = true)]
        private static extern long write(
            int fd,
            byte[] buffer,
            ulong count);

        public void SignalHost()
        {
            byte[] Bytes = BitConverter.GetBytes(1);
            long result = write(
                FileDescriptor,
                Bytes,
                Convert.ToUInt64(Bytes.Length));
            if (result != Bytes.Length)
            {
                throw new Exception("SignalHost Failed");
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern long pread(
            int fd,
            byte[] buffer,
            ulong count,
            long offset);

        public void WaitHost()
        {
            byte[] Bytes = BitConverter.GetBytes(1);
            long result = pread(
                FileDescriptor,
                Bytes,
                Convert.ToUInt64(Bytes.Length),
                0);
            if (result < 0)
            {
                const int EINTR = 4;
                const int EAGAIN = 11;
                int Errno = Marshal.GetLastWin32Error();
                if (Errno != EINTR && Errno != EAGAIN)
                {
                    throw new Exception("WaitHost Failed");
                }
            }
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
            int DescriptorSize = Marshal.SizeOf<PacketDescriptor>();
            int HeaderSize = Marshal.SizeOf<PipeHeader>();
            PacketDescriptor Descriptor = new PacketDescriptor();
            Descriptor.Type = PacketType.DataInBand;
            Descriptor.DataOffset = DescriptorSize;
            Descriptor.Length = DescriptorSize + HeaderSize + Content.Length;
            Descriptor.CompletionRequested = false;
            Descriptor.TransactionId = ulong.MaxValue;
            PipeHeader Header = new PipeHeader();
            Header.Type = PipeMessageType.Data;
            Header.DataSize = Convert.ToUInt32(Content.Length);
            byte[] Current = new byte[Descriptor.Length];
            GCHandle DescriptorHandle = GCHandle.Alloc(
                Descriptor,
                GCHandleType.Pinned);
            Marshal.Copy(
                DescriptorHandle.AddrOfPinnedObject(),
                Current,
                0,
                DescriptorSize);
            DescriptorHandle.Free();
            GCHandle HeaderHandle = GCHandle.Alloc(
                Header,
                GCHandleType.Pinned);
            Marshal.Copy(
                HeaderHandle.AddrOfPinnedObject(),
                Current,
                DescriptorSize,
                HeaderSize);
            HeaderHandle.Free();
            Content.CopyTo(Current, DescriptorSize + HeaderSize);
            WriteBytes(Current);
            SignalHost();
        }

        public byte[] Receive()
        {
            byte[] RawBytes = PeekAvailableBytes();
            if (RawBytes.Length == 0)
            {
                WaitHost();
                return Array.Empty<byte>();
            }
            int DescriptorSize = Marshal.SizeOf<PacketDescriptor>();
            int HeaderSize = Marshal.SizeOf<PipeHeader>();
            PacketDescriptor Descriptor =
                Utilities.BytesToStructure<PacketDescriptor>(
                    RawBytes.Take(DescriptorSize).ToArray());
            if (Descriptor.Type != PacketType.DataInBand)
            {
                throw new Exception("Unexpected PacketType");
            }
            PipeHeader Header =
                Utilities.BytesToStructure<PipeHeader>(
                    RawBytes.Skip(DescriptorSize).Take(HeaderSize).ToArray());
            if (Header.Type != PipeMessageType.Data)
            {
                throw new Exception("Unexpected PipeMessageType");
            }
            int ContentSize = Convert.ToInt32(Header.DataSize);
            byte[] RawContent =
                RawBytes.Skip(DescriptorSize + HeaderSize).ToArray();
            if (RawContent.Length < ContentSize)
            {
                WaitHost();
                return Array.Empty<byte>();
            }
            byte[] Content = RawContent.Take(ContentSize).ToArray();
            CommitReadOperation(Descriptor.Length + sizeof(ulong));
            return Content;
        }
    }
}
