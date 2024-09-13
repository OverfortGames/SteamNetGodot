using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class InteractableCollider : Node
    {
        [Export]
        private Interactable interactable;

        public Interactable GetInteractable()
        {
            return interactable;
        }
    }
}

