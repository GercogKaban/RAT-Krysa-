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
using Protocol;
using System.Threading.Tasks;


[Serializable]
class Test
{
    public int A { get; set; }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        //WinAPIModule.hookId = WinAPIModule.SetHook(WinAPIModule.HookCallback);

        TcpClient Client = new TcpClient();
        {
            Client.Connect("localhost", 8888);

            unsafe
            {
                var val = Client.SendBufferSize;
                var ptr = &val;
            }
            TCPProtocolLL<Test> TCPProtocol = new TCPProtocolLL<Test>(Client);
            Random Rand = new Random();
            while (Client.Connected)
            {
                Test TestPackage = new Test();
                TestPackage.A = Rand.Next(0, 100);
                await TCPProtocol.SendPackage(TestPackage);
            }
            Client.Close();
        }
           
        return 0;
    }

    //static Bitmap TakeScreenshot()
    //{
    //    return WinAPIModule.TakeScreenshot();
    //}

    //static int WaitForResponse(ref NetworkStream Stream)
    //{
    //    byte[] Buffer = new byte[1];
    //    int BytesRead = Stream.Read(Buffer, 0, 1);
    //    return BytesRead;
    //}
}
//class WinAPIModule
//{
//    [DllImport("user32.dll")]
//    static extern IntPtr GetDesktopWindow();

//    [DllImport("user32.dll")]
//    static extern IntPtr GetWindowDC(IntPtr hwnd);

//    [DllImport("gdi32.dll")]
//    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

//    [DllImport("gdi32.dll")]
//    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

//    [DllImport("gdi32.dll")]
//    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

//    [DllImport("gdi32.dll")]
//    static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, CopyPixelOperation rop);

//    [DllImport("gdi32.dll")]
//    static extern bool DeleteDC(IntPtr hdc);

//    [DllImport("gdi32.dll")]
//    public static extern bool DeleteObject(IntPtr hObject);

//    [DllImport("user32.dll")]
//    static extern bool ReleaseDC(IntPtr hwnd, IntPtr hdc);

//    [DllImport("user32.dll")]
//    static extern int GetSystemMetrics(int nIndex);

//    [DllImport("user32.dll")]
//    public static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

//    [DllImport("user32.dll")]
//    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

//    [DllImport("user32.dll")]
//    private static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

//    private const int WH_KEYBOARD_LL = 13;
//    private const int WM_KEYDOWN = 0x0100;
//    public static IntPtr hookId = IntPtr.Zero;

//    public delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
//    public static IntPtr SetHook(HookProc proc)
//    {
//        using (Process curProcess = Process.GetCurrentProcess())
//        using (ProcessModule curModule = curProcess.MainModule)
//        {
//            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
//        }
//    }
//    public static int HookCallback(int code, IntPtr wParam, IntPtr lParam)
//    {
//        if (code >= 0 && wParam.ToInt32() == WM_KEYDOWN)
//        {
//            Console.WriteLine("Key pressed");
//        }
//        return CallNextHookEx(hookId, code, wParam, lParam);
//    }

//    [DllImport("kernel32.dll")]
//    private static extern IntPtr GetModuleHandle(string lpModuleName);

//    public static Bitmap TakeScreenshot()
//    {
//        IntPtr hwnd = GetDesktopWindow();
//        IntPtr hdcSrc = GetWindowDC(hwnd);
//        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
//        int width = GetSystemMetrics(0);
//        int height = GetSystemMetrics(1);
//        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
//        IntPtr hOld = SelectObject(hdcDest, hBitmap);
//        BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
//        Bitmap Screenshot = Image.FromHbitmap(hBitmap);
//        SelectObject(hdcDest, hOld);
//        DeleteDC(hdcDest);
//        DeleteObject(hBitmap);
//        ReleaseDC(hwnd, hdcSrc);
//        return Screenshot;
//    }
//}