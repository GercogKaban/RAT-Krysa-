using System;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        WinAPIModule.hookId = WinAPIModule.SetHook(WinAPIModule.HookCallback);

        TcpClient Client = new TcpClient("localhost", 8888);
        NetworkStream Stream = Client.GetStream();
        using BinaryWriter Writer = new BinaryWriter(Stream);

        byte[] Buffer = new byte[2048];
        int BytesRead;

        while (true)
        {
            using (Bitmap Screenshot = TakeScreenshot())
            {
                using MemoryStream BmpStream = new MemoryStream();

                var EncoderParameters = new EncoderParameters(1);
                EncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L); ;
                var codecInfo = ImageCodecInfo.GetImageDecoders()
                    .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                if (codecInfo == null)
                {
                    throw new InvalidOperationException("JPEG codec is not found");
                }

                Screenshot.Save(BmpStream, codecInfo, EncoderParameters);
                BmpStream.Seek(0, SeekOrigin.Begin);

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

        //UnhookWindowsHookEx(hookId);

        Writer.Dispose();
        Writer.Close();
        Stream.Dispose();
        Stream.Close();
        Client.Dispose();
        Client.Close();
    }

    static Bitmap TakeScreenshot()
    {
        return WinAPIModule.TakeScreenshot();
    }
    static int WaitForResponse(ref NetworkStream Stream)
    {
        byte[] Buffer = new byte[1];
        int BytesRead = Stream.Read(Buffer, 0, 1);
        return BytesRead;
    }
}

class WinAPIModule
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, CopyPixelOperation rop);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    static extern bool ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    public static IntPtr hookId = IntPtr.Zero;

    public delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
    public static IntPtr SetHook(HookProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }
    public static int HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam.ToInt32() == WM_KEYDOWN)
        {
            Console.WriteLine("Key pressed");
        }
        return CallNextHookEx(hookId, code, wParam, lParam);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public static Bitmap TakeScreenshot()
    {
        IntPtr hwnd = GetDesktopWindow();
        IntPtr hdcSrc = GetWindowDC(hwnd);
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        int width = GetSystemMetrics(0);
        int height = GetSystemMetrics(1);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
        IntPtr hOld = SelectObject(hdcDest, hBitmap);
        BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
        Bitmap Screenshot = Image.FromHbitmap(hBitmap);
        SelectObject(hdcDest, hOld);
        DeleteDC(hdcDest);
        DeleteObject(hBitmap);
        ReleaseDC(hwnd, hdcSrc);
        return Screenshot;
    }
}