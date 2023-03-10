using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Timers;
using Protocol;

[Serializable]
class Test
{
    public int a;
}
class Program
{
    static Task Main(string[] args)
    {
        TcpListener Server = new TcpListener(IPAddress.Any, 8888);
        Server.Start();

        while (true)
        {
            using TcpClient Client = Server.AcceptTcpClient();
            Console.WriteLine("Client connected");
            //using NetworkStream Stream = Client.GetStream();

            //using BinaryWriter Writer = new BinaryWriter(Stream);
            //using MemoryStream BmpStream = new MemoryStream();

            //byte[] SizeBytes = new byte[4];

            //using Timer StatTimer = new Timer(1000);
            //StatTimer.Elapsed += TimerElapsed;
            //StatTimer.Start();

            TCPProtocolLL<Test> TCPProtocol = new TCPProtocolLL<Test>(Client);

            try
            {
                while (Client.Connected)
                {
                    if (!TCPProtocol.IsReceivingPackage)
                    {
                        Test Package = TCPProtocol.ReceivePackage().Result;
                        if (Package != null)
                        {
                            Console.Out.WriteLine(Package.a.ToString());
                        }
                    }
                }
            }

            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            BytesPerSecond = 0;
            Console.WriteLine("Client disconnected");
        }
        Server.Stop();
    }

    static void TimerElapsed(object Sender, ElapsedEventArgs E)
    {
        Console.WriteLine("FPS: {0}\n{1} Kb/s", FPS, BytesPerSecond / 1024);
        BytesPerSecond = 0;
        FPS = 0;
    }

    static private int BytesPerSecond = 0;
    static private int FPS = 0;
}