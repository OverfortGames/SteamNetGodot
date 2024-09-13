namespace LiteNetLib.Utils
{
    public interface INetSerializable
    {
        void Deserialize(NetDataReader reader);
        void Serialize(NetDataWriter writer);
    }
}
