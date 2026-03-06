#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GlueFactoryAutoApplySceneLayout
{
    private const string Key = "GlueFactory.AutoLayoutApplied.v2";

    static GlueFactoryAutoApplySceneLayout()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    private static void ApplyOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorPrefs.GetBool(Key, false))
        {
            return;
        }

        try
        {
            GlueFactorySceneLayoutBuilder.BuildEditableSceneLayout();
            EditorPrefs.SetBool(Key, true);
            Debug.Log("Glue Factory: SampleScene editable layout applied.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Glue Factory: failed to auto-apply scene layout: " + ex.Message);
        }
    }

    public static void ReApplyNow()
    {
        EditorPrefs.DeleteKey(Key);
        GlueFactorySceneLayoutBuilder.BuildEditableSceneLayout();
        EditorPrefs.SetBool(Key, true);
    }
}
#endif
