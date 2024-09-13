using LiteNetLib.Utils;
using Steamworks.Data;
using System;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public struct PlayerData : INetSerializable
    {
        public ulong steamId;
        public string name;
        public int ping;

        public void Deserialize(NetDataReader reader)
        {
            steamId = reader.GetULong();
            name = reader.GetString();
            ping = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(steamId);
            writer.Put(name);
            writer.Put(ping);
        }
    }

    public class ObjectState
    {
        public const uint INVALID_NETWORK_ID = 521525125;
        public const uint SERVER_OWNERSHIP = 12525125;

        public readonly uint networkId;
        public ulong owner = SERVER_OWNERSHIP;

        private Pawn pawn;

        public bool IsAPlayer = false;

        public PlayerData playerData;

        private bool isDestroyed;

        public ObjectState(uint networkId)
        {
            this.networkId = networkId;

            NetworkManager.Instance.Client_SubscribeRPC<ObjectStatePacket>(Client_OnObjectStatePacketReceived, () => isDestroyed == true);
            NetworkManager.Instance.Client_SubscribeRPC<PlayerDataPacket>(Client_OnPlayerDataPacketReceived, () => isDestroyed == true);
            NetworkManager.Instance.Client_SubscribeRPC<OwnershipPacket>(Client_OnOwnershipPacketReceived, () => isDestroyed == true);

            NetworkManager.Instance.Server_SubscribeRPC<ObjectStateRequestPacket, Connection>(Server_OnObjectStateRequestPacketReceived, () => isDestroyed == true);
            NetworkManager.Instance.Server_SubscribeRPC<PlayerDataRequestPacket, Connection>(Server_OnPlayerDataRequestPacketReceived, () => isDestroyed == true);
        }

        public void MarkAsDestroyed()
        {
            isDestroyed = true;
        }

        public void SetOwner(ulong newOwner)
        {
            owner = newOwner;

            if (pawn.IsValid())
                pawn.networkOwner = owner;

            if (NetworkManager.Instance.IsServer())
            {
                OwnershipPacket packet = new OwnershipPacket();
                packet.networkId = networkId;
                packet.owner = newOwner;

                NetworkManager.Instance.Server.BroadcastExceptLocalhost(packet, Steamworks.Data.SendType.Reliable);
            }
        }

        public void SetPawn(Pawn newPawn)
        {
            pawn = newPawn;
            pawn.networkId = networkId;
            pawn.networkOwner = owner;
        }

        public Pawn GetPawn()
        {
            return pawn;
        }

        #region Client

        private void Client_OnPlayerDataPacketReceived(PlayerDataPacket packet)
        {
            if (networkId != packet.networkId)
                return;

            playerData = packet.playerData;
        }

        public void Client_SendPlayerDataPacketRequest()
        {
            NetworkManager.Instance.Client.Send(new PlayerDataRequestPacket() { networkId = this.networkId }, SendType.Reliable);
        }

        private void Client_OnObjectStatePacketReceived(ObjectStatePacket packet)
        {
            if (networkId != packet.networkId)
                return;

            owner = packet.owner;
            IsAPlayer = packet.isAPlayer;
            var pawn = SimpleResourceLoader.Instance.LoadResourceAndInstantiate(packet.pawnResourceId, SceneLoader.Instance.GameplayScene, (x) => {
                var _pawn = x as Pawn;
                _pawn.SetName(packet.name, true);

                SetPawn(_pawn);
            }) as Pawn;

            pawn.GlobalPosition = packet.pawnInitialPosition;
            pawn.Quaternion = packet.pawnInitialRotation;
        }

        private void Client_OnOwnershipPacketReceived(OwnershipPacket packet)
        {
            if (networkId != packet.networkId)
                return;

            owner = packet.owner;

            if (pawn.IsValid())
                pawn.networkOwner = owner;
        }

        public void Client_SendObjectStatePacketRequest()
        {
            var objectStateRequestPacket = new ObjectStateRequestPacket();
            objectStateRequestPacket.networkId = networkId;

            NetworkManager.Instance.Client.Send(objectStateRequestPacket, SendType.Reliable);
        }

        #endregion

        #region Server

        private void Server_OnObjectStateRequestPacketReceived(ObjectStateRequestPacket packet, Connection fromConnection)
        {
            if (networkId != packet.networkId)
                return;

            Server_SendObjectStatePacket(fromConnection);
        }

        public void Server_SendObjectStatePacket(Connection toConnection)
        {
            ObjectStatePacket objectStatePacket = new ObjectStatePacket();
            objectStatePacket.networkId = networkId;
            objectStatePacket.owner = owner;
            objectStatePacket.pawnResourceId = pawn.resourceId;
            objectStatePacket.pawnInitialPosition = pawn.GlobalPosition;
            objectStatePacket.pawnInitialRotation = pawn.Quaternion;
            objectStatePacket.isAPlayer = IsAPlayer;
            objectStatePacket.name = pawn.Name;

            NetworkManager.Instance.Server.Send<ObjectStatePacket>(objectStatePacket, toConnection, SendType.Reliable);
        }

        private void Server_OnPlayerDataRequestPacketReceived(PlayerDataRequestPacket packet, Connection fromConnection)
        {
            if (networkId != packet.networkId)
                return;

            Server_SendPlayerDataPacket(fromConnection);
        }

        private bool _sendingPlayerDataPacked = false;
        public async void Server_SendPlayerDataPacket()
        {
            if (_sendingPlayerDataPacked)
                return;

            _sendingPlayerDataPacked = true;

            await Server_UpdatePlayerDataAsync();
            PlayerDataPacket playerDataPacket = new PlayerDataPacket();
            playerDataPacket.networkId = networkId;
            playerDataPacket.playerData = playerData;

            NetworkManager.Instance.Server.BroadcastExceptLocalhost(playerDataPacket, SendType.Reliable);

            _sendingPlayerDataPacked = false;
        }

        public async void Server_SendPlayerDataPacket(Connection toConnection = default)
        {
            if (_sendingPlayerDataPacked)
                return;

            _sendingPlayerDataPacked = true;

            await Server_UpdatePlayerDataAsync();
            PlayerDataPacket playerDataPacket = new PlayerDataPacket();
            playerDataPacket.networkId = networkId;
            playerDataPacket.playerData = playerData;

            if (toConnection.Id == 0)
            {
                NetworkManager.Instance.Server.Broadcast(playerDataPacket, SendType.Reliable, NetworkManager.Instance.Server.GetLocalConnection());
            }
            else
            {
                NetworkManager.Instance.Server.Send(playerDataPacket, toConnection, SendType.Reliable);
            }

            _sendingPlayerDataPacked = false;
        }

        public async Task Server_UpdatePlayerDataAsync()
        {
            playerData = new PlayerData();
            playerData.steamId = owner;
            playerData.name = await NetworkManager.Instance.Server.GetName(owner);
            playerData.ping = NetworkManager.Instance.Server.GetPing(owner);
        }

        #endregion

        public static uint GetRandomNetworkId()
        {
            Random random = new Random();
            int value = ((int)random.Next() << 32) | (int)random.Next();
            return (uint)value;
        }
    }
}