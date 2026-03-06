#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GlueFactorySceneLayoutBuilder
{
    [MenuItem("GlueFactory/Build Editable Scene Layout")]
    public static void BuildEditableSceneLayout()
    {
        const string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            if (File.Exists(sampleScenePath))
            {
                scene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
            }
        }

        if (!scene.IsValid())
        {
            Debug.LogError("Could not open SampleScene.");
            return;
        }

        var legacyRuntimeCanvas = GameObject.Find("GlueCanvas");
        if (legacyRuntimeCanvas != null)
        {
            Object.DestroyImmediate(legacyRuntimeCanvas);
        }

        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
        }

        var managers = EnsureGO(null, "MANAGERS");
        var gameManagerGo = EnsureGO(managers.transform, "GameManager");
        var upgradeManagerGo = EnsureGO(managers.transform, "UpgradeManager");
        var saveSystemGo = EnsureGO(managers.transform, "SaveSystem");
        var audioManagerGo = EnsureGO(managers.transform, "AudioManager");
        var machineSlotMgrGo = EnsureGO(managers.transform, "MachineSlotMgr");
        var uiManagerGo = EnsureGO(managers.transform, "UIManager");

        var rootManagers = GameObject.Find("GlueFactoryManagers") ?? new GameObject("GlueFactoryManagers");
        if (rootManagers.GetComponent<GlueFactoryBootstrap>() == null) rootManagers.AddComponent<GlueFactoryBootstrap>();
        var productDefaults = rootManagers.transform.Find("GlueProductDefaults");
        if (productDefaults == null)
        {
            var go = new GameObject("GlueProductDefaults");
            go.transform.SetParent(rootManagers.transform, false);
            productDefaults = go.transform;
        }
        var productDefinition = productDefaults.GetComponent<GlueProductDefinition>() ?? productDefaults.gameObject.AddComponent<GlueProductDefinition>();
        productDefinition.EnsureDefaultProducts();
        var upgradeDefaults = rootManagers.transform.Find("GlueUpgradeDefaults");
        if (upgradeDefaults == null)
        {
            var go = new GameObject("GlueUpgradeDefaults");
            go.transform.SetParent(rootManagers.transform, false);
            upgradeDefaults = go.transform;
        }
        var upgradeDefinition = upgradeDefaults.GetComponent<GlueUpgradeDefinition>() ?? upgradeDefaults.gameObject.AddComponent<GlueUpgradeDefinition>();
        upgradeDefinition.EnsureDefaults();
        var cheatDefaults = rootManagers.transform.Find("GlueCheatDefaults");
        if (cheatDefaults == null)
        {
            var go = new GameObject("GlueCheatDefaults");
            go.transform.SetParent(rootManagers.transform, false);
            cheatDefaults = go.transform;
        }
        var cheatDefinition = cheatDefaults.GetComponent<GlueFactoryCheatDefinition>() ?? cheatDefaults.gameObject.AddComponent<GlueFactoryCheatDefinition>();
        cheatDefinition.EnsureDefaults();

        RemoveMissingScriptComponents(managers);
        RemoveMissingScriptComponents(gameManagerGo);
        RemoveMissingScriptComponents(upgradeManagerGo);
        RemoveMissingScriptComponents(saveSystemGo);
        RemoveMissingScriptComponents(audioManagerGo);
        RemoveMissingScriptComponents(machineSlotMgrGo);
        RemoveMissingScriptComponents(uiManagerGo);

        if (gameManagerGo.GetComponent<GlueFactoryGameManager>() == null) gameManagerGo.AddComponent<GlueFactoryGameManager>();
        if (saveSystemGo.GetComponent<GlueFactorySaveSystem>() == null) saveSystemGo.AddComponent<GlueFactorySaveSystem>();
        if (audioManagerGo.GetComponent<GlueFactoryAudioManager>() == null) audioManagerGo.AddComponent<GlueFactoryAudioManager>();
        if (machineSlotMgrGo.GetComponent<GlueFactoryWorldManager>() == null) machineSlotMgrGo.AddComponent<GlueFactoryWorldManager>();
        if (uiManagerGo.GetComponent<GlueFactoryUIManager>() == null) uiManagerGo.AddComponent<GlueFactoryUIManager>();
        var productCatalog = upgradeManagerGo.GetComponent<GlueProductCatalog>() ?? upgradeManagerGo.AddComponent<GlueProductCatalog>();
        EnsureCatalogMasterData(productCatalog, productDefaults);
        productDefinition.EnsureDefaultProducts();
        var balanceConfig = EnsureBalanceConfigAsset();
        ConfigureGameManager(gameManagerGo.GetComponent<GlueFactoryGameManager>(), balanceConfig, saveSystemGo.GetComponent<GlueFactorySaveSystem>());
        ConfigureLegacyUiManager(uiManagerGo.GetComponent<GlueFactoryUIManager>(), gameManagerGo.GetComponent<GlueFactoryGameManager>());

        var worldCamera = EnsureWorldSceneLayout();
        ConfigureWorldManager(machineSlotMgrGo.GetComponent<GlueFactoryWorldManager>(), gameManagerGo.GetComponent<GlueFactoryGameManager>(), worldCamera);

        var canvasRt = EnsureCanvas("Canvas");
        EnsureEventSystem();

        var root = EnsurePanelAbs(canvasRt, "Root", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1920, 1080), new Color(0f, 0f, 0f, 0f));
        root.GetComponent<Image>().raycastTarget = false;

        var header = EnsurePanelAbs(root, "Header", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1920, 64), new Color(0.04f, 0.04f, 0.08f, 0.96f));
        var titleText = EnsureText(header, "TitleText", "GLUE FACTORY", new Vector2(20, -10), new Vector2(300, 46), 24, TextAnchor.MiddleLeft, new Color(0.96f, 0.77f, 0.1f));
        var moneyText = EnsureText(header, "MoneyText", "$0", new Vector2(330, -10), new Vector2(240, 46), 24, TextAnchor.MiddleLeft, new Color(0.22f, 1f, 0.38f));
        var perClickText = EnsureText(header, "PerClickText", "+$1/click", new Vector2(580, -10), new Vector2(240, 46), 16, TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));
        var autoIncomeText = EnsureText(header, "AutoIncomeText", "$0/s auto", new Vector2(820, -10), new Vector2(240, 46), 16, TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));
        var totalEarnedText = EnsureText(header, "TotalEarnedText", "Total: $0", new Vector2(1060, -10), new Vector2(220, 46), 16, TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));
        var saveButton = EnsureButton(header, "SaveButton", "SAVE", new Vector2(1620, -14), new Vector2(130, 38), false);
        var deleteButton = EnsureButton(header, "DeleteSaveButton", "DELETE", new Vector2(1760, -14), new Vector2(140, 38), false);
        SetButtonColor(saveButton, new Color(0.14f, 0.32f, 0.15f, 0.96f), new Color(0.72f, 1f, 0.72f, 1f));
        SetButtonColor(deleteButton, new Color(0.36f, 0.12f, 0.12f, 0.96f), new Color(1f, 0.72f, 0.72f, 1f));

        var factoryPanel = EnsurePanelAbs(root, "FactoryPanel", new Vector2(0, 1), new Vector2(0, -64), new Vector2(1540, 1016), new Color(0f, 0f, 0f, 0f));
        var conveyorArea = EnsurePanelAbs(factoryPanel, "ConveyorBeltArea", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1540, 1016), new Color(0, 0, 0, 0));
        var beltBackground = EnsurePanelAbs(conveyorArea, "BeltBackground", new Vector2(0.5f, 0.5f), new Vector2(-260, 170), new Vector2(520, 86), new Color(0f, 0f, 0f, 0f));
        var beltBgImage = beltBackground.GetComponent<Image>();
        if (beltBgImage != null)
        {
            beltBgImage.raycastTarget = false;
        }

        EnsureRectOnly(conveyorArea, "BeltItemParent", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1540, 1016));
        EnsureRectOnly(conveyorArea, "SlotsContainer", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1540, 1016));

        var slot0 = EnsurePanelAbs(factoryPanel, "SlotParent0", new Vector2(0, 1), new Vector2(470, -430), new Vector2(230, 170), new Color(0.2f, 0.2f, 0.24f, 0.95f));
        var slot1 = EnsurePanelAbs(factoryPanel, "SlotParent1", new Vector2(0, 1), new Vector2(720, -430), new Vector2(230, 170), new Color(0.2f, 0.2f, 0.24f, 0.95f));
        var slot2 = EnsurePanelAbs(factoryPanel, "SlotParent2", new Vector2(0, 1), new Vector2(970, -430), new Vector2(230, 170), new Color(0.2f, 0.2f, 0.24f, 0.95f));
        BuildSlotUI(slot0);
        BuildSlotUI(slot1);
        BuildSlotUI(slot2);
        slot0.gameObject.SetActive(false);
        slot1.gameObject.SetActive(false);
        slot2.gameObject.SetActive(false);

        var rightPanel = EnsurePanelAbs(root, "RightPanel", new Vector2(1, 1), new Vector2(-380, -64), new Vector2(380, 1016), new Color(0.05f, 0.05f, 0.09f, 0.98f));
        var tabController = EnsureGO(rightPanel.transform, "TabController_GO");
        EnsureText(rightPanel, "GF_UpgradesHeaderLabel", "UPGRADES", new Vector2(0, -4), new Vector2(380, 34), 24, TextAnchor.MiddleCenter, new Color(0.92f, 0.93f, 0.96f, 1f));
        var tabRow = EnsurePanelAbs(rightPanel, "TabRow", new Vector2(0, 1), new Vector2(0, -44), new Vector2(380, 46), new Color(0.08f, 0.08f, 0.12f, 1f));
        var upgTabBtn = EnsureButton(tabRow, "UpgradesTabBtn", "FACTORY", new Vector2(0, 0), new Vector2(190, 46), false);
        var machineTabBtn = EnsureButton(tabRow, "MachinesTabBtn", "MACHINE", new Vector2(190, 0), new Vector2(190, 46), false);
        SetButtonColor(upgTabBtn, new Color(0.28f, 0.20f, 0.05f, 0.98f), new Color(1f, 0.90f, 0.45f, 1f));
        SetButtonColor(machineTabBtn, new Color(0.08f, 0.20f, 0.30f, 0.98f), new Color(0.72f, 0.92f, 1f, 1f));

        var selectedSlotText = EnsureText(rightPanel, "SelectedSlotText", "", new Vector2(12, -56), new Vector2(356, 28), 14, TextAnchor.MiddleLeft, new Color(0.95f, 0.77f, 0.1f));
        var selectedMachineText = EnsureText(rightPanel, "SelectedMachineText", "", new Vector2(12, -84), new Vector2(356, 44), 14, TextAnchor.UpperLeft, Color.white);
        Object.DestroyImmediate(selectedSlotText.gameObject);
        Object.DestroyImmediate(selectedMachineText.gameObject);
        selectedSlotText = null;
        selectedMachineText = null;

        var installButton = EnsureButton(rightPanel, "InstallButton", "INSTALL TO SELECTED SLOT", new Vector2(12, -132), new Vector2(356, 36), false);
        var sellButton = EnsureButton(rightPanel, "SellButton", "SELL SELECTED SLOT", new Vector2(12, -174), new Vector2(356, 34), false);
        installButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);

        var upgradesTab = EnsurePanelAbs(rightPanel, "UpgradesTab", new Vector2(0, 1), new Vector2(0, -104), new Vector2(380, 800), Color.clear);
        CreateUpgradeCard(upgradesTab, "ClickUpgCard", 8f, out var clickLevel, out var clickBuy);
        CreateUpgradeCard(upgradesTab, "ConveyorCard", 198f, out var conveyorLevel, out var conveyorBuy);
        CreateUpgradeCard(upgradesTab, "ExportCard", 388f, out var exportLevel, out var exportBuy);
        CreateUpgradeCard(upgradesTab, "SpeedCard", 578f, out var speedLevel, out var speedBuy);

        var machinesTab = EnsurePanelAbs(rightPanel, "MachinesTab", new Vector2(0, 1), new Vector2(0, -104), new Vector2(380, 800), Color.clear);
        machinesTab.gameObject.SetActive(false);
        var legacyScroll = FindByName(machinesTab, "ScrollView");
        if (legacyScroll != null)
        {
            Object.DestroyImmediate(legacyScroll.gameObject);
        }
        var legacyContent = FindByName(machinesTab, "Content");
        if (legacyContent != null)
        {
            Object.DestroyImmediate(legacyContent.gameObject);
        }
        // Keep machine list layout identical to factory cards (no legacy scroll-frame styling).
        var content = EnsureRectOnly(machinesTab, "MachinesContent", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(380, 800));

        var toastPanel = EnsureBottomCenterPanel(root, "ToastPanel", 480f, 46f, 16f, new Color(0.05f, 0.05f, 0.09f, 0.95f));
        var toastText = EnsureText(toastPanel, "ToastText", "Toast", new Vector2(0, -4), new Vector2(480, 38), 14, TextAnchor.MiddleCenter, new Color(0.95f, 0.77f, 0.1f));
        toastPanel.gameObject.SetActive(false);

        var floatingTextParent = EnsureRectOnly(root, "FloatingTextParent", new Vector2(0, 0), new Vector2(1, 1));

        RemoveMissingScriptComponents(canvasRt.gameObject);
        var canvasSceneUi = canvasRt.gameObject.GetComponent<GlueFactorySceneUIManager>();
        if (canvasSceneUi != null)
        {
            Object.DestroyImmediate(canvasSceneUi);
        }
        var sceneUi = uiManagerGo.GetComponent<GlueFactorySceneUIManager>() ?? uiManagerGo.AddComponent<GlueFactorySceneUIManager>();
        BindSceneUI(sceneUi, titleText, moneyText, perClickText, autoIncomeText, totalEarnedText, saveButton, deleteButton,
            upgTabBtn, machineTabBtn, upgradesTab, machinesTab, null, installButton, sellButton, selectedMachineText,
            selectedSlotText, clickLevel, conveyorLevel, exportLevel, speedLevel, clickBuy, conveyorBuy, exportBuy, speedBuy,
            content, toastPanel.gameObject, toastPanel.GetComponent<Image>(), toastText, gameManagerGo.GetComponent<GlueFactoryGameManager>(), canvasRt);

        EditorUtility.SetDirty(canvasRt.gameObject);
        EditorUtility.SetDirty(uiManagerGo);
        EditorUtility.SetDirty(rootManagers);
        EditorUtility.SetDirty(productCatalog);
        EditorUtility.SetDirty(productDefinition);
        EditorUtility.SetDirty(upgradeDefinition);
        EditorUtility.SetDirty(cheatDefinition);
        EditorUtility.SetDirty(audioManagerGo);
        EditorUtility.SetDirty(machineSlotMgrGo);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasRt.gameObject;

        Debug.Log("Built SampleScene editable layout and synced to latest machine-slot upgrade UI flow.");
    }

    private static void BindSceneUI(
        GlueFactorySceneUIManager ui,
        Text titleText,
        Text moneyText,
        Text perClickText,
        Text autoIncomeText,
        Text totalEarnedText,
        Button saveButton,
        Button deleteButton,
        Button upgradesTabButton,
        Button machinesTabButton,
        RectTransform upgradesTab,
        RectTransform machinesTab,
        Button glueButton,
        Button installButton,
        Button sellButton,
        Text selectedMachineText,
        Text selectedSlotText,
        Text clickLevelText,
        Text conveyorLevelText,
        Text exportLevelText,
        Text speedLevelText,
        Button clickBuyButton,
        Button conveyorBuyButton,
        Button exportBuyButton,
        Button speedBuyButton,
        RectTransform machineShopContent,
        GameObject toastPanel,
        Image toastBackground,
        Text toastText,
        GlueFactoryGameManager gameManager,
        Transform uiSearchRoot)
    {
        var so = new SerializedObject(ui);
        so.FindProperty("game").objectReferenceValue = gameManager;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("moneyText").objectReferenceValue = moneyText;
        so.FindProperty("perClickText").objectReferenceValue = perClickText;
        so.FindProperty("autoIncomeText").objectReferenceValue = autoIncomeText;
        so.FindProperty("totalEarnedText").objectReferenceValue = totalEarnedText;
        so.FindProperty("saveButton").objectReferenceValue = saveButton;
        so.FindProperty("deleteSaveButton").objectReferenceValue = deleteButton;
        so.FindProperty("upgradesTabButton").objectReferenceValue = upgradesTabButton;
        so.FindProperty("machinesTabButton").objectReferenceValue = machinesTabButton;
        so.FindProperty("upgradesTab").objectReferenceValue = upgradesTab;
        so.FindProperty("machinesTab").objectReferenceValue = machinesTab;
        so.FindProperty("glueButton").objectReferenceValue = glueButton;
        so.FindProperty("installButton").objectReferenceValue = installButton;
        so.FindProperty("sellButton").objectReferenceValue = sellButton;
        so.FindProperty("selectedMachineText").objectReferenceValue = selectedMachineText;
        so.FindProperty("selectedSlotText").objectReferenceValue = selectedSlotText;
        so.FindProperty("clickLevelText").objectReferenceValue = clickLevelText;
        so.FindProperty("clickDescText").objectReferenceValue = FindByName(clickLevelText.transform.parent, "DescText")?.GetComponent<Text>();
        so.FindProperty("clickCostText").objectReferenceValue = FindByName(clickLevelText.transform.parent, "CostText")?.GetComponent<Text>();
        so.FindProperty("conveyorLevelText").objectReferenceValue = conveyorLevelText;
        so.FindProperty("conveyorDescText").objectReferenceValue = FindByName(conveyorLevelText.transform.parent, "DescText")?.GetComponent<Text>();
        so.FindProperty("conveyorCostText").objectReferenceValue = FindByName(conveyorLevelText.transform.parent, "CostText")?.GetComponent<Text>();
        so.FindProperty("exportLevelText").objectReferenceValue = exportLevelText;
        so.FindProperty("exportDescText").objectReferenceValue = FindByName(exportLevelText.transform.parent, "DescText")?.GetComponent<Text>();
        so.FindProperty("exportCostText").objectReferenceValue = FindByName(exportLevelText.transform.parent, "CostText")?.GetComponent<Text>();
        so.FindProperty("speedLevelText").objectReferenceValue = speedLevelText;
        so.FindProperty("speedDescText").objectReferenceValue = FindByName(speedLevelText.transform.parent, "DescText")?.GetComponent<Text>();
        so.FindProperty("speedCostText").objectReferenceValue = FindByName(speedLevelText.transform.parent, "CostText")?.GetComponent<Text>();
        so.FindProperty("clickBuyButton").objectReferenceValue = clickBuyButton;
        so.FindProperty("conveyorBuyButton").objectReferenceValue = conveyorBuyButton;
        so.FindProperty("exportBuyButton").objectReferenceValue = exportBuyButton;
        so.FindProperty("speedBuyButton").objectReferenceValue = speedBuyButton;
        so.FindProperty("machineShopContent").objectReferenceValue = machineShopContent;
        so.FindProperty("toastPanel").objectReferenceValue = toastPanel;
        so.FindProperty("toastBackground").objectReferenceValue = toastBackground;
        so.FindProperty("toastText").objectReferenceValue = toastText;

        var slotButtons = so.FindProperty("slotSelectButtons");
        var slotTexts = so.FindProperty("slotTexts");
        var slotDetails = so.FindProperty("slotDetailTexts");
        var slotProgress = so.FindProperty("slotProgressFills");
        var slotCards = so.FindProperty("slotCardBackgrounds");
        slotButtons.ClearArray();
        slotTexts.ClearArray();
        slotDetails.ClearArray();
        slotProgress.ClearArray();
        slotCards.ClearArray();
        for (var i = 0; i < 3; i++)
        {
            var slotRoot = FindByName(uiSearchRoot, "SlotParent" + i);
            slotButtons.InsertArrayElementAtIndex(i);
            slotTexts.InsertArrayElementAtIndex(i);
            slotDetails.InsertArrayElementAtIndex(i);
            slotProgress.InsertArrayElementAtIndex(i);
            slotCards.InsertArrayElementAtIndex(i);
            slotButtons.GetArrayElementAtIndex(i).objectReferenceValue = FindByName(slotRoot, "SelectButton")?.GetComponent<Button>();
            slotTexts.GetArrayElementAtIndex(i).objectReferenceValue = FindByName(slotRoot, "SlotText")?.GetComponent<Text>();
            slotDetails.GetArrayElementAtIndex(i).objectReferenceValue = FindByName(slotRoot, "SlotDetailText")?.GetComponent<Text>();
            slotProgress.GetArrayElementAtIndex(i).objectReferenceValue = FindByName(slotRoot, "ProgressFill")?.GetComponent<Image>();
            slotCards.GetArrayElementAtIndex(i).objectReferenceValue = slotRoot?.GetComponent<Image>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform EnsureCanvas(string name)
    {
        var existing = GameObject.Find(name);
        var go = existing ?? new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>() ?? go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private static Camera EnsureWorldSceneLayout()
    {
        var cameraGo = EnsureWorldGO(null, "Main Camera");
        var cam = cameraGo.GetComponent<Camera>() ?? cameraGo.AddComponent<Camera>();
        cameraGo.tag = "MainCamera";
        // Match the reference factory overview angle (top-corner isometric feel).
        cameraGo.transform.position = new Vector3(-8.5f, 7.2f, -8.8f);
        cameraGo.transform.rotation = Quaternion.Euler(31f, 45f, 0f);
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 52f;

        var lightGo = EnsureWorldGO(null, "Directional Light");
        var light = lightGo.GetComponent<Light>() ?? lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = new Color(1f, 0.98f, 0.92f);
        lightGo.transform.position = new Vector3(0f, 6f, 0f);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Legacy generated conveyor is no longer used by runtime slot system.
        // Remove it during rebuild so it does not reappear in scene.
        var beltRoot = GameObject.Find("ConveyorBelt");
        if (beltRoot != null)
        {
            Object.DestroyImmediate(beltRoot);
        }

        // Keep rebuild clean: remove old generated floor/walls and do not recreate them.
        var floor = GameObject.Find("Factory_Floor");
        if (floor != null)
        {
            Object.DestroyImmediate(floor);
        }

        var walls = GameObject.Find("Walls");
        if (walls != null)
        {
            Object.DestroyImmediate(walls);
        }

        return cam;
    }

    private static void ConfigureWorldManager(GlueFactoryWorldManager worldManager, GlueFactoryGameManager game, Camera worldCamera)
    {
        if (worldManager == null)
        {
            return;
        }

        var so = new SerializedObject(worldManager);
        so.FindProperty("game").objectReferenceValue = game;
        so.FindProperty("worldCamera").objectReferenceValue = worldCamera;
        so.FindProperty("slotObjectName").stringValue = "MachineSlot_0";
        so.FindProperty("slotYOffset").floatValue = 1.4f;
        so.FindProperty("machineVisualLocalScale").vector3Value = new Vector3(0.0109999999f, 0.0109999999f, 0.0109999999f);
        so.FindProperty("normalizeMachineVisualHeight").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsureWorldGO(Transform parent, string name)
    {
        Transform found;
        if (parent == null)
        {
            found = GameObject.Find(name)?.transform;
        }
        else
        {
            found = parent.Find(name);
        }

        if (found != null)
        {
            return found.gameObject;
        }

        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static GameObject EnsurePrimitiveChild(Transform parent, string name, PrimitiveType type)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject EnsurePrimitiveRoot(string name, PrimitiveType type)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        return go;
    }

    private static void EnsureWall(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
    {
        var wall = EnsurePrimitiveChild(parent, name, PrimitiveType.Cube);
        wall.transform.localPosition = localPosition;
        wall.transform.localScale = localScale;
    }

    private static GameObject EnsureGO(Transform parent, string name)
    {
        Transform found;
        if (parent == null)
        {
            found = GameObject.Find(name)?.transform;
        }
        else
        {
            found = parent.Find(name);
        }

        if (found != null)
        {
            return found.gameObject;
        }

        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static RectTransform EnsureRectOnly(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2? anchoredPos = null, Vector2? sizeDelta = null)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos ?? Vector2.zero;
        rt.sizeDelta = sizeDelta ?? Vector2.zero;
        return rt;
    }

    private static void EnsureEventSystem()
    {
        var go = EventSystem.current != null ? EventSystem.current.gameObject : GameObject.Find("EventSystem");
        if (go == null)
        {
            go = new GameObject("EventSystem");
        }

        if (go.GetComponent<EventSystem>() == null)
        {
            go.AddComponent<EventSystem>();
        }

        var standalone = go.GetComponent<StandaloneInputModule>() ?? go.AddComponent<StandaloneInputModule>();
        var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        Behaviour inputSystemModule = null;
        if (inputSystemType != null)
        {
            if (go.GetComponent(inputSystemType) == null)
            {
                go.AddComponent(inputSystemType);
            }
            inputSystemModule = go.GetComponent(inputSystemType) as Behaviour;
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (inputSystemModule != null) inputSystemModule.enabled = true;
        standalone.enabled = false;
#else
        if (inputSystemModule != null) inputSystemModule.enabled = false;
        standalone.enabled = true;
#endif
    }

    private static RectTransform EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float left, float bottom, float right, float top, Color color)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        return rt;
    }

    private static RectTransform EnsurePanelAbs(Transform parent, string name, Vector2 anchorMin, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMin;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        return rt;
    }

    private static RectTransform EnsureBottomCenterPanel(Transform parent, string name, float width, float height, float bottomOffset, Color color)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomOffset);
        rt.sizeDelta = new Vector2(width, height);

        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        return rt;
    }

    private static Text EnsureText(Transform parent, string name, string content, Vector2 anchoredPos, Vector2 size, int fontSize = 12, TextAnchor align = TextAnchor.MiddleLeft, Color? color = null)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color ?? Color.white;
        text.text = content;
        return text;
    }

    private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, bool fromRight)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        if (fromRight)
        {
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
        }

        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.22f, 0.25f, 0.92f);
        var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();

        var text = EnsureText(go.transform, "Text", label, Vector2.zero, size, 10, TextAnchor.MiddleCenter, Color.white);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        return button;
    }

    private static void SetButtonColor(Button button, Color background, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = background;
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.color = textColor;
        }
    }

    private static Image EnsureImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Scrollbar EnsureScrollbar(Transform parent, string name)
    {
        var go = EnsureGO(parent, name);
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-16, 0);
        rt.offsetMax = new Vector2(0, 0);

        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.1f);
        var scrollbar = go.GetComponent<Scrollbar>() ?? go.AddComponent<Scrollbar>();

        var slidingArea = EnsureRectOnly(go.transform, "Sliding Area", new Vector2(0, 0), new Vector2(1, 1));
        var handle = EnsurePanel(slidingArea, "Handle", new Vector2(0, 0), new Vector2(1, 1), 0, 0, 0, 0, new Color(0.8f, 0.8f, 0.8f, 0.9f));
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.handleRect = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static void CreateUpgradeCard(Transform parent, string name, float y, out Text levelText, out Button buyButton)
    {
        var cardColor = name switch
        {
            "ClickUpgCard" => new Color(0.20f, 0.14f, 0.07f, 0.92f),
            "ConveyorCard" => new Color(0.10f, 0.17f, 0.22f, 0.92f),
            "ExportCard" => new Color(0.14f, 0.12f, 0.24f, 0.92f),
            "SpeedCard" => new Color(0.14f, 0.22f, 0.14f, 0.92f),
            _ => new Color(0.12f, 0.12f, 0.15f, 0.92f)
        };
        var card = EnsurePanelAbs(parent, name, new Vector2(0, 1), new Vector2(8, -y), new Vector2(364, 188), cardColor);
        levelText = EnsureText(card, "LevelText", "Lv 0", new Vector2(14, -12), new Vector2(336, 30), 14, TextAnchor.MiddleLeft, Color.white);
        var desc = EnsureText(card, "DescText", "Description", new Vector2(14, -50), new Vector2(336, 42), 12, TextAnchor.UpperLeft, new Color(0.65f, 0.65f, 0.68f));
        desc.lineSpacing = 1.2f;
        EnsureText(card, "CostText", "Cost", new Vector2(14, -98), new Vector2(336, 24), 12, TextAnchor.MiddleLeft, new Color(0.95f, 0.77f, 0.1f));
        buyButton = EnsureButton(card, "BuyButton", "BUY", new Vector2(14, -126), new Vector2(336, 36), false);
        SetButtonColor(buyButton, new Color(0.28f, 0.20f, 0.05f, 0.96f), new Color(1f, 0.92f, 0.56f, 1f));

        var sliderGO = EnsureGO(card, "ProgressSlider");
        var sliderRT = sliderGO.GetComponent<RectTransform>() ?? sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0, 1);
        sliderRT.anchorMax = new Vector2(0, 1);
        sliderRT.pivot = new Vector2(0, 1);
        sliderRT.anchoredPosition = new Vector2(14, -168);
        sliderRT.sizeDelta = new Vector2(336, 12);
        var slider = sliderGO.GetComponent<Slider>() ?? sliderGO.AddComponent<Slider>();
        slider.interactable = false;
    }

    private static void BuildSlotUI(RectTransform slot)
    {
        var iconBg = EnsurePanel(slot, "SlotIconBg", new Vector2(0, 1), new Vector2(0, 1), 86, -52, 144, -10, new Color(0.10f, 0.10f, 0.12f, 0.96f));
        var icon = EnsureImage(iconBg, "SlotProductIcon", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.9f));
        icon.preserveAspect = true;
        icon.enabled = false;

        EnsureText(slot, "SlotText", "EMPTY", new Vector2(8, -60), new Vector2(214, 30), 13, TextAnchor.MiddleCenter, Color.white);
        EnsureText(slot, "SlotDetailText", "Install machine", new Vector2(8, -86), new Vector2(214, 18), 9, TextAnchor.MiddleCenter, new Color(0.78f, 0.82f, 0.88f, 1f));
        var selectBtn = EnsureButton(slot, "SelectButton", "UNAVAILABLE", new Vector2(62, -132), new Vector2(106, 30), false);
        SetButtonColor(selectBtn, new Color(0.08f, 0.23f, 0.35f, 0.96f), new Color(0.73f, 0.93f, 1f, 1f));
        selectBtn.interactable = false;

        var bg = EnsurePanel(slot, "ProgressBg", new Vector2(0, 1), new Vector2(0, 1), 8, -114, 222, -100, new Color(0.12f, 0.12f, 0.14f, 1f));
        var fill = EnsurePanel(bg, "ProgressFill", new Vector2(0, 0), new Vector2(1, 1), 0, 0, 0, 0, new Color(0.96f, 0.77f, 0.1f, 1f));
        var fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindByName(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void RemoveMissingScriptComponents(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
    }

    private static GlueFactoryBalanceConfig EnsureBalanceConfigAsset()
    {
        const string resourcesFolder = "Assets/Resources";
        const string assetPath = "Assets/Resources/GlueFactoryBalance.asset";
        var config = AssetDatabase.LoadAssetAtPath<GlueFactoryBalanceConfig>(assetPath);
        if (config != null)
        {
            return config;
        }

        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        config = ScriptableObject.CreateInstance<GlueFactoryBalanceConfig>();
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return config;
    }

    private static void ConfigureGameManager(GlueFactoryGameManager gameManager, GlueFactoryBalanceConfig config, GlueFactorySaveSystem saveSystem)
    {
        if (gameManager == null)
        {
            return;
        }

        var so = new SerializedObject(gameManager);
        so.FindProperty("config").objectReferenceValue = config;
        so.FindProperty("saveSystem").objectReferenceValue = saveSystem;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureLegacyUiManager(GlueFactoryUIManager uiManager, GlueFactoryGameManager gameManager)
    {
        if (uiManager == null)
        {
            return;
        }

        var so = new SerializedObject(uiManager);
        so.FindProperty("game").objectReferenceValue = gameManager;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureCatalogMasterData(GlueProductCatalog catalog, Transform productDefaultsRoot)
    {
        if (catalog == null || productDefaultsRoot == null)
        {
            return;
        }

        var so = new SerializedObject(catalog);
        so.FindProperty("useSceneDefinitions").boolValue = true;
        so.FindProperty("definitionsRoot").objectReferenceValue = productDefaultsRoot;
        so.FindProperty("includeInactive").boolValue = true;
        so.FindProperty("createDefaultProductsWhenMissing").boolValue = true;
        so.FindProperty("ensureDefaultProductsInSameList").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        catalog.EnsureDefaultDefinitions();
    }
}
#endif
