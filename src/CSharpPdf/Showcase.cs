using CSharpPdf.Layout;
using CSharpPdf.Text;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf;

/// <summary>
/// Progressively built showcase sections. Each <c>SectionXxx</c> method returns a
/// self-contained <see cref="UIElement"/> that the engine can place sequentially.
/// The samples (35, 36, 37, ...) call these in order so each successive sample
/// shows one additional section.
/// </summary>
internal static class Showcase
{
    static readonly Font Body = Standard14Font.Helvetica;
    static readonly Font Bold = Standard14Font.HelveticaBold;

    // ----- small text builders shared across sections -----

    public static TextElement SectionHeading(int number, string title) => new($"{number}. {title}", Bold, 22)
    {
        FontColor = Colors.DarkBlue,
        Padding = 4,
    };

    public static TextElement Subheading(string text) => new(text, Bold, 13)
    {
        FontColor = Colors.DarkBlue,
        Padding = 4,
    };

    public static TextElement Caption(string text) => new(text, Body, 10)
    {
        FontColor = Colors.Gray,
        Padding = 4,
    };

    public static TextElement Label(string text) => new(text, Body, 10) { Padding = 6 };

    // ----- section 1: Rows with Fixed / Auto / Relative sizing -----

    public static UIElement SectionRows() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(1, "Rows") },
            new SlotElement { Content = Caption(
                "Rows stacks slots vertically. Each row's height is its sizing intent: " +
                "Fixed (in points), Auto (sized to content), or Relative (sharing the " +
                "remaining height by weight).") },
            new SlotElement { Content = Subheading("Fixed rows") },
            new SlotElement { Sizing = Sizing.Fixed, Length = 30, Background = Colors.PaleRed,
                Content = Label("Fixed 30 pt") },
            new SlotElement { Sizing = Sizing.Fixed, Length = 50, Background = Colors.PaleGreen,
                Content = Label("Fixed 50 pt") },
            new SlotElement { Sizing = Sizing.Fixed, Length = 20, Background = Colors.PaleBlue,
                Content = Label("Fixed 20 pt") },

            new SlotElement { Content = Subheading("Auto rows (sized to content)") },
            new SlotElement { Background = Colors.PaleRed, Content = Label("Short content") },
            new SlotElement { Background = Colors.PaleGreen,
                Content = Label("Slightly longer content. Auto rows take exactly the height " +
                                "their content needs at the available width.") },

            new SlotElement { Content = Subheading("Relative rows (share remaining height by weight)") },
            new SlotElement
            {
                Sizing = Sizing.Fixed, Length = 180, Background = Colors.PaleGray,
                Content = new RowsElement
                {
                    Slots =
                    {
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleRed,
                            Content = Label("Relative — weight 1") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 2, Background = Colors.PaleGreen,
                            Content = Label("Relative — weight 2 (takes twice the share)") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleBlue,
                            Content = Label("Relative — weight 1") },
                    },
                },
            },

            new SlotElement { Content = Subheading("Mixed sizing (Fixed + Auto + Relative inside a 180 pt frame)") },
            new SlotElement
            {
                Sizing = Sizing.Fixed, Length = 180, Background = Colors.PaleGray,
                Content = new RowsElement
                {
                    Slots =
                    {
                        new SlotElement { Sizing = Sizing.Fixed, Length = 30, Background = Colors.PaleRed,
                            Content = Label("Fixed 30 pt") },
                        new SlotElement { Sizing = Sizing.Auto, Background = Colors.PaleGreen,
                            Content = Label("Auto — sized to content") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleBlue,
                            Content = Label("Relative — fills the rest") },
                    },
                },
            },
        },
    };
}
