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
            using NetworkStream Stream = Client.GetStream();

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
                    Test Package = TCPProtocol.ReceivePackage().Result;
                    Console.Out.WriteLine(Package?.a.ToString());
                    //Stream.Read(SizeBytes, 0, 4);
                    //int ImageSize = BitConverter.ToInt32(SizeBytes, 0);
                    //int BytesRead = 0;
                    //byte[] Buffer = new byte[ImageSize];
                    //while (BytesRead < ImageSize)
                    //{
                    //    int ChunkSize = Stream.Read(Buffer, BytesRead, ImageSize - BytesRead);
                    //    if (ChunkSize == 0)
                    //    {
                    //        break;
                    //    }
                    //    BytesRead += ChunkSize;
                    //    BytesPerSecond += ChunkSize;
                    //}

                    //if (ImageSize != 0)
                    //{
                    //    using (MemoryStream ImageStream = new MemoryStream(Buffer))
                    //    {
                    //        Bitmap Screenshot = new Bitmap(ImageStream);
                    //        Screenshot.Save("test.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    //        Screenshot.Dispose();
                    //        Writer.Write(1);
                    //        FPS++;
                    //    }
                    //}
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