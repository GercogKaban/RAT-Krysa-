using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

class Program
{
    static Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        while (true)
        {
            TcpClient Client = server.AcceptTcpClient();
            Console.WriteLine("Клиент подключился.");
            NetworkStream Stream = Client.GetStream();

            using BinaryWriter Writer = new BinaryWriter(Stream);
            using MemoryStream BmpStream = new MemoryStream();

            byte[] SizeBytes = new byte[4];

            while (Client.Connected)
            {
                Stream.Read(SizeBytes, 0, 4);
                int ImageSize = BitConverter.ToInt32(SizeBytes, 0);


                int BytesRead = 0;
                byte[] Buffer = new byte[ImageSize];
                while (BytesRead < ImageSize)
                {
                    int ChunkSize = Stream.Read(Buffer, BytesRead, ImageSize - BytesRead);
                    if (ChunkSize == 0)
                    {
                        break;
                    }
                    BytesRead += ChunkSize;
                }

                if (ImageSize != 0)
                {
                    using (MemoryStream ImageStream = new MemoryStream(Buffer))
                    {
                        //ImageStream.Seek(0, SeekOrigin.Begin);
                        //using (FileStream FileStream = new FileStream("example.bin", FileMode.Create))
                        //{
                        //    ImageStream.CopyTo(FileStream);
                        //    ImageStream.Flush();
                        //}

                        Bitmap Screenshot = new Bitmap(ImageStream);
                        Screenshot.Save("test.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        Writer.Write(1);
                    }
                }
            }

            Stream.Close();
            Client.Close();
            Console.WriteLine("Клиент отключился.");
        }
    }
}