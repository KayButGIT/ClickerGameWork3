using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[ExecuteAlways]
public sealed class GlueFactorySceneUIManager : MonoBehaviour
{
    private const float TabsTopOffset = -104f;

    [Header("Core")]
    [SerializeField] private GlueFactoryGameManager game;

    [Header("Header")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text moneyText;
    [SerializeField] private Text perClickText;
    [SerializeField] private Text autoIncomeText;
    [SerializeField] private Text totalEarnedText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button deleteSaveButton;
    [SerializeField] private Button cheatButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GlueFactoryCheatDefinition cheatDefinition;

    [Header("Tabs")]
    [SerializeField] private Button upgradesTabButton;
    [SerializeField] private Button machinesTabButton;
    [SerializeField] private RectTransform upgradesTab;
    [SerializeField] private RectTransform machinesTab;

    [Header("Actions")]
    [SerializeField] private Button glueButton;
    [SerializeField] private Button installButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Text selectedMachineText;
    [SerializeField] private Text selectedSlotText;

    [Header("Slots")]
    [SerializeField] private List<Button> slotSelectButtons = new List<Button>();
    [SerializeField] private List<Text> slotTexts = new List<Text>();
    [SerializeField] private List<Text> slotDetailTexts = new List<Text>();
    [SerializeField] private List<Image> slotProductIcons = new List<Image>();
    [SerializeField] private List<Image> slotProgressFills = new List<Image>();
    [SerializeField] private List<Image> slotCardBackgrounds = new List<Image>();

    [Header("Upgrade Cards")]
    [SerializeField] private Text clickLevelText;
    [SerializeField] private Text clickDescText;
    [SerializeField] private Text clickCostText;
    [SerializeField] private Text conveyorLevelText;
    [SerializeField] private Text conveyorDescText;
    [SerializeField] private Text conveyorCostText;
    [SerializeField] private Text exportLevelText;
    [SerializeField] private Text exportDescText;
    [SerializeField] private Text exportCostText;
    [SerializeField] private Text speedLevelText;
    [SerializeField] private Text speedDescText;
    [SerializeField] private Text speedCostText;
    [SerializeField] private Button clickBuyButton;
    [SerializeField] private Button conveyorBuyButton;
    [SerializeField] private Button exportBuyButton;
    [SerializeField] private Button speedBuyButton;

    [Header("Machines Tab")]
    [SerializeField] private RectTransform machineShopContent;
    [SerializeField] private bool enableRuntimeProductTool;
    [SerializeField] private GlueProductCatalog productCatalog;
    [SerializeField] private GlueProductDefinition productDefinition;
    [SerializeField] private GlueUpgradeDefinition upgradeDefinition;

    [Header("Toast")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private Image toastBackground;
    [SerializeField] private Text toastText;
    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private Text loadingOverlayText;
    [SerializeField] private Image loadingStartupBackgroundImage;
    [SerializeField] private Sprite loadingStartupBackgroundSprite;
    [SerializeField] private Color loadingStartupBackgroundTint = new Color(0.16f, 0.24f, 0.32f, 0.9f);

    private bool bound;
    private bool toastVisible;
    private float toastTimer;
    private bool loadingOverlayVisible;
    private float loadingOverlayTimer;
    private int loadingOverlayDotCount;
    private string loadingOverlayBaseText = "Loading";
    private GlueFactoryWorldManager startupWorldManager;
    private RectTransform factoryClickZone;
    private readonly List<Image> machineRowBackgrounds = new List<Image>();
    private readonly List<Button> machineSelectButtons = new List<Button>();
    private readonly List<Text> machineRowDetailTexts = new List<Text>();
    private readonly List<Image> machineRowCurrentIcons = new List<Image>();
    private readonly List<Image> machineRowNextIcons = new List<Image>();
    private readonly List<Sprite> machineDisplayIcons = new List<Sprite>();
    private GameObject machineUpgradeDialog;
    private Text machineUpgradeDialogTitle;
    private Text machineUpgradeDialogBody;
    private Image machineUpgradeCurrentIcon;
    private Image machineUpgradeNewIcon;
    private Text machineUpgradeCurrentDetail;
    private Text machineUpgradeNewDetail;
    private Button machineUpgradeConfirmButton;
    private Button machineUpgradeCancelButton;
    private GameObject resetConfirmDialog;
    private Button resetConfirmYesButton;
    private Button resetConfirmNoButton;
    private GameObject exitConfirmDialog;
    private Button exitConfirmYesButton;
    private Button exitConfirmNoButton;
    private GameObject cheatDialog;
    private InputField cheatAmountInput;
    private Text cheatDialogHintText;
    private Button cheatDialogAddButton;
    private Button cheatDialogCloseButton;
    private int pendingUpgradeSlot = -1;
    private int pendingUpgradeMachine = -1;
    private GameObject productToolPanel;
    private Text productIndexText;
    private Text productListText;
    private InputField productIdInput;
    private InputField productNameInput;
    private InputField pieceValueInput;
    private InputField machineCostInput;
    private InputField shopOrderInput;
    private Toggle includeInShopToggle;
    private Button productToolToggleButton;
    private int currentProductIndex;
#if UNITY_EDITOR
    private bool editorPreviewQueued;
#endif

    private void Awake()
    {
        EnsureEventSystem();
        AutoResolveReferences();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorPreviewTheme();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            QueueEditorPreviewTheme();
        }
    }

    private void QueueEditorPreviewTheme()
    {
        if (editorPreviewQueued)
        {
            return;
        }

        editorPreviewQueued = true;
        EditorApplication.delayCall += ApplyQueuedEditorPreviewTheme;
    }

    private void ApplyQueuedEditorPreviewTheme()
    {
        EditorApplication.delayCall -= ApplyQueuedEditorPreviewTheme;
        editorPreviewQueued = false;

        if (this == null || Application.isPlaying)
        {
            return;
        }

        ApplyEditorPreviewTheme();
    }
#endif

    private void Update()
    {
        if (game != null &&
            IsPrimaryPointerDownThisFrame() &&
            !IsAnyModalDialogOpen() &&
            !IsPrimaryPointerOverBlockingUi(PrimaryPointerPosition()) &&
            IsInsideFactoryClickZone(PrimaryPointerPosition()))
        {
            game.ProduceByClick();
        }

        if (toastVisible && toastPanel != null)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f)
            {
                toastVisible = false;
                toastPanel.SetActive(false);
            }
        }

        if (!loadingOverlayVisible || loadingOverlayText == null)
        {
            return;
        }

        loadingOverlayTimer += Time.unscaledDeltaTime;
        if (loadingOverlayTimer >= 0.3f)
        {
            loadingOverlayTimer = 0f;
            loadingOverlayDotCount = (loadingOverlayDotCount + 1) % 4;
            loadingOverlayText.text = loadingOverlayBaseText + new string('.', loadingOverlayDotCount);
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
        AutoResolveReferences();
        ResolveUpgradeCardReferences();
        ResolveFactoryClickZone();
        NormalizeFactoryOverlayVisual();
        NormalizeGlobalOverlayVisual();
        EnsureExitButtonInHeader();
        ResolveCheatSource();
        EnsureCheatButtonInHeader();
        ResolveProductSources();
        ResolveUpgradeSource();
        WireButtons();
        HideLegacyWorldSlotCards();
        CompactTopArea();
        ApplyHeaderAndUpgradeTheme();
        EnsureMachineUpgradeDialogUi();
        EnsureResetConfirmDialogUi();
        EnsureExitConfirmDialogUi();
        EnsureCheatDialogUi();
        EnsureLoadingOverlayUi();
        if (enableRuntimeProductTool)
        {
            EnsureProductToolUi();
        }
        RebuildMachineShop();
        ShowUpgradesTab();
        ShowStartupLoadingOverlay();

        game.OnChanged += Refresh;
        game.OnToast += ShowToast;
        bound = true;
        RefreshCheatUiState();
        Refresh();
    }

    private void OnDestroy()
    {
        if (!bound || game == null)
        {
            return;
        }

        game.OnChanged -= Refresh;
        game.OnToast -= ShowToast;
    }

    private void WireButtons()
    {
        RefreshCheatUiState();
        BindButton(cheatButton, ShowCheatDialog);
        BindButton(saveButton, () => game.SaveNow());
        BindButton(deleteSaveButton, ShowResetConfirmDialog);
        BindButton(exitButton, ShowExitConfirmDialog);
        if (glueButton != null)
        {
            glueButton.gameObject.SetActive(false);
        }
        if (installButton != null)
        {
            installButton.gameObject.SetActive(false);
        }

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(false);
        }

        BindButton(upgradesTabButton, ShowUpgradesTab);
        BindButton(machinesTabButton, ShowMachinesTab);

        for (var i = 0; i < slotSelectButtons.Count; i++)
        {
            var slot = i;
            BindButton(slotSelectButtons[i], () => HandleSlotUpgradeButton(slot));
        }

