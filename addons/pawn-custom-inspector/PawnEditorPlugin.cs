using Godot;

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
    public partial class PawnEditorPlugin : EditorPlugin
    {
        PawnEditorInspectorPlugin inspectorPlugin;

        public override void _EnterTree()
        {
            inspectorPlugin = new PawnEditorInspectorPlugin();
            AddInspectorPlugin(inspectorPlugin);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(inspectorPlugin);
        }
    }

#endif
}