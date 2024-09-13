using Godot;
using Steamworks.Data;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIFindLobbies : Control
    {
        private const string UNDEFINED_LOBBY_TEXT = "Undefined Lobby";
        private const string REFRESH_IN_PROGRESS = "Searching...";

        [Export]
        public Button[] joinLobbyButtons;

        [Export]
        public Button refreshButton;

        [Export]
        public Button cancelButton;

        [Export]
        public Button friendOnlyButton;

        [Export]
        private UILobbyDescriptionPanel descriptionPanel;

        private Lobby[] lobbies;

        private bool isRefreshing;

        private string originalRefreshButtonText;
        private bool friendsOnly;

        public override void _Ready()
        {
            originalRefreshButtonText = refreshButton.Text;
            cancelButton.Visible = false;

            DisableAllJoinLobbyButtons();

            Refresh();

            refreshButton.Pressed += Refresh;
            cancelButton.Pressed += LobbyManager.Instance.CancelFindLobbies;
            friendOnlyButton.Toggled += ToggleFriendsOnly;

            LobbyManager.Instance.OnBeginFindLobby += OnBeginFindLobby;
            LobbyManager.Instance.OnEndFindLobby += OnEndFindLobby;
        }

        public override void _ExitTree()
        {
            refreshButton.Pressed -= Refresh;
            cancelButton.Pressed -= LobbyManager.Instance.CancelFindLobbies;

            LobbyManager.Instance.OnBeginFindLobby -= OnBeginFindLobby;
            LobbyManager.Instance.OnEndFindLobby -= OnEndFindLobby;
        }

        private void OnEndFindLobby()
        {
            cancelButton.Visible = false;
        }

        private void OnBeginFindLobby()
        {
            cancelButton.Visible = true;
        }

        private void ToggleFriendsOnly(bool toggledOn)
        {
            friendsOnly = toggledOn;
            Refresh();
        }

        private void DisableAllJoinLobbyButtons()
        {
            foreach (var button in joinLobbyButtons)
            {
                UIUtilities.ResetButtonPressedCallbacks(button);
                button.Visible = false;
                button.Text = UNDEFINED_LOBBY_TEXT;
            }
        }

        private async void Refresh()
        {
            descriptionPanel.ShowDefault();

            if (isRefreshing)
                return;

            isRefreshing = true;
            refreshButton.Text = REFRESH_IN_PROGRESS;

            lobbies = await LobbyManager.Instance.FindLobbies(friendsOnly, joinLobbyButtons.Length);
            GD.Print($"Lobbies found {lobbies.Length}");
            DisableAllJoinLobbyButtons();

            for (int i = 0; i < lobbies.Length; i++)
            {
                var lobby = lobbies[i];
                var joinLobbyButton = joinLobbyButtons[i];
                joinLobbyButton.Pressed += () => descriptionPanel.Update(lobby);
                joinLobbyButton.Visible = true;
                joinLobbyButton.Text = $"{lobby.GetData(LobbyManager.NAME_LOBBYKEY)} ({lobby.GetData(LobbyManager.OWNER_LOBBYKEY)}) Players Count: {lobby.GetData(LobbyManager.PLAYERSCOUNT_LOBBYKEY)}";
            }

            refreshButton.Text = originalRefreshButtonText;
            isRefreshing = false;

            if (lobbies.Length > 0)
            {
                descriptionPanel.Update(lobbies[0]);
            }
        }
    }

}

