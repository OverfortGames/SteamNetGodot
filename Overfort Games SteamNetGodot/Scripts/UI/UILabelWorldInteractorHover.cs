using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILabelWorldInteractorHover : Label
    {
        [Export]
        private Pawn pawn;

        [Export]
        private WorldInteractor worldInteractor;

        public override void _Ready()
        {
            if (pawn.HasOwnership() == false)
            {
                QueueFree();
                return;
            }
        }

        public override void _Process(double delta)
        {
            Interactable hoverInteractable = worldInteractor.HoverInteractable;

            if (hoverInteractable != null)
            {
                string interactionName = hoverInteractable.GetInteractionName();

                if (string.IsNullOrEmpty(interactionName))
                {
                    Visible = false;
                }
                else
                {
                    Text = interactionName;
                    Visible = true;
                }
            }
            else
            {
                Text = "";
                Visible = false;
            }
        }
    }

}