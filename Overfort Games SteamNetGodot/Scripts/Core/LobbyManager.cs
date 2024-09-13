using Godot;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace OverfortGames.SteamNetGodot
{
    public partial class LobbyManager : Node
    {
        public const int MIN_LOBBY_SIZE = 1;
        public const string GAMEVERSION_LOBBYKEY = "game_version";
        public const string MAXPLAYERS_LOBBYKEY = "max_players";
        public const string GAMENAME_LOBBYKEY = "game";
        public const string GAMENAME_LOBBYVALUE = "SteamNetGodot";
        public const string NAME_LOBBYKEY = "name";
        public const string OWNER_LOBBYKEY = "owner";
        public const string PLAYERSCOUNT_LOBBYKEY = "players_count";
        public const string DESCRIPTION_LOBBYKEY = "description";

        public const string CONNECT_RICHPRESENCEKEY = "connect";
        public const string STEAMDISPLAY_RICHPRESENCEKEY = "steam_display";
        public const string STEAMDISPLAY_STATUS_RICHPRESENCEKEY = "#Status";
        public const string STATUS_RICHPRESENCEKEY = "status";
        public const string STATUS_LOBBY_RICHPRESENCEVALUE = "In lobby";
        public const string STATUS_INGAME_RICHPRESENCEVALUE = "Inside a server";

        public static LobbyManager Instance { get; private set; }
        
        private CancellationTokenSource browseCancellationToken;

        public Lobby LobbyLocalhost { get; private set; }
        
        public int LobbyLocalhostMaxPlayers { get; private set; }

        
        public event Action OnBeginFindLobby = delegate { };

        
        public event Action OnEndFindLobby = delegate { };

        public override void _Ready()
        {
            if (Instance != null)
            {
                GD.PrintErr($"{GetType().Name} already instanced. This node will be destroyed");
                QueueFree();
                return;
            }

            Instance = this;

            browseCancellationToken = new CancellationTokenSource();

            SteamFriends.OnGameRichPresenceJoinRequested += SteamFriends_OnGameRichPresenceJoinRequested;

            SetStatusRichPresence(STATUS_LOBBY_RICHPRESENCEVALUE);
        }

        public override void _ExitTree()
        {
            SteamFriends.OnGameRichPresenceJoinRequested -= SteamFriends_OnGameRichPresenceJoinRequested;
        }

        private void SteamFriends_OnGameRichPresenceJoinRequested(Friend friend, string steamIdServer)
        {
            Join(steamIdServer);
        }

        public void Join(Lobby lobby)
        {
            if (lobby.GetData(GAMEVERSION_LOBBYKEY) != GameVersion.VERSION)
            {
                UINotifications.Instance.PushNotification($"Server game version not matching yours {GameVersion.VERSION}", UINotifications.NotificationType.Error);
                return;
            }

            Join(lobby.GetData(OWNER_LOBBYKEY));
        }

        public void Join(string steamIdServer)
        {
            if (ulong.TryParse(steamIdServer, out var result))
            {
                UINotifications.Instance.PushNotification($"Parse successful {result}");
                NetworkManager.startupCommand = NetworkManager.StartupCommand.Connect;
                NetworkManager.connectSteamId = result;
                SceneLoader.Instance.LoadSceneAsync(ResourceId.Game, useFakeLoading: true, isGameplayScene: true);
            }
            else
            {
                UINotifications.Instance.PushNotification($"Cannot parse {steamIdServer}");
            }
        }

        public async Task<Lobby[]> FindLobbies(bool onlyFriends, int maxResults)
        {
            Lobby[] lobbies;
            if (onlyFriends)
            {
                List<Lobby> lobbiesList = new List<Lobby>();

                OnBeginFindLobby();

                foreach (var friend in SteamFriends.GetFriends())
                {
                    string friendConnect = "";
                    if (friend.IsPlayingThisGame && string.IsNullOrEmpty(friendConnect = friend.GetRichPresence(CONNECT_RICHPRESENCEKEY)) == false)
                    {

                        var findLobbiesQuery = new LobbyQuery();
                        findLobbiesQuery.maxResults = maxResults;
                        var friendLobby = await Task.Run(() => findLobbiesQuery.WithKeyValue(OWNER_LOBBYKEY, friendConnect).RequestAsync(), browseCancellationToken.Token);
                        lobbiesList.Add(friendLobby.FirstOrDefault());
                    }
                }

                OnEndFindLobby();

                lobbies = lobbiesList.ToArray();
            }
            else
            {
                OnBeginFindLobby();

                var findLobbiesQuery = new LobbyQuery();
                findLobbiesQuery.maxResults = maxResults;
                findLobbiesQuery.WithKeyValue(GAMENAME_LOBBYKEY, GAMENAME_LOBBYVALUE);
                lobbies = await Task.Run(() => findLobbiesQuery.RequestAsync(), browseCancellationToken.Token);

                OnEndFindLobby();
            }

            if (lobbies == null)
                lobbies = new Lobby[0];

            return lobbies;
        }

        public void CancelFindLobbies()
        {
            browseCancellationToken.Cancel();
            browseCancellationToken.Dispose();
            browseCancellationToken = new CancellationTokenSource();
        }

        public async Task<bool> CreateLobby(string name, string description)
        {
            if (NameChecker.IsValid(name, out var nameError, 30, 3) == false)
            {
                string errorText = "";
                switch (nameError)
                {
                    case NameChecker.ErrorMessage.None:
                        errorText = "Can't determine error";
                        break;
                    case NameChecker.ErrorMessage.InvalidCharPattern:
                        errorText = "You can't use special characters";
                        break;
                    case NameChecker.ErrorMessage.MultipleSpaces:
                        errorText = "Too many spaces";
                        break;
                    case NameChecker.ErrorMessage.InvalidLenght:
                        errorText = $"Too long... Maximum is {30} characters";
                        break;
                    case NameChecker.ErrorMessage.Empty:
                        errorText = $"Empty";
                        break;
                    default:
                        break;
                }

                UINotifications.Instance.PushNotification($"Name: {errorText}", UINotifications.NotificationType.Error);
                return false;
            }

            if (NameChecker.IsValid(description, out var descriptionError, 200, -1) == false)
            {
                string errorText = "";
                switch (descriptionError)
                {
                    case NameChecker.ErrorMessage.None:
                        errorText = "Can't determine error";
                        break;
                    case NameChecker.ErrorMessage.InvalidCharPattern:
                        errorText = "You can't use special characters";
                        break;
                    case NameChecker.ErrorMessage.MultipleSpaces:
                        errorText = "Too many spaces";
                        break;
                    case NameChecker.ErrorMessage.InvalidLenght:
                        errorText = $"Too long... Maximum is {200} characters";
                        break;
                    case NameChecker.ErrorMessage.Empty:
                        errorText = $"Empty";
                        break;
                    default:
                        break;
                }

                UINotifications.Instance.PushNotification($"Description: {errorText}", UINotifications.NotificationType.Error);
                return false;
            }

            Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync();
            if (lobby != null)
            {
                lobby.Value.SetData(GAMENAME_LOBBYKEY, GAMENAME_LOBBYVALUE);
                lobby.Value.SetData(NAME_LOBBYKEY, name);
                lobby.Value.SetData(OWNER_LOBBYKEY, SteamClient.SteamId.ToString());
                lobby.Value.SetData(DESCRIPTION_LOBBYKEY, description);
                lobby.Value.SetData(PLAYERSCOUNT_LOBBYKEY, 1.ToString());
                lobby.Value.SetData(MAXPLAYERS_LOBBYKEY, LobbyLocalhostMaxPlayers.ToString());
                lobby.Value.SetData(GAMEVERSION_LOBBYKEY, GameVersion.VERSION);
                UINotifications.Instance.PushNotification($"Lobby created succesfully", UINotifications.NotificationType.Normal);
            }
            else
            {
                UINotifications.Instance.PushNotification($"Couldn't create lobby", UINotifications.NotificationType.Error);
            }

            if (lobby.HasValue)
                LobbyLocalhost = lobby.Value;

            return lobby.HasValue;
        }

        public void SetConnectedRichPresence(SteamId steamId)
        {
            SteamFriends.SetRichPresence(CONNECT_RICHPRESENCEKEY, steamId.ToString());

            if (steamId == 0)
                SetStatusRichPresence(STATUS_LOBBY_RICHPRESENCEVALUE);
            else
                SetStatusRichPresence(STATUS_INGAME_RICHPRESENCEVALUE);
        }

        public void SetStatusRichPresence(string value)
        {
            SteamFriends.SetRichPresence(STATUS_RICHPRESENCEKEY, value);
            SteamFriends.SetRichPresence(STEAMDISPLAY_RICHPRESENCEKEY, STEAMDISPLAY_STATUS_RICHPRESENCEKEY);
        }

        public void SetPlayersCount(int count) 
        {
            LobbyLocalhost.SetData(PLAYERSCOUNT_LOBBYKEY, count.ToString());
        }

        public void SetMaxPlayers(int maxPlayers)
        {
            LobbyLocalhostMaxPlayers = maxPlayers;

            if (LobbyLocalhostMaxPlayers < MIN_LOBBY_SIZE)
                LobbyLocalhostMaxPlayers = MIN_LOBBY_SIZE;

            LobbyLocalhost.SetData(MAXPLAYERS_LOBBYKEY, LobbyLocalhostMaxPlayers.ToString());
        }
    }

}