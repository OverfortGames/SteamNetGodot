using Godot;

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
    public partial class PawnEditorInspectorPlugin : EditorInspectorPlugin
    {
        private Pawn target;

        public override bool _CanHandle(GodotObject @object)
        {
            return @object is Pawn;
        }

        public override void _ParseBegin(GodotObject @object)
        {
            Button button = new Button();
            button.Text = "Randomize Network Id";
            button.Connect(Button.SignalName.Pressed, Callable.From(OnRandomizeButtonPressed));

            target = @object as Pawn;

            if (target.networkId == ObjectState.INVALID_NETWORK_ID)
            {
                target.RandomizeNetworkId();
            }
            AddCustomControl(button);
        }

        private void OnRandomizeButtonPressed()
        {
            target.RandomizeNetworkId();
        }
    }
#endif
}