using Steamworks;
using LiteNetLib.Utils;
using System;
using Godot;
using Steamworks.Data;
using System.Runtime.InteropServices;

namespace OverfortGames.SteamNetGodot
{
    public class BaseServer : SocketManager
    {
        private const string LOG_NAME = "SERVER";

        public const int PORT = 7777;

        public Processor Processor { get; private set; }

        public BaseServer()
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

        public override void OnConnectionChanged(Connection connection, ConnectionInfo info)
        {
            base.OnConnectionChanged(connection, info);

            GD.Print($"[{LOG_NAME}] {info.identity.SteamId} - {info.State}");
        }

        public override void OnMessage(Steamworks.Data.Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);

            Processor.Read(data, size, connection);
        }

        public void Broadcast<T>(T packet, SendType sendType, Connection except = default) where T : struct, INetSerializable
        {
            IntPtr data = IntPtr.Zero;
         
            try
            {
                data = Processor.WriteGetIntPtr(packet, out var size);

                foreach (var connection in Connected)
                {
                    if (connection != except)
                    {
                        connection.SendMessage(data, size, sendType);
                    }
                }
            }
            finally
            {
                if (data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(data); // Ensure memory is always freed
                }
            }
        }

        public void BroadcastExceptLocalhost<T>(T packet, SendType sendType) where T : struct, INetSerializable
        {
            Broadcast(packet, sendType, GetLocalConnection());
        }

        public void Send<T>(T packet, Connection toConnection, SendType sendType) where T : struct, INetSerializable
        {
            IntPtr data = IntPtr.Zero;

            try
            {
                data = Processor.WriteGetIntPtr(packet, out var size);
                toConnection.SendMessage(data, size, sendType);
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

        private Connection _localConnectionCache;
        public Connection GetLocalConnection()
        {
            if (_localConnectionCache.Id == 0)
            {
                foreach (var connection in Connected)
                {
                    if (connection.UserData == (long)SteamClient.SteamId.Value)
                        _localConnectionCache = connection;
                }
            }

            return _localConnectionCache;
        }
    }


}
