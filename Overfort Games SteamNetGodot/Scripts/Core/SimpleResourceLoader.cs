using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class SimpleResourceLoader : Node
    {
        public ResourceLinker[] resources;

        public static SimpleResourceLoader Instance { get; private set; }

        public static event Action<string> OnLoadResourceBegin = delegate { };
        public static event Action<string> OnLoadResourceEnd = delegate { };
        public static event Action<string, float> OnLoadResourceProgress = delegate { };
        private bool loadingResource;

        public override void _EnterTree()
        {
            base._EnterTree();
            string directory = "Overfort Games SteamNetGodot/Prefabs/Resource Linkers/";
            var paths = DirAccess.GetFilesAt("res://" + directory);

            resources = new ResourceLinker[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                string path = directory + paths[i];
                resources[i] = GD.Load<ResourceLinker>(path);
            }
        }

        public override void _Ready()
        {
            Instance = this;
        }

        public Node LoadResourceAndInstantiate(string resourceId, Node parent, Action<Node> preAddChildCallback = null)
        {
            var resourceToLoad = GetResource(resourceId);
            if (resourceToLoad != null)
            {
                return LoadResourceAndInstantiateFromPath(resourceToLoad.ResourcePath, parent, preAddChildCallback);
            }

            return null;
        }

        public Node LoadResourceAndInstantiateFromPath(string path, Node parent, Action<Node> preAddChildCallback = null)
        {
            Node node = null;
            if (string.IsNullOrEmpty(path) == false)
            {
                PackedScene packed = GD.Load(path) as PackedScene;
                node = packed.Instantiate();

                preAddChildCallback?.Invoke(node);

                parent.AddChild(node, true);
            }

            return node;
        }

        public async Task<Node> LoadResourceAndInstantiateAsync(string resourceId, Node parent, Action<Node> preAddChildCallback = null)
        {
            var resource = GetResource(resourceId);
            if (resource != null)
            {
                return await LoadResourceAndInstantiateAsync(resource.ResourcePath, parent, preAddChildCallback: preAddChildCallback);
            }

            return null;
        }

        private Godot.Collections.Array progress = new Godot.Collections.Array();

        public async Task<Node> LoadResourceAndInstantiateAsync(string path, Node parent, bool useFakeLoading = false, float fakeLoadingDuration = 1, Action<Node> preAddChildCallback = null)
        {
            if (loadingResource)
                return null;

            loadingResource = true;

            Node node = null;
            var result = ResourceLoader.LoadThreadedRequest(path);
            GD.Print($"Loading resource multithread at path {path} RESULT: {result}");
            OnLoadResourceBegin(path);

            ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(path, progress);
            double fakeLoading = 0;
            while ((status = ResourceLoader.LoadThreadedGetStatus(path, progress)) == ResourceLoader.ThreadLoadStatus.InProgress || (useFakeLoading && fakeLoading < 1))
            {
                fakeLoading += GetProcessDeltaTime() / fakeLoadingDuration;
                OnLoadResourceProgress(path, (float)Math.Min((float)progress[0], fakeLoading));
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            OnLoadResourceEnd(path);

            if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                PackedScene packed = ResourceLoader.LoadThreadedGet(path) as PackedScene;
                node = packed.Instantiate() as Node;

                preAddChildCallback?.Invoke(node);

                parent.AddChild(node);
            }
            loadingResource = false;

            return node;
        }


        // Be careful... string and Linq = garbage
        public Resource GetResource(string resourceId)
        {
            var linker = resources.Where(x => x.resourceId == resourceId).FirstOrDefault();
            if (linker == null)
            {
                return null;
            }

            return linker.resource;
        }

        private async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
    }

}