using System.Collections.Generic;
using UnityEngine;

public sealed class GlueProductCatalog : MonoBehaviour
{
    [SerializeField] private bool useSceneDefinitions = true;
    [SerializeField] private Transform definitionsRoot;
    [SerializeField] private bool includeInactive;
    [SerializeField] private bool createDefaultProductsWhenMissing = true;
    [SerializeField] private bool ensureDefaultProductsInSameList = true;

    public bool ApplyTo(GlueFactoryBalanceConfig config)
    {
        if (!useSceneDefinitions || config == null)
        {
            return false;
        }

        var defs = CollectDefinitions();
        if (createDefaultProductsWhenMissing && (defs.Count == 0 || ensureDefaultProductsInSameList))
        {
            EnsureDefaultDefinitions();
            defs = CollectDefinitions();
        }

        if (defs.Count == 0)
        {
            return false;
        }

        var entries = CollectEntries(defs);
        if (entries.Count == 0)
        {
            return false;
        }

        entries.Sort((a, b) => a.ShopOrder.CompareTo(b.ShopOrder));
        config.machines = new List<GlueFactoryBalanceConfig.MachineConfig>(entries.Count);

        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].IncludeInShop)
            {
                config.machines.Add(entries[i].ToMachineConfig());
            }
        }

        return config.machines.Count > 0;
    }

    private List<GlueProductDefinition> CollectDefinitions()
    {
        var result = new List<GlueProductDefinition>();

        if (definitionsRoot != null)
        {
            definitionsRoot.GetComponentsInChildren(includeInactive, result);
            return result;
        }

        var all = FindObjectsByType<GlueProductDefinition>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        result.AddRange(all);
        return result;
    }

    public void EnsureDefaultDefinitions()
    {
        var root = definitionsRoot;
        if (root == null)
        {
            var go = new GameObject("GlueProductDefaults");
            go.transform.SetParent(transform, false);
            root = go.transform;
            definitionsRoot = root;
        }

        var listComponent = root.GetComponent<GlueProductDefinition>();
        if (listComponent == null)
        {
            listComponent = root.gameObject.AddComponent<GlueProductDefinition>();
        }
        listComponent.EnsureDefaultProducts();
    }

    public static (string id, string name, double piece, double cost)[] DefaultList()
    {
        return new (string id, string name, double piece, double cost)[]
        {
            ("glue_stick", "Glue Stick", 1, 25),
            ("white_glue_pva", "White Glue (PVA)", 2, 100),
            ("wood_glue", "Wood Glue", 5, 1000),
            ("super_glue", "Super Glue", 15, 2500),
            ("plastic_resin_glue", "Plastic Resin Glue", 35, 7500),
            ("e6000_glue", "E6000 Glue", 50, 10000),
            ("polyurethane_glue", "Polyurethane Glue", 100, 50000),
            ("construction_glue", "Construction Glue", 200, 100000),
            ("epoxy_glue", "Epoxy Glue", 400, 250000),
            ("aerospace_glue", "Aerospace Glue", 600, 500000),
            ("epoxy_resin_glue_bucket", "Epoxy Resin Glue Bucket", 2500, 1000000),
            ("edible_multi_purpose_glue", "Edible Multi-Purpose Glue", 3500, 10000000),
            ("military_defense_certified_space_glue", "Military Defense-Certified Space Glue", 5000, 20000000),
            ("space_glue", "Space Glue", 75000, 150000000),
            ("holy_glue", "Holy Glue", 150000, 1000000000),
        };
    }

    private static List<GlueProductDefinition.ProductEntry> CollectEntries(List<GlueProductDefinition> defs)
    {
        var map = new Dictionary<string, GlueProductDefinition.ProductEntry>();
        var order = new List<string>();

        for (var i = 0; i < defs.Count; i++)
        {
            var def = defs[i];
            if (def == null || def.Products == null)
            {
                continue;
            }

            for (var j = 0; j < def.Products.Count; j++)
            {
                var entry = def.Products[j];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ProductId))
                {
                    continue;
                }

                if (!map.ContainsKey(entry.ProductId))
                {
                    order.Add(entry.ProductId);
                }

                map[entry.ProductId] = entry;
            }
        }

        var result = new List<GlueProductDefinition.ProductEntry>(order.Count);
        for (var i = 0; i < order.Count; i++)
        {
            result.Add(map[order[i]]);
        }

        return result;
    }
}
