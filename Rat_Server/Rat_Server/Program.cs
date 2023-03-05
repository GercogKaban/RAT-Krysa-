using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Drawing;

class Program
{
    static void Main(string[] args)
    {
        // Создаем экземпляр объекта TcpListener для прослушивания входящих подключений на порту 8888
        TcpListener server = new TcpListener(IPAddress.Any, 8888);
        server.Start();

        while (true)
        {
            // Принимаем входящее подключение и создаем объект TcpClient для работы с клиентом
            TcpClient Client = server.AcceptTcpClient();

            // Получаем поток для чтения данных из сети
            NetworkStream Stream = Client.GetStream();

            // Создание объекта BinaryWriter для записи данных в поток
            using BinaryWriter Writer = new BinaryWriter(Stream);
            using MemoryStream BmpStream = new MemoryStream();

            byte[] sizeBytes = new byte[4];

            while (Client.Connected)
            {
                Stream.Read(sizeBytes, 0, 4);
                int ImageSize = BitConverter.ToInt32(sizeBytes, 0);

                // Прочитать данные изображения
                int bytesRead = 0;
                byte[] buffer = new byte[ImageSize];
                while (bytesRead < ImageSize)
                {
                    int chunkSize = Stream.Read(buffer, bytesRead, ImageSize - bytesRead);
                    if (chunkSize == 0)
                    {
                        break;
                    }
                    bytesRead += chunkSize;
                }

                if (ImageSize != 0)
                {
                    // Создать объект Bitmap из полученных данных
                    using (MemoryStream imageStream = new MemoryStream(buffer))
                    {
                        imageStream.Seek(0, SeekOrigin.Begin);
                        using (FileStream FileStream = new FileStream("example.bin", FileMode.Create))
                        {
                            imageStream.CopyTo(FileStream);
                            imageStream.Flush();
                        }

                        Bitmap screenshot = new Bitmap(imageStream);
                        screenshot.Save("test.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                        Writer.Write(1);
                    }
                }
            }

            // Закрываем поток и клиентский сокет
            Stream.Close();
            Client.Close();
            Console.WriteLine("Клиент отключился.");
        }
    }
}