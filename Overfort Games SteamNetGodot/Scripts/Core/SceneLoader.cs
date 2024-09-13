using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class SceneLoader : Node
    {
        [Export]
        public int showSplashScreenForMilliseconds = 1500;

        private Dictionary<string, Node> loadedScenes = new Dictionary<string, Node>();

        public static SceneLoader Instance { get; private set; }

        public Node LastLoadedScene { get; private set; }

        public Node GameplayScene { get; private set; }

        private bool isAddingSceneAsync;
        private bool isLoadingSceneAsync;

        public override async void _Ready()
        {
            if (Instance != null)
            {
                GD.PrintErr($"{GetType().Name} already instanced. This node will be destroyed");
                QueueFree();
                return;
            }

            Instance = this;

            await Task.Delay(100);

            await LoadSceneAsync(ResourceId.SplashScreen);

            await Task.Delay(showSplashScreenForMilliseconds);

            LoadSceneAsync(ResourceId.Home, true, 1);
        }

        public async Task<Node> LoadSceneAsyncFromPath(string scenePath, Node parent = null, bool useFakeLoading = false, float fakeLoadingDuration = 1)
        {
            if (isLoadingSceneAsync)
                return null;

            isLoadingSceneAsync = true;
            foreach (var scene in loadedScenes)
            {
                RemoveSceneIfLoadedFromScenePath(scene.Key);
            }

            var sceneInstance = await AddSceneAsyncFromPath(scenePath, parent, useFakeLoading, fakeLoadingDuration);

            isLoadingSceneAsync = false;

            return sceneInstance;
        }

        public async Task<Node> LoadSceneAsync(string sceneResourceId, bool useFakeLoading = false, float fakeLoadingDuration = 1, bool isGameplayScene = false)
        {
            var loadedScene = await LoadSceneAsyncFromPath(SimpleResourceLoader.Instance.GetResource(sceneResourceId).ResourcePath, null, useFakeLoading, fakeLoadingDuration);

            if (isGameplayScene)
                GameplayScene = loadedScene;

            return loadedScene;
        }

        public async Task<Node> AddSceneAsync(string sceneResourceId, bool useFakeLoading = false, float fakeLoadingDuration = 1)
        {
            return await AddSceneAsyncFromPath(SimpleResourceLoader.Instance.GetResource(sceneResourceId).ResourcePath, null, useFakeLoading, fakeLoadingDuration);
        }

        public async Task<Node> AddSceneAsyncFromPath(string scenePath, Node parent = null, bool useFakeLoading = false, float fakeLoadingDuration = 1)
        {
            if (isAddingSceneAsync)
                return null;

            isAddingSceneAsync = true;

            if (parent == null)
                parent = this;

            RemoveSceneIfLoadedFromScenePath(scenePath);

            Node sceneInstance = await SimpleResourceLoader.Instance.LoadResourceAndInstantiateAsync(scenePath, parent, useFakeLoading, fakeLoadingDuration);
            if (sceneInstance != null)
            {
                loadedScenes[scenePath] = sceneInstance;
                LastLoadedScene = loadedScenes[scenePath];
            }
            else
            {
                GD.PrintErr($"Failed to load the scene at path: {scenePath}");
            }

            isAddingSceneAsync = false;

            return sceneInstance;
        }


        public Node AddScene(string sceneResourceId)
        {
            return AddSceneFromPath(SimpleResourceLoader.Instance.GetResource(sceneResourceId).ResourcePath, null);
        }

        public Node AddSceneFromPath(string scenePath, Node parent = null)
        {
            if (parent == null)
                parent = this;

            RemoveSceneIfLoadedFromScenePath(scenePath);

            Node sceneInstance = SimpleResourceLoader.Instance.LoadResourceAndInstantiateFromPath(scenePath, parent);
            if (sceneInstance != null)
            {
                loadedScenes[scenePath] = sceneInstance;
                LastLoadedScene = loadedScenes[scenePath];
            }
            else
            {
                GD.PrintErr($"Failed to load the scene at path: {scenePath}");
            }

            return sceneInstance;
        }

        public void RemoveSceneIfLoaded(string sceneResourceId)
        {
            RemoveSceneIfLoadedFromScenePath(SimpleResourceLoader.Instance.GetResource(sceneResourceId).ResourcePath);
        }

        public void RemoveSceneIfLoadedFromScenePath(string scenePath)
        {
            if (loadedScenes.ContainsKey(scenePath))
            {
                loadedScenes[scenePath].QueueFree();
                loadedScenes.Remove(scenePath);
            }
            else
            {
                //   GD.Print($"Scene at path: {scenePath} is not loaded.");
            }
        }

    }
}
