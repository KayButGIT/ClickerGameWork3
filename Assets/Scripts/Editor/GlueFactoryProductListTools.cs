#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlueFactoryProductListTools
{
    [MenuItem("GlueFactory/Generate Editable Default Product List")]
    public static void GenerateEditableDefaultProductList()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene.");
            return;
        }

        var managers = GameObject.Find("GlueFactoryManagers");
        if (managers == null)
        {
            managers = new GameObject("GlueFactoryManagers");
            Undo.RegisterCreatedObjectUndo(managers, "Create GlueFactoryManagers");
        }

        var catalog = managers.GetComponent<GlueProductCatalog>();
        if (catalog == null)
        {
            catalog = Undo.AddComponent<GlueProductCatalog>(managers);
        }

        Undo.RegisterFullObjectHierarchyUndo(managers, "Generate Glue Product Defaults");
        catalog.EnsureDefaultDefinitions();

        EditorUtility.SetDirty(managers);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("Generated/ensured editable default GlueProductDefinition list under GlueFactoryManagers.");
        Selection.activeGameObject = managers;
    }
}
#endif
