using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GlueFactoryBootstrap : MonoBehaviour
{
    private static bool initialized;
    private GlueFactoryBalanceConfig runtimeConfig;
    private bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInitFlag()
    {
        initialized = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var root = GameObject.Find("GlueFactoryManagers");
        if (root == null)
        {
            root = new GameObject("GlueFactoryManagers");
            DontDestroyOnLoad(root);
        }

        var bootstrap = root.GetComponent<GlueFactoryBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = root.AddComponent<GlueFactoryBootstrap>();
        }

        bootstrap.Setup(root, !initialized);
        initialized = true;
    }

    private void OnEnable()
    {
        RegisterSceneReloadHook();
    }

    private void OnDisable()
    {
        UnregisterSceneReloadHook();
    }

    private void RegisterSceneReloadHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private void UnregisterSceneReloadHook()
    {
        if (!sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        sceneHookRegistered = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!initialized || this == null)
        {
            return;
        }

        Setup(gameObject, false);
    }

    public static void ForceRebindActiveScene()
    {
        var bootstrap = FindFirstObjectByType<GlueFactoryBootstrap>();
        if (bootstrap == null)
        {
            return;
        }

        bootstrap.Setup(bootstrap.gameObject, false);
    }

    private void Setup(GameObject root, bool initializeGameSystems)
    {
        var game = FindOrCreateManager<GlueFactoryGameManager>(root, "GameManager");
        var world = FindOrCreateManager<GlueFactoryWorldManager>(root, "MachineSlotManager", "MachineSlotMgr");
        var audio = FindOrCreateManager<GlueFactoryAudioManager>(root, "AudioManager", "SoundManager");

        if (initializeGameSystems || runtimeConfig == null || game.Config == null)
        {
            InitializeRuntimeSystems(root, game, audio);
        }
        else
        {
            RebindAudio(audio, game);
        }

        BindSceneSystems(root, game, world);
    }

    private void InitializeRuntimeSystems(GameObject root, GlueFactoryGameManager game, GlueFactoryAudioManager audio)
    {
        var config = Resources.Load<GlueFactoryBalanceConfig>("GlueFactoryBalance");
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GlueFactoryBalanceConfig>();
        }

        if (runtimeConfig != null)
        {
            Destroy(runtimeConfig);
        }

        runtimeConfig = Instantiate(config);
        runtimeConfig.name = config.name + "_Runtime";
        runtimeConfig.hideFlags = HideFlags.DontSave;

        var upgradeDefinition = FindFirstObjectByType<GlueUpgradeDefinition>();
        if (upgradeDefinition != null)
        {
            upgradeDefinition.ApplyTo(runtimeConfig);
        }
        runtimeConfig.conveyorUpgrade.maxLevel = 2;

        var save = FindOrCreateManager<GlueFactorySaveSystem>(root, "SaveSystem");
        var catalog = FindOrCreateManager<GlueProductCatalog>(root, "UpgradeManager");
        catalog.ApplyTo(runtimeConfig);
        game.Configure(runtimeConfig, save);

        RebindAudio(audio, game);
    }

    private static void RebindAudio(GlueFactoryAudioManager audio, GlueFactoryGameManager game)
    {
        if (audio == null || game == null)
        {
            return;
        }

        var audioConfig = Resources.Load<GlueFactoryAudioConfig>("GlueFactoryAudio");
        if (audioConfig == null)
        {
            audioConfig = ScriptableObject.CreateInstance<GlueFactoryAudioConfig>();
        }
        audioConfig.EnsureDefaults();
        audio.Configure(audioConfig, game);
    }

    private static void BindSceneSystems(GameObject root, GlueFactoryGameManager game, GlueFactoryWorldManager world)
    {
        var sceneUi = ResolveSceneUiManager();
        if (sceneUi != null)
        {
            var legacyCanvas = GameObject.Find("GlueCanvas");
            if (legacyCanvas != null)
            {
                Destroy(legacyCanvas);
            }

            var runtimeUi = root.GetComponent<GlueFactoryUIManager>();
            if (runtimeUi != null)
            {
                runtimeUi.enabled = false;
            }

            sceneUi.Bind(game);
        }
        else
        {
            var ui = FindOrCreateManager<GlueFactoryUIManager>(root, "UIManager");
            ui.enabled = true;
            ui.Bind(game);
        }

        world.Bind(game);
    }

    private static T FindOrCreateManager<T>(GameObject root, params string[] managerObjectNames) where T : Component
    {
        var existing = FindFirstObjectByType<T>();
        if (existing != null)
        {
            return existing;
        }

        var managers = GameObject.Find("MANAGERS");
        if (managers != null)
        {
            for (var i = 0; i < managerObjectNames.Length; i++)
            {
                var child = managers.transform.Find(managerObjectNames[i]);
                if (child == null)
                {
                    continue;
                }

                return child.GetComponent<T>() ?? child.gameObject.AddComponent<T>();
            }
        }

        return root.GetComponent<T>() ?? root.AddComponent<T>();
    }

    private static GlueFactorySceneUIManager ResolveSceneUiManager()
    {
        var managers = GameObject.Find("MANAGERS");
        if (managers != null)
        {
            var uiGo = managers.transform.Find("UIManager");
            if (uiGo != null)
            {
                var preferred = uiGo.GetComponent<GlueFactorySceneUIManager>();
                if (preferred != null)
                {
                    return preferred;
                }
            }
        }

        return FindFirstObjectByType<GlueFactorySceneUIManager>();
    }
}
