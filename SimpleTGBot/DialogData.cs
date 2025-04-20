namespace SimpleTGBot;

internal class DialogData
{
    public DialogState state;
    public string? inputPictureFilename;
    public string? inputTitle;
    public UserPreset[]? shownPresets;
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
}
