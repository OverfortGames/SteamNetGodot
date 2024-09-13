using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class Mutable : Interactable
    {
        [Export]
        private Pawn pawn;

        [Export]
        private InteractableCollider collider;

        [Export]
        private Node3D showWhenMute;

        public override void _Ready()
        {
            if (pawn.HasOwnership())
            {
                showWhenMute.Visible = false;
                collider.QueueFree();
                QueueFree();
                return;
            }

            showWhenMute.Visible = VOIPSettings.IsMuted(pawn.networkOwner);
        }

        public override string GetInteractionName()
        {
            if (pawn.HasOwnership())
                return "";

            if (VOIPSettings.IsMuted(pawn.networkOwner))
            {
                return "Unmute";
            }

            return "Mute";
        }

        public override void TryInteract(WorldInteractor interactor)
        {
            if (pawn.HasOwnership())
                return;

            if (VOIPSettings.IsMuted(pawn.networkOwner))
            {
                showWhenMute.Visible = false;
                VOIPSettings.Unmute(pawn.networkOwner);
            }
            else
            { 
                showWhenMute.Visible = true;
                VOIPSettings.Mute(pawn.networkOwner);
            }
        }
    }
}