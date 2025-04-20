using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text;

namespace SimpleTGBot.MemeGen;

public class DemotivatorGen
{
    private static FontFamily defaultFontFamily;

    public DemotivatorGen()
    {
        
    }

    static DemotivatorGen()
    {
        try
        {
            defaultFontFamily = new FontFamily("DejaVu Serif");
        }
        catch
        {
            defaultFontFamily = new FontFamily(GenericFontFamilies.SansSerif);
        }
    }

    public static DemotivatorStyle DefaultStyle()
    {
        return new DemotivatorStyle()
        {
            BorderThickness = 6,
            Padding = 10,
            OuterMargin = 20,
            CaptionSpacing = 32,
            AdditionalTextWidth = 60,
            OutlineColor = Color.FromArgb(255, 255, 255, 255),
            TitleColor = Color.FromArgb(255, 255, 255, 255),
            SubtitleColor = Color.FromArgb(255, 255, 255, 255),
            BackgroundColor = Color.FromArgb(255, 0, 0, 0),
            TitleFont = new Font(defaultFontFamily, 50),
            SubtitleFont = new Font(defaultFontFamily, 25),
        };
    }

#pragma warning disable CA1416 // мы точно работаем под Windows 7+ (проверено в Program.Main)
    public static MemoryStream MakePictureDemotivator(string picturePath, DemotivatorText[] texts, DemotivatorStyle style)
    {
        Bitmap picture = new Bitmap(picturePath);
        // данная bitmap предназначена только для подсчёта размера текста
        Bitmap bitmap = new Bitmap(1, 1);
        Graphics g = Graphics.FromImage(bitmap);
        Func<string, float> measureTitleString = s => g.MeasureString(s, style.TitleFont).Width;
        Func<string, float> measureSubtitleString = s => g.MeasureString(s, style.SubtitleFont).Width;

        // код расчёта
        SizeF[] subdemSizes = new SizeF[texts.Length];
        float aspectRatio = (float)picture.Height / picture.Width;
        float scaledPictureWidth = Math.Clamp(picture.Width, 800, 1200);
        float scaledPictureHeight = scaledPictureWidth * aspectRatio;
        float contentWidth = scaledPictureWidth + style.Padding * 2;
        float contentHeight = scaledPictureHeight + style.Padding * 2;
        float titleFontHeight = style.TitleFont.GetHeight();
        float subtitleFontHeight = style.SubtitleFont.GetHeight();
        string[][] titles = new string[texts.Length][];
        string[][] subtitles = new string[texts.Length][];
        for (int i = 0; i < texts.Length; ++i)
        {
            float frameWidth = contentWidth + style.Padding * 2.0f + style.BorderThickness * 2.0f;
            float frameHeight = contentHeight + style.Padding * 2.0f + style.BorderThickness * 2.0f;

            string title = texts[i].Title;
            WordWrapResult titleWrap = wordWrap(title, frameWidth + style.AdditionalTextWidth * 2f, frameWidth * 1.5f, measureTitleString);
            titles[i] = titleWrap.lines;

            string subtitle = texts[i].Subtitle;
            WordWrapResult subtitleWrap = wordWrap(subtitle, frameWidth + style.AdditionalTextWidth * 2f, titleWrap.actualWidth, measureSubtitleString);
            subtitles[i] = subtitleWrap.lines;

            float subdemWidth = Math.Max(frameWidth, Math.Max(titleWrap.actualWidth, subtitleWrap.actualWidth));
            subdemWidth += style.Padding * 2f;

            int titleLineCount = titleWrap.lines.Length;
            int subtitleLineCount = subtitleWrap.lines.Length;
            float titleHeight = titleFontHeight * titleLineCount;
            float subtitleHeight = subtitleFontHeight * subtitleLineCount;

            float subdemHeight = style.Padding * 2f + frameHeight + titleHeight + subtitleHeight;
            subdemSizes[i] = new SizeF(subdemWidth, subdemHeight);
            contentWidth = subdemWidth;
            contentHeight = subdemHeight;
        }
        contentHeight += style.OuterMargin * 2f;

        g.Dispose();
        bitmap = new Bitmap((int)contentWidth + (int)style.OuterMargin * 2, (int)contentHeight + (int)style.OuterMargin * 2);
        g = Graphics.FromImage(bitmap);

        // код рисования
        SolidBrush backgroundBrush = new SolidBrush(style.BackgroundColor);
        SolidBrush outlineBrush = new SolidBrush(style.OutlineColor);
        SolidBrush titleBrush = new SolidBrush(style.TitleColor);
        SolidBrush subtitleBrush = new SolidBrush(style.SubtitleColor);
        g.FillRectangle(backgroundBrush, 0, 0, bitmap.Width, bitmap.Height);

        PointF currentOrigin = new PointF(style.OuterMargin, style.OuterMargin);
        for (int j = texts.Length - 1; j >= 0; --j)
        {
            float contWidth = j != 0 ? subdemSizes[j - 1].Width : (scaledPictureWidth + style.Padding * 2f);
            float contHeight = j != 0 ? subdemSizes[j - 1].Height : (scaledPictureHeight + style.Padding * 2f);

            float availableWidth = subdemSizes[j].Width;
            float contX = currentOrigin.X + (availableWidth - contWidth) / 2f;
            float contY = currentOrigin.Y + style.Padding * 2f;

            float bt = style.BorderThickness;
            float pad = style.Padding;
            float capSp = style.CaptionSpacing;

            g.FillRectangle(outlineBrush, contX, contY, contWidth, contHeight);
            g.FillRectangle(backgroundBrush, contX + bt, contY + bt, contWidth - bt * 2f, contHeight - bt * 2f);
            if (j == 0)
            {
                g.DrawImage(picture, contX + bt + pad, contY + bt + pad, contWidth - bt * 2 - pad * 2, contHeight - bt * 2 - pad * 2);
            }

            float titleY = contY + contHeight + capSp;
            foreach (string titleLine in titles[j])
            {
                float titleX = currentOrigin.X + (availableWidth - g.MeasureString(titleLine, style.TitleFont).Width) / 2f;
                g.DrawString(titleLine, style.TitleFont, titleBrush, titleX, titleY);
                titleY += titleFontHeight;
            }

            float subtitleY = titleY + capSp;
            string[] subtitleLines = subtitles[j];
            if (subtitleLines.Length > 0)
            {
                foreach (string subtitleLine in subtitleLines)
                {
                    float subtitleX = currentOrigin.X + (availableWidth - measureSubtitleString(subtitleLine)) / 2f;
                    g.DrawString(subtitleLine, style.SubtitleFont, subtitleBrush, subtitleX, subtitleY);
                    subtitleY += subtitleFontHeight;
                }
            }
            currentOrigin.X += bt + (availableWidth - contWidth) / 2f;
            currentOrigin.Y += bt + pad;
        }

        picture.Dispose();
        MemoryStream outStream = new MemoryStream();
        bitmap.Save(outStream, ImageFormat.Png);
        outStream.Seek(0, SeekOrigin.Begin);
        return outStream;
    }

