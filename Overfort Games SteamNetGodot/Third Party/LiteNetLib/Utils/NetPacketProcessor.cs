using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OverfortGames.SteamNetGodot;

namespace LiteNetLib.Utils
{
    public class NetPacketProcessor
    {
        private static class HashCache<T>
        {
            public static readonly ulong Id;

            //FNV-1 64 bit hash
            static HashCache()
            {
                ulong hash = 14695981039346656037UL; //offset
                string typeName = typeof(T).ToString();
                for (var i = 0; i < typeName.Length; i++)
                {
                    hash ^= typeName[i];
                    hash *= 1099511628211UL; //prime
                }
                Id = hash;
            }
        }

        protected delegate void SubscribeDelegate(NetDataReader reader, object userData);
        protected delegate void SubscribeINetSerializable(INetSerializable packet, object userData);
        private readonly NetSerializer _netSerializer;
        private readonly Dictionary<ulong, List<SubscribeDelegate>> _callbacks = new Dictionary<ulong, List<SubscribeDelegate>>();
        private readonly Dictionary<ulong, INetSerializable> subscribedPackets= new Dictionary<ulong, INetSerializable>();

        public NetPacketProcessor()
        {
            _netSerializer = new NetSerializer();
        }

        public NetPacketProcessor(int maxStringLength)
        {
            _netSerializer = new NetSerializer(maxStringLength);
        }

        protected virtual ulong GetHash<T>()
        {
            var hash = HashCache<T>.Id;
            return hash;
        }

        private List<SubscribeDelegate> subscribeDelegatesToRemove = new List<SubscribeDelegate>();
        protected virtual List<SubscribeDelegate> GetCallbacksFromData(ulong hash)
        {
            if (!_callbacks.TryGetValue(hash, out var callbacks))
            {
                return null;
            }

            //Auto remove empty callbacks (from destroyed objects)
            subscribeDelegatesToRemove.Clear();

            foreach (var callback in callbacks)
            {
                if (callback.Target == null)
                {
                    subscribeDelegatesToRemove.Add(callback);
                }
            }

            foreach (var subscribeDelegateToRemove in subscribeDelegatesToRemove)
            {
                callbacks.Remove(subscribeDelegateToRemove);
            }
            return callbacks;
        }

