using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class HideIfHasOwnership : Node
    {
        [Export]
        private Pawn pawn;

        [Export]
        private Node3D[] visuals;

        public override void _Ready()
        {
            if (pawn.HasOwnership())
            {
                foreach (var visual in visuals)
                {
                    visual.Visible = false;
                }
            }
        }
    }
}
