#if TOOLS
using Godot;
#endif

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
    public partial class ResourceIdEditorPlugin : EditorPlugin
    {
        ResourceIdEditorInspectorPlugin inspectorPlugin;

        public override void _EnterTree()
        {
            inspectorPlugin = new ResourceIdEditorInspectorPlugin();
            AddInspectorPlugin(inspectorPlugin);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(inspectorPlugin);
        }
    }
#endif
}