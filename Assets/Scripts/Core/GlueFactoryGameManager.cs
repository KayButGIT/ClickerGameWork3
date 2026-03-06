using System;
using UnityEngine;

public sealed class GlueFactoryGameManager : MonoBehaviour
{
    public readonly struct GameSnapshot
    {
        public readonly double Money;
        public readonly double TotalEarned;
        public readonly int ClickLevel;
        public readonly int ConveyorLevel;
        public readonly int BoostLevel;
        public readonly int SpeedLevel;
        public readonly int SelectedSlot;
        public readonly int SelectedMachine;
        public readonly int[] SlotMachineIds;
        public readonly float[] SlotProgress01;

        public GameSnapshot(
            double money,
            double totalEarned,
            int clickLevel,
            int conveyorLevel,
            int boostLevel,
            int speedLevel,
            int selectedSlot,
            int selectedMachine,
            int[] slotMachineIds,
            float[] slotProgress01)
        {
            Money = money;
            TotalEarned = totalEarned;
            ClickLevel = clickLevel;
            ConveyorLevel = conveyorLevel;
            BoostLevel = boostLevel;
            SpeedLevel = speedLevel;
            SelectedSlot = selectedSlot;
            SelectedMachine = selectedMachine;
            SlotMachineIds = slotMachineIds;
            SlotProgress01 = slotProgress01;
        }
    }

    [SerializeField] private GlueFactoryBalanceConfig config;
    [SerializeField] private GlueFactorySaveSystem saveSystem;
    [SerializeField] private GlueUpgradeDefinition upgradeDefinition;
    [SerializeField] private bool enableAutoMachines = true;
    [SerializeField] private bool forceStartWithOnlyFirstSlotUnlocked = true;
    [SerializeField] private bool disableMachineSell = true;

    private double money;
    private double totalEarned;
    private int clickLevel;
    private int conveyorLevel;
    private int boostLevel;
    private int speedLevel;
    private int selectedSlot;
    private int selectedMachine;
    private int[] slotMachineIds;
    private float[] machineTimers;
    private float autoSaveTimer;
    private int lastManualProduceFrame = -1;
    private int stateVersion;

    public event Action OnChanged;
    public event Action<string> OnToast;
    public event Action<string> OnFloatText;
    public event Action<double> OnManualProduced;
    public event Action<int, double> OnMachineProduced;
    public event Action OnResetState;

    public GlueFactoryBalanceConfig Config => config;
    public int StateVersion => stateVersion;

    public void Configure(GlueFactoryBalanceConfig balanceConfig, GlueFactorySaveSystem system)
    {
        config = balanceConfig;
        saveSystem = system;
        InitState();
    }

