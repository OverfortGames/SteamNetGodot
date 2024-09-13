using Godot;
using Steamworks;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public class GameState
    {
        public Dictionary<uint, ObjectState> objectStates = new Dictionary<uint, ObjectState>();

        public List<uint> objectStatesToAdd = new List<uint>();
        public List<uint> objectStatesToRemove = new List<uint>();

        private ushort updatePlayersDataTickFrequency = 240;

        public GameState(bool isServer)
        {
            // Ignore if localhost
            if (isServer == false)
            {
                NetworkManager.Instance.Client_SubscribeRPC<GameStatePacket>(Client_OnGameStatePacketReceived, () => this == null);
            }
        }

        ~GameState()
        {
            RemoveAllObjects();
        }

        public ObjectState CreateObjectState(uint networkId, ulong owner = 0, bool isAPlayer = false, bool sendObjectStatePacketRequest = false)
        {
            if (objectStates.ContainsKey(networkId) == true)
            {
                GD.Print($"Trying to add the network object of key {networkId} but that exist already in the dictionary");
                return null;
            }

            if (networkId == ObjectState.INVALID_NETWORK_ID)
            {
                GD.PrintErr($"Trying to add the network object of key with INVALID_NETWORK_ID");
                return null;
            }

            ObjectState objectState = new ObjectState(networkId);
            if (owner != 0)
            {
                objectState.SetOwner(owner);
            }

            objectState.IsAPlayer = isAPlayer;

            objectStates.Add(networkId, objectState);

            if (sendObjectStatePacketRequest)
                objectState.Client_SendObjectStatePacketRequest();

            GD.Print($"Added to GameState {networkId} total count: {objectStates.Count}");

            if(NetworkManager.Instance.IsServer())
                Server_ReplicateGameState();

            return objectState;
        }

        public void RemoveFromGameState(uint networkId)
        {
            if (objectStates.ContainsKey(networkId) == false)
            {
                GD.Print($"Trying to remove the network object of key {networkId} but that doesn't exist in the dictionary");
                return;
            }

            ObjectState objectState = objectStates[networkId];
            Pawn pawn = objectState.GetPawn();
            if (pawn != null)
            {
                pawn.QueueFree();
            }

            objectState.MarkAsDestroyed();
            objectStates.Remove(networkId);

            if (NetworkManager.Instance.IsServer())
                Server_ReplicateGameState();

            GD.Print("Removed from GameState " + networkId);
        }
        public void RemoveAllObjects()
        {
            foreach (var item in objectStates)
            {
                RemoveFromGameState(item.Key);
            }
        }

        public static uint[] ConvertKeyCollectionToArray(Dictionary<uint, ObjectState>.KeyCollection keyCollection)
        {
            // Rent an array from the pool
            uint[] buffer = ArrayPool<uint>.Shared.Rent(keyCollection.Count);

            int index = 0;
            foreach (uint key in keyCollection)
            {
                buffer[index++] = key;
            }

            // Optionally, return a new array if the keyCollection is smaller than the buffer
            if (index < buffer.Length)
            {
                uint[] result = new uint[index];
                Array.Copy(buffer, result, index);
                ArrayPool<uint>.Shared.Return(buffer);
                return result;
            }

            return buffer;
        }

        private List<PlayerData> _playersDataCache = new List<PlayerData>();
        public List<PlayerData> GetPlayersData()
        {
            _playersDataCache.Clear();

            foreach (var objectState in objectStates.Values)
            {
                if (objectState.IsAPlayer)
                {
                    _playersDataCache.Add(objectState.playerData);
                }
            }

            return _playersDataCache;
        }

        #region Server

        public void ServerTick(ulong tick)
        {
            if ((tick % updatePlayersDataTickFrequency) == 0)
            {
                Server_ReplicateGameState();

                Server_UpdatePlayersData();
            }
        }

        private void Server_ReplicateGameState()
        {
            GameStatePacket gameStatePacket = new GameStatePacket();
            gameStatePacket.serverTick = NetworkManager.Instance.Tick;
            gameStatePacket.objectStates = ConvertKeyCollectionToArray(objectStates.Keys);

            NetworkManager.Instance.Server.Broadcast(gameStatePacket, Steamworks.Data.SendType.Reliable);
        }

        private bool _serverUpdatingPlayersData;
        public void Server_UpdatePlayersData()
        {
            if (NetworkManager.Instance.IsServer() == false)
                return;

            if (_serverUpdatingPlayersData)
                return;

            _serverUpdatingPlayersData = true;

            foreach (var networkIdObjectStatePair in objectStates)
            {
                if (networkIdObjectStatePair.Value.IsAPlayer)
                {
                    networkIdObjectStatePair.Value.Server_SendPlayerDataPacket();
                }
            }

            _serverUpdatingPlayersData = false;
        }

        public ObjectState Server_CreateObjectStateFromPawn(Pawn from)
        {
            var objectState = CreateObjectState(from.networkId, SteamClient.SteamId, false, false);

            if (objectState == null)
                return null;

            Action<Node> preOnAddChild = (x) =>
            {
                var _pawn = x as Pawn;

                _pawn.SetName(from.Name);
                _pawn.networkId = objectState.networkId;
                _pawn.networkOwner = objectState.owner;
               
            };

            var newPawn = SimpleResourceLoader.Instance.LoadResourceAndInstantiate(from.resourceId, SceneLoader.Instance.GameplayScene, preOnAddChild) as Pawn;

            newPawn.GlobalPosition = from.GlobalPosition;
            newPawn.Quaternion = from.Quaternion;

            if (objectState != null)
            {
                objectState.SetPawn(newPawn);
            }

            return objectState;
        }

        public ObjectState Server_CreateObjectStateAndPawn(uint networkId, string pawnResourceId,
            string pawnName = "", ulong owner = 0, bool isAPlayer = false, Vector3? pawnPosition = null, Quaternion? pawnRotation = null)
        {
            owner = owner != 0 ? owner : SteamClient.SteamId;

            var objectState = CreateObjectState(networkId, owner, isAPlayer, false);

            Action<Node> preOnAddChild = (x) =>
            {
                var _pawn = x as Pawn;

                if (string.IsNullOrEmpty(pawnName) == false)
                    _pawn.SetName(pawnName);

                _pawn.networkId = objectState.networkId;
                _pawn.networkOwner = objectState.owner;
            };

            var newPawn = SimpleResourceLoader.Instance.LoadResourceAndInstantiate(pawnResourceId, SceneLoader.Instance.GameplayScene, preOnAddChild) as Pawn;

            if (pawnPosition.HasValue)
            {
                newPawn.GlobalPosition = pawnPosition.Value;
            }
            if (pawnRotation.HasValue)
            {
                newPawn.Quaternion = pawnRotation.Value;
            }

            if (objectState != null)
            {
                objectState.SetPawn(newPawn);
            }

            return objectState;
        }

        #endregion

        #region Client

        private void Client_OnGameStatePacketReceived(GameStatePacket gameStatePacket)
        {
            NetworkManager.Instance.OverrideTick(gameStatePacket.serverTick);

            objectStatesToAdd.Clear();
            objectStatesToRemove.Clear();

            // object to add
            foreach (var networkObject in gameStatePacket.objectStates)
            {
                if (objectStates.ContainsKey(networkObject) == false)
                {
                    objectStatesToAdd.Add(networkObject);
                }
            }

            // object to remove
            foreach (var pair in objectStates)
            {
                bool found = false;
                for (int i = 0; i < gameStatePacket.objectStates.Length; i++)
                {
                    if (gameStatePacket.objectStates[i] == pair.Key)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    objectStatesToRemove.Add(pair.Key);
                }
            }

            foreach (var networkObjectKey in objectStatesToAdd)
            {
                CreateObjectState(networkObjectKey, sendObjectStatePacketRequest: true);
            }

            foreach (var networkObjectKey in objectStatesToRemove)
            {
                RemoveFromGameState(networkObjectKey);
            }
        }

        public void SetOwnership(uint networkId, ulong newOwner)
        {
            objectStates[networkId].SetOwner(newOwner);
        }

        public async void SetOwnership(uint networkId, ulong newOwner, float delayInSeconds)
        {
            for (int i = 0; i < 10; i++)
            {
                var gameplaySceneNode = SceneLoader.Instance.GameplayScene;
                await gameplaySceneNode.ToSignal(gameplaySceneNode.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
           
            objectStates[networkId].SetOwner(newOwner);
        }

        #endregion

    }
}