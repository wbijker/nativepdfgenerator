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

    /// <summary>An 8-bit RGB diagonal-gradient buffer for demo images.</summary>
    public static byte[] GradientRgb(int width, int height)
    {
        var rgb = new byte[width * height * 3];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 3;
                rgb[i + 0] = (byte)(255 * x / (width - 1));
                rgb[i + 1] = (byte)(255 * y / (height - 1));
                rgb[i + 2] = (byte)(255 - 255 * x / (width - 1));
            }
        }
        return rgb;
    }

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

    // ----- section 2: Cols with Fixed / Auto / Relative width sizing -----

    public static UIElement SectionCols() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(2, "Cols") },
            new SlotElement { Content = Caption(
                "Cols arranges slots side by side. Each column's width is its sizing intent: " +
                "Fixed (in points), Auto (sized to content's natural width), or Relative " +
                "(sharing the remaining width by weight).") },

            new SlotElement { Content = Subheading("Fixed-width columns (80 / 120 / 200 pt)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Sizing = Sizing.Fixed, Length = 80, Background = Colors.PaleRed,
                            Content = Label("80 pt") },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 120, Background = Colors.PaleGreen,
                            Content = Label("120 pt") },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 200, Background = Colors.PaleBlue,
                            Content = Label("200 pt") },
                    },
                },
            },

            new SlotElement { Content = Subheading("Auto-width columns (sized to content)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Background = Colors.PaleRed, Content = Label("Short") },
                        new SlotElement { Background = Colors.PaleGreen, Content = Label("A medium label") },
                        new SlotElement { Background = Colors.PaleBlue, Content = Label("A noticeably longer label") },
                    },
                },
            },

            new SlotElement { Content = Subheading("Relative-width columns (share remaining width by weight)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleRed,
                            Content = Label("Weight 1") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 2, Background = Colors.PaleGreen,
                            Content = Label("Weight 2 — takes twice the share") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleBlue,
                            Content = Label("Weight 1") },
                    },
                },
            },

            new SlotElement { Content = Subheading("Mixed widths (Fixed + Auto + Relative)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Sizing = Sizing.Fixed, Length = 100, Background = Colors.PaleRed,
                            Content = Label("Fixed 100 pt") },
                        new SlotElement { Background = Colors.PaleGreen, Content = Label("Auto — sized to content") },
                        new SlotElement { Sizing = Sizing.Relative, Length = 1, Background = Colors.PaleBlue,
                            Content = Label("Relative — fills the rest") },
                    },
                },
            },
        },
    };

    // ----- section 3: ExtendHorizontal (full-width bands) -----

    public static UIElement SectionExtends() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(3, "ExtendHorizontal") },
            new SlotElement { Content = Caption(
                "ExtendHorizontal makes an element claim the full available width, so " +
                "background and border span the page (not just the content). Without it " +
                "the element is sized to its content and aligned within the parent.") },

            new SlotElement { Content = Subheading("Full-width band on a TextElement") },
            new SlotElement
            {
                Content = new TextElement("Banner with ExtendHorizontal = true", Bold, 13)
                {
                    Background = Colors.DarkBlue,
                    FontColor = Colors.White,
                    Padding = 10,
                    ExtendHorizontal = true,
                },
            },

            new SlotElement { Content = Subheading("Full-width band on a ColsElement (left / right with a relative spacer)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Background = Colors.PaleGreen,
                    Padding = 8,
                    ExtendHorizontal = true,
                    Slots =
                    {
                        new SlotElement { Content = new TextElement("Left edge", Bold, 11) },
                        new SlotElement { Sizing = Sizing.Relative },
                        new SlotElement { Content = new TextElement("Right edge", Bold, 11) },
                    },
                },
            },

            new SlotElement { Content = Subheading("Content-sized band (no ExtendHorizontal)") },
            new SlotElement
            {
                Content = new TextElement("Content-sized banner — sized to the text plus padding", Bold, 13)
                {
                    Background = Colors.PaleRed,
                    Padding = 10,
                },
            },
        },
    };

    // ----- section 4: Image (raster, DeviceRGB) -----

    public static UIElement SectionImage() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(4, "Image") },
            new SlotElement { Content = Caption(
                "ImageElement embeds an 8-bit DeviceRGB image as a PDF XObject. " +
                "The bytes are sent once and the same reference is reused if the element " +
                "is rendered on multiple pages.") },

            new SlotElement { Content = Subheading("Single image (120 × 80 pt) with a thin border") },
            new SlotElement
            {
                Content = new ImageElement(GradientRgb(128, 128), 128, 128, 120, 80)
                {
                    BorderColor = Colors.Gray,
                    BorderThickness = 1,
                },
            },

            new SlotElement { Content = Subheading("Three sizes side by side") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement
                        {
                            Content = new ImageElement(GradientRgb(64, 64), 64, 64, 80, 60)
                                { BorderColor = Colors.Gray, BorderThickness = 1 },
                        },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 12 },
                        new SlotElement
                        {
                            Content = new ImageElement(GradientRgb(96, 96), 96, 96, 100, 70)
                                { BorderColor = Colors.Gray, BorderThickness = 1 },
                        },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 12 },
                        new SlotElement
                        {
                            Content = new ImageElement(GradientRgb(128, 128), 128, 128, 120, 80)
                                { BorderColor = Colors.Gray, BorderThickness = 1 },
                        },
                    },
                },
            },
        },
    };
}
