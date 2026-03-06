using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GlueUpgradeDefinition : MonoBehaviour
{
    [Serializable]
    public sealed class UpgradeEntry
    {
        [SerializeField] private string upgradeId = "click";
        [SerializeField] private string displayName = "Player Glue Value";
        [SerializeField] private string description = "Increase the sell price of manually produced glue.";
        [SerializeField] private int maxLevel = 999;
        [SerializeField] private double baseCost = 50d;
        [SerializeField] private double growth = 1.7d;
        [SerializeField] private double effectStartValue = 1d;
        [SerializeField] private double effectPerLevel = 1d;
        [SerializeField] private string effectUnit = "/per click";
        [SerializeField] private bool effectAsPercent;

        public string UpgradeId => string.IsNullOrWhiteSpace(upgradeId) ? "click" : upgradeId.Trim().ToLowerInvariant();
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? UpgradeId : displayName;
        public string Description => string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        public int MaxLevel => Mathf.Max(0, maxLevel);
        public double BaseCost => Math.Max(0d, baseCost);
        public double Growth => Math.Max(1d, growth);
        public double EffectStartValue => effectStartValue;
        public double EffectPerLevel => effectPerLevel;
        public string EffectUnit => string.IsNullOrWhiteSpace(effectUnit) ? string.Empty : effectUnit;
        public bool EffectAsPercent => effectAsPercent;

        public void SetData(
            string id,
            string name,
            string desc,
            int levelCap,
            double cost,
            double growthRate,
            double startValue = 0d,
            double perLevelValue = 0d,
            string unit = "",
            bool asPercent = false)
        {
            upgradeId = id;
            displayName = name;
            description = desc;
            maxLevel = Mathf.Max(0, levelCap);
            baseCost = Math.Max(0d, cost);
            growth = Math.Max(1d, growthRate);
            effectStartValue = startValue;
            effectPerLevel = perLevelValue;
            effectUnit = unit ?? string.Empty;
            effectAsPercent = asPercent;
        }
    }

    [SerializeField] private List<UpgradeEntry> upgrades = new List<UpgradeEntry>();

    public List<UpgradeEntry> Upgrades
    {
        get
        {
            if (upgrades == null)
            {
                upgrades = new List<UpgradeEntry>();
            }

            if (upgrades.Count == 0)
            {
                EnsureDefaults();
            }

            return upgrades;
        }
    }

    private void Reset()
    {
        _ = Upgrades;
    }

    private void OnValidate()
    {
        _ = Upgrades;
    }

    public void EnsureDefaults()
    {
        AddOrReplace("click", "Player Glue Value", "Increase the sell price of manually produced glue.", 999, 50d, 1.7d, 1d, 1d, "/per click");
        AddOrReplace("conveyor", "Conveyor Slot", "Unlock a new conveyor and double manual glue production.", 2, 1200d, 10d, 1d, 1d, "slots");
        AddOrReplace("boost", "Export Value", "Boost all sale values.", 5, 500d, 2.2d, 0d, 2d, "%", true);
        AddOrReplace("speed", "Machine Production Speed", "Increase the rate of automatic production.", 4, 1000d, 2d, 5d, -1d, "s/cycle");
    }

    public bool ApplyTo(GlueFactoryBalanceConfig config)
    {
        if (config == null)
        {
            return false;
        }

        var any = false;
        for (var i = 0; i < Upgrades.Count; i++)
        {
            var u = Upgrades[i];
            if (u == null)
            {
                continue;
            }

            var target = u.UpgradeId;
            if (target == "click")
            {
                config.clickValueUpgrade.maxLevel = u.MaxLevel;
                config.clickValueUpgrade.baseCost = u.BaseCost;
                config.clickValueUpgrade.growth = u.Growth;
                any = true;
            }
            else if (target == "conveyor")
            {
                config.conveyorUpgrade.maxLevel = u.MaxLevel;
                config.conveyorUpgrade.baseCost = u.BaseCost;
                config.conveyorUpgrade.growth = u.Growth;
                any = true;
            }
            else if (target == "boost")
            {
                config.factoryBoostUpgrade.maxLevel = u.MaxLevel;
                config.factoryBoostUpgrade.baseCost = u.BaseCost;
                config.factoryBoostUpgrade.growth = u.Growth;
                any = true;
            }
            else if (target == "speed")
            {
                config.speedUpgrade.maxLevel = u.MaxLevel;
                config.speedUpgrade.baseCost = u.BaseCost;
                config.speedUpgrade.growth = u.Growth;
                any = true;
            }
        }

        return any;
    }

    public string GetDescription(string upgradeId, string fallback)
    {
        var id = string.IsNullOrWhiteSpace(upgradeId) ? string.Empty : upgradeId.Trim().ToLowerInvariant();
        for (var i = 0; i < Upgrades.Count; i++)
        {
            var u = Upgrades[i];
            if (u != null && u.UpgradeId == id)
            {
                return string.IsNullOrWhiteSpace(u.Description) ? fallback : u.Description;
            }
        }

        return fallback;
    }

    public string GetDisplayName(string upgradeId, string fallback)
    {
        var id = string.IsNullOrWhiteSpace(upgradeId) ? string.Empty : upgradeId.Trim().ToLowerInvariant();
        for (var i = 0; i < Upgrades.Count; i++)
        {
            var u = Upgrades[i];
            if (u != null && u.UpgradeId == id)
            {
                return string.IsNullOrWhiteSpace(u.DisplayName) ? fallback : u.DisplayName;
            }
        }

        return fallback;
    }

    public bool TryGetEffectInfo(string upgradeId, out double startValue, out double perLevelValue, out string unit, out bool asPercent)
    {
        startValue = 0d;
        perLevelValue = 0d;
        unit = string.Empty;
        asPercent = false;

        var id = string.IsNullOrWhiteSpace(upgradeId) ? string.Empty : upgradeId.Trim().ToLowerInvariant();
        for (var i = 0; i < Upgrades.Count; i++)
        {
            var u = Upgrades[i];
            if (u == null || u.UpgradeId != id)
            {
                continue;
            }

            startValue = u.EffectStartValue;
            perLevelValue = u.EffectPerLevel;
            unit = u.EffectUnit;
            asPercent = u.EffectAsPercent;
            return true;
        }

        return false;
    }

    private void AddOrReplace(string id, string name, string desc, int maxLevel, double baseCost, double growth, double effectStart = 0d, double effectStep = 0d, string effectUnit = "", bool effectAsPercent = false)
    {
        for (var i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] != null && upgrades[i].UpgradeId == id)
            {
                upgrades[i].SetData(id, name, desc, maxLevel, baseCost, growth, effectStart, effectStep, effectUnit, effectAsPercent);
                return;
            }
        }

        var item = new UpgradeEntry();
        item.SetData(id, name, desc, maxLevel, baseCost, growth, effectStart, effectStep, effectUnit, effectAsPercent);
        upgrades.Add(item);
    }
}