        protected virtual void WriteHash<T>(NetDataWriter writer)
        {
            writer.Put(GetHash<T>());
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <typeparam name="T">INetSerializable structure</typeparam>
        public void RegisterNestedType<T>() where T : struct, INetSerializable
        {
            _netSerializer.RegisterNestedType<T>();
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <param name="writeDelegate"></param>
        /// <param name="readDelegate"></param>
        public void RegisterNestedType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
        {
            _netSerializer.RegisterNestedType<T>(writeDelegate, readDelegate);
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <typeparam name="T">INetSerializable class</typeparam>
        public void RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable
        {
            _netSerializer.RegisterNestedType(constructor);
        }

        /// <summary>
        /// Reads all available data from NetDataReader and calls OnReceive delegates
        /// </summary>
        /// <param name="reader">NetDataReader with packets data</param>
        public void ReadAllPackets(NetDataReader reader)
        {
            while (reader.AvailableBytes > 0)
                ReadPacket(reader);
        }

        /// <summary>
        /// Reads all available data from NetDataReader and calls OnReceive delegates
        /// </summary>
        /// <param name="reader">NetDataReader with packets data</param>
        /// <param name="userData">Argument that passed to OnReceivedEvent</param>
        /// <exception cref="ParseException">Malformed packet</exception>
        public void ReadAllPackets(NetDataReader reader, object userData)
        {
            while (reader.AvailableBytes > 0)
                ReadPacket(reader, userData);
        }

        /// <summary>
        /// Reads one packet from NetDataReader and calls OnReceive delegate
        /// </summary>
        /// <param name="reader">NetDataReader with packet</param>
        /// <exception cref="ParseException">Malformed packet</exception>
        public void ReadPacket(NetDataReader reader)
        {
            ReadPacket(reader, null);
        }

        public void Write<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
        T>(NetDataWriter writer, T packet) where T : class, new()
        {
            WriteHash<T>(writer);
            _netSerializer.Serialize(writer, packet);
        }

        public void WriteNetSerializable<T>(NetDataWriter writer, ref T packet) where T : INetSerializable
        {
            WriteHash<T>(writer);
            packet.Serialize(writer);
        }

        /// <summary>
        /// Reads one packet from NetDataReader and calls OnReceive delegate
        /// </summary>
        /// <param name="reader">NetDataReader with packet</param>
        /// <param name="userData">Argument that passed to OnReceivedEvent</param>
        /// <exception cref="ParseException">Malformed packet</exception>
        public void ReadPacket(NetDataReader reader, object userData)
        {
            ulong hash = reader.GetULong();

            List<SubscribeDelegate> callbacks = GetCallbacksFromData(hash);

            if (callbacks == null)
                return;

            foreach (var callback in callbacks)
            {
                callback?.Invoke(reader, userData);
            }
        }

        List<SubscribeDelegate> callbacksToRemove = new List<SubscribeDelegate>();

        public void ReadPacket(byte[] readBuffer, int size, NetDataReader reader, object userData)
        {
            ulong hash = reader.GetULong();
            if (subscribedPackets.ContainsKey(hash) == false)
                return;

            //foreach (var packet in subscribedPackets)
            //{
            //    GD.Print($"Hash {packet.Key} Type:{packet.Value.GetType()}");
            //}
            List<SubscribeDelegate> callbacks = GetCallbacksFromData(hash);

            INetSerializable preLoadedPacket = subscribedPackets[hash];
            reader.SetSource(readBuffer, sizeof(ulong), size);
            preLoadedPacket.Deserialize(reader);

            if (callbacks == null)
                return;

            callbacksToRemove.Clear();
            foreach (var callback in callbacks)
            {
              //  reader.SetSource(readBuffer, sizeof(ulong), size);
                callback?.Invoke(reader, userData);
            }

            foreach (var callback in callbacksToRemove)
            {
                if (callbacks.Contains(callback))
                    callbacks.Remove(callback);
            }
        }

#region Subscribe Unused
        /// <summary>
        /// Register and subscribe to packet receive event
        /// </summary>
        /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
        /// <param name="packetConstructor">Method that constructs packet instead of slow Activator.CreateInstance</param>
        /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
        public void Subscribe<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
        T>(Action<T> onReceive, Func<T> packetConstructor) where T : class, new()
        {
            _netSerializer.Register<T>();
            _callbacks[GetHash<T>()].Add((reader, userData) =>
            {
                var reference = packetConstructor();
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference);
            });
        }

        /// <summary>
        /// Register and subscribe to packet receive event (with userData)
        /// </summary>
        /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
        /// <param name="packetConstructor">Method that constructs packet instead of slow Activator.CreateInstance</param>
        /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
        public void Subscribe<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
        T, TUserData>(Action<T, TUserData> onReceive, Func<T> packetConstructor) where T : class, new()
        {
            _netSerializer.Register<T>();
            _callbacks[GetHash<T>()].Add((reader, userData) =>
            {
                var reference = packetConstructor();
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference, (TUserData)userData);
            });
        }

        /// <summary>
        /// Register and subscribe to packet receive event
        /// This method will overwrite last received packet class on receive (less garbage)
        /// </summary>
        /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
        /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
        public void SubscribeReusable<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
        T>(Action<T> onReceive) where T : class, new()
        {
            _netSerializer.Register<T>();
            var reference = new T();
            _callbacks[GetHash<T>()].Add((reader, userData) =>
            {
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference);
            });
        }

        /// <summary>
        /// Register and subscribe to packet receive event
        /// This method will overwrite last received packet class on receive (less garbage)
        /// </summary>
        /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
        /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
        public void SubscribeReusable<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
        T, TUserData>(Action<T, TUserData> onReceive) where T : class, new()
        {
            _netSerializer.Register<T>();
            var reference = new T();
            _callbacks[GetHash<T>()].Add((reader, userData) =>
            {
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference, (TUserData)userData);
            });
        }

