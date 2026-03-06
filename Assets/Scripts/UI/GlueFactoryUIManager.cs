using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GlueFactoryUIManager : MonoBehaviour
{
    [SerializeField] private GlueFactoryGameManager game;
    private bool built;

    private Text moneyText;
    private Text perClickText;
    private Text autoIncomeText;
    private Text totalText;
    private Text selectedMachineText;
    private Text selectedSlotText;
    private Text toastText;
    private Image toastBackground;

    private RectTransform upgradesTab;
    private RectTransform machinesTab;
    private readonly List<Text> slotTexts = new List<Text>();
    private readonly List<Image> slotBoxImages = new List<Image>();
    private readonly List<RectTransform> slotProgressFills = new List<RectTransform>();
    private readonly List<Button> slotActionButtons = new List<Button>();

    private Text clickCardInfo;
    private Text conveyorCardInfo;
    private Text boostCardInfo;
    private Text speedCardInfo;

    private bool toastVisible;
    private float toastTimer;
    private RectTransform factoryClickZone;

    private enum ToastType
    {
        Info,
        Warning,
        Success,
        Error
    }

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void Update()
    {
        if (game != null &&
            IsPrimaryPointerDownThisFrame() &&
            !IsPrimaryPointerOverBlockingUi(PrimaryPointerPosition()) &&
            IsInsideFactoryClickZone(PrimaryPointerPosition()))
        {
            game.ProduceByClick();
        }

        if (toastVisible)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f)
            {
                toastVisible = false;
                toastText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void Bind(GlueFactoryGameManager gameManager)
    {
        if (game != null)
        {
            game.OnChanged -= Refresh;
            game.OnToast -= ShowToast;
        }

        game = gameManager;
        if (!built)
        {
            BuildUi();
            built = true;
        }
        ResolveFactoryClickZone();

        game.OnChanged += Refresh;
        game.OnToast += ShowToast;
        Refresh();
    }

    private void OnDestroy()
    {
        if (game == null)
        {
            return;
        }

        game.OnChanged -= Refresh;
        game.OnToast -= ShowToast;
    }

    private void Refresh()
    {
        if (game == null)
        {
            return;
        }

        var snap = game.Snapshot();

        moneyText.text = game.MoneyText(snap.Money);
        perClickText.text = "+" + game.MoneyText(game.ClickValuePerTap()) + "/click";
        autoIncomeText.text = game.RateText(game.AutoIncomePerSecondEstimate()) + "/s auto";
        totalText.text = "Slots " + (snap.ConveyorLevel + 1) + "/" + game.Config.maxSlots;

        if (selectedSlotText != null)
        {
            selectedSlotText.gameObject.SetActive(false);
        }
        if (selectedMachineText != null)
        {
            selectedMachineText.gameObject.SetActive(false);
        }

        clickCardInfo.text = "Lv " + snap.ClickLevel + " | Buy " + game.MoneyText(game.UpgradeCostClick());
        conveyorCardInfo.text = "Lv " + snap.ConveyorLevel + " | Buy " + game.MoneyText(game.UpgradeCostConveyor());
        boostCardInfo.text = "Lv " + snap.BoostLevel + " | Buy " + game.MoneyText(game.UpgradeCostBoost());
        speedCardInfo.text = game.MachineIntervalSeconds().ToString("0.0") + "s | Buy " + game.MoneyText(game.UpgradeCostSpeed());

        for (var i = 0; i < slotTexts.Count; i++)
        {
            var slotAction = i < slotActionButtons.Count ? slotActionButtons[i] : null;
            var slotActionText = slotAction != null ? slotAction.GetComponentInChildren<Text>() : null;
            if (i > snap.ConveyorLevel)
            {
                slotTexts[i].text = "LOCKED";
                slotBoxImages[i].color = new Color(0.16f, 0.16f, 0.18f, 0.72f);
                SetProgressWidth(slotProgressFills[i], 0f, 184f);
                if (slotAction != null)
                {
                    slotAction.interactable = false;
                }

                if (slotActionText != null)
                {
                    slotActionText.text = "UNAVAILABLE";
                }
                continue;
            }

            var machineId = snap.SlotMachineIds[i];
            var selected = i == snap.SelectedSlot;
            var nextMachineForSlot = game.NextMachineForSlot(i);
            if (machineId < 0)
            {
                slotTexts[i].text = "EMPTY";
                slotBoxImages[i].color = selected ? new Color(0.45f, 0.33f, 0.1f, 0.95f) : new Color(0.2f, 0.2f, 0.24f, 0.95f);
                SetProgressWidth(slotProgressFills[i], 0f, 184f);
            }
            else
            {
                slotTexts[i].text = game.Config.machines[machineId].displayName;
                slotBoxImages[i].color = selected ? new Color(0.56f, 0.43f, 0.09f, 0.95f) : new Color(0.26f, 0.26f, 0.31f, 0.95f);
                SetProgressWidth(slotProgressFills[i], Mathf.Clamp01(snap.SlotProgress01[i]), 184f);
            }

            if (slotAction != null)
            {
                slotAction.interactable = nextMachineForSlot >= 0;
            }

            if (slotActionText != null)
            {
                slotActionText.text = nextMachineForSlot < 0 ? "MAX" : (machineId < 0 ? "UNLOCKED" : "UPGRADE");
            }
        }
    }

    private void ShowToast(string message)
    {
        toastText.text = message;
        var toastGo = toastText.transform.parent.gameObject;
        toastGo.SetActive(true);
        toastGo.transform.SetAsLastSibling();
        ApplyToastStyle(InferToastType(message));
        toastVisible = true;
        toastTimer = 2.5f;
    }

    private void BuildUi()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("GlueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var root = Panel(canvasGo.transform, "Root", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1920, 1080), new Color(0f, 0f, 0f, 0f));

        var header = Panel(root, "Header", new Vector2(0, 1), new Vector2(0, 0), new Vector2(1920, 64), new Color(0.04f, 0.04f, 0.08f, 0.96f));
        Text(header, font, "GLUE FACTORY", 24, new Vector2(20, -10), new Vector2(300, 46), TextAnchor.MiddleLeft, new Color(0.96f, 0.77f, 0.1f));
        moneyText = Text(header, font, "$0", 24, new Vector2(330, -10), new Vector2(240, 46), TextAnchor.MiddleLeft, new Color(0.22f, 1f, 0.38f));
        perClickText = Text(header, font, "$0/click", 16, new Vector2(580, -10), new Vector2(240, 46), TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));
        autoIncomeText = Text(header, font, "$0/s auto", 16, new Vector2(820, -10), new Vector2(240, 46), TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));
        totalText = Text(header, font, "Slots 1/3", 16, new Vector2(1060, -10), new Vector2(220, 46), TextAnchor.MiddleLeft, new Color(0.74f, 0.74f, 0.78f));

        Button(header, font, "SAVE", new Vector2(1620, -14), new Vector2(130, 38), () => game.SaveNow(), new Color(0.2f, 0.22f, 0.1f), 14);
        Button(header, font, "DELETE", new Vector2(1760, -14), new Vector2(140, 38), () => game.DeleteSaveAndReset(), new Color(0.28f, 0.1f, 0.1f), 14);

        var world = Panel(root, "World", new Vector2(0, 1), new Vector2(0, -64), new Vector2(1540, 1016), new Color(0f, 0f, 0f, 0f));
        factoryClickZone = world;
        var belt = Panel(world, "Belt", new Vector2(0.5f, 0.5f), new Vector2(-260, 170), new Vector2(520, 86), new Color(0f, 0f, 0f, 0f));
        var beltImage = belt.GetComponent<Image>();
        if (beltImage != null)
        {
            beltImage.raycastTarget = false;
        }

        for (var i = 0; i < 3; i++)
        {
            var slotIndex = i;
            var x = 490 + i * 220;
            var slotPanel = Panel(world, "Slot" + i, new Vector2(0, 1), new Vector2(x, -420), new Vector2(200, 150), new Color(0.2f, 0.2f, 0.24f, 0.95f));
            slotBoxImages.Add(slotPanel.GetComponent<Image>());
            Text(slotPanel, font, "SLOT " + (i + 1), 14, new Vector2(8, -8), new Vector2(184, 24), TextAnchor.MiddleCenter, new Color(0.95f, 0.77f, 0.1f));
            slotTexts.Add(Text(slotPanel, font, "EMPTY", 13, new Vector2(8, -38), new Vector2(184, 48), TextAnchor.MiddleCenter, Color.white));

            var progressBg = Panel(slotPanel, "ProgressBg", new Vector2(0, 1), new Vector2(8, -94), new Vector2(184, 12), new Color(0.12f, 0.12f, 0.14f, 1f));
            var progressFill = Panel(progressBg, "ProgressFill", new Vector2(0, 1), new Vector2(0, 0), new Vector2(184, 12), new Color(0.96f, 0.77f, 0.1f, 1f));
            slotProgressFills.Add(progressFill);

            var slotButton = Button(slotPanel, font, "UPGRADE", new Vector2(48, -112), new Vector2(104, 28), () => HandleSlotUpgradeButton(slotIndex), new Color(0.15f, 0.2f, 0.28f), 12);
            slotActionButtons.Add(slotButton);
        }

        var right = Panel(root, "RightPanel", new Vector2(1, 1), new Vector2(-380, -64), new Vector2(380, 1016), new Color(0.05f, 0.05f, 0.09f, 0.98f));
        var tabRow = Panel(right, "Tabs", new Vector2(0, 1), new Vector2(0, 0), new Vector2(380, 46), new Color(0.08f, 0.08f, 0.12f, 1f));
        Button(tabRow, font, "UPGRADES", new Vector2(0, 0), new Vector2(190, 46), ShowUpgrades, new Color(0.28f, 0.20f, 0.05f), 14);
        Button(tabRow, font, "MACHINES", new Vector2(190, 0), new Vector2(190, 46), ShowMachines, new Color(0.08f, 0.20f, 0.30f), 14);

        upgradesTab = Panel(right, "UpgradesTab", new Vector2(0, 1), new Vector2(0, -56), new Vector2(380, 800), Color.clear);
        clickCardInfo = UpgradeCard(upgradesTab, font, 8, "Click Value", "Increase click output", () => game.UpgradeClick());
        conveyorCardInfo = UpgradeCard(upgradesTab, font, 198, "Conveyor", "Unlock more slots", BuyConveyorUpgradeWithPopup);
        boostCardInfo = UpgradeCard(upgradesTab, font, 388, "Export Value", "Boost all sale values", () => game.UpgradeBoost());
        speedCardInfo = UpgradeCard(upgradesTab, font, 578, "Machine Speed", "Faster production", () => game.UpgradeSpeed());

        machinesTab = Panel(right, "MachinesTab", new Vector2(0, 1), new Vector2(0, -56), new Vector2(380, 800), Color.clear);
        BuildMachineButtons(machinesTab, font);
        machinesTab.gameObject.SetActive(false);

        var toast = PanelBottomCenter(root, "Toast", new Vector2(0, 16), new Vector2(480, 46), new Color(0.05f, 0.05f, 0.09f, 0.95f));
        toastBackground = toast.GetComponent<Image>();
        toastText = Text(toast, font, "", 14, new Vector2(0, -4), new Vector2(480, 38), TextAnchor.MiddleCenter, new Color(0.95f, 0.77f, 0.1f));
        toast.gameObject.SetActive(false);
    }

    private void BuildMachineButtons(RectTransform parent, Font font)
    {
        var y = 8f;
        for (var i = 0; i < game.Config.maxSlots; i++)
        {
            var slot = i;
            var row = Panel(parent, "MachineSlot_" + i, new Vector2(0, 1), new Vector2(8, -y), new Vector2(364, 72), new Color(0.12f, 0.12f, 0.15f, 0.92f));
            Text(row, font, "SLOT " + (i + 1), 12, new Vector2(12, -6), new Vector2(170, 24), TextAnchor.MiddleLeft, Color.white);
            Text(row, font, "Upgrade machine slot", 10, new Vector2(12, -30), new Vector2(170, 22), TextAnchor.MiddleLeft, new Color(0.72f, 0.72f, 0.72f));
            Button(row, font, "UPGRADE", new Vector2(246, -14), new Vector2(108, 36), () => HandleSlotUpgradeButton(slot), new Color(0.16f, 0.16f, 0.22f), 12);
            y += 78f;
        }
    }

    private void HandleSlotUpgradeButton(int slotIndex)
    {
        if (game == null)
        {
            return;
        }

        var snap = game.Snapshot();
        if (slotIndex <= snap.ConveyorLevel)
        {
            game.PurchaseNextMachineForSlot(slotIndex);
            return;
        }

        var next = snap.ConveyorLevel + 1;
        if (slotIndex != next)
        {
            ShowToast("Unavailable. Unlock slots in order.");
            return;
        }

        ShowToast("Next upgrade item: Slot " + (slotIndex + 1) + ". Buy Conveyor in Upgrades tab.");
    }

    private void BuyConveyorUpgradeWithPopup()
    {
        if (game == null)
        {
            return;
        }

        var before = game.Snapshot();
        game.UpgradeConveyor();
        var after = game.Snapshot();
        if (after.ConveyorLevel <= before.ConveyorLevel)
        {
            return;
        }

        var unlockedSlot = after.ConveyorLevel + 1;
        var nextSlot = after.ConveyorLevel + 2;
        if (nextSlot <= game.Config.maxSlots)
        {
            ShowToast("Unlocked Slot " + unlockedSlot + ". Next upgrade item: Slot " + nextSlot + ".");
        }
        else
        {
            ShowToast("Unlocked Slot " + unlockedSlot + ". All slots unlocked.");
        }
    }

    private Text UpgradeCard(RectTransform parent, Font font, float y, string name, string desc, Action action)
    {
        var card = Panel(parent, name + "Card", new Vector2(0, 1), new Vector2(8, -y), new Vector2(364, 180), new Color(0.12f, 0.12f, 0.15f, 0.92f));
        Text(card, font, name, 15, new Vector2(10, -8), new Vector2(340, 28), TextAnchor.MiddleLeft, Color.white);
        Text(card, font, desc, 12, new Vector2(10, -38), new Vector2(340, 24), TextAnchor.MiddleLeft, new Color(0.65f, 0.65f, 0.68f));
        var info = Text(card, font, "", 12, new Vector2(10, -66), new Vector2(340, 24), TextAnchor.MiddleLeft, new Color(0.95f, 0.77f, 0.1f));
        Button(card, font, "BUY", new Vector2(10, -96), new Vector2(340, 34), action, new Color(0.2f, 0.16f, 0.08f), 13);
        return info;
    }

    private void ShowUpgrades()
    {
        upgradesTab.gameObject.SetActive(true);
        machinesTab.gameObject.SetActive(false);
        ShowToast("Tab: Upgrades");
    }

    private void ShowMachines()
    {
        upgradesTab.gameObject.SetActive(false);
        machinesTab.gameObject.SetActive(true);
        ShowToast("Tab: Machines");
    }

    private static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMin;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static RectTransform PanelBottomCenter(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static Text Text(Transform parent, Font font, string content, int size, Vector2 pos, Vector2 boxSize, TextAnchor align, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = boxSize;

        var txt = go.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = color;
        txt.text = content;
        return txt;
    }

    private static Button Button(Transform parent, Font font, string label, Vector2 pos, Vector2 size, Action click, Color color, int fontSize)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = color;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => click?.Invoke());

        var txt = Text(go.transform, font, label, fontSize, new Vector2(0, 0), size, TextAnchor.MiddleCenter, Color.white);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;
        txt.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        return btn;
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
        var inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
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
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
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

    private bool IsPrimaryPointerOverBlockingUi(Vector2 pointerPos)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPos
        };
        var results = new List<RaycastResult>(16);
        EventSystem.current.RaycastAll(pointerEventData, results);
        for (var i = 0; i < results.Count; i++)
        {
            var hit = results[i].gameObject != null ? results[i].gameObject.transform : null;
            if (hit == null)
            {
                continue;
            }

            if (factoryClickZone != null && hit.IsChildOf(factoryClickZone))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ResolveFactoryClickZone()
    {
        if (factoryClickZone != null)
        {
            return;
        }

        factoryClickZone = transform.Find("GlueCanvas/Root/World") as RectTransform;
        if (factoryClickZone == null)
        {
            var go = GameObject.Find("World") ?? GameObject.Find("FactoryPanel");
            if (go != null)
            {
                factoryClickZone = go.GetComponent<RectTransform>();
            }
        }
    }

    private bool IsInsideFactoryClickZone(Vector2 screenPosition)
    {
        if (factoryClickZone == null)
        {
            ResolveFactoryClickZone();
        }

        if (factoryClickZone == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(factoryClickZone, screenPosition, null);
    }

    private static void SetProgressWidth(RectTransform rt, float progress01, float maxWidth)
    {
        if (rt == null)
        {
            return;
        }

        rt.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(progress01), rt.sizeDelta.y);
    }

    private void ApplyToastStyle(ToastType type)
    {
        if (toastBackground == null || toastText == null)
        {
            return;
        }

        switch (type)
        {
            case ToastType.Success:
                toastBackground.color = new Color(0.05f, 0.14f, 0.08f, 0.96f);
                toastText.color = new Color(0.42f, 1f, 0.55f, 1f);
                break;
            case ToastType.Warning:
                toastBackground.color = new Color(0.2f, 0.16f, 0.05f, 0.96f);
                toastText.color = new Color(1f, 0.86f, 0.33f, 1f);
                break;
            case ToastType.Error:
                toastBackground.color = new Color(0.2f, 0.05f, 0.05f, 0.96f);
                toastText.color = new Color(1f, 0.45f, 0.45f, 1f);
                break;
            default:
                toastBackground.color = new Color(0.07f, 0.11f, 0.18f, 0.96f);
                toastText.color = new Color(0.6f, 0.83f, 1f, 1f);
                break;
        }
    }

    private static ToastType InferToastType(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ToastType.Info;
        }

        var lower = message.ToLowerInvariant();
        if (lower.Contains("failed") || lower.Contains("error") || lower.Contains("invalid"))
        {
            return ToastType.Error;
        }

        if (lower.Contains("not enough") || lower.Contains("locked") || lower.Contains("no machine") || lower.Contains("need"))
        {
            return ToastType.Warning;
        }

        if (lower.Contains("saved") || lower.Contains("installed") || lower.Contains("sold") || lower.Contains("upgraded") || lower.Contains("produced") || lower.Contains("loaded") || lower.Contains("bought"))
        {
            return ToastType.Success;
        }

        return ToastType.Info;
    }
}
