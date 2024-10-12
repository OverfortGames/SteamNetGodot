
# SteamNetGodot
<img src="https://github.com/OverfortGames/SteamNetGodot/blob/main/package9.gif" width="600" height="361" />

## Is this for me?

In it's core SteamNetGodot is the combination of [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) and [LiteNetLib](https://github.com/RevenantX/LiteNetLib). 
A performant, easy to use C# Steam Networking library for the wonderful Godot engine. 

It will be regularly updated since it's currently in use on our game [Taverna](https://store.steampowered.com/app/3219160/Taverna).

## Goodies

- Compressed Steam VoIP (3-5 KB/s)
- Smart matchmaking through Steam Lobbies / internal logic
- RPC system
- Very simple C# code
- Bunch of cool networking code as example (replication, resource management, audio, kick, mute, ban...)
- Open Source
- Godot 4.3
- MIT license

## Do Stuff!

### Your packets

```csharp
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
```
### Subscribe RPC

```csharp
NetworkManager.Instance.Client_SubscribeRPC<VoicePacket, Connection>(Client_OnVoicePacketReceived, () => this.IsValid() == false);
NetworkManager.Instance.Server_SubscribeRPC<VoicePacket, Connection>(Server_OnVoicePacketReceived, () => this.IsValid() == false);
```

### Create and send

```csharp
VoicePacket voicePacket = new VoicePacket();
voicePacket.compressedVoiceData = compressedVoiceData;
voicePacket.size = compressedVoiceData.Count;
voicePacket.internalVoiceTick = internalVoiceTick;
voicePacket.networkId = pawn.networkId;

NetworkManager.Instance.Client.Send<VoicePacket>(voicePacket, SendType.Reliable);
```

### Instantiate networked objects

```csharp
ObjectState player = gameState.Server_CreateObjectStateAndPawn((uint)steamId, ResourceId.PawnPlayer, "", steamId, true);

Friend playerSteam = new Friend(steamId);
await playerSteam.RequestInfoAsync();
player.GetPawn().SetName(playerSteam.Name);

UINotifications.Instance.PushNotification($"Instantiated Pawn for player {playerSteam.Name}");
```

## I don't like this networking architecture

Great! And I don't like you. 
Jokes aside, the base code is divided from the gameplay solution. For sake of easier maintainability it was all put inside one project.

Get ***BaseClient***, ***BaseServer***, ***Processor*** and you are good to move to your solution.

You still need to import Facepunch.Steamworks and LiteNetLib as they are in the backbone of the library.

## Performance info

Aimed to get zero garbage on receive / read . If you find some bad stuff let us know.

VOIP bandwidth is between 3-5KB/s.

Transform replication has little optimization. When moving between 15-20 KB/s 60h tick rate. I would recommend to strip it down to make it usable. I will remove this little paragr as soon as I commit the improvements.

## Contact

overfortgames@gmail.com

[Discord](https://discord.gg/hvPkJrqQRX)

## Q&A

#### Steam doesn't connect in build? 
Make sure to add steam_api64.dll to your exported build

#### Does it support Rigidbodies?
Yes but not no prediction. It will be added in the future.

#### Prediction?
No prediction.

#### What is a ResourceID?
A string that represent a resource. It's a convenient way to do local instantiation and replication.  
We added a little addon to easily handle them in the inspector
```csharp
#if TOOLS
        [ResourceIdDropdown]
#endif
        [Export]
        public string resourceId;
```

You still need to add them manually in ResourceId.cs

#### What is Rich Presence?
https://partner.steamgames.com/doc/features/enhancedrichpresence

