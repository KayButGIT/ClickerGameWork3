#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GlueFactoryProductMasterDataWindow : EditorWindow
{
    private GlueProductDefinition definition;
    private SerializedObject serializedDefinition;
    private SerializedProperty productsProperty;
    private Vector2 scroll;
    private ReorderableList reorderableList;

    [MenuItem("GlueFactory/Open Product Master Data")]
    public static void OpenWindow()
    {
        var window = GetWindow<GlueFactoryProductMasterDataWindow>("Product Master Data");
        window.minSize = new Vector2(720, 420);
        window.RefreshBinding();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshBinding();
    }

    private void OnHierarchyChange()
    {
        if (definition == null)
        {
            RefreshBinding();
        }
    }

    private void RefreshBinding()
    {
        definition = FindDefinition();
        if (definition == null)
        {
            serializedDefinition = null;
            productsProperty = null;
            reorderableList = null;
            return;
        }

        serializedDefinition = new SerializedObject(definition);
        productsProperty = serializedDefinition.FindProperty("products");
        BuildReorderableList();
    }

    private void OnGUI()
    {
        DrawHeader();
        if (definition == null || serializedDefinition == null || productsProperty == null)
        {
            EditorGUILayout.HelpBox("No GlueProductDefinition found. Click Generate Defaults first.", MessageType.Warning);
            if (GUILayout.Button("Generate Editable Default Product List", GUILayout.Height(28)))
            {
                GlueFactoryProductListTools.GenerateEditableDefaultProductList();
                RefreshBinding();
            }
            return;
        }

        serializedDefinition.Update();
        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (reorderableList != null)
        {
            reorderableList.DoLayoutList();
        }
        EditorGUILayout.EndScrollView();

        NormalizeOrderAndIds();
        serializedDefinition.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Glue Factory Product Master Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Source", definition == null ? "<None>" : definition.name);
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add New", GUILayout.Height(26)))
        {
            AddProduct();
        }

        if (GUILayout.Button("Ensure Defaults", GUILayout.Height(26)))
        {
            Undo.RecordObject(definition, "Ensure Default Products");
            definition.EnsureDefaultProducts();
            RefreshBinding();
            NormalizeOrderAndIds();
            EditorUtility.SetDirty(definition);
            MarkSceneDirtyIfNeeded();
        }

        if (GUILayout.Button("Apply To Running Game", GUILayout.Height(26)))
        {
            ApplyToRunningGame();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
    }

    private void BuildReorderableList()
    {
        if (productsProperty == null)
        {
            return;
        }

        reorderableList = new ReorderableList(serializedDefinition, productsProperty, true, true, true, true);
        reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Products (Drag to reorder. Index = Shop Order)");
        };

        reorderableList.elementHeight = 226f;
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            DrawProductElement(rect, index);
        };

        reorderableList.onAddCallback = _ => AddProduct();
        reorderableList.onRemoveCallback = list =>
        {
            DeleteProduct(list.index);
        };
        reorderableList.onReorderCallback = _ =>
        {
            NormalizeOrderAndIds();
            serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            MarkSceneDirtyIfNeeded();
        };
    }

    private void DrawProductElement(Rect rect, int index)
    {
        var item = productsProperty.GetArrayElementAtIndex(index);
        if (item == null)
        {
            return;
        }

        rect.y += 2f;
        var line = new Rect(rect.x, rect.y, rect.width, 18f);
        var displayName = item.FindPropertyRelative("displayName");
        EditorGUI.LabelField(line, $"#{index + 1}  {(string.IsNullOrWhiteSpace(displayName.stringValue) ? "<Unnamed>" : displayName.stringValue)}", EditorStyles.boldLabel);

        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("includeInShop"), new GUIContent("Include In Shop"));

        line.y += 20f;
        var orderProp = item.FindPropertyRelative("shopOrder");
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.IntField(line, "Shop Order (Auto)", orderProp.intValue);
        EditorGUI.EndDisabledGroup();

        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("productId"), new GUIContent("Product Id (Auto if empty)"));
        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("displayName"), new GUIContent("Display Name"));
        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("pieceValue"), new GUIContent("Price / Piece"));
        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("machineCost"), new GUIContent("Price / Machine"));
        line.y += 20f;
        var scaleProp = item.FindPropertyRelative("movingGlueScale");
        if (scaleProp != null)
        {
            var labelRect = new Rect(line.x, line.y, 160f, 18f);
            EditorGUI.LabelField(labelRect, "Moving Glue Scale");

            var fieldsX = line.x + 170f;
            var fieldW = Mathf.Max(48f, (line.width - 170f - 24f) / 3f);
            var xRect = new Rect(fieldsX, line.y, fieldW, 18f);
            var yRect = new Rect(fieldsX + fieldW + 8f, line.y, fieldW, 18f);
            var zRect = new Rect(fieldsX + (fieldW + 8f) * 2f, line.y, fieldW, 18f);

            var v = scaleProp.vector3Value;
            v.x = EditorGUI.FloatField(xRect, "X", v.x);
            v.y = EditorGUI.FloatField(yRect, "Y", v.y);
            v.z = EditorGUI.FloatField(zRect, "Z", v.z);
            scaleProp.vector3Value = v;
        }
        line.y += 20f;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("uiIcon"), new GUIContent("UI Icon"));

        var copyRect = new Rect(rect.x + rect.width - 112f, rect.y, 52f, 18f);
        var delRect = new Rect(rect.x + rect.width - 56f, rect.y, 52f, 18f);
        if (GUI.Button(copyRect, "Copy"))
        {
            DuplicateProduct(index);
        }
        if (GUI.Button(delRect, "Delete"))
        {
            DeleteProduct(index);
        }
    }

    private void AddProduct()
    {
        Undo.RecordObject(definition, "Add Product");
        var list = definition.Products;
        definition.AddProduct($"new_product_{list.Count}", "New Product", 1d, 100d, list.Count, true, null);
        NormalizeRuntimeDefinitionData();
        EditorUtility.SetDirty(definition);
        MarkSceneDirtyIfNeeded();
        RefreshBinding();
    }

    private void DuplicateProduct(int index)
    {
        var list = definition.Products;
        if (index < 0 || index >= list.Count)
        {
            return;
        }

        var source = list[index];
        var copyId = source.ProductId + "_copy";
        var suffix = 2;
        while (definition.HasProductId(copyId))
        {
            copyId = source.ProductId + "_copy_" + suffix;
            suffix++;
        }

        Undo.RecordObject(definition, "Duplicate Product");
        definition.AddProduct(copyId, source.DisplayName + " Copy", source.PieceValue, source.MachineCost, list.Count, source.IncludeInShop, source.UiIcon, source.MovingGlueScale);
        NormalizeRuntimeDefinitionData();
        EditorUtility.SetDirty(definition);
        MarkSceneDirtyIfNeeded();
        RefreshBinding();
    }

    private void DeleteProduct(int index)
    {
        var list = definition.Products;
        if (index < 0 || index >= list.Count)
        {
            return;
        }

        Undo.RecordObject(definition, "Delete Product");
        list.RemoveAt(index);
        NormalizeRuntimeDefinitionData();
        EditorUtility.SetDirty(definition);
        MarkSceneDirtyIfNeeded();
        RefreshBinding();
    }

    private void ApplyToRunningGame()
    {
        serializedDefinition.ApplyModifiedProperties();
        NormalizeRuntimeDefinitionData();

        var catalog = FindFirstObjectByType<GlueProductCatalog>();
        var game = FindFirstObjectByType<GlueFactoryGameManager>();
        if (catalog == null || game == null || game.Config == null)
        {
            Debug.LogWarning("Apply failed: need GlueProductCatalog and GlueFactoryGameManager in scene.");
            return;
        }

        try
        {
            catalog.ApplyTo(game.Config);
            game.OnMachineCatalogChanged();
            Debug.Log("Applied product master data to running game.");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void NormalizeOrderAndIds()
    {
        if (productsProperty == null)
        {
            return;
        }

        var used = new HashSet<string>();
        for (var i = 0; i < productsProperty.arraySize; i++)
        {
            var item = productsProperty.GetArrayElementAtIndex(i);
            if (item == null)
            {
                continue;
            }

            item.FindPropertyRelative("shopOrder").intValue = i;

            var idProp = item.FindPropertyRelative("productId");
            var nameProp = item.FindPropertyRelative("displayName");
            var baseId = string.IsNullOrWhiteSpace(idProp.stringValue) ? Slugify(nameProp.stringValue) : Slugify(idProp.stringValue);
            if (string.IsNullOrWhiteSpace(baseId))
            {
                baseId = "glue_product_" + (i + 1);
            }

            var uniqueId = baseId;
            var suffix = 2;
            while (used.Contains(uniqueId))
            {
                uniqueId = baseId + "_" + suffix;
                suffix++;
            }

            idProp.stringValue = uniqueId;
            used.Add(uniqueId);
        }
    }

    private void NormalizeRuntimeDefinitionData()
    {
        if (definition == null)
        {
            return;
        }

        var list = definition.Products;
        var used = new HashSet<string>();
        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null)
            {
                continue;
            }

            var baseId = Slugify(entry.ProductId);
            if (string.IsNullOrWhiteSpace(baseId))
            {
                baseId = Slugify(entry.DisplayName);
            }
            if (string.IsNullOrWhiteSpace(baseId))
            {
                baseId = "glue_product_" + (i + 1);
            }

            var uniqueId = baseId;
            var suffix = 2;
            while (used.Contains(uniqueId))
            {
                uniqueId = baseId + "_" + suffix;
                suffix++;
            }

            used.Add(uniqueId);
            entry.SetData(uniqueId, entry.DisplayName, entry.PieceValue, entry.MachineCost, i, entry.IncludeInShop, entry.UiIcon, entry.MovingGlueScale);
        }
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        var prevUnderscore = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = char.ToLowerInvariant(value[i]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                sb.Append(c);
                prevUnderscore = false;
            }
            else
            {
                if (!prevUnderscore)
                {
                    sb.Append('_');
                    prevUnderscore = true;
                }
            }
        }

        var result = sb.ToString().Trim('_');
        return result;
    }

    private static GlueProductDefinition FindDefinition()
    {
        var managersRoot = GameObject.Find("GlueFactoryManagers");
        if (managersRoot != null)
        {
            var fromManagers = managersRoot.GetComponentInChildren<GlueProductDefinition>(true);
            if (fromManagers != null)
            {
                return fromManagers;
            }
        }

        var sceneRoot = GameObject.Find("GlueProductDefaults");
        if (sceneRoot != null)
        {
            var fromScene = sceneRoot.GetComponent<GlueProductDefinition>();
            if (fromScene != null)
            {
                return fromScene;
            }
        }

        return FindFirstObjectByType<GlueProductDefinition>(FindObjectsInactive.Include);
    }

    private static void MarkSceneDirtyIfNeeded()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
