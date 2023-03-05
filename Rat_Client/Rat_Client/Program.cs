using System;
using System.Net.Sockets;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Net;
using System.IO;


class Program
{
    static void Main(string[] args)
    {
        // Создаем экземпляр объекта TcpClient и подключаемся к серверу на порту 8888
        TcpClient Client = new TcpClient("localhost", 8888);

        // Получаем поток для чтения данных из сети
        NetworkStream Stream = Client.GetStream();

        // Создание объекта BinaryWriter для записи данных в поток
        using BinaryWriter Writer = new BinaryWriter(Stream);

        // Читаем сообщение от сервера
        byte[] Buffer = new byte[2048];
        int BytesRead;

        while (true)
        {
            using (Bitmap Screenshot = DoScreenshot())
            {
                using MemoryStream BmpStream = new MemoryStream();
                Screenshot.Save(BmpStream, System.Drawing.Imaging.ImageFormat.Bmp);
                BmpStream.Seek(0, SeekOrigin.Begin);

                using (FileStream FileStream = new FileStream("example.bin", FileMode.Create))
                {
                    BmpStream.CopyTo(FileStream);
                    BmpStream.Flush();
                }

                long BmpSize = BmpStream.Length;
                byte[] ImgSize = BitConverter.GetBytes(BmpSize);

                Writer.Write(ImgSize, 0, 4);
                BmpStream.Seek(0, SeekOrigin.Begin);
                while ((BytesRead = BmpStream.Read(Buffer, 0, Buffer.Length)) > 0)
                {
                    Writer.Write(Buffer, 0, BytesRead);
                }
            }
            Writer.Flush();
            WaitForResponse(ref Stream);
        }

        // Закрываем поток и клиентский сокет
        Stream.Close();
        Client.Close();
    }

    static Bitmap DoScreenshot()
    {
        // Получить первый экран
        var ScreenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

        // Создать объект Bitmap с размером экрана
        var Bitmap = new Bitmap(ScreenBounds.Width, ScreenBounds.Height);

        using (var Graphics_ = Graphics.FromImage(Bitmap))
        {
            Graphics_.CopyFromScreen(ScreenBounds.X, ScreenBounds.Y, 0, 0, ScreenBounds.Size);
        }

        return Bitmap;
    }

    static int GetBitmapSize(Bitmap Bmp)
    {
        // Получить размер одного пикселя в байтах
        int pixelSize = Image.GetPixelFormatSize(Bmp.PixelFormat) / 8;

        // Получить количество пикселей в изображении
        int width = Bmp.Width;
        int height = Bmp.Height;

        // Вычислить общий размер в байтах
        return width * height * pixelSize;
    }

    static int WaitForResponse(ref NetworkStream Stream)
    {
        byte[] Buffer = new byte[1];
        int BytesRead = Stream.Read(Buffer, 0, 1);
        return BytesRead;
    }
}