    private void Awake()
    {
        if (config != null && saveSystem != null)
        {
            InitState();
        }
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        TickMachines(Time.deltaTime);

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= config.autoSaveIntervalSeconds)
        {
            autoSaveTimer = 0f;
            SaveNow(false);
            SendToast("Auto saved");
        }
    }

    private void OnApplicationQuit()
    {
        SaveNow(false);
    }

    public GameSnapshot Snapshot()
    {
        var slotCopy = new int[slotMachineIds.Length];
        var progressCopy = new float[machineTimers.Length];
        Array.Copy(slotMachineIds, slotCopy, slotMachineIds.Length);

        var interval = Mathf.Max(0.01f, MachineIntervalSeconds());
        for (var i = 0; i < progressCopy.Length; i++)
        {
            progressCopy[i] = Mathf.Clamp01(machineTimers[i] / interval);
        }

        return new GameSnapshot(money, totalEarned, clickLevel, conveyorLevel, boostLevel, speedLevel, selectedSlot, selectedMachine, slotCopy, progressCopy);
    }

    public float MachineIntervalSeconds()
    {
        var value = config.baseMachineIntervalSeconds - speedLevel * config.machineIntervalStepSeconds;
        return Mathf.Max(config.minimumMachineIntervalSeconds, value);
    }

    public double ClickValuePerTap()
    {
        ResolveUpgradeDefinition();

        var startValue = 1d;
        var perLevelValue = 1d;
        if (upgradeDefinition != null &&
            upgradeDefinition.TryGetEffectInfo("click", out var configuredStart, out var configuredStep, out _, out _))
        {
            startValue = configuredStart;
            perLevelValue = configuredStep;
        }

        return Math.Max(0d, startValue + perLevelValue * clickLevel);
    }

    public double AutoIncomePerSecondEstimate()
    {
        if (!enableAutoMachines)
        {
            return 0d;
        }

        var interval = MachineIntervalSeconds();
        if (interval <= 0f)
        {
            return 0;
        }

        var totalPerPiece = 0d;
        for (var i = 0; i < slotMachineIds.Length; i++)
        {
            if (i > conveyorLevel)
            {
                continue;
            }

            var machine = slotMachineIds[i];
            if (machine < 0 || machine >= config.machines.Count)
            {
                continue;
            }

            totalPerPiece += config.machines[machine].pieceValue * FactoryMultiplier();
        }

        return totalPerPiece / interval;
    }

    public double UpgradeCostClick() => UpgradeCost(config.clickValueUpgrade, clickLevel);
    public double UpgradeCostConveyor() => UpgradeCost(config.conveyorUpgrade, conveyorLevel);
    public double UpgradeCostBoost() => UpgradeCost(config.factoryBoostUpgrade, boostLevel);
    public double UpgradeCostSpeed() => UpgradeCost(config.speedUpgrade, speedLevel);

    public void ProduceByClick()
    {
        // Guard against duplicate click dispatch from multiple UI listeners in the same frame.
        if (lastManualProduceFrame == Time.frameCount)
        {
            return;
        }
        lastManualProduceFrame = Time.frameCount;

        var amount = ClickValuePerTap();
        var manualProduced = OnManualProduced;
        if (manualProduced != null)
        {
            manualProduced.Invoke(amount);
        }
        else
        {
            // Fallback so manual clicks still earn income if world listeners are temporarily unbound (e.g. during reset/rebind).
            CollectManualProduced(amount);
        }
        // SendToast("Produced piece. Waiting for conveyor end.");
        NotifyChanged();
    }

    public void CollectManualProduced(double amount)
    {
        if (amount <= 0d)
        {
            return;
        }

        AddMoney(amount, true);
        OnFloatText?.Invoke("+" + MoneyText(amount));
        // SendToast("Sold " + MoneyText(amount));
        NotifyChanged();
    }

    public void CollectAutoProduced(double amount)
    {
        if (amount <= 0d)
        {
            return;
        }

        AddMoney(amount, true);
        OnFloatText?.Invoke("+" + MoneyText(amount));
        NotifyChanged();
    }

    public void AddCheatMoney(double amount, bool showToast = true)
    {
        if (amount <= 0d)
        {
            return;
        }

        AddMoney(amount, false);
        OnFloatText?.Invoke("+" + MoneyText(amount));
        if (showToast)
        {
            SendToast("Cheat added " + MoneyText(amount));
        }
        NotifyChanged();
    }

    public void UpgradeClick() => TryUpgrade(ref clickLevel, config.clickValueUpgrade, "Click value upgraded");

    public void UpgradeConveyor()
    {
        if (conveyorLevel >= config.conveyorUpgrade.maxLevel)
        {
            SendToast("Conveyor maxed");
            return;
        }

        TryUpgrade(ref conveyorLevel, config.conveyorUpgrade, "Conveyor upgraded");
    }

    public void UpgradeBoost() => TryUpgrade(ref boostLevel, config.factoryBoostUpgrade, "Factory boost upgraded");
    public void UpgradeSpeed() => TryUpgrade(ref speedLevel, config.speedUpgrade, "Machine speed upgraded");

    public void SelectSlot(int slot)
    {
        var clamped = Mathf.Clamp(slot, 0, config.maxSlots - 1);
        if (clamped > conveyorLevel)
        {
            SendToast("Slot " + (clamped + 1) + " is locked.");
            return;
        }

        selectedSlot = clamped;
        SendToast("Selected slot " + (selectedSlot + 1));
        NotifyChanged();
    }

    public void SelectMachine(int machine)
    {
        if (config == null || config.machines == null || config.machines.Count == 0)
        {
            SendToast("No products in machine catalog.");
            return;
        }

        selectedMachine = Mathf.Clamp(machine, 0, config.machines.Count - 1);
        SendToast("Selected " + config.machines[selectedMachine].displayName);
        NotifyChanged();
    }

    public void InstallSelectedMachine()
    {
        if (config == null || config.machines == null || config.machines.Count == 0)
        {
            SendToast("No products in machine catalog.");
            return;
        }

        if (selectedSlot > conveyorLevel)
        {
            SendToast("Slot " + (selectedSlot + 1) + " is locked. Upgrade conveyor first.");
            return;
        }

        var targetCost = config.machines[selectedMachine].machineCost;
        if (money < targetCost)
        {
            SendInsufficientMoneyToast("Install " + config.machines[selectedMachine].displayName, targetCost);
            return;
        }

        var currentMachine = slotMachineIds[selectedSlot];
        if (currentMachine >= 0)
        {
            SendToast("Slot " + (selectedSlot + 1) + " already has " + config.machines[currentMachine].displayName + ". Sell/remove it first.");
            return;
        }

        money -= targetCost;
        slotMachineIds[selectedSlot] = selectedMachine;
        machineTimers[selectedSlot] = 0f;
        SendToast("Installed " + config.machines[selectedMachine].displayName + " in slot " + (selectedSlot + 1));
        NotifyChanged();
    }

    public int NextMachineForSlot(int slot)
    {
        if (config == null || config.machines == null || config.machines.Count == 0)
        {
            return -1;
        }

        if (slot < 0 || slot >= slotMachineIds.Length)
        {
            return -1;
        }

        if (slot > conveyorLevel)
        {
            return -1;
        }

        var current = slotMachineIds[slot];
        if (current < 0)
        {
            return 0;
        }

        var next = current + 1;
        return next < config.machines.Count ? next : -1;
    }

    public void PurchaseNextMachineForSlot(int slot)
    {
        if (config == null || config.machines == null || config.machines.Count == 0)
        {
            SendToast("No products in machine catalog.");
            return;
        }

        if (slot < 0 || slot >= slotMachineIds.Length)
        {
            SendToast("Invalid slot.");
            return;
        }

        if (slot > conveyorLevel)
        {
            SendToast("Slot " + (slot + 1) + " is locked.");
            return;
        }

        var nextMachine = NextMachineForSlot(slot);
        if (nextMachine < 0)
        {
            SendToast("Slot " + (slot + 1) + " is already at max machine.");
            return;
        }

        var cost = config.machines[nextMachine].machineCost;
        if (money < cost)
        {
            SendInsufficientMoneyToast("Upgrade to " + config.machines[nextMachine].displayName, cost);
            return;
        }

        money -= cost;
        slotMachineIds[slot] = nextMachine;
        machineTimers[slot] = 0f;
        selectedSlot = slot;
        selectedMachine = nextMachine;

        var installedText = nextMachine == 0 ? "Installed " : "Upgraded to ";
        SendToast(installedText + config.machines[nextMachine].displayName + " in slot " + (slot + 1));
        NotifyChanged();
    }

    public void SellSelectedSlot()
    {
        if (disableMachineSell)
        {
            SendToast("Machine removal is disabled.");
            return;
        }

        if (selectedSlot > conveyorLevel)
        {
            SendToast("Slot " + (selectedSlot + 1) + " is locked.");
            return;
        }

        var currentMachine = slotMachineIds[selectedSlot];
        if (currentMachine < 0)
        {
            SendToast("No machine in slot " + (selectedSlot + 1) + ".");
            return;
        }

        var refund = config.machines[currentMachine].machineCost * config.machineSellRefundRate;
        slotMachineIds[selectedSlot] = -1;
        machineTimers[selectedSlot] = 0f;
        AddMoney(refund, true);
        SendToast("Sold for " + MoneyText(refund));
        NotifyChanged();
    }

    public void SaveNow(bool showToast = true)
    {
        if (saveSystem == null || slotMachineIds == null)
        {
            return;
        }

        saveSystem.Save(new GlueFactorySaveSystem.SaveData
        {
            money = money,
            totalEarned = totalEarned,
            clickLevel = clickLevel,
            conveyorLevel = conveyorLevel,
            boostLevel = boostLevel,
            speedLevel = speedLevel,
            selectedSlot = selectedSlot,
            selectedMachine = selectedMachine,
            slotMachineIds = (int[])slotMachineIds.Clone()
        });

        if (showToast)
        {
            SendToast("Saved");
        }
    }

    public void DeleteSaveAndReset()
    {
        saveSystem?.DeleteSave();
        stateVersion++;
        InitFreshState();
        autoSaveTimer = 0f;
        lastManualProduceFrame = -1;
        OnResetState?.Invoke();
        SendToast("Save deleted");
        NotifyChanged();
    }

    public string MoneyText(double value)
    {
        if (value >= 1_000_000_000d) return "$" + (value / 1_000_000_000d).ToString("0.##") + "B";
        if (value >= 1_000_000d) return "$" + (value / 1_000_000d).ToString("0.##") + "M";
        if (value >= 1_000d) return "$" + (value / 1_000d).ToString("0.##") + "K";
        return "$" + value.ToString("0");
    }

    public string RateText(double value)
    {
        if (value >= 1000d)
        {
            return MoneyText(value);
        }

        return "$" + value.ToString("0.##");
    }

    public string MachineLabel(int index)
    {
        if (config == null || config.machines == null || index < 0 || index >= config.machines.Count)
        {
            return "Empty";
        }

        var machine = config.machines[index];
        return machine.displayName + " (" + MoneyText(machine.pieceValue) + "/piece)";
    }

    private void InitState()
    {
        slotMachineIds = new int[config.maxSlots];
        machineTimers = new float[config.maxSlots];
        InitFreshState();

        GlueFactorySaveSystem.SaveData data = null;
        var loaded = saveSystem != null && saveSystem.TryLoad(out data);
        if (loaded)
        {
            money = Math.Max(0, data.money);
            totalEarned = Math.Max(0, data.totalEarned);
            if (totalEarned < money)
            {
                totalEarned = money;
            }
            clickLevel = Math.Max(0, data.clickLevel);
            conveyorLevel = Mathf.Clamp(data.conveyorLevel, 0, config.conveyorUpgrade.maxLevel);
            boostLevel = Mathf.Clamp(data.boostLevel, 0, config.factoryBoostUpgrade.maxLevel);
            speedLevel = Mathf.Clamp(data.speedLevel, 0, config.speedUpgrade.maxLevel);
            selectedSlot = Mathf.Clamp(data.selectedSlot, 0, config.maxSlots - 1);
            selectedMachine = Mathf.Clamp(data.selectedMachine, 0, config.machines.Count - 1);

            if (data.slotMachineIds != null)
            {
                for (var i = 0; i < slotMachineIds.Length && i < data.slotMachineIds.Length; i++)
                {
                    var id = data.slotMachineIds[i];
                    slotMachineIds[i] = id >= 0 && id < config.machines.Count ? id : -1;
                }
            }

            SendToast("Save loaded");
        }
        else
        {
            // Ensure a default save key exists on first launch.
            SaveNow(false);
        }

        if (forceStartWithOnlyFirstSlotUnlocked && !loaded)
        {
            // Enforce strict unlock flow: only slot 1 available until player buys conveyor upgrade again.
            conveyorLevel = 0;
            selectedSlot = 0;
            for (var i = 0; i < slotMachineIds.Length; i++)
            {
                if (i > conveyorLevel)
                {
                    slotMachineIds[i] = -1;
                    machineTimers[i] = 0f;
                }
            }
        }

        NotifyChanged();
    }

    public void OnMachineCatalogChanged()
    {
        if (config == null || config.machines == null || !EnsureRuntimeStateInitialized())
        {
            return;
        }

        if (config.machines.Count == 0)
        {
            selectedMachine = 0;
            for (var i = 0; i < slotMachineIds.Length; i++)
            {
                slotMachineIds[i] = -1;
                machineTimers[i] = 0f;
            }

            SendToast("Catalog updated: no machines in shop.");
            NotifyChanged();
            return;
        }

        selectedMachine = Mathf.Clamp(selectedMachine, 0, config.machines.Count - 1);
        selectedSlot = Mathf.Clamp(selectedSlot, 0, config.maxSlots - 1);

        for (var i = 0; i < slotMachineIds.Length; i++)
        {
            if (slotMachineIds[i] < 0)
            {
                continue;
            }

            if (slotMachineIds[i] >= config.machines.Count)
            {
                slotMachineIds[i] = -1;
                machineTimers[i] = 0f;
            }
        }

        SendToast("Catalog updated.");
        NotifyChanged();
    }

    public void OnUpgradeConfigChanged()
    {
        if (config == null || !EnsureRuntimeStateInitialized())
        {
            return;
        }

        clickLevel = Mathf.Clamp(clickLevel, 0, config.clickValueUpgrade.maxLevel);
        conveyorLevel = Mathf.Clamp(conveyorLevel, 0, config.conveyorUpgrade.maxLevel);
        boostLevel = Mathf.Clamp(boostLevel, 0, config.factoryBoostUpgrade.maxLevel);
        speedLevel = Mathf.Clamp(speedLevel, 0, config.speedUpgrade.maxLevel);

        selectedSlot = Mathf.Clamp(selectedSlot, 0, Mathf.Max(0, config.maxSlots - 1));
        if (selectedSlot > conveyorLevel)
        {
            selectedSlot = conveyorLevel;
        }

        SendToast("Upgrade config updated.");
        NotifyChanged();
    }

    private void InitFreshState()
    {
        money = 0;
        totalEarned = 0;
        clickLevel = 0;
        conveyorLevel = 0;
        boostLevel = 0;
        speedLevel = 0;
        selectedSlot = 0;
        selectedMachine = 0;

        for (var i = 0; i < slotMachineIds.Length; i++)
        {
            slotMachineIds[i] = -1;
            machineTimers[i] = 0f;
        }
    }

    private void ResolveUpgradeDefinition()
    {
        if (upgradeDefinition != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        upgradeDefinition = FindFirstObjectByType<GlueUpgradeDefinition>(FindObjectsInactive.Include);
#else
        upgradeDefinition = FindObjectOfType<GlueUpgradeDefinition>(true);
#endif
    }

    private bool EnsureRuntimeStateInitialized()
    {
        if (config == null || config.maxSlots <= 0)
        {
            return false;
        }

        if (slotMachineIds == null || slotMachineIds.Length != config.maxSlots)
        {
            var previous = slotMachineIds;
            slotMachineIds = new int[config.maxSlots];
            for (var i = 0; i < slotMachineIds.Length; i++)
            {
                slotMachineIds[i] = -1;
            }

            if (previous != null)
            {
                var copy = Mathf.Min(previous.Length, slotMachineIds.Length);
                for (var i = 0; i < copy; i++)
                {
                    slotMachineIds[i] = previous[i];
                }
            }
        }

        if (machineTimers == null || machineTimers.Length != config.maxSlots)
        {
            var previous = machineTimers;
            machineTimers = new float[config.maxSlots];
            if (previous != null)
            {
                var copy = Mathf.Min(previous.Length, machineTimers.Length);
                for (var i = 0; i < copy; i++)
                {
                    machineTimers[i] = previous[i];
                }
            }
        }

        selectedSlot = Mathf.Clamp(selectedSlot, 0, config.maxSlots - 1);
        if (config.machines != null && config.machines.Count > 0)
        {
            selectedMachine = Mathf.Clamp(selectedMachine, 0, config.machines.Count - 1);
        }
        else
        {
            selectedMachine = 0;
        }

        return true;
    }

    private void TickMachines(float delta)
    {
        if (!enableAutoMachines)
        {
            return;
        }

        var interval = MachineIntervalSeconds();
        var changed = false;

        for (var i = 0; i < slotMachineIds.Length; i++)
        {
            if (i > conveyorLevel)
            {
                continue;
            }

            var machine = slotMachineIds[i];
            if (machine < 0 || machine >= config.machines.Count)
            {
                continue;
            }

            machineTimers[i] += delta;
            while (machineTimers[i] >= interval)
            {
                machineTimers[i] -= interval;
                var amount = config.machines[machine].pieceValue * FactoryMultiplier();
                if (OnMachineProduced != null)
                {
                    OnMachineProduced.Invoke(i, amount);
                }
                else
                {
                    CollectAutoProduced(amount);
                }
                changed = true;
            }
        }

        if (changed || HasRunningMachines())
        {
            NotifyChanged();
        }
    }

    private bool HasRunningMachines()
    {
        for (var i = 0; i < slotMachineIds.Length; i++)
        {
            if (i <= conveyorLevel && slotMachineIds[i] >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void TryUpgrade(ref int level, GlueFactoryBalanceConfig.UpgradeConfig upgrade, string successMessage)
    {
        if (level >= upgrade.maxLevel)
        {
            SendToast("Upgrade maxed.");
            return;
        }

        var cost = UpgradeCost(upgrade, level);
        if (money < cost)
        {
            SendInsufficientMoneyToast("Upgrade", cost);
            return;
        }

        money -= cost;
        level++;
        SendToast(successMessage);
        NotifyChanged();
    }

    private static double UpgradeCost(GlueFactoryBalanceConfig.UpgradeConfig cfg, int currentLevel)
    {
        return cfg.baseCost * Math.Pow(cfg.growth, currentLevel);
    }

    private double FactoryMultiplier()
    {
        return 1d + boostLevel * config.factoryBoostPerLevel;
    }

    private void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    private void SendToast(string msg)
    {
        OnToast?.Invoke(msg);
    }

    private void SendInsufficientMoneyToast(string action, double required)
    {
        var missing = Math.Max(0, required - money);
        SendToast(action + " failed: Need " + MoneyText(required) + ", Have " + MoneyText(money) + " (Missing " + MoneyText(missing) + ").");
    }

    private void AddMoney(double amount, bool countAsEarned)
    {
        if (amount <= 0d)
        {
            return;
        }

        money += amount;
        if (countAsEarned)
        {
            totalEarned += amount;
        }
    }
}
