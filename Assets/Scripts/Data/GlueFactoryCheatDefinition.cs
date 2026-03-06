using UnityEngine;

public sealed class GlueFactoryCheatDefinition : MonoBehaviour
{
    [SerializeField] private bool enableCheatButton = true;
    [SerializeField] private bool showToastOnApply = true;
    [SerializeField] private double defaultTypedAmount = 1000d;
    [SerializeField] private bool showSaveButton = true;
    [SerializeField] private bool showResetButton = true;
    [SerializeField] private bool showExitButton = true;
    [SerializeField] private bool showCheatButton = true;

    public bool EnableCheatButton => enableCheatButton;
    public bool ShowToastOnApply => showToastOnApply;
    public double DefaultTypedAmount => defaultTypedAmount;
    public bool ShowSaveButton => showSaveButton;
    public bool ShowResetButton => showResetButton;
    public bool ShowExitButton => showExitButton;
    public bool ShowCheatButton => showCheatButton;

    public void EnsureDefaults()
    {
        if (defaultTypedAmount < 0d)
        {
            defaultTypedAmount = 0d;
        }
    }
}
