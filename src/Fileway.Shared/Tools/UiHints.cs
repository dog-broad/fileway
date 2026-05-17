namespace Fileway.Shared.Tools;

[Flags]
public enum UiHints
{
    None = 0,
    ShowQualitySlider = 1 << 0,
    ShowPageSelector = 1 << 1,
    ShowDimensionInputs = 1 << 2,
    ShowOrderableList = 1 << 3,
    ShowSplitControls = 1 << 4
}
