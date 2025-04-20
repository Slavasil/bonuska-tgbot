namespace SimpleTGBot;

internal class DialogData
{
    public DialogState state;
    public string? inputPictureFilename;
    public string? inputTitle;
}

enum DialogState
{
    Initial,
    AwaitingPicture,
    AwaitingTitle,
    AwaitingSubtitle,
    ShowingResult,
}
