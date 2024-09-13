using Godot;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIGameMenuPlayers : Node
    {
        [Export]
        public Resource playerEntryResource;

        [Export]
        public Node playerEntryContainer;

        [Export]
        private double refreshTimeInSeconds = 1;

        private List<UIGameMenuPlayerEntry> playerEntries = new List<UIGameMenuPlayerEntry>();

        private int preloadSize = 30;

        private double refreshTimer = 0;

        public override void _EnterTree()
        {
            base._EnterTree();

            if (NetworkManager.Instance == null)
                return;

            for (int i = 0; i < preloadSize; i++)
            {
                UIGameMenuPlayerEntry playerEntry = SimpleResourceLoader.Instance.LoadResourceAndInstantiateFromPath(playerEntryResource.ResourcePath,
                    playerEntryContainer) as UIGameMenuPlayerEntry;
                playerEntry.Visible = false;
                playerEntries.Add(playerEntry);
            }

            Refresh();
        }

        public override void _Process(double delta)
        {
            refreshTimer += delta;

            if (refreshTimer > refreshTimeInSeconds)
            {
                Refresh();
                refreshTimer = 0;
            }
        }

        public void Refresh()
        {
            GameState gameState = NetworkManager.Instance.GetGameState();

            int i = 0;
            foreach (var playerData in gameState.GetPlayersData())
            {
                playerEntries[i].Init(playerData);
                playerEntries[i].Visible = true;

                i++;
            }

            for (; i < playerEntries.Count; i++)
            {
                playerEntries[i].Visible = false;
            }

        }
    }
}