using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Stateful pointer into a paragraph's word list. The same instance is
/// threaded through a Paragraph's continuation chain — when a Paragraph
/// renders Partial, the continuation wraps THIS iterator so the next
/// slot resumes exactly where the previous left off.
/// </summary>
public sealed class WordIterator
{
    private readonly string[] _words;
    private int _i;

    public WordIterator(string[] words)
    {
        _words = words;
    }

    public bool Done => _i >= _words.Length;

    public bool TryPeek(out string word)
    {
        if (Done) { word = string.Empty; return false; }
        word = _words[_i];
        return true;
    }

    public void MoveNext() => _i++;
}

public class Paragraph : Element
{
    private readonly WordIterator _iterator;

    public Paragraph(string text, Font font, double fontSize)
        : this(new WordIterator(text.Split(' ', StringSplitOptions.RemoveEmptyEntries)), font, fontSize)
    {
        Text = text;
    }

    public Paragraph(WordIterator iterator, Font font, double fontSize)
    {
        _iterator = iterator;
        Font = font;
        FontSize = fontSize;
        LineHeight = Font.GetVerticalMetrics(FontSize).LineHeight;
        Text = string.Empty;
    }

    public double LineHeight { get; }

    public string Text { get; set; }
    public Font Font { get; set; }
    public double FontSize { get; set; }

    /// <summary>Fill colour for the glyphs. <c>null</c> = device default (black).</summary>
    public PdfColor? Color { get; set; }

    /// <summary>When true a horizontal rule is drawn under each wrapped line at ~10% of <see cref="FontSize"/> below the baseline. Currently not drawn by <see cref="RenderCore"/>; surface preserved for the fluent <see cref="IText"/> API.</summary>
    public bool Underline { get; set; }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        double maxWordWidth = 0;
        foreach (var word in Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = Font.MeasureText(word, FontSize);
            if (w > maxWordWidth) maxWordWidth = w;
        }
        return new PdfSizeHint(maxWordWidth, LineHeight, null, null);
    }

    protected override RenderResult RenderCore(ContentStream cs, PdfSize available)
    {
        if (_iterator.Done) return RenderResult.Done(0);
        if (available.Height < LineHeight) return RenderResult.DoesNotFit(this);

        double spaceWidth = Font.MeasureText(" ", FontSize);
        var text = cs.AddText(Font, FontSize).SetLeading(LineHeight);
        if (Color is { } colour) text.SetFillColor(colour);

        double y = 0;
        bool first = true;
        while (!_iterator.Done && y + LineHeight <= available.Height)
        {
            var line = TakeLine(available.Width, spaceWidth);
            if (line.Length == 0) break;

            if (first)
            {
                text.Show(0, 0, line);
                first = false;
            }
            else
            {
                text.NextLineShowText(line);
            }
            y += LineHeight;
        }
        text.Build();

        if (_iterator.Done) 
            return RenderResult.Done(y);
        
        return new RenderResult(y, new Paragraph(_iterator, Font, FontSize) { Color = Color });
    }

    /// <summary>
    /// Greedily consume words from the iterator until the next one would
    /// overflow <paramref name="width"/>. A single word wider than the
    /// slot is force-emitted on its own line to guarantee forward
    /// progress.
    /// </summary>
    private string TakeLine(double width, double spaceWidth)
    {
        var sb = new System.Text.StringBuilder();
        double x = 0;
        while (_iterator.TryPeek(out var word))
        {
            double w = Font.MeasureText(word, FontSize);
            double next = x == 0 ? w : x + spaceWidth + w;
            if (next > width)
            {
                if (x == 0)
                {
                    _iterator.MoveNext();
                    return word;
                }
                break;
            }
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(word);
            x = next;
            _iterator.MoveNext();
        }
        return sb.ToString();
    }
}
