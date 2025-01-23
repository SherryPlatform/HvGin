using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace HvGin
{
    internal class SynthRdp
    {
        private static readonly string ControlClassId =
            "f8e65716-3cb3-4a06-9a60-1889c5cccab5";

        private static readonly string ControlInstanceId =
            "99221fa0-24ad-11e2-be98-001aa01bbf6e";

        private static readonly string DataClassId =
            "f9e9c0d3-b511-4a48-8046-d38079a8830c";

        private static readonly string[] DataInstanceIds =
        {
            "99221fa1-24ad-11e2-be98-001aa01bbf6e",
            "99221fa2-24ad-11e2-be98-001aa01bbf6e",
            "99221fa3-24ad-11e2-be98-001aa01bbf6e",
            "99221fa4-24ad-11e2-be98-001aa01bbf6e",
            "99221fa5-24ad-11e2-be98-001aa01bbf6e"
        };

        private static UioHvDevice WaitForDataChannel()
        {
            string DataChannelInstanceId = string.Empty;
            while (string.IsNullOrEmpty(DataChannelInstanceId))
            {
                foreach (string InstanceId in DataInstanceIds)
                {
                    try
                    {
                        UioHv.GetDeviceInformation(InstanceId);
                        DataChannelInstanceId = InstanceId;
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            Console.WriteLine("DataChannelInstanceId: " + DataChannelInstanceId);
            return new UioHvDevice(DataChannelInstanceId);
        }

        public static bool IsRunning = true;

        public static bool DebugMode = false;

        private static void ServiceMain()
        {
            UioHv.RegisterDevice(ControlClassId);
            UioHv.RegisterDevice(DataClassId);

            UioHvDevice Device = new UioHvDevice(ControlInstanceId);

            SynthRdpVersionRequest Request = new SynthRdpVersionRequest();
            Request.Header.Type = SynthRdpMessageType.VersionRequest;
            Request.Header.Size = Marshal.SizeOf<SynthRdpVersionRequest>();
            Request.Version.MajorVersion = 1;
            Request.Version.MinorVersion = 0;
            Device.Send(Utilities.StructureToBytes(Request));

            SynthRdpVersionResponse Response =
                Utilities.BytesToStructure<SynthRdpVersionResponse>(
                    Device.Receive());
            if (!Response.IsAcceptedWithVersionExchange())
            {
                throw new Exception("SynthRdpVersionResponse Invalid");
            }

            Console.WriteLine("SynthRdp Service Initialized");

            while (IsRunning)
            {
                UioHvDevice DataChannel = WaitForDataChannel();

                TcpClient RdpServer = new TcpClient();
                RdpServer.Connect("127.0.0.1", 3389);

                Socket RdpServerSocket = RdpServer.Client;
                RdpServerSocket.Blocking = false;

                // X.224 Connection Request PDU (Patched)
                try
                {
                    byte[] Content = DataChannel.Receive();
                    // Set requestedProtocols to PROTOCOL_RDP (0x00000000).
                    Content[15] = 0x00;
                    if (DebugMode)
                    {
                        Utilities.PrintBytes(
                            "VMBus -> TCP",
                            Content);
                    }
                    RdpServerSocket.Send(Content);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                bool ShouldRunning = true;

                Thread Vmbus2TcpThread = new Thread(() =>
                {
                    while (ShouldRunning)
                    {
                        try
                        {
                            byte[] ReceiveContent = DataChannel.Receive();
                            if (ReceiveContent.Length != 0)
                            {
                                if (DebugMode)
                                {
                                    Utilities.PrintBytes(
                                        "VMBus -> TCP",
                                        ReceiveContent);
                                }
                                RdpServerSocket.Send(ReceiveContent);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            break;
                        }
                    }
                });

                Thread Tcp2VmbusThread = new Thread(() =>
                {
                    byte[] SendBuffer = new byte[16384];
                    while (ShouldRunning)
                    {
                        try
                        {
                            if (RdpServerSocket.Available != 0)
                            {
                                Array.Clear(SendBuffer, 0, SendBuffer.Length);
                                int Count = RdpServerSocket.Receive(
                                    SendBuffer,
                                    SendBuffer.Length,
                                    SocketFlags.None);
                                byte[] SendContent =
                                    SendBuffer.Take(Count).ToArray();
                                if (DebugMode)
                                {
                                    Utilities.PrintBytes(
                                        "TCP -> VMBus",
                                        SendContent);
                                }
                                DataChannel.Send(SendContent);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            break;
                        }
                    }
                });

                Vmbus2TcpThread.Start();
                Tcp2VmbusThread.Start();

                while (true)
                {
                    Thread.Sleep(100);
                    if (!Vmbus2TcpThread.IsAlive || !Tcp2VmbusThread.IsAlive)
                    {
                        ShouldRunning = false;
                        break;
                    }
                }

                RdpServer.Close();
            }

            Console.WriteLine("Exit SynthRdp Service");
        }

        private static Thread? ServiceThread = null;

        public static void Start()
        {
            if (ServiceThread != null)
            {
                throw new Exception("SynthRdp Service is already running");
            }
            IsRunning = true;
            ServiceThread = new Thread(ServiceMain);
            ServiceThread.Start();
        }

        public static void Stop()
        {
            if (ServiceThread == null)
            {
                throw new Exception("SynthRdp Service is not running");
            }
            IsRunning = false;
            ServiceThread.Join();
            ServiceThread = null;
        }
    }
}
