using Godot;
using LiteNetLib.Utils;
using System;

namespace OverfortGames.SteamNetGodot
{
    public struct TestPacket : INetSerializable
    {
        public string message;

        public void Deserialize(NetDataReader reader)
        {
            message = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(message);
        }
    }

    public struct VoicePacket : INetSerializable
    {
        public uint networkId;
        public ulong internalVoiceTick;
        public int size;
        public ArraySegment<byte> compressedVoiceData;
        public ulong fromUser;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            internalVoiceTick = reader.GetULong();
            size = reader.GetInt();
            compressedVoiceData = reader.GetBytesSegment(size);
            fromUser = reader.GetULong();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(internalVoiceTick);
            writer.Put(size);
            writer.PutBytesSegment(compressedVoiceData);
            writer.Put(fromUser);
        }
    }

    public struct ObjectStateRequestPacket : INetSerializable
    {
        public uint networkId;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
        }
    }

    public struct PlayerDataRequestPacket : INetSerializable
    {
        public uint networkId;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
        }
    }

    public struct ObjectStatePacket : INetSerializable
    {
        public uint networkId;
        public ulong owner;
        public bool isAPlayer;
        public string pawnResourceId;
        public Vector3 pawnInitialPosition;
        public Quaternion pawnInitialRotation;
        public string name;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(owner);
            writer.Put(isAPlayer);
            writer.Put(pawnResourceId);
            writer.Put(Vector3Serializable.FromVector3(pawnInitialPosition));
            writer.Put(QuaternionSerializable.FromQuaternion(pawnInitialRotation));
            writer.Put(name);
        }

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();  
            owner = reader.GetULong();
            isAPlayer = reader.GetBool();
            pawnResourceId = reader.GetString();
            pawnInitialPosition = reader.Get<Vector3Serializable>().GetVector3();
            pawnInitialRotation = reader.Get<QuaternionSerializable>().GetQuaternion();
            name = reader.GetString();
        }
    }

    public struct OwnershipPacket : INetSerializable
    {
        public uint networkId;
        public ulong owner;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(owner);
        }

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            owner = reader.GetULong();
        }
    }

    public struct PlayerDataPacket : INetSerializable
    {
        public uint networkId;
        public PlayerData playerData;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(playerData);
        }

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            playerData = reader.Get<PlayerData>();
        }
    }

    public struct GameStatePacket : INetSerializable
    {
        public ulong serverTick;
        public uint[] objectStates;

        public void Deserialize(NetDataReader reader)
        {
            serverTick = reader.GetULong();
            objectStates = reader.GetUIntArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(serverTick);
            writer.PutArray(objectStates);
        }
    }

    public struct PlayOneShotPacket : INetSerializable
    {
        public Vector3 position;
        public float volume;
        public string audioClipResourceId;
        public string bus;

        public void Deserialize(NetDataReader reader)
        {
            position = reader.Get<Vector3Serializable>().GetVector3();
            volume = reader.GetFloat();
            audioClipResourceId = reader.GetString();
            bus = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Vector3Serializable.FromVector3(position));
            writer.Put(volume);
            writer.Put(audioClipResourceId);
            writer.Put(bus);
        }
    }

    public struct Vector3Serializable : INetSerializable
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3 GetVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public void Deserialize(NetDataReader reader)
        {
            X = reader.GetFloat();
            Y = reader.GetFloat();
            Z = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(X);
            writer.Put(Y);
            writer.Put(Z);
        }

        public static Vector3Serializable FromVector3(Vector3 vector3)
        {
            var vector3Serializable = new Vector3Serializable();
            vector3Serializable.X = vector3.X;
            vector3Serializable.Y = vector3.Y;
            vector3Serializable.Z = vector3.Z;

            return vector3Serializable;
        }
    }

    public struct QuaternionSerializable : INetSerializable
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Quaternion GetQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }

        public void Deserialize(NetDataReader reader)
        {
            X = reader.GetFloat();
            Y = reader.GetFloat();
            Z = reader.GetFloat();
            W = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(X);
            writer.Put(Y);
            writer.Put(Z);
            writer.Put(W);
        }

        public static QuaternionSerializable FromQuaternion(Quaternion quaternion)
        {
            var quaternionSerializable = new QuaternionSerializable();
            quaternionSerializable.X = quaternion.X;
            quaternionSerializable.Y = quaternion.Y;
            quaternionSerializable.Z = quaternion.Z;
            quaternionSerializable.W = quaternion.W;

            return quaternionSerializable;
        }
    }

    public struct TransformPacket : INetSerializable
    {
        public uint networkId;
        public uint internalId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 forward;
        public string parentPath;
        public bool physicsFreeze;
        public Vector3 physicsLinearVelocity;
        public Vector3 physicsAngularVelocity;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            internalId = reader.GetUInt();
            position = reader.Get<Vector3Serializable>().GetVector3();
            rotation = reader.Get<QuaternionSerializable>().GetQuaternion();
            forward = reader.Get<Vector3Serializable>().GetVector3();
            parentPath = reader.GetString();
            physicsFreeze = reader.GetBool();
            physicsLinearVelocity = reader.Get<Vector3Serializable>().GetVector3();
            physicsAngularVelocity = reader.Get<Vector3Serializable>().GetVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(internalId);
            writer.Put(Vector3Serializable.FromVector3(position));
            writer.Put(QuaternionSerializable.FromQuaternion(rotation));
            writer.Put(Vector3Serializable.FromVector3(forward));
            writer.Put(parentPath);
            writer.Put(physicsFreeze);
            writer.Put(Vector3Serializable.FromVector3(physicsLinearVelocity));
            writer.Put(Vector3Serializable.FromVector3(physicsAngularVelocity));
        }
    }

    public struct TransformRequestPacket : INetSerializable
    {
        public uint networkId;
        public uint internalId;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            internalId = reader.GetUInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(internalId);
        }
    }

    public struct PickupRequestPacket : INetSerializable
    {
        public uint networkId;
        public string interactorPath;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            interactorPath = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put(interactorPath);
        }
    }

    public struct PickableStatePacket : INetSerializable
    {
        public uint networkId;
        public Pickable.PickableState state;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
            state = (Pickable.PickableState)reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
            writer.Put((int)state);
        }
    }

    public struct ThrowRequestPacket : INetSerializable
    {
        public uint networkId;

        public void Deserialize(NetDataReader reader)
        {
            networkId = reader.GetUInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(networkId);
        }
    }
}