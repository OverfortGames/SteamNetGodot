using Godot;

namespace OverfortGames.SteamNetGodot
{
    public abstract partial class Interactable : Node
    {
        public abstract void TryInteract(WorldInteractor interactor);
        public abstract string GetInteractionName();
    }
}

