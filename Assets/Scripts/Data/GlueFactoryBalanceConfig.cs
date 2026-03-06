using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GlueFactory/Balance Config", fileName = "GlueFactoryBalance")]
public sealed class GlueFactoryBalanceConfig : ScriptableObject
{
    [Serializable]
    public sealed class UpgradeConfig
    {
        public int maxLevel;
        public double baseCost;
        public double growth;
    }

    [Serializable]
    public sealed class MachineConfig
    {
        public string id;
        public string displayName;
        public double pieceValue;
        public double machineCost;
        public Sprite icon;
        public Vector3 movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    [Header("Core")]
    public int maxSlots = 3;
    public float baseMachineIntervalSeconds = 5f;
    public float minimumMachineIntervalSeconds = 1f;
    public float machineIntervalStepSeconds = 1f;
    public float autoSaveIntervalSeconds = 600f;
    [Range(0f, 1f)] public float machineSellRefundRate = 0.4f;

    [Header("Factory Boost")]
    [Range(0f, 1f)] public float factoryBoostPerLevel = 0.02f;

    [Header("Upgrades")]
    public UpgradeConfig clickValueUpgrade = new UpgradeConfig { maxLevel = 999, baseCost = 50, growth = 1.7 };
    public UpgradeConfig conveyorUpgrade = new UpgradeConfig { maxLevel = 2, baseCost = 1200, growth = 10.0 };
    public UpgradeConfig factoryBoostUpgrade = new UpgradeConfig { maxLevel = 5, baseCost = 500, growth = 2.2 };
    public UpgradeConfig speedUpgrade = new UpgradeConfig { maxLevel = 4, baseCost = 1000, growth = 2.0 };

    [Header("Machines")]
    public List<MachineConfig> machines = new List<MachineConfig>
    {
        new MachineConfig { id = "glue_stick", displayName = "Glue Stick", pieceValue = 1, machineCost = 25, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "white_glue", displayName = "White Glue (PVA)", pieceValue = 2, machineCost = 100, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "wood_glue", displayName = "Wood Glue", pieceValue = 5, machineCost = 1000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "super_glue", displayName = "Super Glue", pieceValue = 15, machineCost = 2500, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "plastic_resin", displayName = "Plastic Resin Glue", pieceValue = 35, machineCost = 7500, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "e6000", displayName = "E6000 Glue", pieceValue = 50, machineCost = 10000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "polyurethane", displayName = "Polyurethane Glue", pieceValue = 100, machineCost = 50000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "construction", displayName = "Construction Glue", pieceValue = 200, machineCost = 100000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "epoxy", displayName = "Epoxy Glue", pieceValue = 400, machineCost = 250000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "aerospace", displayName = "Aerospace Glue", pieceValue = 600, machineCost = 500000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "epoxy_bucket", displayName = "Epoxy Resin Glue Bucket", pieceValue = 2500, machineCost = 1000000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "edible", displayName = "Edible Multi-Purpose Glue", pieceValue = 3500, machineCost = 10000000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "military", displayName = "Military Defense-Certified Space Glue", pieceValue = 5000, machineCost = 20000000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "space", displayName = "Space Glue", pieceValue = 75000, machineCost = 150000000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) },
        new MachineConfig { id = "holy", displayName = "Holy Glue", pieceValue = 150000, machineCost = 1000000000, movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f) }
    };
}
