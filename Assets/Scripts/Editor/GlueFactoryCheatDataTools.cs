#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlueFactoryCheatDataTools
{
    [MenuItem("GlueFactory/Generate Editable Default Cheat Data")]
    public static void GenerateEditableDefaultCheatData()
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

        var defaultsRoot = managers.transform.Find("GlueCheatDefaults");
        if (defaultsRoot == null)
        {
            var go = new GameObject("GlueCheatDefaults");
            go.transform.SetParent(managers.transform, false);
            defaultsRoot = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create GlueCheatDefaults");
        }

        var cheatDefinition = defaultsRoot.GetComponent<GlueFactoryCheatDefinition>();
        if (cheatDefinition == null)
        {
            cheatDefinition = Undo.AddComponent<GlueFactoryCheatDefinition>(defaultsRoot.gameObject);
        }

        Undo.RegisterFullObjectHierarchyUndo(defaultsRoot.gameObject, "Generate Cheat Defaults");
        cheatDefinition.EnsureDefaults();
        EditorUtility.SetDirty(cheatDefinition);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = defaultsRoot.gameObject;

        Debug.Log("Generated/ensured editable cheat data under GlueFactoryManagers/GlueCheatDefaults.");
    }
}
#endif
