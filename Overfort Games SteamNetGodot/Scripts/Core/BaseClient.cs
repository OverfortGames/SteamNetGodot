using Steamworks;
using LiteNetLib.Utils;
using System;
using Godot;
using Steamworks.Data;
using System.Runtime.InteropServices;

namespace OverfortGames.SteamNetGodot
{
    public class BaseClient : ConnectionManager
    {
        private const string LOG_NAME = "CLIENT";

        public Processor Processor { get; private set; }

        public BaseClient()
        {
            Processor = new Processor();
        }

        public void SubscribeRPC<T>(Action<T> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
            Processor.PacketProcessor.SubscribeNetSerializable<T>(callback, destroyPredicate);
        }

        public void SubscribeRPC<T, TUserData>(Action<T, TUserData> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
            Processor.PacketProcessor.SubscribeNetSerializable<T, TUserData>(callback, destroyPredicate);
        }

        public override void OnConnectionChanged(ConnectionInfo info)
        {
            base.OnConnectionChanged(info);

            GD.Print($"[{LOG_NAME}] {info.identity.SteamId} - {info.State}");
        }

        public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            base.OnMessage(data, size, messageNum, recvTime, channel);

            Processor.Read(data, size, Connection);
        }

        public void Send<T>(T packet, SendType sendType) where T : struct, INetSerializable
        {
            if (Connected == false)
                return;

            IntPtr data = IntPtr.Zero;
            try
            {
                data = Processor.WriteGetIntPtr(packet, out var size);
                Connection.SendMessage(data, size, sendType);
            }
            finally
            {
                if (data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(data); // Ensure memory is always freed
                }
            }
        }

        public virtual void Tick(ulong tick) { }
    }
}
