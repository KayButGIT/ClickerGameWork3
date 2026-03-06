#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlueFactoryUpgradeListTools
{
    [MenuItem("GlueFactory/Generate Editable Default Upgrade List")]
    public static void GenerateEditableDefaultUpgradeList()
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

        var upgradeRoot = managers.transform.Find("GlueUpgradeDefaults");
        if (upgradeRoot == null)
        {
            var go = new GameObject("GlueUpgradeDefaults");
            go.transform.SetParent(managers.transform, false);
            upgradeRoot = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create GlueUpgradeDefaults");
        }

        var def = upgradeRoot.GetComponent<GlueUpgradeDefinition>();
        if (def == null)
        {
            def = Undo.AddComponent<GlueUpgradeDefinition>(upgradeRoot.gameObject);
        }

        Undo.RecordObject(def, "Ensure Upgrade Defaults");
        def.EnsureDefaults();

        EditorUtility.SetDirty(managers);
        EditorUtility.SetDirty(def);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = upgradeRoot.gameObject;

        Debug.Log("Generated/ensured editable GlueUpgradeDefinition list under GlueFactoryManagers/GlueUpgradeDefaults.");
    }
}
#endif
