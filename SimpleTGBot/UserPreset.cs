using System.Drawing;

namespace SimpleTGBot;

internal struct UserPreset
{
    public long Id;
    public long OwnerId;
    public string Name;
    public Color OutlineColor;
    public Color TitleColor;
    public Color SubtitleColor;

    public static UserPreset Default() => new UserPreset()
    {
        Id = 0,
        OwnerId = 0,
        Name = "По умолчанию",
        OutlineColor = Color.FromArgb(255, 255, 255, 255),
        TitleColor = Color.FromArgb(255, 255, 255, 255),
        SubtitleColor = Color.FromArgb(255, 255, 255, 255),
    };
}
