using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class GlueFactoryWorldManager : MonoBehaviour
{
    private static readonly Vector3 DefaultMachineVisualScale = new Vector3(0.0109999999f, 0.0109999999f, 0.0109999999f);

    [SerializeField] private GlueFactoryGameManager game;
    [SerializeField] private Camera worldCamera;

    [Header("Camera Attachment")]
    [SerializeField] private bool attachMainCameraToWorldManager = true;
    [SerializeField] private Vector3 attachedCameraWorldPosition = new Vector3(28.46f, 21.3f, -29.9f);
    [SerializeField] private Quaternion attachedCameraWorldRotation = Quaternion.Euler(23.309f, -38.331f, -0.002f);
    [SerializeField] private bool applyCameraLensOnStart = true;
    [SerializeField] private bool cameraOrthographic = true;
    [SerializeField] private float cameraOrthographicSize = 9f;
    [SerializeField] private float cameraNearClip = 0.3f;
    [SerializeField] private float cameraFarClip = 1000f;

    [Header("Slot Anchors")]
    [SerializeField] private List<Transform> slotAnchorOverrides = new List<Transform>();
    [SerializeField] private string slotObjectName = "Auto_Glue_Machine1";
    [SerializeField] private float slotYOffset = 1.4f;
    [SerializeField] private bool useFixedRuntimeSlotAnchors = true;
    [SerializeField] private Vector3 slot1WorldPosition = new Vector3(3f, 0f, 6f);
    [SerializeField] private Vector3 slot2WorldPosition = new Vector3(3f, 0f, -0.44020343f);
    [SerializeField] private Vector3 slot3WorldPosition = new Vector3(3f, 0f, -7f);

    [Header("Machine Visuals")]
    [SerializeField] private GameObject machineVisualPrefab;
    [SerializeField] private string machineVisualPrefabResourceName = "MachineSet";
    [SerializeField] private Vector3 machineVisualLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 machineVisualLocalEuler = Vector3.zero;
    [SerializeField] private Vector3 machineVisualLocalScale = new Vector3(0.0109999999f, 0.0109999999f, 0.0109999999f);
    [SerializeField] private bool normalizeMachineVisualHeight = false;
    [SerializeField] private float machineVisualTargetHeight = 3.6f;
    [SerializeField] private bool alignMachineVisualBottomToOffset = true;
    [SerializeField] private bool hideSceneSlotModelsWhenEmpty = true;
    [SerializeField] private bool showWorldSlotOverlay = false;

    [Header("Glue Movement")]
    [SerializeField] private GameObject playerGluePrefab;
    [SerializeField] private string playerGluePrefabResourceName = "Player_Glue";
    [SerializeField] private Vector3 playerGlueVisualScale = new Vector3(0.011f, 0.011f, 0.011f);
    [SerializeField] private Transform manualSpawnAnchorOverride;
    [SerializeField] private string[] manualSpawnAnchorNameCandidates = { "PlayerMachine1", "PlayerMachine2", "PlayerMachine3", "PlayerMachine" };
    [SerializeField] private Vector3 movingGlueSpawnOffset = new Vector3(2f, 0.6f, 0f);
    [SerializeField] private Vector3 movingGlueGateOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private Vector3 movingGlueVisualScale = new Vector3(0.01f, 0.01f, 0.01f);
    [SerializeField] private bool dynamicMovingGlueScale = true;
    [SerializeField] private bool useDynamicGlueScaleList = true;
    [SerializeField] private bool autoSyncDynamicGlueScaleList = true;
    [SerializeField] private Vector2 dynamicGlueScaleMultiplierRange = new Vector2(0.9f, 2.4f);
    [SerializeField] private float dynamicGlueScaleJitter = 0f;
    [SerializeField] private List<GlueScaleEntry> dynamicGlueScaleList = new List<GlueScaleEntry>();
    [SerializeField] private bool normalizeMovingGlueHeight = false;
    [SerializeField] private float movingGlueTargetHeight = 0.35f;
    [SerializeField] private float movingGlueBottomClearance = 0.01f;
    [SerializeField] private float movingGlueTravelSeconds = 2.2f;
    [SerializeField] private Vector3 manualStopPointWorld = new Vector3(-8.0f, 1.8f, 6.460558f);
    [SerializeField] private bool manualClickUsesAllUnlockedSlots = true;
    [SerializeField] private bool usePerSlotManualRoutePoints = true;
    [SerializeField] private Vector3[] manualSpawnWorldBySlot =
    {
        new Vector3(3f, 1.8f, 6.4f),
        new Vector3(3f, 1.8f, -0.0f),
        new Vector3(3f, 1.8f, -6.6f)
    };
    [SerializeField] private Vector3[] manualSellExitWorldBySlot =
    {
        new Vector3(-6.52522707f, 1.8f, 6.4f),
        new Vector3(-6.52522707f, 1.8f, 0.0f),
        new Vector3(-6.52522707f, 1.8f, -6.6f)
    };
    [SerializeField] private bool autoMachineUseSellLaneRoute = true;
    [SerializeField] private float autoSellLaneWorldX = -8.0f;
    [SerializeField] private float autoTravelWorldY = 1.8f;
    [SerializeField] private Vector3[] autoSpawnWorldBySlot =
    {
        new Vector3(-1.59800005f, 4.2931447f, 6.4f),
        new Vector3(-1.59800005f, 4.2931447f, 0.0f),
        new Vector3(-1.59800005f, 4.2931447f, -6.6f)
    };

    [Header("Sell Gate")]
    [SerializeField] private Transform sellGateOverride;
    [SerializeField] private string[] sellGateNameCandidates = { "Gate2", "Gate3", "Gate" };
    [SerializeField] private Transform manualSellGateOverride;
    [SerializeField] private string[] manualSellGateNameCandidates = { "Gate" };
    [SerializeField] private string[] autoSlotGateNameBySlot = { "Gate", "Gate2", "Gate3" };
    [SerializeField] private List<Transform> autoSlotGateOverrides = new List<Transform>();
    [SerializeField] private bool removeRebuiltRuntimeConveyorBelt = true;

    [Header("Gate Progression")]
    [SerializeField] private GameObject gatePrefab;
    [SerializeField] private string gatePrefabResourceName = "Gate";
    [SerializeField] private Transform gate2Override;
    [SerializeField] private Transform gate3Override;
    [SerializeField] private Vector3 gate2ClosedWorldPosition = new Vector3(-6.52522707f, 2.44615936f, 0.013409576f);
    [SerializeField] private Vector3 gate3ClosedWorldPosition = new Vector3(-6.52522707f, 2.44615936f, -6.60762119f);
    [SerializeField] private Vector3 gateClosedLocalScale = new Vector3(0.01f, 0.01f, 0.01f);
    [SerializeField] private float gateOpenSlideDistance = 3.2f;
    [SerializeField] private float gateSlideDuration = 0.55f;
    [SerializeField] private bool hideGateWhenOpened = true;
    [SerializeField, Range(0f, 1f)] private float gateHideStartProgress01 = 0.5f;

    private readonly List<Transform> slotAnchors = new List<Transform>();
    private readonly List<Renderer[]> slotSceneRenderers = new List<Renderer[]>();
    private readonly List<Renderer[]> nonSlotSceneRenderers = new List<Renderer[]>();
    private readonly List<GameObject> slotPrefabVisuals = new List<GameObject>();
    private readonly List<Transform> slotAutoSpawnAnchors = new List<Transform>();
    private readonly List<Vector3> slotConveyorEntryPoints = new List<Vector3>();
    private readonly List<Vector3> slotConveyorExitPoints = new List<Vector3>();
    private readonly List<bool> slotHasConveyorPath = new List<bool>();
    private readonly List<int> slotVisualMachineIds = new List<int>();
    private readonly List<RectTransform> worldProgress = new List<RectTransform>();
    private readonly List<Text> worldLabels = new List<Text>();
    private readonly List<Collider> slotClickColliders = new List<Collider>();
    private readonly Dictionary<Collider, int> colliderToSlot = new Dictionary<Collider, int>();
    private readonly Dictionary<string, GameObject> machinePrefabCache = new Dictionary<string, GameObject>();
    private readonly List<MovingGluePiece> movingPieces = new List<MovingGluePiece>();
    private readonly List<Collider> allGateColliders = new List<Collider>();
    private readonly List<Transform> slotGateTargets = new List<Transform>();

    private Transform sellGateTarget;
    private Transform manualSellGateTarget;
    private Transform manualSpawnAnchor;
    private Collider sellGateCollider;
    private bool gateProgressionInitialized;
    private GateRuntimeState gate2State;
    private GateRuntimeState gate3State;

    private sealed class MovingGluePiece
    {
        public GameObject Instance;
        public Vector3 Start;
        public Vector3 End;
        public float Duration;
        public float Elapsed;
        public double Amount;
        public bool CollectOnArrival;
        public bool IsManual;
        public Vector3[] PathPoints;
        public Collider TargetCollider;
        public float StopDistanceXZ;
        public bool UseForcedStopPoint;
    }

    [Serializable]
    private sealed class GlueScaleEntry
    {
        public string machineId;
        public string prefabName;
        public Vector3 localScale = new Vector3(0.01f, 0.01f, 0.01f);
        public bool enabled = true;
    }

    private sealed class GateRuntimeState
    {
        public Transform Root;
        public Vector3 ClosedPosition;
        public Quaternion ClosedRotation;
        public Vector3 ClosedLocalScale;
        public Renderer[] Renderers;
        public Collider[] Colliders;
        public float Progress01;
        public float Target01;
    }

    public void Bind(GlueFactoryGameManager gameManager)
    {
        if (game != null)
        {
            game.OnChanged -= Refresh;
            game.OnMachineProduced -= HandleMachineProduced;
            game.OnManualProduced -= HandleManualProduced;
        }

        game = gameManager;
        if (autoSyncDynamicGlueScaleList)
        {
            EnsureDynamicGlueScaleList();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                worldCamera = FindFirstObjectByType<Camera>();
            }
        }

        AttachAndAlignWorldCamera();

        if (machineVisualPrefab == null && !string.IsNullOrWhiteSpace(machineVisualPrefabResourceName))
        {
            machineVisualPrefab = LoadRuntimePrefab(machineVisualPrefabResourceName);
        }
        if (machineVisualPrefab == null)
        {
            machineVisualPrefab = LoadMachineSetPrefabFallback();
        }

        if (playerGluePrefab == null && !string.IsNullOrWhiteSpace(playerGluePrefabResourceName))
        {
            playerGluePrefab = LoadRuntimePrefab(playerGluePrefabResourceName);
        }
        if (gatePrefab == null && !string.IsNullOrWhiteSpace(gatePrefabResourceName))
        {
            gatePrefab = LoadRuntimePrefab(gatePrefabResourceName);
        }
        if (gatePrefab == null)
        {
            gatePrefab = LoadGatePrefabFallback();
        }

        sellGateTarget = ResolveSellGateTarget();
        manualSpawnAnchor = ResolveManualSpawnAnchor();
        manualSellGateTarget = ResolveManualSellGateTarget();
        sellGateCollider = sellGateTarget != null ? sellGateTarget.GetComponentInChildren<Collider>() : null;
        InitializeGateProgressionStates();
        CacheAllGateColliders();
        if (removeRebuiltRuntimeConveyorBelt)
        {
            RemoveLegacyRebuiltConveyorBelt();
        }
        machinePrefabCache.Clear();

        BuildWorldBindings();

        game.OnChanged += Refresh;
        game.OnMachineProduced += HandleMachineProduced;
        game.OnManualProduced += HandleManualProduced;
        Refresh();
    }

    private void AttachAndAlignWorldCamera()
    {
        if (!attachMainCameraToWorldManager || worldCamera == null)
        {
            return;
        }

        var camTransform = worldCamera.transform;
        camTransform.SetPositionAndRotation(attachedCameraWorldPosition, attachedCameraWorldRotation);
        camTransform.SetParent(transform, true);
        camTransform.localScale = Vector3.one;

        if (!applyCameraLensOnStart)
        {
            return;
        }

        worldCamera.orthographic = cameraOrthographic;
        if (cameraOrthographic)
        {
            worldCamera.orthographicSize = Mathf.Max(0.01f, cameraOrthographicSize);
        }
        worldCamera.nearClipPlane = Mathf.Max(0.01f, cameraNearClip);
        worldCamera.farClipPlane = Mathf.Max(worldCamera.nearClipPlane + 0.01f, cameraFarClip);
    }

    public bool IsMainCameraReadyForGameplay()
    {
        if (!attachMainCameraToWorldManager)
        {
            return worldCamera != null || Camera.main != null;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return false;
        }

        var t = cam.transform;
        var posReady = Vector3.SqrMagnitude(t.position - attachedCameraWorldPosition) <= 0.0025f; // 5cm tolerance
        var rotReady = Quaternion.Angle(t.rotation, attachedCameraWorldRotation) <= 0.5f;
        return posReady && rotReady;
    }

    private void Update()
    {
        if (game == null || worldCamera == null)
        {
            return;
        }

        TickGateProgression(Time.deltaTime);
        TickMovingPieces(Time.deltaTime);
        TrySelectSlotByClick();
    }

    private void OnDestroy()
    {
        if (game != null)
        {
            game.OnChanged -= Refresh;
            game.OnMachineProduced -= HandleMachineProduced;
            game.OnManualProduced -= HandleManualProduced;
        }

        gateProgressionInitialized = false;
        ClearMovingPieces();
    }

    private void BuildWorldBindings()
    {
        slotAnchors.Clear();
        slotSceneRenderers.Clear();
        nonSlotSceneRenderers.Clear();
        slotPrefabVisuals.Clear();
        slotAutoSpawnAnchors.Clear();
        slotConveyorEntryPoints.Clear();
        slotConveyorExitPoints.Clear();
        slotHasConveyorPath.Clear();
        slotVisualMachineIds.Clear();
        worldProgress.Clear();
        worldLabels.Clear();
        slotClickColliders.Clear();
        colliderToSlot.Clear();
        slotGateTargets.Clear();

        if (useFixedRuntimeSlotAnchors)
        {
            EnsureFixedRuntimeSlotAnchorsAtConfiguredPositions();
            CollectLegacySceneSlotRenderersForHiding();
        }
        else
        {
            for (var i = 0; i < slotAnchorOverrides.Count; i++)
            {
                if (slotAnchorOverrides[i] != null && !slotAnchors.Contains(slotAnchorOverrides[i]))
                {
                    slotAnchors.Add(slotAnchorOverrides[i]);
                }
            }

            CollectSlotAnchorsByCommonNames();
            if (slotAnchors.Count == 0)
            {
                var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i].name.StartsWith("Auto_Glue_Machine", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!slotAnchors.Contains(all[i]))
                        {
                            slotAnchors.Add(all[i]);
                        }
                    }
                    else if (all[i].name == slotObjectName)
                    {
                        if (!slotAnchors.Contains(all[i]))
                        {
                            slotAnchors.Add(all[i]);
                        }
                    }
                }
            }

            EnsureFallbackSlotAnchorsAtKnownPositions();
        }

        if (slotAnchors.Count == 0)
        {
            return;
        }

        // Scene uses Z spacing for Machine1/2/3 (front to back), so keep slots ordered by world Z.
        slotAnchors.Sort((a, b) => b.position.z.CompareTo(a.position.z));
        var max = Mathf.Min(3, slotAnchors.Count);

        for (var i = 0; i < max; i++)
        {
            var anchor = slotAnchors[i];

            CleanupRuntimeObjects(anchor);
            slotSceneRenderers.Add(anchor.GetComponentsInChildren<Renderer>(true));
            slotPrefabVisuals.Add(null);
            slotAutoSpawnAnchors.Add(anchor);
            slotConveyorEntryPoints.Add(anchor.position);
            slotConveyorExitPoints.Add(anchor.position);
            slotHasConveyorPath.Add(false);
            slotVisualMachineIds.Add(int.MinValue);
            slotGateTargets.Add(ResolveAutoSlotGateTarget(i));

            var clickTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            clickTarget.name = "GF_ClickTarget_Slot_" + i;
            clickTarget.transform.SetParent(anchor, false);
            clickTarget.transform.localPosition = new Vector3(0f, slotYOffset, 0f);
            clickTarget.transform.localScale = Vector3.one * 0.25f;
            var renderer = clickTarget.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            var collider = clickTarget.GetComponent<Collider>();
            if (collider != null)
            {
                colliderToSlot[collider] = i;
            }
            slotClickColliders.Add(collider);

            if (showWorldSlotOverlay)
            {
                var canvasGo = new GameObject("GF_WorldCanvas_" + i, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(anchor, false);
                canvasGo.transform.localPosition = new Vector3(0f, slotYOffset + 0.22f, 0f);
                canvasGo.transform.localRotation = Quaternion.identity;
                canvasGo.transform.localScale = Vector3.one * 0.01f;

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = worldCamera;

                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(220, 60);

                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(canvasGo.transform, false);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0, 1);
                labelRt.anchorMax = new Vector2(0, 1);
                labelRt.pivot = new Vector2(0, 1);
                labelRt.anchoredPosition = new Vector2(0, 0);
                labelRt.sizeDelta = new Vector2(220, 28);
                var label = labelGo.GetComponent<Text>();
                label.font = font;
                label.fontSize = 18;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.95f, 0.9f, 0.7f);
                label.text = "Slot " + (i + 1);

                var bgGo = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(canvasGo.transform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0, 1);
                bgRt.anchorMax = new Vector2(0, 1);
                bgRt.pivot = new Vector2(0, 1);
                bgRt.anchoredPosition = new Vector2(10, -34);
                bgRt.sizeDelta = new Vector2(200, 16);
                var bg = bgGo.GetComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);

                var fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(bgGo.transform, false);
                var fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.anchorMin = new Vector2(0, 0);
                fillRt.anchorMax = new Vector2(0, 1);
                fillRt.pivot = new Vector2(0, 0.5f);
                fillRt.anchoredPosition = Vector2.zero;
                fillRt.sizeDelta = new Vector2(0, 16);
                var fill = fillGo.GetComponent<Image>();
                fill.color = new Color(0.96f, 0.77f, 0.1f, 1f);

                worldLabels.Add(label);
                worldProgress.Add(fillRt);
            }
        }

        for (var i = max; i < slotAnchors.Count; i++)
        {
            nonSlotSceneRenderers.Add(slotAnchors[i].GetComponentsInChildren<Renderer>(true));
        }
    }

    private void OnValidate()
    {
        if (dynamicGlueScaleList == null)
        {
            dynamicGlueScaleList = new List<GlueScaleEntry>();
        }
    }

    private void EnsureFixedRuntimeSlotAnchorsAtConfiguredPositions()
    {
        var root = GameObject.Find("GF_RuntimeMachineSlots");
        if (root == null)
        {
            root = new GameObject("GF_RuntimeMachineSlots");
        }

        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var positions = new[] { slot1WorldPosition, slot2WorldPosition, slot3WorldPosition };
        for (var i = 0; i < positions.Length; i++)
        {
            var name = "GF_RuntimeMachineSlot_" + i;
            var child = root.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                child = go.transform;
            }

            child.SetPositionAndRotation(positions[i], Quaternion.identity);
            child.localScale = Vector3.one;

            if (!slotAnchors.Contains(child))
            {
                slotAnchors.Add(child);
            }
        }
    }

    private void CollectLegacySceneSlotRenderersForHiding()
    {
        var fixedRoot = GameObject.Find("GF_RuntimeMachineSlots");
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (var i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null)
            {
                continue;
            }

            if (fixedRoot != null && t.IsChildOf(fixedRoot.transform))
            {
                continue;
            }

            var isLegacySlot =
                t.name.StartsWith("Auto_Glue_Machine", StringComparison.OrdinalIgnoreCase) ||
                t.name.StartsWith("MachineSlot_", StringComparison.OrdinalIgnoreCase) ||
                t.name == slotObjectName;

            if (!isLegacySlot)
            {
                continue;
            }

            var renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                nonSlotSceneRenderers.Add(renderers);
            }
        }
    }

    private void CollectSlotAnchorsByCommonNames()
    {
        TryAddAnchor("MachineSlot_0");
        TryAddAnchor("MachineSlot_1");
        TryAddAnchor("MachineSlot_2");
        TryAddAnchor("Auto_Glue_Machine1");
        TryAddAnchor("Auto_Glue_Machine2");
        TryAddAnchor("Auto_Glue_Machine3");
    }

    private void TryAddAnchor(string name)
    {
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (var i = 0; i < all.Length; i++)
        {
            if (all[i].name == name && !slotAnchors.Contains(all[i]))
            {
                slotAnchors.Add(all[i]);
            }
        }
    }

    private void Refresh()
    {
        if (game == null)
        {
            return;
        }

        var snap = game.Snapshot();
        UpdateGateProgressionTargets(snap.ConveyorLevel, !gateProgressionInitialized);
        gateProgressionInitialized = true;
        var count = snap.SlotMachineIds.Length;

        for (var i = 0; i < count; i++)
        {
            var unlocked = i <= snap.ConveyorLevel;
            var machine = snap.SlotMachineIds[i];
            var installed = unlocked && machine >= 0;

            UpdateSlotVisual(i, unlocked, machine);

            if (i < slotClickColliders.Count && slotClickColliders[i] != null)
            {
                slotClickColliders[i].enabled = unlocked;
            }

            if (i >= worldLabels.Count || i >= worldProgress.Count)
            {
                continue;
            }

            if (worldLabels[i] != null && worldLabels[i].transform != null && worldLabels[i].transform.parent != null)
            {
                worldLabels[i].transform.parent.gameObject.SetActive(unlocked);
            }

            if (!unlocked)
            {
                worldLabels[i].text = "Slot " + (i + 1) + " LOCKED";
                SetProgressWidth(worldProgress[i], 0f, 200f);
                continue;
            }

            if (!installed)
            {
                worldLabels[i].text = "Slot " + (i + 1) + " EMPTY";
                SetProgressWidth(worldProgress[i], 0f, 200f);
            }
            else
            {
                worldLabels[i].text = game.Config.machines[machine].displayName;
                SetProgressWidth(worldProgress[i], snap.SlotProgress01[i], 200f);
            }
        }

        HideNonSlotSceneModels();
    }

    private void InitializeGateProgressionStates()
    {
        gate2State = BuildGateRuntimeState(
            EnsureGateTransform(gate2Override, "Gate2", gate2ClosedWorldPosition),
            gate2ClosedWorldPosition);
        gate3State = BuildGateRuntimeState(
            EnsureGateTransform(gate3Override, "Gate3", gate3ClosedWorldPosition),
            gate3ClosedWorldPosition);
    }

    private GateRuntimeState BuildGateRuntimeState(Transform gateRoot, Vector3 closedPosition)
    {
        if (gateRoot == null)
        {
            return null;
        }

        return new GateRuntimeState
        {
            Root = gateRoot,
            ClosedPosition = closedPosition,
            ClosedRotation = gateRoot.rotation,
            ClosedLocalScale = gateClosedLocalScale,
            Renderers = gateRoot.GetComponentsInChildren<Renderer>(true),
            Colliders = gateRoot.GetComponentsInChildren<Collider>(true),
            Progress01 = 0f,
            Target01 = 0f
        };
    }

    private static Transform FindGateByName(string gateName)
    {
        if (string.IsNullOrWhiteSpace(gateName))
        {
            return null;
        }

        var gateGo = GameObject.Find(gateName);
        return gateGo != null ? gateGo.transform : null;
    }

    private Transform EnsureGateTransform(Transform gateOverride, string gateName, Vector3 closedPosition)
    {
        if (gateOverride != null)
        {
            gateOverride.gameObject.SetActive(true);
            return gateOverride;
        }

        var existingGate = FindGateByNameIncludingInactive(gateName);
        if (existingGate != null)
        {
            existingGate.gameObject.SetActive(true);
            return existingGate;
        }

        if (gatePrefab != null)
        {
            var spawned = Instantiate(gatePrefab, closedPosition, Quaternion.identity);
            spawned.name = gateName;
            spawned.transform.localScale = gateClosedLocalScale;
            spawned.SetActive(true);
            return spawned.transform;
        }

        var fallbackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallbackGo.name = gateName;
        fallbackGo.transform.SetPositionAndRotation(closedPosition, Quaternion.identity);
        fallbackGo.transform.localScale = new Vector3(1f, 2f, 0.2f);
        fallbackGo.SetActive(true);
        return fallbackGo.transform;
    }

    private static Transform FindGateByNameIncludingInactive(string gateName)
    {
        if (string.IsNullOrWhiteSpace(gateName))
        {
            return null;
        }

        var active = GameObject.Find(gateName);
        if (active != null)
        {
            return active.transform;
        }

        var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null)
            {
                continue;
            }

            if (string.Equals(t.name, gateName, StringComparison.Ordinal))
            {
                return t;
            }
        }

        return null;
    }

    private void UpdateGateProgressionTargets(int conveyorLevel, bool instant)
    {
        var clampedLevel = Mathf.Clamp(conveyorLevel, 0, 2);
        SetGateTargetOpen(gate2State, clampedLevel >= 1, instant);
        SetGateTargetOpen(gate3State, clampedLevel >= 2, instant);
    }

    private void SetGateTargetOpen(GateRuntimeState gateState, bool isOpen, bool instant)
    {
        if (gateState == null)
        {
            return;
        }

        gateState.Target01 = isOpen ? 1f : 0f;
        if (instant)
        {
            gateState.Progress01 = gateState.Target01;
        }

        ApplyGatePose(gateState);
    }

    private void TickGateProgression(float deltaTime)
    {
        TickGateState(gate2State, deltaTime);
        TickGateState(gate3State, deltaTime);
    }

    private void TickGateState(GateRuntimeState gateState, float deltaTime)
    {
        if (gateState == null || gateState.Root == null)
        {
            return;
        }

        if (!Mathf.Approximately(gateState.Progress01, gateState.Target01))
        {
            var step = deltaTime / Mathf.Max(0.05f, gateSlideDuration);
            gateState.Progress01 = Mathf.MoveTowards(gateState.Progress01, gateState.Target01, step);
        }

        ApplyGatePose(gateState);
    }

    private void ApplyGatePose(GateRuntimeState gateState)
    {
        if (gateState == null || gateState.Root == null)
        {
            return;
        }

        var progress = Mathf.Clamp01(gateState.Progress01);
        var upOffset = Vector3.up * Mathf.Max(0f, gateOpenSlideDistance) * progress;
        gateState.Root.SetPositionAndRotation(gateState.ClosedPosition + upOffset, gateState.ClosedRotation);
        gateState.Root.localScale = gateState.ClosedLocalScale;

        var hideProgress = Mathf.Clamp01(gateHideStartProgress01);
        var visible = !(hideGateWhenOpened && progress >= hideProgress);
        if (gateState.Renderers != null)
        {
            for (var i = 0; i < gateState.Renderers.Length; i++)
            {
                if (gateState.Renderers[i] != null)
                {
                    gateState.Renderers[i].enabled = visible;
                }
            }
        }

        if (gateState.Colliders != null)
        {
            for (var i = 0; i < gateState.Colliders.Length; i++)
            {
                if (gateState.Colliders[i] != null)
                {
                    gateState.Colliders[i].enabled = visible;
                }
            }
        }
    }

    private void HandleMachineProduced(int slot, double amount)
    {
        SpawnMovingGlueForMachine(slot, amount);
        Refresh();
    }

    private void HandleManualProduced(double amount)
    {
        if (game == null || amount <= 0d)
        {
            return;
        }

        SpawnManualMovingGlueForUnlockedSlots(amount);
    }

    private void UpdateSlotVisual(int slotIndex, bool unlocked, int machineId)
    {
        if (slotIndex >= slotSceneRenderers.Count)
        {
            return;
        }

        var installed = unlocked && machineId >= 0;
        var hasPrefabVisual = unlocked && EnsureSlotPrefabVisual(slotIndex, machineId);

        if (slotIndex < slotPrefabVisuals.Count && slotPrefabVisuals[slotIndex] != null)
        {
            slotPrefabVisuals[slotIndex].SetActive(unlocked);
            SetAutoMachineDropVisualsActive(slotPrefabVisuals[slotIndex].transform, installed);
        }

        var visibleSceneModel = hasPrefabVisual ? false : (!hideSceneSlotModelsWhenEmpty || installed);
        var renderers = slotSceneRenderers[slotIndex];
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null)
            {
                continue;
            }

            if (r.gameObject.name.StartsWith("GF_", StringComparison.Ordinal))
            {
                continue;
            }

            r.enabled = visibleSceneModel;
        }
    }

    private bool EnsureSlotPrefabVisual(int slotIndex, int machineId)
    {
        if (slotIndex < 0 || slotIndex >= slotAnchors.Count)
        {
            return false;
        }

        if (slotIndex < slotVisualMachineIds.Count && slotVisualMachineIds[slotIndex] == machineId && slotIndex < slotPrefabVisuals.Count && slotPrefabVisuals[slotIndex] != null)
        {
            return true;
        }

        if (slotIndex < slotPrefabVisuals.Count && slotPrefabVisuals[slotIndex] != null)
        {
            Destroy(slotPrefabVisuals[slotIndex]);
            slotPrefabVisuals[slotIndex] = null;
        }

        var sourcePrefab = machineVisualPrefab;

        if (sourcePrefab == null)
        {
            slotVisualMachineIds[slotIndex] = int.MinValue;
            return false;
        }

        var anchor = slotAnchors[slotIndex];
        var go = Instantiate(sourcePrefab, anchor);
        go.name = "GF_MachineVisual_" + slotIndex;
        go.transform.localPosition = machineVisualLocalOffset;
        go.transform.localRotation = Quaternion.Euler(machineVisualLocalEuler);
        go.transform.localScale = DefaultMachineVisualScale;
        NormalizeHeight(go.transform, machineVisualTargetHeight, normalizeMachineVisualHeight);
        if (alignMachineVisualBottomToOffset)
        {
            AlignBottomToAnchorOffset(go.transform, anchor, machineVisualLocalOffset.y);
        }
        go.SetActive(true);

        if (slotIndex < slotAutoSpawnAnchors.Count)
        {
            slotAutoSpawnAnchors[slotIndex] = ResolveAutoMachineSpawnAnchor(go.transform, anchor);
        }
        if (slotIndex < slotConveyorEntryPoints.Count)
        {
            var gate = slotIndex < slotGateTargets.Count ? slotGateTargets[slotIndex] : null;
            var spawn = slotIndex < slotAutoSpawnAnchors.Count && slotAutoSpawnAnchors[slotIndex] != null
                ? slotAutoSpawnAnchors[slotIndex].position + movingGlueSpawnOffset
                : anchor.position + movingGlueSpawnOffset;
            if (TryResolveConveyorPathPoints(go.transform, spawn, gate, out var entryPoint, out var exitPoint))
            {
                slotConveyorEntryPoints[slotIndex] = entryPoint;
                slotConveyorExitPoints[slotIndex] = exitPoint;
                slotHasConveyorPath[slotIndex] = true;
            }
            else
            {
                slotConveyorEntryPoints[slotIndex] = anchor.position;
                slotConveyorExitPoints[slotIndex] = anchor.position;
                slotHasConveyorPath[slotIndex] = false;
            }
        }

        slotPrefabVisuals[slotIndex] = go;
        slotVisualMachineIds[slotIndex] = machineId;
        return true;
    }

    private void HideNonSlotSceneModels()
    {
        for (var i = 0; i < nonSlotSceneRenderers.Count; i++)
        {
            var renderers = nonSlotSceneRenderers[i];
            for (var j = 0; j < renderers.Length; j++)
            {
                var r = renderers[j];
                if (r == null)
                {
                    continue;
                }

                if (r.gameObject.name.StartsWith("GF_", StringComparison.Ordinal))
                {
                    continue;
                }

                r.enabled = false;
            }
        }
    }

    private GameObject ResolveMachinePrefab(int machineId)
    {
        if (game == null || game.Config == null || game.Config.machines == null)
        {
            return null;
        }

        if (machineId < 0 || machineId >= game.Config.machines.Count)
        {
            return null;
        }

        var machine = game.Config.machines[machineId];
        var key = string.IsNullOrWhiteSpace(machine.id) ? machine.displayName : machine.id;
        if (machinePrefabCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var prefabName = GetMachinePrefabName(machine.id, machine.displayName);
        GameObject prefab = null;

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(prefabName))
        {
            var path = "Assets/Auto Glues/" + prefabName + ".prefab";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif

        if (prefab == null && !string.IsNullOrWhiteSpace(prefabName))
        {
            prefab = LoadRuntimePrefab(prefabName);
        }

        machinePrefabCache[key] = prefab;
        return prefab;
    }

    private static string GetMachinePrefabName(string machineId, string displayName)
    {
        var raw = (machineId + " " + displayName).ToLowerInvariant().Replace("-", "_").Replace(" ", "_");

        if (raw.Contains("epoxy_resin") || raw.Contains("bucket")) return "Epoxy_Resin_Bucket_Glue";
        if (raw.Contains("glue_stick") || raw.Contains("stick")) return "Glue_Stick";
        if (raw.Contains("white")) return "White_Glue";
        if (raw.Contains("wood")) return "Wood_Glue";
        if (raw.Contains("super")) return "Super_Glue";
        if (raw.Contains("plastic") || raw.Contains("resin")) return "Plastic_Resin_Glue";
        if (raw.Contains("e6000")) return "E6000_Glue";
        if (raw.Contains("poly")) return "Poly_Glue";
        if (raw.Contains("construction")) return "Construction_Glue";
        if (raw.Contains("aerospace")) return "Aerospace_Glue";
        if (raw.Contains("edible")) return "Edible_Multi_Purpose_Glue";
        if (raw.Contains("military") || raw.Contains("defense")) return "Military_Defense_Glue";
        if (raw.Contains("space")) return "Space_Glue";
        if (raw.Contains("holy")) return "Holy_Glue";
        if (raw.Contains("epoxy")) return "Epoxy_Glue";

        return string.Empty;
    }

    private static GameObject LoadMachineSetPrefabFallback()
    {
#if UNITY_EDITOR
        var fromAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/MachineSet.prefab");
        if (fromAsset != null)
        {
            return fromAsset;
        }
#endif
        return LoadRuntimePrefab("MachineSet");
    }

    private static GameObject LoadGatePrefabFallback()
    {
#if UNITY_EDITOR
        var fromAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Gate.prefab");
        if (fromAsset != null)
        {
            return fromAsset;
        }
#endif
        return LoadRuntimePrefab("Gate");
    }

    private static GameObject LoadRuntimePrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return null;
        }

        // Try legacy direct resource key first to keep compatibility with existing scenes.
        var prefab = Resources.Load<GameObject>(prefabName);
        if (prefab != null)
        {
            return prefab;
        }

        // Build-safe fallback locations used by this project.
        prefab = Resources.Load<GameObject>("GlueFactoryPrefabs/" + prefabName);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>("GlueFactoryPrefabs/AutoGlues/" + prefabName);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>("AutoGlues/" + prefabName);
        if (prefab != null)
        {
            return prefab;
        }

        return Resources.Load<GameObject>("Auto Glues/" + prefabName);
    }

    private float ResolveAutoLaneExitZ(int slotIndex, Transform slotGate)
    {
        if (slotIndex >= 0 && slotIndex < manualSellExitWorldBySlot.Length)
        {
            return manualSellExitWorldBySlot[slotIndex].z;
        }

        if (slotGate != null)
        {
            return slotGate.position.z;
        }

        return manualStopPointWorld.z;
    }

    private Vector3 ResolveAutoSpawnPoint(int slotIndex, Transform fallbackAnchor)
    {
        if (slotIndex >= 0 && slotIndex < autoSpawnWorldBySlot.Length)
        {
            return autoSpawnWorldBySlot[slotIndex];
        }

        return fallbackAnchor != null ? fallbackAnchor.position + movingGlueSpawnOffset : Vector3.zero;
    }

    private static void SetAutoMachineDropVisualsActive(Transform root, bool isActive)
    {
        if (root == null)
        {
            return;
        }

        var all = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null)
            {
                continue;
            }

            var n = t.name;
            if (n.IndexOf("Auto_Glue_Machine", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("AutoGlueMachine", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Drop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Dropper", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                t.gameObject.SetActive(isActive);
            }
        }
    }

    private void SpawnMovingGlueForMachine(int slot, double amount)
    {
        if (game == null)
        {
            return;
        }

        if (slot < 0 || slot >= slotAnchors.Count)
        {
            return;
        }

        var snap = game.Snapshot();
        var machineId = (slot >= 0 && slot < snap.SlotMachineIds.Length) ? snap.SlotMachineIds[slot] : -1;
        var slotGate = slot < slotGateTargets.Count ? slotGateTargets[slot] : null;
        var spawnAnchor = slot < slotAutoSpawnAnchors.Count && slotAutoSpawnAnchors[slot] != null
            ? slotAutoSpawnAnchors[slot]
            : slotAnchors[slot];
        if (autoMachineUseSellLaneRoute)
        {
            var start = ResolveAutoSpawnPoint(slot, spawnAnchor);
            var laneZ = ResolveAutoLaneExitZ(slot, slotGate);
            var end = new Vector3(autoSellLaneWorldX, autoTravelWorldY, laneZ);
            // Enforce vertical drop first to Y=1.8, then continue toward sell lane X=-8.
            var dropPoint = new Vector3(start.x, autoTravelWorldY, start.z);
            var alignLaneZPoint = new Vector3(start.x, autoTravelWorldY, laneZ);
            SpawnMovingGlueFromPoints(start, end, machineId, amount, false, true, slotGate, dropPoint, alignLaneZPoint);
            return;
        }

        var hasConveyorPath = slot < slotHasConveyorPath.Count && slotHasConveyorPath[slot];
        if (hasConveyorPath)
        {
            var path = new[]
            {
                slotConveyorEntryPoints[slot],
                slotConveyorExitPoints[slot]
            };
            SpawnMovingGlueFromAnchor(spawnAnchor, machineId, amount, false, true, slotGate, null, path);
            return;
        }

        SpawnMovingGlueFromAnchor(spawnAnchor, machineId, amount, false, true, slotGate);
    }

    private void SpawnManualMovingGlueForUnlockedSlots(double amount)
    {
        if (!manualClickUsesAllUnlockedSlots || game == null)
        {
            SpawnManualMovingGlue(amount);
            return;
        }

        var snap = game.Snapshot();
        var unlockedCount = Mathf.Clamp(snap.ConveyorLevel + 1, 0, slotAnchors.Count);
        if (unlockedCount <= 1)
        {
            SpawnManualMovingGlue(amount);
            return;
        }

        var amountPerSlot = amount / unlockedCount;
        if (amountPerSlot <= 0d)
        {
            return;
        }

        for (var slot = 0; slot < unlockedCount; slot++)
        {
            SpawnManualMovingGlueFromSlot(slot, amountPerSlot);
        }
    }

    private void SpawnManualMovingGlue(double amount)
    {
        var manualSlot = ResolveManualSlotIndex();
        SpawnManualMovingGlueFromSlot(manualSlot, amount);
    }

    private void SpawnManualMovingGlueFromSlot(int manualSlot, double amount)
    {
        var anchor = ResolveManualAnchor(manualSlot);
        var machineId = ResolveSlotMachineId(manualSlot);

        if (anchor == null)
        {
            return;
        }

        var manualGate = ResolveManualGate(manualSlot);
        if (TryGetPerSlotManualRoute(manualSlot, out var routeStart, out var routeEnd))
        {
            SpawnMovingGlueFromPoints(routeStart, routeEnd, machineId, amount, true, true, manualGate);
            return;
        }

        var start = anchor.position + movingGlueSpawnOffset;
        if (TryResolveConveyorPathPoints(anchor, start, manualGate, out var entry, out var exit))
        {
            var raisedY = manualStopPointWorld.y;
            entry.y = raisedY;
            exit.y = raisedY;
            SpawnMovingGlueFromPoints(start, manualGate != null ? manualGate.position + movingGlueGateOffset : start, machineId, amount, true, true, manualGate, entry, exit);
            return;
        }

        // Fallback: keep manual pieces axis-aligned instead of diagonal.
        var end = manualGate != null
            ? manualGate.position + movingGlueGateOffset
            : anchor.position + anchor.forward * 3f + new Vector3(0f, movingGlueGateOffset.y, 0f);
        var raisedYFallback = manualStopPointWorld.y;
        var axisMid = new Vector3(start.x, raisedYFallback, end.z);
        SpawnMovingGlueFromPoints(start, end, machineId, amount, true, true, manualGate, axisMid);
    }

    private int ResolveManualSlotIndex()
    {
        if (game == null)
        {
            return 0;
        }

        var snap = game.Snapshot();
        var selected = Mathf.Clamp(snap.SelectedSlot, 0, Mathf.Max(0, slotAnchors.Count - 1));
        if (selected <= snap.ConveyorLevel)
        {
            return selected;
        }

        return 0;
    }

    private Transform ResolveManualAnchor(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotAnchors.Count && slotAnchors[slotIndex] != null)
        {
            return slotAnchors[slotIndex];
        }

        if (slotIndex >= 0 && slotIndex < slotAutoSpawnAnchors.Count && slotAutoSpawnAnchors[slotIndex] != null)
        {
            return slotAutoSpawnAnchors[slotIndex];
        }

        if (manualSpawnAnchor != null)
        {
            return manualSpawnAnchor;
        }

        return slotAnchors.Count > 0 ? slotAnchors[0] : null;
    }

    private Transform ResolveManualGate(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotGateTargets.Count && slotGateTargets[slotIndex] != null)
        {
            return slotGateTargets[slotIndex];
        }

        var autoSlotGate = ResolveAutoSlotGateTarget(slotIndex);
        if (autoSlotGate != null)
        {
            return autoSlotGate;
        }

        return sellGateTarget;
    }

    private bool TryGetPerSlotManualRoute(int slotIndex, out Vector3 start, out Vector3 end)
    {
        start = default;
        end = default;
        if (!usePerSlotManualRoutePoints)
        {
            return false;
        }

        if (!TryGetSlotVector(manualSpawnWorldBySlot, slotIndex, out start))
        {
            return false;
        }

        if (!TryGetSlotVector(manualSellExitWorldBySlot, slotIndex, out end))
        {
            return false;
        }

        start.y = manualStopPointWorld.y;
        end.y = manualStopPointWorld.y;
        return true;
    }

    private static bool TryGetSlotVector(Vector3[] points, int index, out Vector3 point)
    {
        point = default;
        if (points == null || index < 0 || index >= points.Length)
        {
            return false;
        }

        point = points[index];
        return true;
    }

    private int ResolveSlotMachineId(int slotIndex)
    {
        if (game == null || slotIndex < 0)
        {
            return -1;
        }

        var snap = game.Snapshot();
        if (snap.SlotMachineIds == null || slotIndex >= snap.SlotMachineIds.Length)
        {
            return -1;
        }

        return snap.SlotMachineIds[slotIndex];
    }

    private void SpawnMovingGlueFromAnchor(Transform anchor, int machineId, double amount, bool isManual, bool collectOnArrival, Transform targetGate, Vector3? forcedEndWorld = null, params Vector3[] intermediatePoints)
    {
        if (anchor == null)
        {
            return;
        }

        var start = anchor.position + movingGlueSpawnOffset;
        if (isManual)
        {
            start.y = manualStopPointWorld.y;
        }
        var end = forcedEndWorld ?? (targetGate != null
            ? targetGate.position + movingGlueGateOffset
            : anchor.position + anchor.forward * 3f + new Vector3(0f, movingGlueGateOffset.y, 0f));
        if (isManual)
        {
            end.y = forcedEndWorld.HasValue ? forcedEndWorld.Value.y : manualStopPointWorld.y;
        }

        SpawnMovingGlueFromPoints(start, end, machineId, amount, isManual, collectOnArrival, targetGate, intermediatePoints);
    }

    private void SpawnMovingGlueFromPoints(Vector3 start, Vector3 end, int machineId, double amount, bool isManual, bool collectOnArrival, Transform targetGate, params Vector3[] intermediatePoints)
    {
        if (isManual)
        {
            start.y = manualStopPointWorld.y;
            end.y = manualStopPointWorld.y;
        }

        GameObject prefab;
        if (isManual)
        {
            prefab = playerGluePrefab;
        }
        else
        {
            prefab = ResolveMachinePrefab(machineId);
            if (prefab == null)
            {
                prefab = machineVisualPrefab;
            }
        }

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, start, Quaternion.identity);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = start;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = null;
                renderer.material.color = isManual ? new Color(0.95f, 0.77f, 0.1f, 1f) : new Color(0.75f, 0.85f, 1f, 1f);
            }
        }

        go.name = isManual ? "GF_MovingGlue_Manual" : "GF_MovingGlue_Auto";
        go.transform.localScale = ResolveMovingGlueScale(machineId, amount, isManual);
        NormalizeHeight(go.transform, movingGlueTargetHeight, normalizeMovingGlueHeight);

        var verticalLift = CalculatePivotToBottomOffset(go.transform) + Mathf.Max(0f, movingGlueBottomClearance);
        var liftedStart = new Vector3(start.x, start.y + verticalLift, start.z);
        var liftedEnd = new Vector3(end.x, end.y + verticalLift, end.z);
        go.transform.position = liftedStart;

        var colliders = go.GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        var useForcedStopPoint = false;

        movingPieces.Add(new MovingGluePiece
        {
            Instance = go,
            Start = liftedStart,
            End = liftedEnd,
            Duration = Mathf.Max(0.2f, movingGlueTravelSeconds),
            Elapsed = 0f,
            Amount = amount,
            CollectOnArrival = collectOnArrival,
            IsManual = isManual,
            PathPoints = BuildLiftedPathPoints(start, end, intermediatePoints, verticalLift),
            TargetCollider = useForcedStopPoint ? null : (targetGate != null ? targetGate.GetComponentInChildren<Collider>() : null),
            StopDistanceXZ = isManual ? 0.05f : 0.12f,
            UseForcedStopPoint = useForcedStopPoint
        });
    }

    private Vector3 ResolveMovingGlueScale(int machineId, double amount, bool isManual)
    {
        if (isManual)
        {
            return SanitizeScale(playerGlueVisualScale, movingGlueVisualScale);
        }

        var baseScale = movingGlueVisualScale;
        if (!dynamicMovingGlueScale)
        {
            return baseScale;
        }

        if (TryResolveMachineScaleFromConfig(machineId, out var configScale))
        {
            return configScale;
        }

        if (TryResolveDynamicScaleFromList(machineId, out var listedScale))
        {
            return listedScale;
        }

        var minMul = Mathf.Max(0.01f, Mathf.Min(dynamicGlueScaleMultiplierRange.x, dynamicGlueScaleMultiplierRange.y));
        var maxMul = Mathf.Max(minMul, Mathf.Max(dynamicGlueScaleMultiplierRange.x, dynamicGlueScaleMultiplierRange.y));
        var t = ComputeGlueValueProgress01(machineId, amount);
        var multiplier = Mathf.Lerp(minMul, maxMul, t);

        var jitter = Mathf.Clamp01(dynamicGlueScaleJitter);
        if (jitter > 0f)
        {
            var jitterMin = Mathf.Max(0.1f, 1f - jitter);
            multiplier *= UnityEngine.Random.Range(jitterMin, 1f + jitter);
        }

        return baseScale * multiplier;
    }

    private bool TryResolveMachineScaleFromConfig(int machineId, out Vector3 scale)
    {
        scale = movingGlueVisualScale;
        if (game == null || game.Config == null || game.Config.machines == null || machineId < 0 || machineId >= game.Config.machines.Count)
        {
            return false;
        }

        var machine = game.Config.machines[machineId];
        if (machine == null)
        {
            return false;
        }

        scale = SanitizeScale(machine.movingGlueScale, movingGlueVisualScale);
        return true;
    }

    [ContextMenu("Sync Dynamic Glue Scale List")]
    private void SyncDynamicGlueScaleList()
    {
        EnsureDynamicGlueScaleList();
    }

    private void EnsureDynamicGlueScaleList()
    {
        if (dynamicGlueScaleList == null)
        {
            dynamicGlueScaleList = new List<GlueScaleEntry>();
        }

        var cfg = game != null ? game.Config : null;
        if (cfg == null)
        {
            cfg = Resources.Load<GlueFactoryBalanceConfig>("GlueFactoryBalance");
        }
        if (cfg == null || cfg.machines == null)
        {
            return;
        }

        var existing = new Dictionary<string, GlueScaleEntry>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < dynamicGlueScaleList.Count; i++)
        {
            var item = dynamicGlueScaleList[i];
            if (item == null || string.IsNullOrWhiteSpace(item.machineId))
            {
                continue;
            }

            var key = item.machineId.Trim();
            if (!existing.ContainsKey(key))
            {
                existing.Add(key, item);
            }
        }

        var rebuilt = new List<GlueScaleEntry>(cfg.machines.Count);
        for (var i = 0; i < cfg.machines.Count; i++)
        {
            var machine = cfg.machines[i];
            var id = NormalizeMachineId(machine != null ? machine.id : null, i);
            var prefabName = GetMachinePrefabName(machine != null ? machine.id : string.Empty, machine != null ? machine.displayName : string.Empty);
            if (!existing.TryGetValue(id, out var entry) || entry == null)
            {
                entry = new GlueScaleEntry();
            }

            entry.machineId = id;
            entry.prefabName = prefabName;
            entry.localScale = machine != null ? SanitizeScale(machine.movingGlueScale, movingGlueVisualScale) : movingGlueVisualScale;
            entry.enabled = true;
            rebuilt.Add(entry);
        }

        dynamicGlueScaleList = rebuilt;
    }

    private bool TryResolveDynamicScaleFromList(int machineId, out Vector3 scale)
    {
        scale = movingGlueVisualScale;
        if (!useDynamicGlueScaleList || machineId < 0 || game == null || game.Config == null || game.Config.machines == null)
        {
            return false;
        }

        if (machineId >= game.Config.machines.Count)
        {
            return false;
        }

        var machine = game.Config.machines[machineId];
        var id = NormalizeMachineId(machine != null ? machine.id : null, machineId);
        var entry = FindDynamicScaleEntry(id);
        if (entry == null || !entry.enabled)
        {
            return false;
        }

        scale = SanitizeScale(entry.localScale, movingGlueVisualScale);
        return true;
    }

    private GlueScaleEntry FindDynamicScaleEntry(string machineId)
    {
        if (dynamicGlueScaleList == null || dynamicGlueScaleList.Count == 0 || string.IsNullOrWhiteSpace(machineId))
        {
            return null;
        }

        for (var i = 0; i < dynamicGlueScaleList.Count; i++)
        {
            var entry = dynamicGlueScaleList[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.machineId))
            {
                continue;
            }

            if (string.Equals(entry.machineId.Trim(), machineId, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    private static string NormalizeMachineId(string id, int index)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "machine_" + index;
        }

        return id.Trim();
    }

    private static Vector3 SanitizeScale(Vector3 scale, Vector3 fallback)
    {
        var fx = Mathf.Max(0.0001f, Mathf.Abs(fallback.x));
        var fy = Mathf.Max(0.0001f, Mathf.Abs(fallback.y));
        var fz = Mathf.Max(0.0001f, Mathf.Abs(fallback.z));
        var sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        var sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        var sz = Mathf.Max(0.0001f, Mathf.Abs(scale.z));

        return new Vector3(
            float.IsNaN(sx) || float.IsInfinity(sx) ? fx : sx,
            float.IsNaN(sy) || float.IsInfinity(sy) ? fy : sy,
            float.IsNaN(sz) || float.IsInfinity(sz) ? fz : sz);
    }

    private float ComputeGlueValueProgress01(int machineId, double amount)
    {
        if (game == null || game.Config == null || game.Config.machines == null || game.Config.machines.Count == 0)
        {
            return 0f;
        }

        var minValue = double.MaxValue;
        var maxValue = double.MinValue;
        for (var i = 0; i < game.Config.machines.Count; i++)
        {
            var v = Math.Max(0d, game.Config.machines[i].pieceValue);
            if (v < minValue)
            {
                minValue = v;
            }

            if (v > maxValue)
            {
                maxValue = v;
            }
        }

        if (minValue == double.MaxValue || maxValue <= minValue)
        {
            return 0f;
        }

        var value = amount;
        if (machineId >= 0 && machineId < game.Config.machines.Count)
        {
            value = Math.Max(0d, game.Config.machines[machineId].pieceValue);
        }

        var safeMin = Math.Max(0.0001d, minValue);
        var safeMax = Math.Max(safeMin + 0.0001d, maxValue);
        var safeValue = Math.Max(safeMin, value);

        var logMin = Math.Log10(safeMin);
        var logMax = Math.Log10(safeMax);
        var logValue = Math.Log10(safeValue);
        var p = (logValue - logMin) / (logMax - logMin);
        return Mathf.Clamp01((float)p);
    }

    private void TickMovingPieces(float deltaTime)
    {
        if (movingPieces.Count == 0)
        {
            return;
        }

        for (var i = movingPieces.Count - 1; i >= 0; i--)
        {
            var piece = movingPieces[i];
            if (piece == null || piece.Instance == null)
            {
                movingPieces.RemoveAt(i);
                continue;
            }

            piece.Elapsed += deltaTime;
            var t = Mathf.Clamp01(piece.Elapsed / piece.Duration);
            var p = EvaluatePathPosition(piece.PathPoints, piece.Start, piece.End, t);
            piece.Instance.transform.position = p;

            var targetCollider = piece.UseForcedStopPoint
                ? null
                : (piece.TargetCollider != null ? piece.TargetCollider : sellGateCollider);
            var reachedSellCollider = targetCollider != null && targetCollider.bounds.Contains(piece.Instance.transform.position);
            var reachedAnyGate = false;
            var reachedByDistance = false;
            if (piece.IsManual)
            {
                if (piece.UseForcedStopPoint)
                {
                    var horizontal = new Vector2(piece.Instance.transform.position.x - piece.End.x, piece.Instance.transform.position.z - piece.End.z);
                    reachedByDistance = horizontal.magnitude <= Mathf.Max(0.05f, piece.StopDistanceXZ);
                }
                else
                {
                    for (var g = 0; g < allGateColliders.Count; g++)
                    {
                        var c = allGateColliders[g];
                        if (c != null && c.bounds.Contains(piece.Instance.transform.position))
                        {
                            reachedAnyGate = true;
                            break;
                        }
                    }
                }
            }
            if (reachedByDistance || reachedAnyGate || reachedSellCollider || t >= 1f)
            {
                if (game != null && piece.CollectOnArrival)
                {
                    if (piece.IsManual)
                    {
                        game.CollectManualProduced(piece.Amount);
                    }
                    else
                    {
                        game.CollectAutoProduced(piece.Amount);
                    }
                }
                Destroy(piece.Instance);
                movingPieces.RemoveAt(i);
            }
        }
    }

    private void ClearMovingPieces()
    {
        for (var i = movingPieces.Count - 1; i >= 0; i--)
        {
            var piece = movingPieces[i];
            if (piece != null && piece.Instance != null)
            {
                Destroy(piece.Instance);
            }
        }

        movingPieces.Clear();
    }

    private Transform ResolveSellGateTarget()
    {
        if (sellGateOverride != null)
        {
            return sellGateOverride;
        }

        for (var i = 0; i < sellGateNameCandidates.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sellGateNameCandidates[i]))
            {
                continue;
            }

            var gateGo = GameObject.Find(sellGateNameCandidates[i]);
            if (gateGo != null)
            {
                return gateGo.transform;
            }
        }

        var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (var i = 0; i < all.Length; i++)
        {
            if (all[i].name.StartsWith("Gate", StringComparison.OrdinalIgnoreCase) && all[i].name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return all[i];
            }
        }

        return null;
    }

    private Transform ResolveManualSellGateTarget()
    {
        if (manualSellGateOverride != null)
        {
            return manualSellGateOverride;
        }

        var candidates = new List<Transform>();

        for (var i = 0; i < manualSellGateNameCandidates.Length; i++)
        {
            var candidate = manualSellGateNameCandidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var gateGo = GameObject.Find(candidate);
            if (gateGo != null)
            {
                candidates.Add(gateGo.transform);
            }
        }

        if (candidates.Count == 0)
        {
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t.name.StartsWith("Gate", StringComparison.OrdinalIgnoreCase) &&
                    t.name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    candidates.Add(t);
                }
            }
        }

        if (candidates.Count == 0)
        {
            return sellGateTarget;
        }

        if (manualSpawnAnchor == null)
        {
            return candidates[0];
        }

        var best = candidates[0];
        var bestDistance = Vector3.Distance(manualSpawnAnchor.position, best.position);
        for (var i = 1; i < candidates.Count; i++)
        {
            var d = Vector3.Distance(manualSpawnAnchor.position, candidates[i].position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = candidates[i];
            }
        }

        return best;
    }

    private void CacheAllGateColliders()
    {
        allGateColliders.Clear();

        var seen = new HashSet<Collider>();
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (var i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null)
            {
                continue;
            }

            if (!t.name.StartsWith("Gate", StringComparison.OrdinalIgnoreCase) ||
                t.name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            var colliders = t.GetComponentsInChildren<Collider>(true);
            for (var j = 0; j < colliders.Length; j++)
            {
                var c = colliders[j];
                if (c == null || !seen.Add(c))
                {
                    continue;
                }

                allGateColliders.Add(c);
            }
        }
    }

    private Transform ResolveAutoSlotGateTarget(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < autoSlotGateOverrides.Count && autoSlotGateOverrides[slotIndex] != null)
        {
            return autoSlotGateOverrides[slotIndex];
        }

        if (slotIndex >= 0 && slotIndex < autoSlotGateNameBySlot.Length)
        {
            var gateName = autoSlotGateNameBySlot[slotIndex];
            if (!string.IsNullOrWhiteSpace(gateName))
            {
                var gateGo = GameObject.Find(gateName);
                if (gateGo != null)
                {
                    return gateGo.transform;
                }
            }
        }

        return sellGateTarget;
    }

    private static void RemoveLegacyRebuiltConveyorBelt()
    {
        var rootBelt = GameObject.Find("ConveyorBelt");
        if (rootBelt != null)
        {
            Destroy(rootBelt);
        }

        var managers = GameObject.Find("GlueFactoryManagers");
        if (managers == null)
        {
            return;
        }

        for (var i = managers.transform.childCount - 1; i >= 0; i--)
        {
            var child = managers.transform.GetChild(i);
            if (!string.Equals(child.name, "ConveyorBelt", StringComparison.Ordinal))
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private static Transform ResolveAutoMachineSpawnAnchor(Transform machineVisualRoot, Transform fallback)
    {
        if (machineVisualRoot == null)
        {
            return fallback;
        }

        var candidates = machineVisualRoot.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < candidates.Length; i++)
        {
            var t = candidates[i];
            if (t == null)
            {
                continue;
            }

            if (string.Equals(t.name, "Auto_Glue_Machine", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.name, "AutoGlueMachine", StringComparison.OrdinalIgnoreCase) ||
                t.name.IndexOf("Drop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return t;
            }
        }

        return fallback;
    }

    private static bool TryResolveConveyorPathPoints(Transform machineVisualRoot, Vector3 spawn, Transform gate, out Vector3 entryPoint, out Vector3 exitPoint)
    {
        entryPoint = default;
        exitPoint = default;
        if (machineVisualRoot == null)
        {
            return false;
        }

        var conveyors = machineVisualRoot.GetComponentsInChildren<Transform>(true);
        var found = false;
        var bounds = default(Bounds);
        for (var i = 0; i < conveyors.Length; i++)
        {
            var t = conveyors[i];
            if (t == null)
            {
                continue;
            }

            var name = t.name;
            if (name.IndexOf("convenyor", StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("conveyor", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var renderers = t.GetComponentsInChildren<Renderer>(true);
            for (var j = 0; j < renderers.Length; j++)
            {
                var r = renderers[j];
                if (r == null)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }

        if (!found)
        {
            return false;
        }

        var gatePos = gate != null ? gate.position : bounds.center;
        var center = bounds.center;
        var size = bounds.size;

        // Build a conveyor line using the dominant horizontal axis.
        var useX = size.x >= size.z;
        var half = useX ? size.x * 0.5f : size.z * 0.5f;
        if (half <= 0.0001f)
        {
            return false;
        }

        Vector3 a;
        Vector3 b;
        if (useX)
        {
            a = new Vector3(center.x - half, center.y, center.z);
            b = new Vector3(center.x + half, center.y, center.z);
        }
        else
        {
            a = new Vector3(center.x, center.y, center.z - half);
            b = new Vector3(center.x, center.y, center.z + half);
        }

        // Entry: axis-joined point onto the conveyor line to avoid diagonal jump.
        if (useX)
        {
            // Conveyor runs along X; join by moving on Z first.
            var minX = Mathf.Min(a.x, b.x);
            var maxX = Mathf.Max(a.x, b.x);
            var joinX = Mathf.Clamp(spawn.x, minX, maxX);
            entryPoint = new Vector3(joinX, center.y, center.z);
        }
        else
        {
            // Conveyor runs along Z; join by moving on X first.
            var minZ = Mathf.Min(a.z, b.z);
            var maxZ = Mathf.Max(a.z, b.z);
            var joinZ = Mathf.Clamp(spawn.z, minZ, maxZ);
            entryPoint = new Vector3(center.x, center.y, joinZ);
        }
        // Exit: conveyor end that is closest to gate.
        exitPoint = Vector3.Distance(a, gatePos) <= Vector3.Distance(b, gatePos) ? a : b;
        return true;
    }

    private static Vector3[] BuildPathPoints(Vector3 start, Vector3 end, Vector3[] intermediate)
    {
        if (intermediate == null || intermediate.Length == 0)
        {
            return new[] { start, end };
        }

        var points = new List<Vector3>(2 + intermediate.Length) { start };
        for (var i = 0; i < intermediate.Length; i++)
        {
            if (Vector3.Distance(points[points.Count - 1], intermediate[i]) > 0.001f)
            {
                points.Add(intermediate[i]);
            }
        }
        if (Vector3.Distance(points[points.Count - 1], end) > 0.001f)
        {
            points.Add(end);
        }
        else if (points.Count == 1)
        {
            points.Add(end);
        }
        return points.ToArray();
    }

    private static Vector3[] BuildLiftedPathPoints(Vector3 start, Vector3 end, Vector3[] intermediate, float liftY)
    {
        var points = BuildPathPoints(start, end, intermediate);
        if (Mathf.Abs(liftY) <= 0.0001f || points == null || points.Length == 0)
        {
            return points;
        }

        var adjusted = new Vector3[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            adjusted[i] = new Vector3(points[i].x, points[i].y + liftY, points[i].z);
        }

        return adjusted;
    }

    private static Vector3 EvaluatePathPosition(Vector3[] points, Vector3 start, Vector3 end, float t01)
    {
        if (points == null || points.Length < 2)
        {
            return Vector3.Lerp(start, end, t01);
        }

        var total = 0f;
        for (var i = 1; i < points.Length; i++)
        {
            total += Vector3.Distance(points[i - 1], points[i]);
        }
        if (total <= 0.0001f)
        {
            return points[points.Length - 1];
        }

        var distance = Mathf.Clamp01(t01) * total;
        var walked = 0f;
        for (var i = 1; i < points.Length; i++)
        {
            var from = points[i - 1];
            var to = points[i];
            var seg = Vector3.Distance(from, to);
            if (seg <= 0.0001f)
            {
                continue;
            }

            if (walked + seg >= distance)
            {
                var local = (distance - walked) / seg;
                return Vector3.Lerp(from, to, local);
            }

            walked += seg;
        }

        return points[points.Length - 1];
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        var ab = b - a;
        var denom = Vector3.Dot(ab, ab);
        if (denom <= 0.0001f)
        {
            return a;
        }

        var t = Vector3.Dot(p - a, ab) / denom;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    private Transform ResolveManualSpawnAnchor()
    {
        if (manualSpawnAnchorOverride != null)
        {
            return manualSpawnAnchorOverride;
        }

        if (manualSpawnAnchorNameCandidates == null || manualSpawnAnchorNameCandidates.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < manualSpawnAnchorNameCandidates.Length; i++)
        {
            var candidate = manualSpawnAnchorNameCandidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var go = GameObject.Find(candidate);
            if (go != null)
            {
                return go.transform;
            }
        }

        return null;
    }

    private void EnsureFallbackSlotAnchorsAtKnownPositions()
    {
        if (slotAnchors.Count >= 3)
        {
            return;
        }

        var fallbackRoot = GameObject.Find("GF_FallbackMachineSlots");
        if (fallbackRoot == null)
        {
            fallbackRoot = new GameObject("GF_FallbackMachineSlots");
        }

        var positions = new[]
        {
            new Vector3(3f, 0f, 6f),
            new Vector3(3f, 0f, -0.44020343f),
            new Vector3(3f, 0f, -7f)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var name = "GF_FallbackMachineSlot_" + i;
            var child = fallbackRoot.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(fallbackRoot.transform, false);
                go.transform.position = positions[i];
                child = go.transform;
            }
            else
            {
                child.position = positions[i];
            }

            if (!slotAnchors.Contains(child))
            {
                slotAnchors.Add(child);
            }
        }
    }

    private static void CleanupRuntimeObjects(Transform anchor)
    {
        for (var i = anchor.childCount - 1; i >= 0; i--)
        {
            var child = anchor.GetChild(i);
            if (child.name.StartsWith("GF_", StringComparison.Ordinal))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void TrySelectSlotByClick()
    {
        if (!IsPrimaryPointerDownThisFrame())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        var pointerPosition = PrimaryPointerPosition();
        var ray = worldCamera.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out var hit, 500f))
        {
            return;
        }

        if (colliderToSlot.TryGetValue(hit.collider, out var slot))
        {
            game.SelectSlot(slot);
        }
    }

    private static void SetProgressWidth(RectTransform rt, float progress01, float maxWidth)
    {
        if (rt == null)
        {
            return;
        }

        rt.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(progress01), rt.sizeDelta.y);
    }

    private static void NormalizeHeight(Transform target, float desiredHeight, bool enabled)
    {
        if (!enabled || target == null || desiredHeight <= 0f)
        {
            return;
        }

        if (!TryGetRendererBounds(target.gameObject, out var bounds))
        {
            return;
        }

        var currentHeight = bounds.size.y;
        if (currentHeight <= 0.0001f)
        {
            return;
        }

        var scaleFactor = desiredHeight / currentHeight;
        target.localScale *= scaleFactor;
    }

    private static float CalculatePivotToBottomOffset(Transform target)
    {
        if (target == null || !TryGetRendererBounds(target.gameObject, out var bounds))
        {
            return 0f;
        }

        return target.position.y - bounds.min.y;
    }

    private static void AlignBottomToAnchorOffset(Transform visual, Transform anchor, float localOffsetY)
    {
        if (visual == null || anchor == null)
        {
            return;
        }

        if (!TryGetRendererBounds(visual.gameObject, out var bounds))
        {
            return;
        }

        var desiredWorldY = anchor.position.y + localOffsetY;
        var delta = desiredWorldY - bounds.min.y;
        visual.position += new Vector3(0f, delta, 0f);
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var found = false;
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    private static bool IsPrimaryPointerDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static Vector2 PrimaryPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            return mouse.position.ReadValue();
        }

        var touch = Touchscreen.current;
        if (touch != null)
        {
            return touch.primaryTouch.position.ReadValue();
        }

        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
