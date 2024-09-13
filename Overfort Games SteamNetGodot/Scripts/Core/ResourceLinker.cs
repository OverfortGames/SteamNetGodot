using Godot;

namespace OverfortGames.SteamNetGodot
{
#if TOOLS
    [Tool]
#endif
    public partial class ResourceLinker : Resource
    {
#if TOOLS
        [ResourceIdDropdown]
#endif
        [Export]
        public string resourceId;

        [Export]
        public Resource resource;

        // Make sure you provide a parameterless constructor.
        // In C#, a parameterless constructor is different from a
        // constructor with all default values.
        // Without a parameterless constructor, Godot will have problems
        // creating and editing your resource via the inspector.
        public ResourceLinker() : this(ResourceId.Null, null) { }

        public ResourceLinker(string resourceId, Resource resource)
        {
            this.resourceId = resourceId;
            this.resource = resource;
        }
    }
}