        BindButton(clickBuyButton, () => game.UpgradeClick());
        BindButton(conveyorBuyButton, BuyConveyorUpgradeWithPopup);
        BindButton(exportBuyButton, () => game.UpgradeBoost());
        BindButton(speedBuyButton, () => game.UpgradeSpeed());
    }

    private void RebuildMachineShop()
    {
        if (machineShopContent == null || game == null)
        {
            return;
        }

        for (var i = machineShopContent.childCount - 1; i >= 0; i--)
        {
            Destroy(machineShopContent.GetChild(i).gameObject);
        }
        machineRowBackgrounds.Clear();
        machineSelectButtons.Clear();
        machineRowDetailTexts.Clear();
        machineRowCurrentIcons.Clear();
        machineRowNextIcons.Clear();
        machineDisplayIcons.Clear();

        if (enableRuntimeProductTool)
        {
            EnsureProductToolUi();
            if (productToolToggleButton != null)
            {
                productToolToggleButton.transform.SetAsFirstSibling();
            }
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var contentWidth = Mathf.Max(320f, machineShopContent.rect.width);
        var rowWidth = contentWidth - 16f;
        var rowHeight = 188f;
        var buttonWidth = rowWidth - 28f;
        var buttonX = 14f;
        var textX = 14f;
        var infoWidth = Mathf.Max(120f, rowWidth - textX - 98f);
        var y = 8f;
        for (var i = 0; i < game.Config.maxSlots; i++)
        {
            var slotIndex = i;

            var row = CreatePanel(machineShopContent, "MachineSlot_" + i, new Vector2(8, -y), new Vector2(rowWidth, rowHeight), new Color(0.07f, 0.08f, 0.16f, 0.95f));
            var title = CreateText(row, font, "SLOT " + (slotIndex + 1), 14, new Vector2(textX, -12), new Vector2(infoWidth, 30), TextAnchor.MiddleLeft, new Color(0.92f, 0.92f, 0.95f, 1f));
            title.fontStyle = FontStyle.Bold;

            var detail = CreateText(row, font, "Unavailable", 12, new Vector2(textX, -50), new Vector2(infoWidth, 42), TextAnchor.UpperLeft, new Color(0.70f, 0.72f, 0.78f, 1f));
            detail.lineSpacing = 1.2f;

            var iconBg = CreatePanel(row, "CurrentIconBg", new Vector2(rowWidth - 78f, -18f), new Vector2(64f, 64f), new Color(0.10f, 0.11f, 0.20f, 0.94f));
            var iconGo = new GameObject("CurrentIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(iconBg, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(1f, 1f);
            iconRt.offsetMin = new Vector2(6f, 6f);
            iconRt.offsetMax = new Vector2(-6f, -6f);
            var currentIcon = iconGo.GetComponent<Image>();
            currentIcon.preserveAspect = true;
            currentIcon.enabled = false;

            var btn = CreateButton(row, font, "BUY", new Vector2(buttonX, -126), new Vector2(buttonWidth, 36), new Color(0.25f, 0.20f, 0.06f, 0.98f), 11);
            BindButton(btn, () => HandleSlotUpgradeButton(slotIndex));
            machineRowBackgrounds.Add(row.GetComponent<Image>());
            machineSelectButtons.Add(btn);
            machineRowDetailTexts.Add(detail);
            machineRowCurrentIcons.Add(currentIcon);
            machineRowNextIcons.Add(null);

            y += rowHeight + 8f;
        }

        machineShopContent.sizeDelta = new Vector2(machineShopContent.sizeDelta.x, y + 8f);
        UpdateMachinesTabScrollState();
    }

    private void UpdateMachinesTabScrollState()
    {
        if (machineShopContent == null)
        {
            return;
        }

        var scrollRect = machineShopContent.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            return;
        }

        // Legacy scene compatibility: remove old scroll-frame visuals so Machine tab
        // keeps the same clean card style as Factory tab.
        var scrollImage = scrollRect.GetComponent<Image>();
        if (scrollImage != null)
        {
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = false;
        }
        if (scrollRect.viewport != null)
        {
            var viewportImage = scrollRect.viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.color = new Color(0f, 0f, 0f, 0f);
                viewportImage.raycastTarget = false;
            }
        }

        scrollRect.vertical = false;
        scrollRect.horizontal = false;
        if (scrollRect.verticalScrollbar != null)
        {
            scrollRect.verticalScrollbar.gameObject.SetActive(false);
        }

        scrollRect.StopMovement();
        machineShopContent.anchoredPosition = new Vector2(machineShopContent.anchoredPosition.x, 0f);
    }

    private void Refresh()
    {
        if (game == null)
        {
            return;
        }

        RefreshCheatUiState();

        if (clickDescText == null || conveyorDescText == null || exportDescText == null || speedDescText == null)
        {
            AutoResolveReferences();
            ResolveUpgradeCardReferences();
        }

        var snap = game.Snapshot();

        if (titleText != null) titleText.text = "GLUE FACTORY";
        if (moneyText != null) moneyText.text = game.MoneyText(snap.Money);
        if (perClickText != null) perClickText.gameObject.SetActive(false);
        if (autoIncomeText != null)
        {
            autoIncomeText.gameObject.SetActive(true);
            autoIncomeText.text = game.RateText(game.AutoIncomePerSecondEstimate()) + "/s auto";
        }
        if (totalEarnedText != null) totalEarnedText.text = "Total: " + game.MoneyText(snap.TotalEarned);

        if (selectedSlotText != null) selectedSlotText.gameObject.SetActive(false);
        if (selectedMachineText != null) selectedMachineText.gameObject.SetActive(false);

        var clickMax = snap.ClickLevel >= game.Config.clickValueUpgrade.maxLevel;
        var conveyorMax = snap.ConveyorLevel >= game.Config.conveyorUpgrade.maxLevel;
        var boostMax = snap.BoostLevel >= game.Config.factoryBoostUpgrade.maxLevel;
        var speedMax = snap.SpeedLevel >= game.Config.speedUpgrade.maxLevel;

        if (clickLevelText != null) clickLevelText.text = BuildUpgradeTitle("click", "Player Glue Value", snap.ClickLevel);
        if (clickDescText != null) clickDescText.text = BuildUpgradeDescription("click", "Increase the sell price of manually produced glue.", snap.ClickLevel);
        if (clickCostText != null) clickCostText.text = clickMax ? "Lv " + snap.ClickLevel + " | MAX" : "Lv " + snap.ClickLevel + " | Price: " + game.MoneyText(game.UpgradeCostClick());
        SetUpgradeButtonState(clickBuyButton, clickMax);

        if (conveyorLevelText != null) conveyorLevelText.text = BuildUpgradeTitle("conveyor", "Conveyor Slot", snap.ConveyorLevel);
        if (conveyorDescText != null) conveyorDescText.text = BuildUpgradeDescription("conveyor", "Unlock a new conveyor and double manual glue production.", snap.ConveyorLevel);
        if (conveyorCostText != null) conveyorCostText.text = conveyorMax ? "Lv " + snap.ConveyorLevel + " | MAX" : "Lv " + snap.ConveyorLevel + " | Buy " + game.MoneyText(game.UpgradeCostConveyor());
        SetUpgradeButtonState(conveyorBuyButton, conveyorMax);

        if (exportLevelText != null) exportLevelText.text = BuildUpgradeTitle("boost", "Export Value", snap.BoostLevel);
        if (exportDescText != null) exportDescText.text = BuildUpgradeDescription("boost", "Boost all sale values.", snap.BoostLevel);
        if (exportCostText != null) exportCostText.text = boostMax ? "Lv " + snap.BoostLevel + " | MAX" : "Lv " + snap.BoostLevel + " | Buy " + game.MoneyText(game.UpgradeCostBoost());
        SetUpgradeButtonState(exportBuyButton, boostMax);

        if (speedLevelText != null) speedLevelText.text = BuildUpgradeTitle("speed", "Machine Production Speed", snap.SpeedLevel);
        if (speedDescText != null) speedDescText.text = BuildUpgradeDescription("speed", "Increase the rate of automatic production.", snap.SpeedLevel);
        if (speedCostText != null) speedCostText.text = speedMax ? "Lv " + snap.SpeedLevel + " | MAX" : "Lv " + snap.SpeedLevel + " | Buy " + game.MoneyText(game.UpgradeCostSpeed());
        SetUpgradeButtonState(speedBuyButton, speedMax);

        for (var i = 0; i < machineRowBackgrounds.Count; i++)
        {
            var unlocked = i <= snap.ConveyorLevel;
            var currentMachine = i < snap.SlotMachineIds.Length ? snap.SlotMachineIds[i] : -1;
            var rowBg = machineRowBackgrounds[i];
            if (rowBg != null)
            {
                rowBg.color = new Color(0.07f, 0.08f, 0.16f, 0.95f);
            }

            if (i < machineSelectButtons.Count)
            {
                var nextMachine = game.NextMachineForSlot(i);
                var interactable = unlocked && nextMachine >= 0;
                ApplyButtonVisual(
                    machineSelectButtons[i],
                    interactable ? new Color(0.25f, 0.20f, 0.06f, 0.98f) : new Color(0.10f, 0.11f, 0.20f, 0.94f),
                    interactable ? new Color(1f, 0.92f, 0.56f, 1f) : new Color(0.9f, 0.9f, 0.95f, 1f));
                machineSelectButtons[i].interactable = interactable;

                var txt = machineSelectButtons[i].GetComponentInChildren<Text>();
                if (txt != null)
                {
                    if (!unlocked)
                    {
                        txt.text = "UNAVAILABLE";
                    }
                    else if (nextMachine < 0)
                    {
                        txt.text = "MAX";
                    }
                    else
                    {
                        txt.text = currentMachine < 0 ? "UNLOCKED" : "BUY";
                    }
                }
            }

            if (i < machineRowDetailTexts.Count && machineRowDetailTexts[i] != null)
            {
                var nextMachine = game.NextMachineForSlot(i);
                if (unlocked)
                {
                    if (nextMachine < 0)
                    {
                        machineRowDetailTexts[i].text = currentMachine >= 0 && currentMachine < game.Config.machines.Count
                            ? "Current: " + game.Config.machines[currentMachine].displayName + "\nOutput: " + game.MoneyText(game.Config.machines[currentMachine].pieceValue) + "/piece\nStatus: MAX tier"
                            : "Current: EMPTY\nStatus: MAX tier";
                    }
                    else
                    {
                        if (currentMachine >= 0 && currentMachine < game.Config.machines.Count)
                        {
                            var curCfg = game.Config.machines[currentMachine];
                            machineRowDetailTexts[i].text =
                                "Current: " + curCfg.displayName + "\n" +
                                "Output: " + game.MoneyText(curCfg.pieceValue) + "/piece\n" +
                                "Cost: " + game.MoneyText(game.Config.machines[nextMachine].machineCost);
                        }
                        else
                        {
                            machineRowDetailTexts[i].text =
                                "Current: EMPTY\n" +
                                "Install machine\n" +
                                "Cost: " + game.MoneyText(game.Config.machines[nextMachine].machineCost);
                        }
                    }
                }
                else
                {
                    machineRowDetailTexts[i].text = "Locked\nUpgrade conveyor to unlock this slot.";
                    if (i < machineRowCurrentIcons.Count && machineRowCurrentIcons[i] != null) machineRowCurrentIcons[i].enabled = false;
                }

                if (i < machineRowCurrentIcons.Count)
                {
                    var icon = machineRowCurrentIcons[i];
                    if (icon != null && unlocked)
                    {
                        if (currentMachine >= 0 && currentMachine < game.Config.machines.Count)
                        {
                            var currentCfg = game.Config.machines[currentMachine];
                            icon.sprite = ResolveMachineIcon(currentCfg.id, currentCfg.icon);
                            icon.enabled = icon.sprite != null;
                            icon.color = Color.white;
                        }
                        else if (nextMachine >= 0 && nextMachine < game.Config.machines.Count)
                        {
                            var nextCfg = game.Config.machines[nextMachine];
                            icon.sprite = ResolveMachineIcon(nextCfg.id, nextCfg.icon);
                            icon.enabled = icon.sprite != null;
                            icon.color = new Color(1f, 1f, 1f, 0.85f);
                        }
                        else
                        {
                            icon.enabled = false;
                        }
                    }
                }
            }
        }

        for (var i = 0; i < slotTexts.Count; i++)
        {
            if (i >= snap.SlotMachineIds.Length)
            {
                continue;
            }

            var selectedSlot = i == snap.SelectedSlot;
            var slotButton = slotSelectButtons.Count > i ? slotSelectButtons[i] : null;
            var slotCard = slotCardBackgrounds.Count > i ? slotCardBackgrounds[i] : null;
            var slotDetail = slotDetailTexts.Count > i ? slotDetailTexts[i] : null;
            var slotIcon = slotProductIcons.Count > i ? slotProductIcons[i] : null;
            if (slotIcon == null)
            {
                var slotParent = FindRect(transform, "SlotParent" + i);
                if (slotParent != null)
                {
                    slotIcon = FindImage(slotParent, "SlotProductIcon");
                    if (slotIcon != null && slotProductIcons.Count > i)
                    {
                        slotProductIcons[i] = slotIcon;
                    }
                }
            }

            if (i > snap.ConveyorLevel)
            {
                if (slotTexts[i] != null) slotTexts[i].text = "LOCKED";
                if (slotDetail != null) slotDetail.text = "Unlock conveyor upgrade";
                if (slotIcon != null) slotIcon.enabled = false;
                if (slotProgressFills.Count > i && slotProgressFills[i] != null) SetSlotProgress(slotProgressFills[i], 0f);
                if (slotButton != null) slotButton.interactable = false;
                if (slotCard != null)
                {
                    slotCard.color = selectedSlot
                        ? new Color(0.34f, 0.28f, 0.10f, 0.95f)
                        : new Color(0.16f, 0.16f, 0.18f, 0.72f);
                }
                ApplyButtonVisual(
                    slotButton,
                    selectedSlot ? new Color(0.26f, 0.34f, 0.10f, 0.96f) : new Color(0.16f, 0.16f, 0.22f, 0.96f),
                    selectedSlot ? new Color(0.90f, 1f, 0.72f, 1f) : new Color(0.9f, 0.9f, 0.95f, 1f));
                var lockedText = slotButton != null ? slotButton.GetComponentInChildren<Text>() : null;
                if (lockedText != null)
                {
                    lockedText.text = "UNAVAILABLE";
                }
                continue;
            }

            var nextMachineForSlot = game.NextMachineForSlot(i);
            if (slotButton != null) slotButton.interactable = nextMachineForSlot >= 0;

            var machine = snap.SlotMachineIds[i];
            if (machine < 0)
            {
                if (slotTexts[i] != null) slotTexts[i].text = "EMPTY";
                if (slotDetail != null) slotDetail.text = "Unlocked";
                if (slotIcon != null) slotIcon.enabled = false;
                if (slotProgressFills.Count > i && slotProgressFills[i] != null) SetSlotProgress(slotProgressFills[i], 0f);
                if (slotCard != null)
                {
                    slotCard.color = selectedSlot
                        ? new Color(0.45f, 0.33f, 0.10f, 0.95f)
                        : new Color(0.20f, 0.20f, 0.24f, 0.95f);
                }
                ApplyButtonVisual(
                    slotButton,
                    new Color(0.16f, 0.16f, 0.22f, 0.96f),
                    new Color(0.9f, 0.9f, 0.95f, 1f));
            }
            else
            {
                if (slotTexts[i] != null) slotTexts[i].text = game.Config.machines[machine].displayName;
                if (slotProgressFills.Count > i && slotProgressFills[i] != null) SetSlotProgress(slotProgressFills[i], snap.SlotProgress01[i]);
                if (slotDetail != null)
                {
                    var interval = game.MachineIntervalSeconds();
                    var remain = Mathf.Max(0f, interval * (1f - Mathf.Clamp01(snap.SlotProgress01[i])));
                    var pieceValue = game.Config.machines[machine].pieceValue * (1d + snap.BoostLevel * game.Config.factoryBoostPerLevel);
                    var perSec = interval <= 0f ? 0d : pieceValue / interval;
                    slotDetail.text = remain.ToString("0.0") + "s left | +" + game.MoneyText(pieceValue) + " | " + game.RateText(perSec) + "/s";
                }
                if (slotIcon != null)
                {
                    var cfg = game.Config.machines[machine];
                    var resolved = machine < machineDisplayIcons.Count ? machineDisplayIcons[machine] : ResolveMachineIcon(cfg.id, cfg.icon);
                    slotIcon.sprite = resolved;
                    slotIcon.enabled = slotIcon.sprite != null;
                    slotIcon.color = Color.white;
                }
                if (slotCard != null)
                {
                    slotCard.color = selectedSlot
                        ? new Color(0.56f, 0.43f, 0.09f, 0.95f)
                        : new Color(0.26f, 0.26f, 0.31f, 0.95f);
                }
                ApplyButtonVisual(
                    slotButton,
                    new Color(0.16f, 0.16f, 0.22f, 0.96f),
                    new Color(0.9f, 0.9f, 0.95f, 1f));
            }

            var unlockedText = slotButton != null ? slotButton.GetComponentInChildren<Text>() : null;
            if (unlockedText != null)
            {
                unlockedText.text = nextMachineForSlot < 0 ? "MAX" : (machine < 0 ? "UNLOCKED" : "UPGRADE");
            }
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
            var nextMachine = game.NextMachineForSlot(slotIndex);
            if (nextMachine < 0)
            {
                ShowToast("Slot " + (slotIndex + 1) + " is already at max machine.");
                return;
            }

            ShowMachineUpgradeDialog(slotIndex, nextMachine);
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

    private void EnsureMachineUpgradeDialogUi()
    {
        if (machineUpgradeDialog != null)
        {
            return;
        }

        var canvas = titleText != null ? titleText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlay = new GameObject("GF_MachineUpgradeDialog", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.62f);
        overlayImg.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(700f, 400f);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBar.transform.SetParent(panel.transform, false);
        var titleBarRt = titleBar.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.anchoredPosition = Vector2.zero;
        titleBarRt.sizeDelta = new Vector2(0f, 52f);
        var titleBarImg = titleBar.GetComponent<Image>();
        titleBarImg.color = new Color(0.15f, 0.12f, 0.05f, 0.98f);

        machineUpgradeDialogTitle = CreateText(panel.transform as RectTransform, font, "UPGRADE", 22, new Vector2(24f, -10f), new Vector2(652f, 36f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));

        var currentCard = CreatePanel(panel.transform, "CurrentCard", new Vector2(28f, -72f), new Vector2(280f, 232f), new Color(0.08f, 0.09f, 0.17f, 0.96f));
        CreateText(currentCard, font, "CURRENT", 12, new Vector2(12f, -10f), new Vector2(256f, 22f), TextAnchor.MiddleCenter, new Color(0.82f, 0.82f, 0.9f, 1f));
        var currentIconGo = new GameObject("CurrentIcon", typeof(RectTransform), typeof(Image));
        currentIconGo.transform.SetParent(currentCard, false);
        var currentIconRt = currentIconGo.GetComponent<RectTransform>();
        currentIconRt.anchorMin = new Vector2(0f, 1f);
        currentIconRt.anchorMax = new Vector2(0f, 1f);
        currentIconRt.pivot = new Vector2(0f, 1f);
        currentIconRt.anchoredPosition = new Vector2(12f, -40f);
        currentIconRt.sizeDelta = new Vector2(72f, 72f);
        machineUpgradeCurrentIcon = currentIconGo.GetComponent<Image>();
        machineUpgradeCurrentIcon.preserveAspect = true;
        machineUpgradeCurrentIcon.enabled = false;
        machineUpgradeCurrentDetail = CreateText(currentCard, font, "", 12, new Vector2(96f, -40f), new Vector2(172f, 180f), TextAnchor.UpperLeft, new Color(0.84f, 0.86f, 0.92f, 1f));
        machineUpgradeCurrentDetail.supportRichText = true;
        machineUpgradeCurrentDetail.lineSpacing = 1.2f;

        CreateText(panel.transform as RectTransform, font, "->", 34, new Vector2(318f, -158f), new Vector2(64f, 46f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));

        var newCard = CreatePanel(panel.transform, "NewCard", new Vector2(392f, -72f), new Vector2(280f, 232f), new Color(0.10f, 0.09f, 0.18f, 0.96f));
        CreateText(newCard, font, "NEW", 12, new Vector2(12f, -10f), new Vector2(256f, 22f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));
        var newIconGo = new GameObject("NewIcon", typeof(RectTransform), typeof(Image));
        newIconGo.transform.SetParent(newCard, false);
        var newIconRt = newIconGo.GetComponent<RectTransform>();
        newIconRt.anchorMin = new Vector2(0f, 1f);
        newIconRt.anchorMax = new Vector2(0f, 1f);
        newIconRt.pivot = new Vector2(0f, 1f);
        newIconRt.anchoredPosition = new Vector2(12f, -40f);
        newIconRt.sizeDelta = new Vector2(72f, 72f);
        machineUpgradeNewIcon = newIconGo.GetComponent<Image>();
        machineUpgradeNewIcon.preserveAspect = true;
        machineUpgradeNewIcon.enabled = false;
        machineUpgradeNewDetail = CreateText(newCard, font, "", 12, new Vector2(96f, -40f), new Vector2(172f, 180f), TextAnchor.UpperLeft, new Color(0.84f, 0.86f, 0.92f, 1f));
        machineUpgradeNewDetail.supportRichText = true;
        machineUpgradeNewDetail.lineSpacing = 1.2f;

        machineUpgradeDialogBody = CreateText(panel.transform as RectTransform, font, "", 13, new Vector2(28f, -318f), new Vector2(644f, 28f), TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.46f, 1f));
        machineUpgradeDialogBody.supportRichText = true;

        machineUpgradeConfirmButton = CreateButton(panel.transform as RectTransform, font, "CONFIRM UPGRADE", new Vector2(36f, -350f), new Vector2(300f, 42f), new Color(0.25f, 0.20f, 0.06f, 0.98f), 13);
        machineUpgradeCancelButton = CreateButton(panel.transform as RectTransform, font, "CANCEL", new Vector2(364f, -350f), new Vector2(300f, 42f), new Color(0.08f, 0.14f, 0.24f, 0.96f), 13);
        ApplyButtonVisual(machineUpgradeConfirmButton, new Color(0.25f, 0.20f, 0.06f, 0.98f), new Color(1f, 0.92f, 0.56f, 1f));
        ApplyButtonVisual(machineUpgradeCancelButton, new Color(0.08f, 0.14f, 0.24f, 0.96f), new Color(0.74f, 0.90f, 1f, 1f));
        BindButton(machineUpgradeConfirmButton, ConfirmMachineUpgrade);
        BindButton(machineUpgradeCancelButton, CancelMachineUpgrade);

        machineUpgradeDialog = overlay;
        machineUpgradeDialog.SetActive(false);
    }

    private void ShowMachineUpgradeDialog(int slotIndex, int nextMachine)
    {
        EnsureMachineUpgradeDialogUi();
        if (machineUpgradeDialog == null || game == null)
        {
            game.PurchaseNextMachineForSlot(slotIndex);
            return;
        }

        var snap = game.Snapshot();
        var currentMachine = slotIndex >= 0 && slotIndex < snap.SlotMachineIds.Length ? snap.SlotMachineIds[slotIndex] : -1;
        var nextCfg = game.Config.machines[nextMachine];
        var currentName = currentMachine >= 0 && currentMachine < game.Config.machines.Count
            ? game.Config.machines[currentMachine].displayName
            : "EMPTY";
        var currentValue = currentMachine >= 0 && currentMachine < game.Config.machines.Count
            ? game.MoneyText(game.Config.machines[currentMachine].pieceValue) + "/piece"
            : "$0/piece";
        var nextValue = game.MoneyText(nextCfg.pieceValue) + "/piece";

        pendingUpgradeSlot = slotIndex;
        pendingUpgradeMachine = nextMachine;

        if (machineUpgradeDialogTitle != null)
        {
            machineUpgradeDialogTitle.text = "UPGRADE SLOT " + (slotIndex + 1);
        }

        if (machineUpgradeDialogBody != null)
        {
            machineUpgradeDialogBody.text = "<color=#D3A84A>Upgrade Cost: " + game.MoneyText(nextCfg.machineCost) + "</color>    <color=#9BA6BC>| Slot " + (slotIndex + 1) + "</color>";
        }

        if (machineUpgradeCurrentDetail != null)
        {
            var currentMachineCost = currentMachine >= 0 && currentMachine < game.Config.machines.Count
                ? game.MoneyText(game.Config.machines[currentMachine].machineCost)
                : "$0";
            machineUpgradeCurrentDetail.text =
                "<color=#B6BED0>Name</color>: <color=#E8EAF0>" + currentName + "</color>\n" +
                "<color=#B6BED0>Piece</color>: <color=#9ED1A8>" + currentValue + "</color>\n" +
                "<color=#B6BED0>Machine</color>: <color=#9ED1A8>" + currentMachineCost + "</color>";
        }

        if (machineUpgradeNewDetail != null)
        {
            machineUpgradeNewDetail.text =
                "<color=#B6BED0>Name</color>: <color=#F1C76A>" + nextCfg.displayName + "</color>\n" +
                "<color=#B6BED0>Piece</color>: <color=#9ED1A8>" + nextValue + "</color>\n" +
                "<color=#B6BED0>Machine</color>: <color=#9ED1A8>" + game.MoneyText(nextCfg.machineCost) + "</color>";
        }

        if (machineUpgradeCurrentIcon != null)
        {
            if (currentMachine >= 0 && currentMachine < game.Config.machines.Count)
            {
                var currentCfg = game.Config.machines[currentMachine];
                machineUpgradeCurrentIcon.sprite = ResolveMachineIcon(currentCfg.id, currentCfg.icon);
                machineUpgradeCurrentIcon.enabled = machineUpgradeCurrentIcon.sprite != null;
                machineUpgradeCurrentIcon.color = Color.white;
            }
            else
            {
                machineUpgradeCurrentIcon.enabled = false;
            }
        }

        if (machineUpgradeNewIcon != null)
        {
            machineUpgradeNewIcon.sprite = ResolveMachineIcon(nextCfg.id, nextCfg.icon);
            machineUpgradeNewIcon.enabled = machineUpgradeNewIcon.sprite != null;
            machineUpgradeNewIcon.color = Color.white;
        }

        machineUpgradeDialog.SetActive(true);
        machineUpgradeDialog.transform.SetAsLastSibling();
    }

    private void ConfirmMachineUpgrade()
    {
        if (game == null || pendingUpgradeSlot < 0)
        {
            CancelMachineUpgrade();
            return;
        }

        game.PurchaseNextMachineForSlot(pendingUpgradeSlot);
        CancelMachineUpgrade();
    }

    private void CancelMachineUpgrade()
    {
        pendingUpgradeSlot = -1;
        pendingUpgradeMachine = -1;
        if (machineUpgradeDialog != null)
        {
            machineUpgradeDialog.SetActive(false);
        }
    }

    private void EnsureCheatDialogUi()
    {
        if (cheatDialog != null)
        {
            return;
        }

        var canvas = titleText != null ? titleText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlay = new GameObject("GF_CheatDialog", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.62f);
        overlayImg.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 240f);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBar.transform.SetParent(panel.transform, false);
        var titleBarRt = titleBar.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.anchoredPosition = Vector2.zero;
        titleBarRt.sizeDelta = new Vector2(0f, 44f);
        var titleBarImg = titleBar.GetComponent<Image>();
        titleBarImg.color = new Color(0.15f, 0.12f, 0.05f, 0.98f);

        CreateText(panel.transform as RectTransform, font, "CHEAT MONEY", 20, new Vector2(20f, -8f), new Vector2(480f, 30f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));
        cheatDialogHintText = CreateText(panel.transform as RectTransform, font, "Type amount then click ADD MONEY.", 13, new Vector2(20f, -56f), new Vector2(480f, 24f), TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.94f, 1f));

        CreateText(panel.transform as RectTransform, font, "Amount", 12, new Vector2(24f, -96f), new Vector2(120f, 24f), TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.94f, 1f));
        cheatAmountInput = CreateInputField(panel.transform as RectTransform, font, new Vector2(120f, -96f), new Vector2(376f, 28f));
        var cheatInputBg = cheatAmountInput != null ? cheatAmountInput.GetComponent<Image>() : null;
        if (cheatInputBg != null)
        {
            cheatInputBg.color = new Color(0.10f, 0.11f, 0.20f, 0.96f);
        }
        var cheatPlaceholder = cheatAmountInput != null && cheatAmountInput.placeholder != null
            ? cheatAmountInput.placeholder as Text
            : null;
        if (cheatPlaceholder != null)
        {
            cheatPlaceholder.text = "Enter amount (example: 100000)";
        }

        cheatDialogAddButton = CreateButton(panel.transform as RectTransform, font, "ADD MONEY", new Vector2(24f, -138f), new Vector2(472f, 38f), new Color(0.25f, 0.20f, 0.06f, 0.98f), 13);
        ApplyButtonVisual(cheatDialogAddButton, new Color(0.25f, 0.20f, 0.06f, 0.98f), new Color(1f, 0.92f, 0.56f, 1f));
        BindButton(cheatDialogAddButton, ApplyTypedCheatMoney);

        cheatDialogCloseButton = CreateButton(panel.transform as RectTransform, font, "CLOSE", new Vector2(24f, -182f), new Vector2(472f, 34f), new Color(0.08f, 0.14f, 0.24f, 0.96f), 13);
        ApplyButtonVisual(cheatDialogCloseButton, new Color(0.08f, 0.14f, 0.24f, 0.96f), new Color(0.74f, 0.90f, 1f, 1f));
        BindButton(cheatDialogCloseButton, HideCheatDialog);

        cheatDialog = overlay;
        cheatDialog.SetActive(false);
    }

    private void SyncCheatDialogDefaults()
    {
        if (cheatAmountInput != null)
        {
            var defaultAmount = cheatDefinition != null ? cheatDefinition.DefaultTypedAmount : 1000d;
            if (defaultAmount < 0d)
            {
                defaultAmount = 0d;
            }

            cheatAmountInput.text = defaultAmount.ToString(CultureInfo.InvariantCulture);
        }

        if (cheatDialogHintText != null)
        {
            cheatDialogHintText.text = "Type amount then click ADD MONEY.";
        }
    }

    private void ShowCheatDialog()
    {
        if (!IsCheatEnabled())
        {
            return;
        }

        EnsureCheatDialogUi();
        if (cheatDialog == null)
        {
            return;
        }

        SyncCheatDialogDefaults();
        cheatDialog.SetActive(true);
        cheatDialog.transform.SetAsLastSibling();
    }

    private void HideCheatDialog()
    {
        if (cheatDialog != null)
        {
            cheatDialog.SetActive(false);
        }
    }

    private void ApplyTypedCheatMoney()
    {
        if (game == null)
        {
            return;
        }

        var raw = SafeText(cheatAmountInput, "0");
        raw = raw.Replace(",", string.Empty);
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount) || amount <= 0d)
        {
            ShowToast("Invalid cheat amount.");
            return;
        }

        var showToast = cheatDefinition == null || cheatDefinition.ShowToastOnApply;
        game.AddCheatMoney(amount, showToast);
    }

    private void EnsureResetConfirmDialogUi()
    {
        if (resetConfirmDialog != null)
        {
            return;
        }

        var canvas = titleText != null ? titleText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlay = new GameObject("GF_ResetConfirmDialog", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.62f);
        overlayImg.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 220f);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBar.transform.SetParent(panel.transform, false);
        var titleBarRt = titleBar.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.anchoredPosition = Vector2.zero;
        titleBarRt.sizeDelta = new Vector2(0f, 44f);
        var titleBarImg = titleBar.GetComponent<Image>();
        titleBarImg.color = new Color(0.15f, 0.12f, 0.05f, 0.98f);

        CreateText(panel.transform as RectTransform, font, "RESET PROGRESS", 20, new Vector2(20f, -8f), new Vector2(480f, 30f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));
        CreateText(panel.transform as RectTransform, font, "This will delete your save and reset all upgrades.\nDo you want to continue?", 14, new Vector2(24f, -78f), new Vector2(472f, 68f), TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.94f, 1f));

        resetConfirmYesButton = CreateButton(panel.transform as RectTransform, font, "RESET", new Vector2(24f, -162f), new Vector2(224f, 40f), new Color(0.31f, 0.09f, 0.10f, 0.98f), 13);
        resetConfirmNoButton = CreateButton(panel.transform as RectTransform, font, "CANCEL", new Vector2(272f, -162f), new Vector2(224f, 40f), new Color(0.08f, 0.14f, 0.24f, 0.96f), 13);
        ApplyButtonVisual(resetConfirmYesButton, new Color(0.31f, 0.09f, 0.10f, 0.98f), new Color(1f, 0.82f, 0.82f, 1f));
        ApplyButtonVisual(resetConfirmNoButton, new Color(0.08f, 0.14f, 0.24f, 0.96f), new Color(0.74f, 0.90f, 1f, 1f));
        BindButton(resetConfirmYesButton, ConfirmReset);
        BindButton(resetConfirmNoButton, CancelReset);

        resetConfirmDialog = overlay;
        resetConfirmDialog.SetActive(false);
    }

    private void EnsureExitConfirmDialogUi()
    {
        if (exitConfirmDialog != null)
        {
            return;
        }

        var canvas = titleText != null ? titleText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlay = new GameObject("GF_ExitConfirmDialog", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.62f);
        overlayImg.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 220f);
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBar.transform.SetParent(panel.transform, false);
        var titleBarRt = titleBar.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.anchoredPosition = Vector2.zero;
        titleBarRt.sizeDelta = new Vector2(0f, 44f);
        var titleBarImg = titleBar.GetComponent<Image>();
        titleBarImg.color = new Color(0.15f, 0.12f, 0.05f, 0.98f);

        CreateText(panel.transform as RectTransform, font, "EXIT GAME", 20, new Vector2(20f, -8f), new Vector2(480f, 30f), TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.46f, 1f));
        CreateText(panel.transform as RectTransform, font, "Do you want to exit the game?", 14, new Vector2(24f, -78f), new Vector2(472f, 68f), TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.94f, 1f));

        exitConfirmYesButton = CreateButton(panel.transform as RectTransform, font, "EXIT", new Vector2(24f, -162f), new Vector2(224f, 40f), new Color(0.31f, 0.09f, 0.10f, 0.98f), 13);
        exitConfirmNoButton = CreateButton(panel.transform as RectTransform, font, "CANCEL", new Vector2(272f, -162f), new Vector2(224f, 40f), new Color(0.08f, 0.14f, 0.24f, 0.96f), 13);
        ApplyButtonVisual(exitConfirmYesButton, new Color(0.31f, 0.09f, 0.10f, 0.98f), new Color(1f, 0.82f, 0.82f, 1f));
        ApplyButtonVisual(exitConfirmNoButton, new Color(0.08f, 0.14f, 0.24f, 0.96f), new Color(0.74f, 0.90f, 1f, 1f));
        BindButton(exitConfirmYesButton, ConfirmExit);
        BindButton(exitConfirmNoButton, CancelExit);

        exitConfirmDialog = overlay;
        exitConfirmDialog.SetActive(false);
    }

    private void ShowResetConfirmDialog()
    {
        EnsureResetConfirmDialogUi();
        if (resetConfirmDialog == null)
        {
            game?.DeleteSaveAndReset();
            return;
        }

        resetConfirmDialog.SetActive(true);
        resetConfirmDialog.transform.SetAsLastSibling();
    }

    private void ConfirmReset()
    {
        CancelReset();
        game?.DeleteSaveAndReset();
        GlueFactoryBootstrap.ForceRebindActiveScene();
        TriggerSceneReloadAfterReset();
    }

    private void CancelReset()
    {
        if (resetConfirmDialog != null)
        {
            resetConfirmDialog.SetActive(false);
        }
    }

    private void ShowExitConfirmDialog()
    {
        EnsureExitConfirmDialogUi();
        if (exitConfirmDialog == null)
        {
            ExitGame();
            return;
        }

        exitConfirmDialog.SetActive(true);
        exitConfirmDialog.transform.SetAsLastSibling();
    }

    private void ConfirmExit()
    {
        CancelExit();
        ExitGame();
    }

    private void CancelExit()
    {
        if (exitConfirmDialog != null)
        {
            exitConfirmDialog.SetActive(false);
        }
    }

    private void TriggerSceneReloadAfterReset()
    {
        if (Application.isPlaying)
        {
            ShowLoadingOverlay("Resetting...");
            StartCoroutine(ReloadActiveSceneNextFrame());
            return;
        }

        ShowLoadingOverlay("Resetting...");
        ReloadActiveSceneNow();
    }

    private System.Collections.IEnumerator ReloadActiveSceneNextFrame()
    {
        yield return null;
        ReloadActiveSceneNow();
    }

    private void ReloadActiveSceneNow()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            HideLoadingOverlay();
            return;
        }

        if (Application.isPlaying)
        {
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
                return;
            }

            if (!string.IsNullOrWhiteSpace(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(activeScene.path))
            {
                EditorSceneManager.LoadSceneInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif

            HideLoadingOverlay();
            return;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(activeScene.path))
        {
            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            return;
        }
#endif

        HideLoadingOverlay();
    }

    private void ShowStartupLoadingOverlay()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        startupWorldManager = FindFirstObjectByType<GlueFactoryWorldManager>();
        ShowLoadingOverlay("Loading...", true);
        StartCoroutine(HideStartupLoadingOverlayWhenReady());
    }

    private System.Collections.IEnumerator HideStartupLoadingOverlayWhenReady()
    {
        var timeout = 3f;
        var elapsed = 0f;
        while (elapsed < timeout)
        {
            var ready = startupWorldManager == null || startupWorldManager.IsMainCameraReadyForGameplay();
            if (ready)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.15f);
        HideLoadingOverlay();
    }

    private void EnsureLoadingOverlayUi()
    {
        if (loadingOverlay != null)
        {
            return;
        }

        var canvas = titleText != null ? titleText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlay = new GameObject("GF_LoadingOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
        overlayImg.raycastTarget = true;

        var bgGo = new GameObject("StartupBackground", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(overlay.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        loadingStartupBackgroundImage = bgGo.GetComponent<Image>();
        loadingStartupBackgroundImage.raycastTarget = false;
        loadingStartupBackgroundImage.enabled = false;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingOverlayText = CreateText(overlay.transform, font, "Loading", 22, Vector2.zero, new Vector2(600f, 64f), TextAnchor.MiddleCenter, new Color(0.92f, 0.92f, 1f, 1f));
        if (loadingOverlayText != null)
        {
            var textRt = loadingOverlayText.rectTransform;
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
        }

        loadingOverlay = overlay;
        loadingOverlay.SetActive(false);
    }

    private void ShowLoadingOverlay(string message, bool showStartupBackground = false)
    {
        EnsureLoadingOverlayUi();
        if (loadingOverlay == null)
        {
            return;
        }

        loadingOverlayVisible = true;
        loadingOverlayTimer = 0f;
        loadingOverlayDotCount = 0;
        loadingOverlay.SetActive(true);
        loadingOverlay.transform.SetAsLastSibling();
        if (loadingStartupBackgroundImage != null)
        {
            loadingStartupBackgroundImage.enabled = showStartupBackground;
            loadingStartupBackgroundImage.sprite = loadingStartupBackgroundSprite;
            loadingStartupBackgroundImage.color = loadingStartupBackgroundTint;
        }
        if (loadingOverlayText != null)
        {
            loadingOverlayBaseText = string.IsNullOrWhiteSpace(message) ? "Loading" : message.TrimEnd('.');
            loadingOverlayText.text = loadingOverlayBaseText;
        }
    }

    private void HideLoadingOverlay()
    {
        loadingOverlayVisible = false;
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
        if (loadingStartupBackgroundImage != null)
        {
            loadingStartupBackgroundImage.enabled = false;
        }
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

    private void HideLegacyWorldSlotCards()
    {
        for (var i = 0; i < slotCardBackgrounds.Count; i++)
        {
            if (slotCardBackgrounds[i] != null)
            {
                slotCardBackgrounds[i].gameObject.SetActive(false);
            }
        }

        for (var i = 0; i < slotSelectButtons.Count; i++)
        {
            if (slotSelectButtons[i] != null)
            {
                slotSelectButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void CompactTopArea()
    {
        if (selectedSlotText != null)
        {
            selectedSlotText.gameObject.SetActive(false);
        }

        if (selectedMachineText != null)
        {
            selectedMachineText.gameObject.SetActive(false);
        }

        if (upgradesTab != null)
        {
            upgradesTab.anchoredPosition = new Vector2(0f, TabsTopOffset);
        }

        if (machinesTab != null)
        {
            machinesTab.anchoredPosition = new Vector2(0f, TabsTopOffset);
        }
    }

    private void ApplyHeaderAndUpgradeTheme()
    {
        // Header strip styling
        var header = titleText != null ? titleText.transform.parent as RectTransform : null;
        if (header != null)
        {
            var headerImage = header.GetComponent<Image>();
            if (headerImage != null)
            {
                headerImage.color = new Color(0.13f, 0.14f, 0.17f, 0.98f);
            }
        }

        if (titleText != null)
        {
            titleText.text = string.Empty;
            titleText.gameObject.SetActive(false);
        }

        if (moneyText != null)
        {
            moneyText.color = new Color(0.18f, 0.86f, 0.38f, 1f);
            var moneyRt = moneyText.rectTransform;
            if (moneyRt != null)
            {
                moneyRt.anchorMin = new Vector2(0f, 1f);
                moneyRt.anchorMax = new Vector2(0f, 1f);
                moneyRt.pivot = new Vector2(0f, 1f);
                moneyRt.anchoredPosition = new Vector2(12f, -10f);
                moneyRt.sizeDelta = new Vector2(240f, 46f);
            }
        }

        if (perClickText != null)
        {
            perClickText.gameObject.SetActive(false);
        }

        if (totalEarnedText != null)
        {
            totalEarnedText.gameObject.SetActive(false);
        }

        if (autoIncomeText != null)
        {
            autoIncomeText.gameObject.SetActive(true);
            autoIncomeText.color = new Color(0.78f, 0.78f, 0.80f, 1f);
            var rt = autoIncomeText.rectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -10f);
                rt.sizeDelta = new Vector2(220f, 46f);
            }
        }

        ApplyButtonVisual(cheatButton, new Color(0.08f, 0.18f, 0.30f, 0.98f), new Color(0.72f, 0.90f, 1f, 1f));
        SetButtonLabel(cheatButton, "CHEAT");
        ApplyButtonVisual(saveButton, new Color(0.18f, 0.27f, 0.10f, 0.98f), new Color(0.84f, 0.95f, 0.72f, 1f));
        ApplyButtonVisual(deleteSaveButton, new Color(0.31f, 0.09f, 0.10f, 0.98f), new Color(1f, 0.82f, 0.82f, 1f));
        SetButtonLabel(deleteSaveButton, "RESET");
        ApplyButtonVisual(exitButton, new Color(0.12f, 0.12f, 0.16f, 0.98f), new Color(0.90f, 0.90f, 0.95f, 1f));
        SetButtonLabel(exitButton, "EXIT");
        RefreshCheatUiState();
        LayoutHeaderActionButtons();

        // Upgrades section styling
        var rightPanel = upgradesTab != null && upgradesTab.parent is RectTransform rtParent ? rtParent : null;
        if (rightPanel != null)
        {
            var rightPanelImage = rightPanel.GetComponent<Image>();
            if (rightPanelImage != null)
            {
                rightPanelImage.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);
            }
            var tabRow = FindByName(rightPanel, "TabRow") as RectTransform;
            if (tabRow != null)
            {
                tabRow.anchorMin = new Vector2(0f, 1f);
                tabRow.anchorMax = new Vector2(0f, 1f);
                tabRow.pivot = new Vector2(0f, 1f);
                tabRow.anchoredPosition = new Vector2(0f, -44f);
                tabRow.sizeDelta = new Vector2(380f, 46f);
            }

            var upgradesHeader = FindByName(rightPanel, "GF_UpgradesHeaderLabel");
            Text upgradesHeaderText;
            if (upgradesHeader == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                upgradesHeaderText = CreateText(rightPanel, font, "UPGRADES", 24, new Vector2(0f, -4f), new Vector2(380f, 34f), TextAnchor.MiddleCenter, new Color(0.92f, 0.93f, 0.96f, 1f));
                upgradesHeaderText.name = "GF_UpgradesHeaderLabel";
            }
            else
            {
                upgradesHeaderText = upgradesHeader.GetComponent<Text>();
            }

            if (upgradesHeaderText != null)
            {
                upgradesHeaderText.text = "UPGRADES";
                upgradesHeaderText.fontStyle = FontStyle.Bold;
                upgradesHeaderText.fontSize = 24;
                upgradesHeaderText.alignment = TextAnchor.MiddleCenter;
                upgradesHeaderText.color = new Color(0.92f, 0.93f, 0.96f, 1f);
                upgradesHeaderText.gameObject.SetActive(true);
                var headerRt = upgradesHeaderText.rectTransform;
                headerRt.anchorMin = new Vector2(0f, 1f);
                headerRt.anchorMax = new Vector2(0f, 1f);
                headerRt.pivot = new Vector2(0f, 1f);
                headerRt.anchoredPosition = new Vector2(0f, -4f);
                headerRt.sizeDelta = new Vector2(380f, 34f);
            }
        }

        SetButtonLabel(upgradesTabButton, "FACTORY");
        SetButtonLabel(machinesTabButton, "MACHINE");
        StyleUpgradeCard(clickLevelText, clickDescText, clickCostText);
        StyleUpgradeCard(conveyorLevelText, conveyorDescText, conveyorCostText);
        StyleUpgradeCard(exportLevelText, exportDescText, exportCostText);
        StyleUpgradeCard(speedLevelText, speedDescText, speedCostText);
    }

    private void ApplyEditorPreviewTheme()
    {
        AutoResolveReferences();
        EnsureExitButtonInHeader();
        ResolveUpgradeCardReferences();
        UpdateMachinesTabScrollState();
        CompactTopArea();
        ApplyHeaderAndUpgradeTheme();
        ApplyEditorPreviewUpgradeTexts();
        if (upgradesTab != null) upgradesTab.gameObject.SetActive(true);
        if (machinesTab != null) machinesTab.gameObject.SetActive(false);
        SetTabButtonVisuals(true);
    }

    private void ApplyEditorPreviewUpgradeTexts()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (clickLevelText != null) clickLevelText.text = BuildUpgradeTitle("click", "Player Glue Value", 0);
        if (clickDescText != null) clickDescText.text = BuildUpgradeDescription("click", "Increase the sell price of manually produced glue.", 0);
        if (clickCostText != null) clickCostText.text = "Lv 0 | Price: " + MoneyTextPreview(GetPreviewUpgradeBaseCost("click", 50d));

        if (conveyorLevelText != null) conveyorLevelText.text = BuildUpgradeTitle("conveyor", "Conveyor Slot", 0);
        if (conveyorDescText != null) conveyorDescText.text = BuildUpgradeDescription("conveyor", "Unlock a new conveyor and double manual glue production.", 0);
        if (conveyorCostText != null) conveyorCostText.text = "Lv 0 | Buy " + MoneyTextPreview(GetPreviewUpgradeBaseCost("conveyor", 1200d));

        if (exportLevelText != null) exportLevelText.text = BuildUpgradeTitle("boost", "Export Value", 0);
        if (exportDescText != null) exportDescText.text = BuildUpgradeDescription("boost", "Boost all sale values.", 0);
        if (exportCostText != null) exportCostText.text = "Lv 0 | Buy " + MoneyTextPreview(GetPreviewUpgradeBaseCost("boost", 500d));

        if (speedLevelText != null) speedLevelText.text = BuildUpgradeTitle("speed", "Machine Production Speed", 0);
        if (speedDescText != null) speedDescText.text = BuildUpgradeDescription("speed", "Increase the rate of automatic production.", 0);
        if (speedCostText != null) speedCostText.text = "Lv 0 | Buy " + MoneyTextPreview(GetPreviewUpgradeBaseCost("speed", 1000d));

        SetUpgradePreviewButton(clickBuyButton);
        SetUpgradePreviewButton(conveyorBuyButton);
        SetUpgradePreviewButton(exportBuyButton);
        SetUpgradePreviewButton(speedBuyButton);
    }

    private double GetPreviewUpgradeBaseCost(string upgradeId, double fallback)
    {
        if (game == null || game.Config == null)
        {
            return fallback;
        }

        switch (upgradeId)
        {
            case "click":
                return game.Config.clickValueUpgrade.baseCost;
            case "conveyor":
                return game.Config.conveyorUpgrade.baseCost;
            case "boost":
                return game.Config.factoryBoostUpgrade.baseCost;
            case "speed":
                return game.Config.speedUpgrade.baseCost;
            default:
                return fallback;
        }
    }

    private string MoneyTextPreview(double amount)
    {
        return game != null ? game.MoneyText(amount) : "$" + amount.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static void SetUpgradePreviewButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = true;
        SetButtonLabel(button, "BUY");
        ApplyButtonVisual(button, new Color(0.25f, 0.20f, 0.06f, 0.98f), new Color(1f, 0.92f, 0.56f, 1f));
    }

    private static void StyleUpgradeCard(Text levelText, Text descText, Text costText)
    {
        if (levelText == null)
        {
            return;
        }

        var card = levelText.transform.parent as RectTransform;
        if (card != null)
        {
            var cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = new Color(0.07f, 0.08f, 0.16f, 0.95f);
            }

            // Increase inner padding for a more balanced card layout.
            card.sizeDelta = new Vector2(card.sizeDelta.x, 188f);
        }

        levelText.color = new Color(0.92f, 0.92f, 0.95f, 1f);
        levelText.fontStyle = FontStyle.Bold;
        levelText.fontSize = 14;
        levelText.rectTransform.anchoredPosition = new Vector2(14f, -12f);
        levelText.rectTransform.sizeDelta = new Vector2(336f, 30f);

        if (descText != null)
        {
            descText.color = new Color(0.70f, 0.72f, 0.78f, 1f);
            descText.fontSize = 12;
            descText.lineSpacing = 1.2f;
            descText.rectTransform.anchoredPosition = new Vector2(14f, -50f);
            descText.rectTransform.sizeDelta = new Vector2(336f, 42f);
        }

        if (costText != null)
        {
            costText.color = new Color(0.91f, 0.75f, 0.25f, 1f);
            costText.fontSize = 12;
            costText.rectTransform.anchoredPosition = new Vector2(14f, -98f);
            costText.rectTransform.sizeDelta = new Vector2(336f, 24f);
        }

        var buyButton = FindButton(levelText.transform.parent, "BuyButton");
        if (buyButton != null)
        {
            var rt = buyButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(14f, -126f);
                rt.sizeDelta = new Vector2(336f, 36f);
            }
        }
    }

    private void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null)
        {
            return;
        }

        toastPanel.SetActive(true);
        toastPanel.transform.SetAsLastSibling();
        toastText.text = message;
        ApplyToastStyle(InferToastType(message));
        toastVisible = true;
        toastTimer = 2.5f;
    }

    private void ShowUpgradesTab()
    {
        if (upgradesTab != null) upgradesTab.gameObject.SetActive(true);
        if (machinesTab != null) machinesTab.gameObject.SetActive(false);
        SetTabButtonVisuals(true);
        GlueFactoryAudioManager.PlaySfx("tab_switch");
        ShowToast("Tab: Factory");
    }

    private void ShowMachinesTab()
    {
        if (upgradesTab != null) upgradesTab.gameObject.SetActive(false);
        if (machinesTab != null) machinesTab.gameObject.SetActive(true);
        UpdateMachinesTabScrollState();
        SetTabButtonVisuals(false);
        GlueFactoryAudioManager.PlaySfx("tab_switch");
        ShowToast("Tab: Machine");
    }

    private enum ToastType
    {
        Info,
        Warning,
        Success,
        Error
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

        if (lower.Contains("not enough") || lower.Contains("locked") || lower.Contains("no machine") || lower.Contains("need") || lower.Contains("already") || lower.Contains("occupied"))
        {
            return ToastType.Warning;
        }

        if (lower.Contains("saved") || lower.Contains("installed") || lower.Contains("sold") || lower.Contains("upgraded") || lower.Contains("produced") || lower.Contains("loaded") || lower.Contains("bought"))
        {
            return ToastType.Success;
        }

        return ToastType.Info;
    }

    private static void BindButton(Button button, Action action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            GlueFactoryAudioManager.PlaySfx("ui_click");
            action?.Invoke();
        });
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private void SetTabButtonVisuals(bool upgradesActive)
    {
        ApplyButtonVisual(
            upgradesTabButton,
            upgradesActive ? new Color(0.28f, 0.20f, 0.05f, 0.98f) : new Color(0.10f, 0.11f, 0.20f, 0.94f),
            upgradesActive ? new Color(1f, 0.90f, 0.45f, 1f) : new Color(0.82f, 0.82f, 0.9f, 1f));
        ApplyButtonVisual(
            machinesTabButton,
            upgradesActive ? new Color(0.08f, 0.14f, 0.24f, 0.94f) : new Color(0.08f, 0.20f, 0.30f, 0.98f),
            upgradesActive ? new Color(0.72f, 0.82f, 0.92f, 1f) : new Color(0.72f, 0.92f, 1f, 1f));
    }

    private static void ApplyButtonVisual(Button button, Color bg, Color fg)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = bg;
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.color = fg;
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = label;
        }
    }

    private static void SetUpgradeButtonState(Button button, bool isMaxed)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = !isMaxed;
        if (isMaxed)
        {
            ApplyButtonVisual(button, new Color(0.16f, 0.16f, 0.20f, 0.96f), new Color(0.70f, 0.70f, 0.75f, 1f));
        }
        else
        {
            ApplyButtonVisual(button, new Color(0.25f, 0.20f, 0.06f, 0.98f), new Color(1f, 0.92f, 0.56f, 1f));
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = isMaxed ? "MAX" : "BUY";
        }
    }

    private void ResolveCheatSource()
    {
        if (cheatDefinition == null)
        {
            cheatDefinition = FindFirstObjectByType<GlueFactoryCheatDefinition>(FindObjectsInactive.Include);
        }
    }

    private bool IsCheatEnabled()
    {
        ResolveCheatSource();
        return cheatDefinition != null && cheatDefinition.EnableCheatButton && cheatDefinition.ShowCheatButton;
    }

    private void RefreshCheatUiState()
    {
        ResolveCheatSource();
        var enabled = IsCheatEnabled();
        if (cheatButton != null)
        {
            cheatButton.gameObject.SetActive(enabled);
        }

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(cheatDefinition == null || cheatDefinition.ShowSaveButton);
        }
        if (deleteSaveButton != null)
        {
            deleteSaveButton.gameObject.SetActive(cheatDefinition == null || cheatDefinition.ShowResetButton);
        }
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(cheatDefinition == null || cheatDefinition.ShowExitButton);
        }

        if (!enabled && cheatDialog != null)
        {
            cheatDialog.SetActive(false);
        }
    }

    private void ResolveProductSources()
    {
        if (productCatalog == null)
        {
            productCatalog = FindFirstObjectByType<GlueProductCatalog>();
        }

        if (productDefinition != null)
        {
            return;
        }

        if (productCatalog != null)
        {
            productDefinition = productCatalog.GetComponentInChildren<GlueProductDefinition>(true);
        }

        if (productDefinition == null)
        {
            // Include inactive roots; master data objects are often disabled in scene.
            var defs = FindObjectsByType<GlueProductDefinition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GlueProductDefinition best = null;
            var bestScore = -1;
            for (var i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                if (def == null || def.Products == null)
                {
                    continue;
                }

                var score = def.Products.Count;
                for (var j = 0; j < def.Products.Count; j++)
                {
                    if (def.Products[j] != null && def.Products[j].UiIcon != null)
                    {
                        score += 1000;
                        break;
                    }
                }

                if (score > bestScore)
                {
                    best = def;
                    bestScore = score;
                }
            }

            productDefinition = best;
        }
    }

    private void ResolveUpgradeSource()
    {
        if (upgradeDefinition == null)
        {
            upgradeDefinition = FindFirstObjectByType<GlueUpgradeDefinition>();
        }
    }

    private void EnsureProductToolUi()
    {
        if (machineShopContent == null)
        {
            return;
        }

        if (productToolToggleButton == null)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var btn = CreateButton(machineShopContent, font, "PRODUCT TOOL", new Vector2(8, -8), new Vector2(132, 28), new Color(0.24f, 0.16f, 0.05f, 0.98f), 10);
            BindButton(btn, ToggleProductToolPanel);
            ApplyButtonVisual(btn, new Color(0.24f, 0.16f, 0.05f, 0.98f), new Color(1f, 0.9f, 0.5f, 1f));
            productToolToggleButton = btn;
        }

        if (productToolPanel != null)
        {
            return;
        }

        var fontRef = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var panelRt = CreatePanel(machineShopContent, "ProductToolPanel", new Vector2(8, -40), new Vector2(336, 430), new Color(0.06f, 0.07f, 0.12f, 0.98f));
        panelRt.gameObject.SetActive(false);
        productToolPanel = panelRt.gameObject;

        CreateText(panelRt, fontRef, "PRODUCT TOOL", 12, new Vector2(10, -8), new Vector2(220, 20), TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.5f, 1f));
        var closeBtn = CreateButton(panelRt, fontRef, "X", new Vector2(304, -6), new Vector2(24, 20), new Color(0.35f, 0.12f, 0.12f, 0.96f), 10);
        BindButton(closeBtn, () => productToolPanel.SetActive(false));

        var prevBtn = CreateButton(panelRt, fontRef, "< PREV", new Vector2(10, -34), new Vector2(72, 24), new Color(0.13f, 0.18f, 0.30f, 0.96f), 9);
        var nextBtn = CreateButton(panelRt, fontRef, "NEXT >", new Vector2(254, -34), new Vector2(72, 24), new Color(0.13f, 0.18f, 0.30f, 0.96f), 9);
        productIndexText = CreateText(panelRt, fontRef, "0 / 0", 10, new Vector2(88, -34), new Vector2(160, 24), TextAnchor.MiddleCenter, Color.white);
        BindButton(prevBtn, SelectPreviousProduct);
        BindButton(nextBtn, SelectNextProduct);
        productListText = CreateText(panelRt, fontRef, "", 9, new Vector2(10, -326), new Vector2(316, 94), TextAnchor.UpperLeft, new Color(0.8f, 0.85f, 0.92f, 1f));

        CreateLabeledField(panelRt, fontRef, "Id", new Vector2(10, -66), out productIdInput);
        CreateLabeledField(panelRt, fontRef, "Name", new Vector2(10, -98), out productNameInput);
        CreateLabeledField(panelRt, fontRef, "Piece Value", new Vector2(10, -130), out pieceValueInput);
        CreateLabeledField(panelRt, fontRef, "Machine Cost", new Vector2(10, -162), out machineCostInput);
        CreateLabeledField(panelRt, fontRef, "Shop Order", new Vector2(10, -194), out shopOrderInput);

        includeInShopToggle = CreateToggle(panelRt, fontRef, "Include In Shop", new Vector2(10, -224), true);

        var addBtn = CreateButton(panelRt, fontRef, "ADD NEW", new Vector2(10, -254), new Vector2(100, 28), new Color(0.10f, 0.26f, 0.18f, 0.96f), 10);
        var dupBtn = CreateButton(panelRt, fontRef, "DUPLICATE", new Vector2(118, -254), new Vector2(100, 28), new Color(0.13f, 0.18f, 0.30f, 0.96f), 10);
        var delBtn = CreateButton(panelRt, fontRef, "DELETE", new Vector2(226, -254), new Vector2(100, 28), new Color(0.34f, 0.10f, 0.12f, 0.96f), 10);
        var applyBtn = CreateButton(panelRt, fontRef, "APPLY TO GAME", new Vector2(10, -288), new Vector2(316, 34), new Color(0.28f, 0.20f, 0.05f, 0.98f), 11);

        BindButton(addBtn, AddProductFromTool);
        BindButton(dupBtn, DuplicateSelectedProductFromTool);
        BindButton(delBtn, DeleteSelectedProductFromTool);
        BindButton(applyBtn, ApplyEditedProductToGame);

        RefreshProductDropdown();
    }

    private static void CreateLabeledField(Transform parent, Font font, string label, Vector2 pos, out InputField input)
    {
        CreateText(parent, font, label, 10, pos, new Vector2(100, 20), TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.9f, 1f));
        input = CreateInputField(parent, font, new Vector2(108, pos.y), new Vector2(218, 24));
    }

    private static InputField CreateInputField(Transform parent, Font font, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8, 2);
        textRt.offsetMax = new Vector2(-8, -2);
        var text = textGo.GetComponent<Text>();
        text.font = font;
        text.fontSize = 10;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        var placeholderRt = placeholderGo.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = new Vector2(8, 2);
        placeholderRt.offsetMax = new Vector2(-8, -2);
        var placeholder = placeholderGo.GetComponent<Text>();
        placeholder.font = font;
        placeholder.fontSize = 10;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(0.6f, 0.6f, 0.65f, 0.8f);
        placeholder.text = "...";

        var input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private static Toggle CreateToggle(Transform parent, Font font, string label, Vector2 pos, bool initialValue)
    {
        var go = new GameObject("IncludeToggle", typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220, 20);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.pivot = new Vector2(0, 0.5f);
        bgRt.sizeDelta = new Vector2(16, 16);
        var bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0.16f, 0.16f, 0.22f, 1f);

        var ck = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        ck.transform.SetParent(bg.transform, false);
        var ckRt = ck.GetComponent<RectTransform>();
        ckRt.anchorMin = Vector2.zero;
        ckRt.anchorMax = Vector2.one;
        ckRt.offsetMin = new Vector2(3, 3);
        ckRt.offsetMax = new Vector2(-3, -3);
        var ckImage = ck.GetComponent<Image>();
        ckImage.color = new Color(0.36f, 0.85f, 0.45f, 1f);

        var lbl = CreateText(go.transform, font, label, 10, new Vector2(22, -1), new Vector2(180, 18), TextAnchor.MiddleLeft, Color.white);
        var toggle = go.GetComponent<Toggle>();
        toggle.graphic = ckImage;
        toggle.targetGraphic = bgImage;
        toggle.isOn = initialValue;
        return toggle;
    }

    private void ToggleProductToolPanel()
    {
        EnsureProductToolUi();
        if (productToolPanel == null)
        {
            return;
        }

        var next = !productToolPanel.activeSelf;
        productToolPanel.SetActive(next);
        if (next)
        {
            RefreshProductDropdown();
        }
    }

    private void RefreshProductDropdown()
    {
        ResolveProductSources();
        if (productIndexText == null)
        {
            return;
        }

        if (productDefinition == null || productDefinition.Products.Count == 0)
        {
            currentProductIndex = 0;
            productIndexText.text = "0 / 0";
            if (productListText != null) productListText.text = "No products";
            LoadSelectedProductIntoFields();
            return;
        }

        currentProductIndex = Mathf.Clamp(currentProductIndex, 0, productDefinition.Products.Count - 1);
        productIndexText.text = (currentProductIndex + 1) + " / " + productDefinition.Products.Count + "  " + productDefinition.Products[currentProductIndex].DisplayName;
        if (productListText != null)
        {
            var lines = "";
            for (var i = 0; i < productDefinition.Products.Count; i++)
            {
                var prefix = i == currentProductIndex ? "> " : "  ";
                lines += prefix + (i + 1) + ". " + productDefinition.Products[i].DisplayName + "\n";
            }

            productListText.text = lines;
        }
        LoadSelectedProductIntoFields();
    }

    private void LoadSelectedProductIntoFields()
    {
        if (!TryGetSelectedProduct(out var entry, out _))
        {
            SetProductFields("glue_product", "Glue Product", "1", "25", "0", true);
            return;
        }

        SetProductFields(
            entry.ProductId,
            entry.DisplayName,
            entry.PieceValue.ToString(CultureInfo.InvariantCulture),
            entry.MachineCost.ToString(CultureInfo.InvariantCulture),
            entry.ShopOrder.ToString(CultureInfo.InvariantCulture),
            entry.IncludeInShop);
    }

    private void SetProductFields(string id, string name, string piece, string cost, string order, bool includeInShop)
    {
        if (productIdInput != null) productIdInput.text = id;
        if (productNameInput != null) productNameInput.text = name;
        if (pieceValueInput != null) pieceValueInput.text = piece;
        if (machineCostInput != null) machineCostInput.text = cost;
        if (shopOrderInput != null) shopOrderInput.text = order;
        if (includeInShopToggle != null) includeInShopToggle.isOn = includeInShop;
    }

    private bool TryGetSelectedProduct(out GlueProductDefinition.ProductEntry entry, out int index)
    {
        entry = null;
        index = -1;
        if (productDefinition == null)
        {
            return false;
        }

        var list = productDefinition.Products;
        if (list == null || list.Count == 0)
        {
            return false;
        }

        index = Mathf.Clamp(currentProductIndex, 0, list.Count - 1);
        entry = list[index];
        return entry != null;
    }

    private void AddProductFromTool()
    {
        ResolveProductSources();
        if (productDefinition == null)
        {
            ShowToast("Product tool: no GlueProductDefinition found.");
            return;
        }

        var order = productDefinition.Products.Count;
        productDefinition.AddProduct("new_product_" + order, "New Product", 1d, 100d, order, true, null);
        currentProductIndex = Mathf.Max(0, productDefinition.Products.Count - 1);
        RefreshProductDropdown();
        ShowToast("Product added.");
    }

    private void DuplicateSelectedProductFromTool()
    {
        if (!TryGetSelectedProduct(out var entry, out _))
        {
            ShowToast("Select a product first.");
            return;
        }

        var baseId = entry.ProductId + "_copy";
        var newId = baseId;
        var n = 2;
        while (productDefinition.HasProductId(newId))
        {
            newId = baseId + "_" + n;
            n++;
        }

        productDefinition.AddProduct(newId, entry.DisplayName + " Copy", entry.PieceValue, entry.MachineCost, productDefinition.Products.Count, entry.IncludeInShop, entry.UiIcon);
        currentProductIndex = Mathf.Max(0, productDefinition.Products.Count - 1);
        RefreshProductDropdown();
        ShowToast("Product duplicated.");
    }

    private void DeleteSelectedProductFromTool()
    {
        if (!TryGetSelectedProduct(out _, out var index))
        {
            ShowToast("Select a product first.");
            return;
        }

        productDefinition.Products.RemoveAt(index);
        currentProductIndex = Mathf.Clamp(currentProductIndex, 0, Mathf.Max(0, productDefinition.Products.Count - 1));
        RefreshProductDropdown();
        ApplyCatalogToGame();
        ShowToast("Product removed.");
    }

    private void SelectPreviousProduct()
    {
        if (productDefinition == null || productDefinition.Products.Count == 0)
        {
            return;
        }

        currentProductIndex--;
        if (currentProductIndex < 0)
        {
            currentProductIndex = productDefinition.Products.Count - 1;
        }

        RefreshProductDropdown();
    }

    private void SelectNextProduct()
    {
        if (productDefinition == null || productDefinition.Products.Count == 0)
        {
            return;
        }

        currentProductIndex++;
        if (currentProductIndex >= productDefinition.Products.Count)
        {
            currentProductIndex = 0;
        }

        RefreshProductDropdown();
    }

    private void ApplyEditedProductToGame()
    {
        if (!TryGetSelectedProduct(out var entry, out _))
        {
            ShowToast("Select a product first.");
            return;
        }

        var id = SafeText(productIdInput, entry.ProductId);
        var name = SafeText(productNameInput, entry.DisplayName);
        if (!TryParseDouble(pieceValueInput, entry.PieceValue, out var piece))
        {
            ShowToast("Piece value invalid.");
            return;
        }

        if (!TryParseDouble(machineCostInput, entry.MachineCost, out var cost))
        {
            ShowToast("Machine cost invalid.");
            return;
        }

        if (!int.TryParse(SafeText(shopOrderInput, entry.ShopOrder.ToString(CultureInfo.InvariantCulture)), out var order))
        {
            order = entry.ShopOrder;
        }

        var include = includeInShopToggle == null || includeInShopToggle.isOn;
        entry.SetData(id, name, piece, cost, order, include, entry.UiIcon);

        ApplyCatalogToGame();
        RefreshProductDropdown();
        ShowToast("Product applied to game.");
    }

    private void ApplyCatalogToGame()
    {
        ResolveProductSources();
        if (game == null)
        {
            RebuildMachineShop();
            return;
        }

        if (productCatalog != null)
        {
            productCatalog.ApplyTo(game.Config);
        }
        else if (productDefinition != null)
        {
            var list = productDefinition.Products;
            list.Sort((a, b) => a.ShopOrder.CompareTo(b.ShopOrder));
            game.Config.machines.Clear();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].IncludeInShop)
                {
                    game.Config.machines.Add(list[i].ToMachineConfig());
                }
            }
        }

        game.OnMachineCatalogChanged();
        RebuildMachineShop();
        Refresh();
    }

    private static bool TryParseDouble(InputField field, double fallback, out double value)
    {
        var text = SafeText(field, fallback.ToString(CultureInfo.InvariantCulture));
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string SafeText(InputField field, string fallback)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text))
        {
            return fallback;
        }

        return field.text.Trim();
    }

    private static void SetSlotProgress(Image fillImage, float progress01)
    {
        if (fillImage == null)
        {
            return;
        }

        var v = Mathf.Clamp01(progress01);

        // Preferred mode: filled image.
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = v;

        // Fallback mode for layouts where Image type is overridden.
        var rt = fillImage.rectTransform;
        if (rt != null && rt.parent is RectTransform parentRt)
        {
            var parentWidth = parentRt.rect.width;
            if (parentWidth > 0f)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(parentWidth * v, rt.sizeDelta.y);
            }
        }
    }

    private string GetUpgradeDescription(string id, string fallback)
    {
        ResolveUpgradeSource();
        return upgradeDefinition == null ? fallback : upgradeDefinition.GetDescription(id, fallback);
    }

    private string GetUpgradeDisplayName(string id, string fallback)
    {
        ResolveUpgradeSource();
        return upgradeDefinition == null ? fallback : upgradeDefinition.GetDisplayName(id, fallback);
    }

    private string BuildUpgradeTitle(string id, string fallbackName, int level)
    {
        var baseName = GetUpgradeDisplayName(id, fallbackName);
        var idx = baseName.IndexOf(" (Current", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            baseName = baseName.Substring(0, idx).TrimEnd();
        }

        var current = FormatUpgradeCurrentValue(id, level);
        if (string.IsNullOrWhiteSpace(current))
        {
            return baseName;
        }

        return baseName;
    }

    private string BuildUpgradeDescription(string id, string fallbackDescription, int level)
    {
        var baseDesc = GetUpgradeDescription(id, fallbackDescription);
        var current = FormatUpgradeCurrentValue(id, level);
        if (string.IsNullOrWhiteSpace(current))
        {
            return baseDesc;
        }

        return baseDesc + "\nCurrent: " + current;
    }

    private string FormatUpgradeCurrentValue(string id, int level)
    {
        ResolveUpgradeSource();
        var lv = Mathf.Max(0, level);

        var start = 0d;
        var step = 0d;
        var unit = string.Empty;
        var asPercent = false;

        if (upgradeDefinition != null && upgradeDefinition.TryGetEffectInfo(id, out var cfgStart, out var cfgStep, out var cfgUnit, out var cfgAsPercent))
        {
            start = cfgStart;
            step = cfgStep;
            unit = cfgUnit ?? string.Empty;
            asPercent = cfgAsPercent;
            if (Math.Abs(start) < 0.0001d && Math.Abs(step) < 0.0001d && string.IsNullOrWhiteSpace(unit))
            {
                switch (id)
                {
                    case "click":
                        start = 1d;
                        step = 1d;
                        unit = "/per click";
                        break;
                    case "conveyor":
                        start = 1d;
                        step = 1d;
                        unit = "slots";
                        break;
                    case "boost":
                        start = 0d;
                        step = 2d;
                        unit = "%";
                        asPercent = true;
                        break;
                    case "speed":
                        start = 5d;
                        step = -1d;
                        unit = "s/cycle";
                        break;
                }
            }
        }
        else
        {
            switch (id)
            {
                case "click":
                    start = 1d;
                    step = 1d;
                    unit = "/per click";
                    break;
                case "conveyor":
                    start = 1d;
                    step = 1d;
                    unit = "slots";
                    break;
                case "boost":
                    start = 0d;
                    step = 2d;
                    unit = "%";
                    asPercent = true;
                    break;
                case "speed":
                    start = 5d;
                    step = -1d;
                    unit = "s/cycle";
                    break;
                default:
                    return string.Empty;
            }
        }

        var value = start + step * lv;
        if (id == "speed")
        {
            if (game != null && game.Config != null)
            {
                var cfg = game.Config;
                value = Mathf.Max(
                    cfg.minimumMachineIntervalSeconds,
                    cfg.baseMachineIntervalSeconds - lv * cfg.machineIntervalStepSeconds);
            }

            unit = "s/cycle";
        }

        if (id == "click")
        {
            return game != null ? game.MoneyText(value) + unit : "$" + value.ToString("0.##", CultureInfo.InvariantCulture) + unit;
        }

        if (id == "conveyor")
        {
            var slots = Mathf.Max(1, Mathf.RoundToInt((float)value));
            return slots + " " + unit;
        }

        if (asPercent || id == "boost")
        {
            var percent = value;
            var sign = percent >= 0d ? "+" : string.Empty;
            return sign + percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        if (id == "speed")
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + unit;
        }

        return value.ToString("0.##", CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(unit) ? string.Empty : " " + unit);
    }

    private Sprite ResolveMachineIcon(string machineId, Sprite configIcon)
    {
        if (configIcon != null)
        {
            return configIcon;
        }

        ResolveProductSources();
        var icon = FindIconInDefinition(productDefinition, machineId);
        if (icon != null)
        {
            return icon;
        }

        // Fallback across all definitions in case active source does not contain icons.
        var defs = FindObjectsByType<GlueProductDefinition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < defs.Length; i++)
        {
            icon = FindIconInDefinition(defs[i], machineId);
            if (icon != null)
            {
                return icon;
            }
        }

        return null;
    }

    private static Sprite FindIconInDefinition(GlueProductDefinition def, string machineId)
    {
        if (def == null || def.Products == null)
        {
            return null;
        }

        for (var i = 0; i < def.Products.Count; i++)
        {
            var entry = def.Products[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.ProductId, machineId, StringComparison.Ordinal))
            {
                return entry.UiIcon;
            }
        }

        return null;
    }

    private static Text CreateText(Transform parent, Font font, string content, int size, Vector2 pos, Vector2 box, TextAnchor align, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.text = content;
        return t;
    }

    private static Button CreateButton(Transform parent, Font font, string text, Vector2 pos, Vector2 size, Color color, int fontSize)
    {
        var go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;

        var btn = go.GetComponent<Button>();
        var txt = CreateText(go.transform, font, text, fontSize, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;
        txt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return btn;
    }

    private void AutoResolveReferences()
    {
        var root = transform;
        if (FindByName(root, "MoneyText") == null || FindByName(root, "GlueButton") == null)
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo != null)
            {
                root = canvasGo.transform;
            }
            else
            {
                var anyCanvas = FindFirstObjectByType<Canvas>();
                if (anyCanvas != null)
                {
                    root = anyCanvas.transform;
                }
            }
        }

        if (titleText == null) titleText = FindText(root, "TitleText");
        if (moneyText == null) moneyText = FindText(root, "MoneyText");
        if (perClickText == null) perClickText = FindText(root, "PerClickText");
        if (autoIncomeText == null) autoIncomeText = FindText(root, "AutoIncomeText");
        if (totalEarnedText == null) totalEarnedText = FindText(root, "TotalEarnedText");
        if (cheatButton == null) cheatButton = FindButton(root, "CheatButton");
        if (saveButton == null) saveButton = FindButton(root, "SaveButton");
        if (deleteSaveButton == null) deleteSaveButton = FindButton(root, "DeleteSaveButton");
        if (exitButton == null) exitButton = FindButton(root, "ExitButton");

        if (upgradesTabButton == null) upgradesTabButton = FindButton(root, "UpgradesTabButton") ?? FindButton(root, "UpgradesTabBtn");
        if (machinesTabButton == null) machinesTabButton = FindButton(root, "MachinesTabButton") ?? FindButton(root, "MachinesTabBtn");
        if (upgradesTab == null) upgradesTab = FindRect(root, "UpgradesTab");
        if (machinesTab == null) machinesTab = FindRect(root, "MachinesTab");

        if (glueButton == null) glueButton = FindButton(root, "GlueButton");
        if (installButton == null) installButton = FindButton(root, "InstallButton");
        if (sellButton == null) sellButton = FindButton(root, "SellButton");
        if (selectedMachineText == null) selectedMachineText = FindText(root, "SelectedMachineText");
        if (selectedSlotText == null) selectedSlotText = FindText(root, "SelectedSlotText");

        if (clickLevelText == null) clickLevelText = FindText(root, "ClickLevelText") ?? FindTextUnder(root, "ClickUpgCard", "LevelText");
        if (clickDescText == null) clickDescText = FindTextUnder(root, "ClickUpgCard", "DescText");
        if (clickCostText == null) clickCostText = FindTextUnder(root, "ClickUpgCard", "CostText");
        if (conveyorLevelText == null) conveyorLevelText = FindText(root, "ConveyorLevelText") ?? FindTextUnder(root, "ConveyorCard", "LevelText");
        if (conveyorDescText == null) conveyorDescText = FindTextUnder(root, "ConveyorCard", "DescText");
        if (conveyorCostText == null) conveyorCostText = FindTextUnder(root, "ConveyorCard", "CostText");
        if (exportLevelText == null) exportLevelText = FindText(root, "ExportLevelText") ?? FindTextUnder(root, "ExportCard", "LevelText");
        if (exportDescText == null) exportDescText = FindTextUnder(root, "ExportCard", "DescText");
        if (exportCostText == null) exportCostText = FindTextUnder(root, "ExportCard", "CostText");
        if (speedLevelText == null) speedLevelText = FindText(root, "SpeedLevelText") ?? FindTextUnder(root, "SpeedCard", "LevelText");
        if (speedDescText == null) speedDescText = FindTextUnder(root, "SpeedCard", "DescText");
        if (speedCostText == null) speedCostText = FindTextUnder(root, "SpeedCard", "CostText");
        if (clickBuyButton == null) clickBuyButton = FindButton(root, "ClickBuyButton") ?? FindButtonUnder(root, "ClickUpgCard", "BuyButton");
        if (conveyorBuyButton == null) conveyorBuyButton = FindButton(root, "ConveyorBuyButton") ?? FindButtonUnder(root, "ConveyorCard", "BuyButton");
        if (exportBuyButton == null) exportBuyButton = FindButton(root, "ExportBuyButton") ?? FindButtonUnder(root, "ExportCard", "BuyButton");
        if (speedBuyButton == null) speedBuyButton = FindButton(root, "SpeedBuyButton") ?? FindButtonUnder(root, "SpeedCard", "BuyButton");

        if (machineShopContent == null) machineShopContent = FindRect(root, "MachinesContent") ?? FindRect(root, "Content");

        if (toastPanel == null)
        {
            var toast = FindRect(root, "ToastPanel");
            if (toast != null) toastPanel = toast.gameObject;
        }

        if (toastBackground == null && toastPanel != null) toastBackground = toastPanel.GetComponent<Image>();
        if (toastText == null) toastText = FindText(root, "ToastText");

        if (slotSelectButtons.Count == 0 || slotTexts.Count == 0 || slotProgressFills.Count == 0)
        {
            slotSelectButtons.Clear();
            slotTexts.Clear();
            slotDetailTexts.Clear();
            slotProductIcons.Clear();
            slotProgressFills.Clear();
            slotCardBackgrounds.Clear();
            for (var i = 0; i < 3; i++)
            {
                var slotParent = FindRect(root, "SlotParent" + i);
                if (slotParent == null)
                {
                    slotSelectButtons.Add(null);
                    slotTexts.Add(null);
                    slotDetailTexts.Add(null);
                    slotProductIcons.Add(null);
                    slotProgressFills.Add(null);
                    slotCardBackgrounds.Add(null);
                    continue;
                }

                var b = FindButton(slotParent, "SelectButton");
                var t = FindText(slotParent, "SlotText");
                var d = FindText(slotParent, "SlotDetailText");
                var icon = FindImage(slotParent, "SlotProductIcon");
                var p = FindImage(slotParent, "ProgressFill");
                var bg = slotParent.GetComponent<Image>();
                slotSelectButtons.Add(b);
                slotTexts.Add(t);
                slotDetailTexts.Add(d);
                slotProductIcons.Add(icon);
                slotProgressFills.Add(p);
                slotCardBackgrounds.Add(bg);
            }
        }

        ResolveCheatSource();
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

    private static RectTransform FindRect(Transform root, string name) => FindByName(root, name) as RectTransform;
    private static Text FindText(Transform root, string name) => FindByName(root, name)?.GetComponent<Text>();
    private static Button FindButton(Transform root, string name) => FindByName(root, name)?.GetComponent<Button>();
    private static Image FindImage(Transform root, string name) => FindByName(root, name)?.GetComponent<Image>();

    private static Text FindTextUnder(Transform root, string parentName, string childName)
    {
        var parent = FindByName(root, parentName);
        return parent == null ? null : FindText(parent, childName);
    }

    private static Button FindButtonUnder(Transform root, string parentName, string childName)
    {
        var parent = FindByName(root, parentName);
        return parent == null ? null : FindButton(parent, childName);
    }

    private void EnsureCheatButtonInHeader()
    {
        if (cheatButton != null)
        {
            return;
        }

        if (saveButton == null)
        {
            return;
        }

        var parent = saveButton.transform.parent;
        if (parent == null)
        {
            return;
        }

        var saveRt = saveButton.GetComponent<RectTransform>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var size = saveRt != null ? saveRt.sizeDelta : new Vector2(110f, 36f);
        var pos = saveRt != null ? saveRt.anchoredPosition - new Vector2(size.x + 8f, 0f) : new Vector2(0f, 0f);
        cheatButton = CreateButton(parent, font, "CHEAT", pos, size, new Color(0.08f, 0.18f, 0.30f, 0.98f), 12);
        cheatButton.name = "CheatButton";

        var cheatRt = cheatButton.GetComponent<RectTransform>();
        if (saveRt != null && cheatRt != null)
        {
            cheatRt.anchorMin = saveRt.anchorMin;
            cheatRt.anchorMax = saveRt.anchorMax;
            cheatRt.pivot = saveRt.pivot;
            cheatRt.localScale = Vector3.one;
        }
    }

    private void EnsureExitButtonInHeader()
    {
        if (exitButton != null)
        {
            return;
        }

        if (deleteSaveButton == null)
        {
            return;
        }

        var parent = deleteSaveButton.transform.parent;
        if (parent == null)
        {
            return;
        }

        var resetRt = deleteSaveButton.GetComponent<RectTransform>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var size = resetRt != null ? resetRt.sizeDelta : new Vector2(110f, 36f);
        var pos = resetRt != null ? resetRt.anchoredPosition + new Vector2(size.x + 8f, 0f) : new Vector2(0f, 0f);
        exitButton = CreateButton(parent, font, "EXIT", pos, size, new Color(0.12f, 0.12f, 0.16f, 0.98f), 12);
        exitButton.name = "ExitButton";

        var exitRt = exitButton.GetComponent<RectTransform>();
        if (resetRt != null && exitRt != null)
        {
            exitRt.anchorMin = resetRt.anchorMin;
            exitRt.anchorMax = resetRt.anchorMax;
            exitRt.pivot = resetRt.pivot;
            exitRt.localScale = Vector3.one;
        }
    }

    private void LayoutHeaderActionButtons()
    {
        var template = deleteSaveButton != null ? deleteSaveButton : saveButton;
        if (template == null)
        {
            return;
        }

        var templateRt = template.GetComponent<RectTransform>();
        var buttonSize = templateRt != null && templateRt.sizeDelta.x > 1f ? templateRt.sizeDelta : new Vector2(104f, 36f);
        var y = templateRt != null ? -templateRt.anchoredPosition.y : 8f;
        const float rightMargin = 20f;
        const float gap = 8f;
        const float leftShift = 0f;

        var nextIndexFromRight = 0;
        ApplyHeaderButtonLayoutIfVisible(exitButton, ref nextIndexFromRight, buttonSize, rightMargin + leftShift, gap, y);
        ApplyHeaderButtonLayoutIfVisible(deleteSaveButton, ref nextIndexFromRight, buttonSize, rightMargin + leftShift, gap, y);
        ApplyHeaderButtonLayoutIfVisible(saveButton, ref nextIndexFromRight, buttonSize, rightMargin + leftShift, gap, y);
        ApplyHeaderButtonLayoutIfVisible(cheatButton, ref nextIndexFromRight, buttonSize, rightMargin + leftShift, gap, y);
    }

    private static void ApplyHeaderButtonLayoutIfVisible(Button button, ref int indexFromRight, Vector2 size, float rightMargin, float gap, float topPadding)
    {
        if (button == null || !button.gameObject.activeSelf)
        {
            return;
        }

        ApplyHeaderButtonLayout(button, indexFromRight, size, rightMargin, gap, topPadding);
        indexFromRight++;
    }

    private static void ApplyHeaderButtonLayout(Button button, int indexFromRight, Vector2 size, float rightMargin, float gap, float topPadding)
    {
        if (button == null)
        {
            return;
        }

        var rt = button.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(-(rightMargin + indexFromRight * (size.x + gap)), -topPadding);
    }

    private static void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveUpgradeCardReferences()
    {
        var root = transform;
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            root = canvasGo.transform;
        }

        if (clickLevelText == null) clickLevelText = FindText(root, "ClickLevelText") ?? FindTextUnder(root, "ClickUpgCard", "LevelText");
        if (clickDescText == null) clickDescText = FindTextUnder(root, "ClickUpgCard", "DescText");
        if (clickCostText == null) clickCostText = FindTextUnder(root, "ClickUpgCard", "CostText");
        if (conveyorLevelText == null) conveyorLevelText = FindText(root, "ConveyorLevelText") ?? FindTextUnder(root, "ConveyorCard", "LevelText");
        if (conveyorDescText == null) conveyorDescText = FindTextUnder(root, "ConveyorCard", "DescText");
        if (conveyorCostText == null) conveyorCostText = FindTextUnder(root, "ConveyorCard", "CostText");
        if (exportLevelText == null) exportLevelText = FindText(root, "ExportLevelText") ?? FindTextUnder(root, "ExportCard", "LevelText");
        if (exportDescText == null) exportDescText = FindTextUnder(root, "ExportCard", "DescText");
        if (exportCostText == null) exportCostText = FindTextUnder(root, "ExportCard", "CostText");
        if (speedLevelText == null) speedLevelText = FindText(root, "SpeedLevelText") ?? FindTextUnder(root, "SpeedCard", "LevelText");
        if (speedDescText == null) speedDescText = FindTextUnder(root, "SpeedCard", "DescText");
        if (speedCostText == null) speedCostText = FindTextUnder(root, "SpeedCard", "CostText");
        if (clickBuyButton == null) clickBuyButton = FindButton(root, "ClickBuyButton") ?? FindButtonUnder(root, "ClickUpgCard", "BuyButton");
        if (conveyorBuyButton == null) conveyorBuyButton = FindButton(root, "ConveyorBuyButton") ?? FindButtonUnder(root, "ConveyorCard", "BuyButton");
        if (exportBuyButton == null) exportBuyButton = FindButton(root, "ExportBuyButton") ?? FindButtonUnder(root, "ExportCard", "BuyButton");
        if (speedBuyButton == null) speedBuyButton = FindButton(root, "SpeedBuyButton") ?? FindButtonUnder(root, "SpeedCard", "BuyButton");
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
        var uiHits = new List<RaycastResult>(16);
        EventSystem.current.RaycastAll(pointerEventData, uiHits);
        for (var i = 0; i < uiHits.Count; i++)
        {
            var hit = uiHits[i].gameObject != null ? uiHits[i].gameObject.transform : null;
            if (hit == null)
            {
                continue;
            }

            // Do not block manual click when hit belongs to the factory click zone itself.
            if (factoryClickZone != null && hit.IsChildOf(factoryClickZone))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsAnyModalDialogOpen()
    {
        return (machineUpgradeDialog != null && machineUpgradeDialog.activeInHierarchy) ||
               (resetConfirmDialog != null && resetConfirmDialog.activeInHierarchy) ||
               (exitConfirmDialog != null && exitConfirmDialog.activeInHierarchy) ||
               (cheatDialog != null && cheatDialog.activeInHierarchy);
    }

    private void ResolveFactoryClickZone()
    {
        if (factoryClickZone != null)
        {
            return;
        }

        var root = transform;
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            root = canvasGo.transform;
        }

        factoryClickZone = FindRect(root, "FactoryPanel");
        if (factoryClickZone == null)
        {
            factoryClickZone = FindRect(root, "World");
        }
        if (factoryClickZone == null)
        {
            var go = GameObject.Find("FactoryPanel") ?? GameObject.Find("World");
            if (go != null)
            {
                factoryClickZone = go.GetComponent<RectTransform>();
            }
        }
    }

    private void NormalizeFactoryOverlayVisual()
    {
        if (factoryClickZone == null)
        {
            return;
        }

        var img = factoryClickZone.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
        }
    }

    private void NormalizeGlobalOverlayVisual()
    {
        var root = transform;
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            root = canvasGo.transform;
        }

        var rootRect = FindRect(root, "Root");
        if (rootRect == null)
        {
            return;
        }

        var img = rootRect.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
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
}
