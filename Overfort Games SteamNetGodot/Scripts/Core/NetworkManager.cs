using Steamworks;
using System;
using Godot;
using System.Threading.Tasks;
using LiteNetLib.Utils;

namespace OverfortGames.SteamNetGodot
{
	public partial class NetworkManager : Node
	{
		public enum StartupCommand
		{ 
			None,
			Localhost,
			Connect
		}

		[Export]
		public ushort tickRate = 60;

		public GameClient Client { get; private set; }
		public GameServer Server { get; private set; }

		public static NetworkManager Instance { get; private set; }

		public event Action OnClientConnected = delegate { };
		public event Action<ulong> OnTick = delegate { };

		public static StartupCommand startupCommand;
		public static ulong connectSteamId;
		public static event Action OnStartup = delegate { };

		public ulong Tick { get; private set; }

		private float tickPerSeconds;

		private double tickTimer;

        public override void _EnterTree()
		{
			if (Instance != null)
			{
				GD.PrintErr($"{GetType().Name} already instanced. This node will be destroyed");
				QueueFree();
				return;
			}

			Instance = this;

			tickPerSeconds = 1 / (float)tickRate;
        }

		public override void _Ready()
		{
			Startup();
		}

		public override void _ExitTree()
		{
			Instance = null;

			startupCommand = StartupCommand.None;
			connectSteamId = default;

            ClientDisconnect();
			StopHost();
        }

		private void Server_Receive()
		{
            if (IsServer())
			{
                Server.Receive();
            }
        }

        private void Client_Receive()
		{
			if (IsClient())
			{
                Client.Receive();
			}
        }

        public override void _Process(double delta)
		{
			base._Process(delta);

            Server_Receive();
            Client_Receive();

            tickTimer += delta;
			if (tickTimer >= tickPerSeconds)
			{
				tickTimer -= tickPerSeconds;

				Tick += 1;

				TickServer(Tick);
				TickClient(Tick);
				OnTick(Tick);
			}
			
		}

		public override void _Input(InputEvent @event)
		{
			base._Input(@event);

			if (Client != null)
			{
				if (@event is InputEventKey eventKey && eventKey.Pressed)
				{
					// Check if the key pressed is the space bar
					if (eventKey.Keycode == Key.R)
					{
						//  Client.Send<TestPacket>(new TestPacket() { message = "test message" }, Steamworks.Data.SendType.Reliable);
					}
				}
			}
		}

		private async void Startup()
		{
			switch (startupCommand)
			{
				case StartupCommand.None:
					UINotifications.Instance.PushNotification("I don't know if I'm localhosting or connecting...", UINotifications.NotificationType.Error);
					SceneLoader.Instance.LoadSceneAsync(ResourceId.Home);
					break;
				case StartupCommand.Localhost:
					Host();
					await ClientConnectLocalhost();
					break;
				case StartupCommand.Connect:
					await ClientConnect(connectSteamId);
					break;
				default:
					break;
			}

			OnStartup();
		}

		public void Host()
		{
			Server = SteamNetworkingSockets.CreateRelaySocket<GameServer>(BaseServer.PORT);
			if (Server != null)
			{
				GD.Print("Server created");
				UINotifications.Instance.PushNotification("Server created");
				Server.Init();
			}
		}

		public void StopHost()
		{
			if (IsServer() == false)
				return;

            Server.Shutdown();
			Server.Close();
			Server = null;

			GC.Collect();

			UINotifications.Instance.PushNotification("Stopped Server");
			GD.Print("Stopped server.");
		}

		public async Task ClientConnectLocalhost()
		{
			await ClientConnect(Steamworks.SteamClient.SteamId);
		}

		public async Task ClientConnect(SteamId steamId)
		{
			ClientDisconnect();

			await Task.Delay(1000);
			Client = SteamNetworkingSockets.ConnectRelay<GameClient>(steamId, BaseServer.PORT);

			if (Client != null)
			{
				UINotifications.Instance.PushNotification("Client connected successfully");
				LobbyManager.Instance.SetConnectedRichPresence(steamId);

                await Task.Delay(500);
                UINotifications.Instance.PushNotification("Receiving a character...");
            }
        }

		public void ClientDisconnect()
		{
			if (IsClient() == false)
				return;

            Client.Close();
			Client = null;

            GC.Collect();

            SceneLoader.Instance.LoadSceneAsync(ResourceId.Home);

            LobbyManager.Instance.SetConnectedRichPresence(default);
        }

        private void TickServer(ulong tick)
		{
			if (Server == null)
				return;

			Server.Tick(tick);
		}

		private void TickClient(ulong tick)
		{
			if (Client == null || Client.Connected == false)
				return;

			Client.Tick(tick);
		}

		public void OverrideTick(ulong newTick)
		{
			Tick = newTick;

			tickTimer = 0;
		}

		public bool IsClientConnected()
		{
			return IsClient() && Client.Connected;
		}

		public bool IsClient()
		{
			return Client != null;
		}

		public bool IsServer()
		{
			return Server != null;
		}

		public void Server_Kick(SteamId playerId)
		{
			if (IsServer() == false)
				return;

			Server.Kick(playerId);
		}

        public void Server_SubscribeRPC<T>(Action<T> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
            if (IsServer() == false)
                return;

            Server.SubscribeRPC(callback, destroyPredicate);
        }

        public bool Server_SubscribeRPC<T, TUserData>(Action<T, TUserData> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
            if (IsServer() == false)
                return false;

            Server.SubscribeRPC<T, TUserData>(callback, destroyPredicate);
			return true;
        }

        public bool Client_SubscribeRPC<T>(Action<T> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
			if (IsClient() == false)
				return false;

            Client.SubscribeRPC<T>(callback, destroyPredicate);
            return true;
        }

        public void Client_SubscribeRPC<T, TUserData>(Action<T, TUserData> callback, Func<bool> destroyPredicate) where T : struct, INetSerializable
        {
            if (IsClient() == false)
                return;

            Client.SubscribeRPC<T, TUserData>(callback, destroyPredicate);
        }

        public GameState GetGameState()
		{
			if (IsServer())
			{
				return Server.gameState;
			}
			else
			{
				if (IsClient())
				{
					return Client.gameState;
				}
			}

			return null;
		}
	}
}
