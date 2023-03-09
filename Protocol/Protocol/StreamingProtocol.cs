using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text;

namespace Protocol
{
    [Serializable]
    struct Header
    {
        public Int32 DataLength;
        public Int32 PackageNum;
        public byte Compression;
        public DateTime SendingStartDate;

        public static bool operator ==(Header a, Header b)
        {
            // need byte check or hash
            return a.DataLength == b.DataLength && a.Compression == b.Compression && a.PackageNum == b.PackageNum && a.SendingStartDate == b.SendingStartDate;
        }

        public static bool operator !=(Header a, Header b)
        {
            // need byte check or hash
            return a.DataLength != b.DataLength || a.Compression != b.Compression || a.PackageNum != b.PackageNum || a.SendingStartDate != b.SendingStartDate;
        }
    }

    public class TCPProtocolLL<T>
    {
        public TCPProtocolLL(TcpClient Client)
        {
            this.Client = Client;
            Stream = Client.GetStream();
        }

        public async Task<bool> SendPackage(T Package)
        {
            return await SendData(Util.Serialize(Package), false);
        }

        public async Task<T> ReceivePackage()
        {
            IsReceivingPackage_ = true;
            byte[] Data = await ReceiveData();
            T Package = Util.Deserialize<T>(Data);
            IsReceivingPackage_ = false;
            return Package;
        }
        private async Task<bool> SendData(byte[] Data, bool Compression)
        {
            try 
            {
                if (!Client.Connected || Data.Length == 0)
                {
                    throw new ArgumentException("Client is not connected or data is empty.");
                }

                using BinaryWriter Writer = new BinaryWriter(Stream);

                byte[] NewData = Compression ? Util.CompressData(Data) : Data;
                Header DataHeader = new Header
                {
                    DataLength = NewData.Length,
                    PackageNum = ++PackageNum,
                    Compression = Compression ? (byte)1 : (byte)0,
                    SendingStartDate = DateTime.Now
                };

                byte[] DataHeaderBytes = Util.StructToBytes(DataHeader);
                using (MemoryStream MemStream = new MemoryStream())
                {
                    MemStream.Write(DataHeaderBytes, 0, DataHeaderBytes.Length);
                    MemStream.Write(NewData, 0, NewData.Length);
                    byte[] DataBuffer = MemStream.ToArray();
                    await AsyncSend(DataBuffer);
                }

                return true;
            }

            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return false;
            }
        }

        private async Task<byte[]> ReceiveData()
        {
            if (CurrentHeader.PackageNum == 0)
            {
                byte[] HeaderBuffer = new byte[Marshal.SizeOf(typeof(Header))];
                int temp = await Stream.ReadAsync(HeaderBuffer);
                Console.WriteLine(temp.ToString());
                CurrentHeader = Util.BytesToStruct<Header>(HeaderBuffer);
                CurrentBytesRead = 0;
            }

            if (CurrentHeader.DataLength != 0)
            {
                byte[] Buffer = new byte[CurrentHeader.DataLength];
                while (CurrentBytesRead < CurrentHeader.DataLength)
                {
                    Int32 ChunkSize =
                        await Stream.ReadAsync(Buffer, CurrentBytesRead, CurrentHeader.DataLength - CurrentBytesRead);

                    if (ChunkSize == 0)
                    {
                        break;
                    }
                    CurrentBytesRead += ChunkSize;
                }
                CurrentHeader = default(Header);
                return Buffer;
            }
            return default(byte[]);
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

        TcpClient Client;
        NetworkStream Stream;
        Int32 PackageNum = 0;
        Int32 CurrentBytesRead = 0;
        Header CurrentHeader = new Header();
        bool IsReceivingPackage_ = false;

        public bool IsReceivingPackage
        {
            get 
            {
                return IsReceivingPackage_;
            }
        }
    }

    class Util
    {
        static public byte[] CompressData(byte[] Data)
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

        static public byte[] DecompressData(byte[] CompressedData)
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

        static public byte[] Serialize<T>(in T InObject)
        {
            if (InObject == null)
            {
                throw new ArgumentNullException(nameof(InObject));
            }

            try
            {
                string JsonStr = JsonConvert.SerializeObject(InObject);
                return Encoding.UTF8.GetBytes(JsonStr);
            }

            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                // Log or handle the exception
                throw ex; 
            }
        }

        static public T Deserialize<T>(byte[] Data)
        {
            string JsonStr = Encoding.UTF8.GetString(Data);
            return JsonConvert.DeserializeObject<T>(JsonStr);
        }

        public static byte[] StructToBytes<T>(T obj) where T : struct
        {
            Int32 size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            T structObj = default(T);
            int size = Marshal.SizeOf(structObj);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, ptr, size);
                structObj = Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return structObj;
        }
    }
}
