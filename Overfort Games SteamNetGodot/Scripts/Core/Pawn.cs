using Godot;
using Steamworks;

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
#endif
    public partial class Pawn : Node3D
    {
#if TOOLS
        [ResourceIdDropdown]
#endif
        [Export]
        public string resourceId;

        public static byte[] EMPTY_PACKED_PAWN = new byte[0];

        [Export]
        public uint networkId = ObjectState.INVALID_NETWORK_ID;

        [Export]
        public ulong networkOwner = 0;

        public override void _EnterTree()
        {
#if TOOLS
            if (Engine.IsEditorHint())
            {
                foreach (var pawn in NodeUtilities.GetNodesOfType<Pawn>(GetTree().Root))
                {
                    if (pawn != this && pawn.networkId == networkId)
                    {
                        GD.PrintErr($"{Name} has the same networkId of {pawn.Name}. It will be randomized");
                        RandomizeNetworkId();
                    }
                }
                return;
            }
#endif

            NetworkManager.OnStartup += OnStartup;
        }

        public override void _ExitTree()
        {
#if TOOLS
            if (Engine.IsEditorHint())
                return;
#endif
            NetworkManager.OnStartup -= OnStartup;
        }

        private void OnStartup()
        {
            if (NetworkManager.Instance.IsServer())
            {
                // Create ObjectState in case this pawn was not spawned by an ObjectState
                var objectState = NetworkManager.Instance.GetGameState().Server_CreateObjectStateFromPawn(this);
                bool hasCreateObjectState = objectState != null;

                if(hasCreateObjectState)
                    QueueFree();
            }
            else
            {
                GD.Print($"deleting {Name}");
                // Avoid duplicates when the server sends this pawn object state data 
                QueueFree();
            }
        }

        public bool HasOwnership()
        {
            return SteamClient.SteamId == networkOwner;
        }

        public void SetOwnership(ulong newOwner)
        {
            networkOwner = newOwner;

            if (NetworkManager.Instance.IsServer())
            {
                NetworkManager.Instance.GetGameState().SetOwnership(networkId, newOwner);
            }
        }

        public void RandomizeNetworkId()
        {
            networkId = ObjectState.GetRandomNetworkId();
            GD.Print("Randomized!");
        }

        public void SetName(string name, bool excludePrefix = false)
        {
            if (excludePrefix)
            {
                Name = name;
            }
            else
            {
                Name = $"Pawn - {name}";
            }
        }
    }

}