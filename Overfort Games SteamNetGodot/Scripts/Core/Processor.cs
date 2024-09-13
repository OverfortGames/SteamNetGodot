using LiteNetLib.Utils;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;

namespace OverfortGames.SteamNetGodot
{
    public class Processor
    {
        public NetPacketProcessor PacketProcessor { get; private set; }

        private NetDataWriter dataWriter;
        private NetDataReader dataReader;

        private byte[] readBuffer;
        private const int READ_BUFFER_SIZE = 4096 * 4;

        public Processor()
        {
            PacketProcessor = new NetPacketProcessor();
            dataWriter = new NetDataWriter();
            dataReader = new NetDataReader();
            readBuffer = new byte[READ_BUFFER_SIZE];
        }

        public IntPtr WriteGetIntPtr<T>(T packet, out int size) where T : struct, INetSerializable
        {
            dataWriter.Reset();
            PacketProcessor.WriteNetSerializable(dataWriter, ref packet);
            size = dataWriter.Length;
            return GetSubset(dataWriter.Data, 0, dataWriter.Length);
        }

        public void Read(IntPtr data, int size, Connection connection)
        {
            if (size > READ_BUFFER_SIZE)
            {
                throw new Exception($"Message size {size} exceeds buffer capacity {READ_BUFFER_SIZE}");
            }

            Marshal.Copy(data, readBuffer, 0, size);
            dataReader.SetSource(readBuffer, 0, size);

            PacketProcessor.ReadPacket(readBuffer, size, dataReader, connection);
        }

        public unsafe IntPtr GetSubset(byte[] data, int startIndex, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (startIndex < 0 || length <= 0 || startIndex + length > data.Length)
            {
                throw new ArgumentOutOfRangeException("Invalid start index or length");
            }

            // Allocate unmanaged memory for the subset
            IntPtr subsetPtr = Marshal.AllocHGlobal(length);

            // Copy the subset of the byte array to unmanaged memory
            Marshal.Copy(data, startIndex, subsetPtr, length);

            // Call the original SendMessage method using the subset
            return subsetPtr;
        }
    }
}