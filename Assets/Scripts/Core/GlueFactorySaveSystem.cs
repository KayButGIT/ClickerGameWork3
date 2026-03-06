using System;
using UnityEngine;

public sealed class GlueFactorySaveSystem : MonoBehaviour
{
    [Serializable]
    public sealed class SaveData
    {
        public double money;
        public double totalEarned;
        public int clickLevel;
        public int conveyorLevel;
        public int boostLevel;
        public int speedLevel;
        public int selectedSlot;
        public int selectedMachine;
        public int[] slotMachineIds;
    }

    [SerializeField] private string saveKey = "GF_SAVE";

    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(saveKey))
        {
            return false;
        }

        var json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            data = null;
        }

        return data != null;
    }

    public void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }
}