        public void SubscribeNetSerializable<T, TUserData>(
            Action<T, TUserData> onReceive,
            Func<T> packetConstructor) where T : INetSerializable
        {
            _callbacks[GetHash<T>()].Add((reader, userData) =>
            {
                var pkt = packetConstructor();
                pkt.Deserialize(reader);
                onReceive(pkt, (TUserData)userData);
            });
        }

        public void SubscribeNetSerializable<T>(
            Action<T> onReceive,
            Func<T> packetConstructor, object target, bool isGodotObject) where T : INetSerializable
        {
            var hash = GetHash<T>();

            if (_callbacks.ContainsKey(hash) == false)
            {
                _callbacks.Add(hash, new List<SubscribeDelegate>());
            }

            SubscribeDelegate callback = null;

            callback = (reader, userData) =>
            {
                if (isGodotObject)
                {
                    GodotObject targetAsGodotObject = target as GodotObject;
                    if (targetAsGodotObject.IsValid() == false)
                    {
                        GD.Print("destroyPredicate");

                        if (callbacksToRemove.Contains(callback) == false)
                            callbacksToRemove.Add(callback);

                        return;
                    }
                }

                if (target == null)
                {
                    if (callbacksToRemove.Contains(callback) == false)
                        callbacksToRemove.Add(callback);

                    return;
                }

                var pkt = packetConstructor();
                pkt.Deserialize(reader);
                onReceive(pkt);
            };

            _callbacks[hash].Add(callback);
        }
#endregion

        public void SubscribeNetSerializable<T, TUserData>(
            Action<T, TUserData> onReceive, Func<bool> destroyPredicate) where T : INetSerializable, new()
        {
            var reference = new T();
            var hash = GetHash<T>();

            if (subscribedPackets.ContainsKey(hash) == false)
                subscribedPackets.Add(hash, reference);

            if (_callbacks.ContainsKey(hash) == false)
            {
                _callbacks.Add(hash, new List<SubscribeDelegate>());
            }

            SubscribeDelegate callback = null;

            callback = (reader, userData) =>
            {
                if (destroyPredicate())
                {
                    GD.Print("destroyPredicate");

                    if (callbacksToRemove.Contains(callback) == false)
                        callbacksToRemove.Add(callback);

                    return;
                }

                // reference.Deserialize(reader);
                // onReceive(reference, (TUserData)userData);
                var cachedReference = subscribedPackets[hash];
                onReceive((T)cachedReference, (TUserData)userData);
            };

            _callbacks[hash].Add(callback);
        }

        public void SubscribeNetSerializable<T>(
            Action<T> onReceive, Func<bool> destroyPredicate) where T : INetSerializable, new()
        {
            var reference = new T();
            var hash = GetHash<T>();

            if (subscribedPackets.ContainsKey(hash) == false)
                subscribedPackets.Add(hash, reference);

            if (_callbacks.ContainsKey(hash) == false)
            {
                _callbacks.Add(hash, new List<SubscribeDelegate>());
            }

            SubscribeDelegate callback = null;

            callback = (reader, userData) =>
            {
                if (destroyPredicate())
                {
                    if (callbacksToRemove.Contains(callback) == false)
                        callbacksToRemove.Add(callback);

                    return;
                }

              //  reference.Deserialize(reader);
              //  onReceive(reference);

                var cachedReference = subscribedPackets[hash];
                onReceive((T)cachedReference);
            };

            _callbacks[GetHash<T>()].Add(callback);
        }

        /// <summary>
        /// Remove any subscriptions by type
        /// </summary>
        /// <typeparam name="T">Packet type</typeparam>
        /// <returns>true if remove is success</returns>
        public bool RemoveAllSubscription<T>()
        {
            return _callbacks.Remove(GetHash<T>());
        }
    }
}
