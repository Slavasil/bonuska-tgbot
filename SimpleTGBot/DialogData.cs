namespace SimpleTGBot;

internal class DialogData
{
    public DialogState state;
    public string? inputPictureFilename;
    public string? inputTitle;
    public UserPreset[]? shownPresets;
    public UserPreset incompletePreset;
}

enum DialogState
{
    Initial,
    AwaitingPicture,
    AwaitingTitle,
    AwaitingSubtitle,
    ShowingResult,
    Settings,
    ViewingPresets,
    ChoosingPreset,
    CreatingPreset_AwaitingName,
    CreatingPreset_AwaitingOutlineColor,
    CreatingPreset_AwaitingTitleColor,
    CreatingPreset_AwaitingSubtitleColor,
}