    private static WordWrapResult wordWrap(string rawText, float width, float maxWidth, Func<string, float> measureString)
    {
        float free = width;
        StringBuilder wrappedText = new StringBuilder();
        int words = (rawText.Length != 0) ? (rawText.Count(c => c == ' ') + 1) : 0;
        int rawPosition = 0;
        float actualWidth = 0;
        bool trailingReturn = false;
        while (rawPosition < rawText.Length)
        {
            string word = takeWord(rawText, rawPosition);
            float wordWidth = measureString(word + ' ');
            if (wordWidth <= free)
            {
                wrappedText.Append(word);
                wrappedText.Append(' ');
                trailingReturn = false;
                free -= wordWidth;
                actualWidth = Math.Max(width - free, actualWidth);
                rawPosition += word.Length + 1;
                words--;
            }
            else if (wordWidth <= width)
            {
                if (!trailingReturn)
                    wrappedText.Append('\n');
                wrappedText.Append(word);
                wrappedText.Append(' ');
                trailingReturn = false;
                free = width - wordWidth;
                rawPosition += word.Length + 1;
                words--;
            }
            else if (wordWidth <= maxWidth)
            {
                actualWidth = Math.Max(actualWidth, wordWidth);
                if (!trailingReturn)
                    wrappedText.Append('\n');
                wrappedText.Append(word);
                wrappedText.Append('\n');
                trailingReturn = true;
                free = width;
                rawPosition += word.Length + 1;
                words--;
            }
            else
            {
                // TODO заменить на что-то поэффективнее
                float substrWidth = 0;
                int substrLength = word.Length;
                for (int c = 0; c < word.Length; ++c)
                {
                    float charWidth = measureString(word[c].ToString());
                    if (substrWidth + charWidth > maxWidth)
                    {
                        substrLength = c;
                        break;
                    }
                    substrWidth += charWidth;
                }
                if (!trailingReturn)
                    wrappedText.Append('\n');
                wrappedText.Append(word.Substring(0, substrLength));
                wrappedText.Append('\n');
                trailingReturn = true;
                free = width;
                rawPosition += substrLength;
                actualWidth = maxWidth;
            }
        }
        return new WordWrapResult()
        {
            lines = wrappedText.ToString().Split('\n'),
            actualWidth = actualWidth,
        };
    }

    private static string takeWord(string text, int position)
    {
        for (int i = position; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                return text.Substring(position, i - position);
            }
        }
        return text.Substring(position);
    }

    struct WordWrapResult
    {
        public string[] lines;
        public float actualWidth;
    }
}
