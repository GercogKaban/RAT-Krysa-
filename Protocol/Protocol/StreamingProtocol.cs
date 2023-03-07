using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Protocol
{
    public class Util
    {
        public static byte[] StructToBytes<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }
    }
    public class TCPProtocolLL
    {
        private struct Header
        {
            public Int32 DataLength;
            public Int32 PackageNum;
            public byte Compression;
        }
        TCPProtocolLL(TcpClient Client)
        {
            this.Client = Client;
        }

        public async void SendData(byte[] Data, bool Compression)
        {
            await SendSemaphore.WaitAsync();

            try
            {
                if (!Client.Connected || Data.Length == 0)
                {
                    throw new ArgumentException("Client is not connected or data is empty.");
                }

                else
                {
                    NetworkStream Stream = Client.GetStream();
                    using BinaryWriter Writer = new BinaryWriter(Stream);

                    byte[] NewData = Compression ? CompressData(Data) : Data;
                    Header DataHeader = new Header { DataLength = NewData.Length, PackageNum = ++PackageNum, Compression = 0 };

                    await AsyncSend(Util.StructToBytes(DataHeader));
                    await AsyncSend(Data);
                    await AsyncReceiveResponce();
                }
            }

            finally
            {
                SendSemaphore.Release();
            }
        }

        public async Task<byte[]> ReceiveData()
        {
            return new byte[1];
        }

        public byte[] CompressData(byte[] Data)
        {
            using (MemoryStream CompressedStream = new MemoryStream())
            {
                using (GZipStream GzipStream = new GZipStream(CompressedStream, CompressionMode.Compress))
                {
                    GzipStream.Write(Data, 0, Data.Length);
                }

                return CompressedStream.ToArray();
            }
        }

        public byte[] DecompressData(byte[] CompressedData)
        {
            using (MemoryStream DecompressedStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(new MemoryStream(CompressedData), CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(DecompressedStream);
                }

                return DecompressedStream.ToArray();
            }
        }

        private async Task AsyncSend(byte[] Data)
        {
            try
            {
                await Stream.WriteAsync(Data, 0, Data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
            }
        }

        private async Task<Header> AsyncReceiveResponce()
        {
            Header Result = new Header();
            try
            {
                byte[] Buffer = new byte[Marshal.SizeOf(typeof(Header))];
                await Stream.ReadAsync(Buffer);
                GCHandle Handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
                Result = Marshal.PtrToStructure<Header>(Handle.AddrOfPinnedObject());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
            }
            return Result;
        }

        TcpClient Client;
        NetworkStream Stream;
        Int32 PackageNum = 0;
        SemaphoreSlim SendSemaphore = new SemaphoreSlim(1);
    }


}
