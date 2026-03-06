#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GlueFactoryPrefabTools
{
    public static void CreateMachinePrefabFromSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Select a scene machine GameObject first.");
            return;
        }

        EnsureFolder("Assets/Resources");

        const string targetPath = "Assets/Resources/MachineVisualPrefab.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(selected, targetPath, InteractionMode.UserAction);

        Debug.Log("Created machine prefab: " + targetPath + "\nWorldManager will auto-load it and only show when slot has installed machine.");
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        var parts = assetPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
