namespace SimpleTGBot;

internal class DialogData
{
    public DialogState state;
    public string? inputPictureFilename;
}

enum DialogState
{
    Initial,
    AwaitingPicture,
    AwaitingTitle,
    AwaitingSubtitle,
    ShowingResult,
}
