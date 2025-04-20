using System.Drawing;

namespace SimpleTGBot.MemeGen;

public record DemotivatorText {
    public string Title { get; init; }
    public string Subtitle { get; init; }
}

public record DemotivatorStyle
{
    public float BorderThickness { get; set; }
    public float Padding { get; set; }
    public float OuterMargin { get; set; }
    public float CaptionSpacing { get; set; }
    public float AdditionalTextWidth { get; set; }
    public Color OutlineColor { get; set; }
    public Color TitleColor { get; set; }
    public Color SubtitleColor { get; set; }
    public Color BackgroundColor { get; set; }
    public Font TitleFont { get; set; }
    public Font SubtitleFont { get; set; }
}
