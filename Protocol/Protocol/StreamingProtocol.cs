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
        }

        public async Task<bool> SendPackage(T Package)
        {
            return await SendData(Util.Serialize(Package), true);
        }

        public async Task<T> ReceivePackage()
        {
            byte[] Data = await ReceiveData();
            T Package = Util.Deserialize<T>(Data);
            return Package;
        }
        private async Task<bool> SendData(byte[] Data, bool Compression)
        {
            if (!Client.Connected || Data.Length == 0)
            {
                throw new ArgumentException("Client is not connected or data is empty.");
            }

            else
            {
                Stream = Client.GetStream();
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
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Write(DataHeaderBytes, 0, DataHeaderBytes.Length);
                    stream.Write(NewData, 0, NewData.Length);
                    byte[] DataBuffer = stream.ToArray();
                    await AsyncSend(DataBuffer);
                }
            }
            return true;
        }

        private async Task<byte[]> ReceiveData()
        {
            await ReceiveSemaphore.WaitAsync();

            try
            {
                Stream = Client.GetStream();
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
                    return Buffer;
                }
            }

            finally
            {
                ReceiveSemaphore.Release();
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
        //SemaphoreSlim SendSemaphore = new SemaphoreSlim(1);
        SemaphoreSlim ReceiveSemaphore = new SemaphoreSlim(1);
        Header CurrentHeader = new Header();
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
            try
            {
                BinaryFormatter Formatter = new BinaryFormatter();
                using (MemoryStream Stream = new MemoryStream())
                {
                    Formatter.Serialize(Stream, InObject);
                    return Stream.ToArray();
                }
            }

            catch (SerializationException ex)
            {
                Console.WriteLine("Error during serialization: {0}", ex.Message);
                return default(byte[]);
            }
        }

        static public byte[] Serialize(Header InHeader)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, InHeader);
                    return stream.ToArray();
                }
            }
            catch (SerializationException ex)
            {
                Console.WriteLine("Error during serialization: {0}", ex.Message);
                return default(byte[]);
            }
        }

        static public T Deserialize<T>(byte[] Data)
        {
            try
            {
                BinaryFormatter Formatter = new BinaryFormatter();

                using (MemoryStream Stream = new MemoryStream(Data))
                {
                    T Result = (T)Formatter.Deserialize(Stream);
                    return Result;
                }
            }

            catch (SerializationException ex)
            {
                Console.WriteLine("Error during deserialization: {0}", ex.Message);
                return default(T);
            }
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
