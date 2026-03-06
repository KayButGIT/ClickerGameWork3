using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GlueProductDefinition : MonoBehaviour
{
    [Serializable]
    public sealed class ProductEntry
    {
        [SerializeField] private bool includeInShop = true;
        [SerializeField] private int shopOrder;
        [SerializeField] private string productId = "glue_product";
        [SerializeField] private string displayName = "Glue Product";
        [SerializeField] private double pieceValue = 1d;
        [SerializeField] private double machineCost = 25d;
        [SerializeField] private Sprite uiIcon;
        [SerializeField] private Vector3 movingGlueScale = new Vector3(0.01f, 0.01f, 0.01f);

        public bool IncludeInShop => includeInShop;
        public int ShopOrder => shopOrder;
        public string ProductId => string.IsNullOrWhiteSpace(productId) ? "glue_product" : productId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ProductId : displayName;
        public double PieceValue => Math.Max(0d, pieceValue);
        public double MachineCost => Math.Max(0d, machineCost);
        public Sprite UiIcon => uiIcon;
        public Vector3 MovingGlueScale => movingGlueScale;

        public void SetData(string id, string name, double valuePerPiece, double costPerMachine, int order, bool inShop = true, Sprite icon = null, Vector3? glueScale = null)
        {
            productId = id;
            displayName = name;
            pieceValue = Math.Max(0d, valuePerPiece);
            machineCost = Math.Max(0d, costPerMachine);
            shopOrder = order;
            includeInShop = inShop;
            uiIcon = icon;
            movingGlueScale = glueScale ?? new Vector3(0.01f, 0.01f, 0.01f);
        }

        public GlueFactoryBalanceConfig.MachineConfig ToMachineConfig()
        {
            return new GlueFactoryBalanceConfig.MachineConfig
            {
                id = CanonicalProductId(ProductId),
                displayName = DisplayName,
                pieceValue = PieceValue,
                machineCost = MachineCost,
                icon = uiIcon,
                movingGlueScale = movingGlueScale
            };
        }

        private static string CanonicalProductId(string id)
        {
            var key = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToLowerInvariant();
            return key switch
            {
                "white_glue_pva" => "white_glue",
                "plastic_resin_glue" => "plastic_resin",
                "e6000_glue" => "e6000",
                "polyurethane_glue" => "polyurethane",
                "epoxy_glue" => "epoxy",
                "epoxy_resin_glue_bucket" => "epoxy_bucket",
                "edible_multi_purpose_glue" => "edible",
                "military_defense_certified_space_glue" => "military",
                _ => string.IsNullOrWhiteSpace(id) ? "glue_product" : id.Trim()
            };
        }
    }

    [SerializeField] private List<ProductEntry> products = new List<ProductEntry>();

    public List<ProductEntry> Products
    {
        get
        {
            if (products == null)
            {
                products = new List<ProductEntry>();
            }

            if (products.Count == 0)
            {
                EnsureDefaultProducts();
            }

            return products;
        }
    }

    private void Reset()
    {
        _ = Products;
    }

    private void OnValidate()
    {
        _ = Products;
    }

    public bool HasProductId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        for (var i = 0; i < products.Count; i++)
        {
            if (products[i] != null && string.Equals(products[i].ProductId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public void AddProduct(string id, string name, double valuePerPiece, double costPerMachine, int order, bool inShop = true, Sprite icon = null, Vector3? glueScale = null)
    {
        var item = new ProductEntry();
        item.SetData(id, name, valuePerPiece, costPerMachine, order, inShop, icon, glueScale);
        products.Add(item);
    }

    public void EnsureDefaultProducts()
    {
        var defaults = GlueProductCatalog.DefaultList();
        for (var i = 0; i < defaults.Length; i++)
        {
            var item = defaults[i];
            if (HasProductId(item.id))
            {
                continue;
            }

            AddProduct(item.id, item.name, item.piece, item.cost, i, true, null);
        }
    }
}
