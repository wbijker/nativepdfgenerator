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

    // ----- section 5: SVG rendering -----

    private const string SvgStar = """
        <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
          <polygon points="50,5 61,38 96,38 68,59 79,93 50,72 21,93 32,59 4,38 39,38"
                   fill="#FFC107" stroke="#B27400" stroke-width="2"/>
        </svg>
        """;

    private const string SvgHeart = """
        <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
          <path d="M50 88 C 8 56, 8 16, 50 30 C 92 16, 92 56, 50 88 Z"
                fill="#E53935" stroke="#7F1D1D" stroke-width="2"/>
        </svg>
        """;

    private const string SvgShapes = """
        <svg viewBox="0 0 240 100" xmlns="http://www.w3.org/2000/svg">
          <rect x="6" y="6" width="60" height="88" fill="#90CAF9" stroke="#0D47A1" stroke-width="2"/>
          <circle cx="110" cy="50" r="38" fill="#A5D6A7" stroke="#1B5E20" stroke-width="2"/>
          <ellipse cx="190" cy="50" rx="44" ry="30" fill="#F8BBD0" stroke="#880E4F" stroke-width="2"/>
        </svg>
        """;

    private const string SvgPolylines = """
        <svg viewBox="0 0 200 100" xmlns="http://www.w3.org/2000/svg">
          <polyline points="10,90 40,30 70,70 100,15 130,80 160,25 190,60"
                    fill="none" stroke="#1565C0" stroke-width="3"/>
          <line x1="10" y1="90" x2="190" y2="90" stroke="gray" stroke-width="1"/>
        </svg>
        """;

    public static UIElement SectionSvg() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(5, "SVG") },
            new SlotElement { Content = Caption(
                "SvgElement parses a subset of SVG (rect, circle, ellipse, line, polygon, " +
                "polyline, path with M/L/H/V/C/S/Q/T/Z and groups with transform) and emits " +
                "PDF content-stream operators. The viewBox is mapped to the requested " +
                "display rectangle.") },

            new SlotElement { Content = Subheading("Polygon — star (fill + stroke)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Content = new SvgElement(SvgStar, 100, 100) },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 20 },
                        new SlotElement { Content = new SvgElement(SvgStar, 60, 60) },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 20 },
                        new SlotElement { Content = new SvgElement(SvgStar, 40, 40) },
                    },
                },
            },

            new SlotElement { Content = Subheading("Path — heart (cubic beziers)") },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { Content = new SvgElement(SvgHeart, 100, 100) },
                        new SlotElement { Sizing = Sizing.Fixed, Length = 20 },
                        new SlotElement { Content = new SvgElement(SvgHeart, 70, 70) },
                    },
                },
            },

            new SlotElement { Content = Subheading("Basic shapes — rect / circle / ellipse") },
            new SlotElement { Content = new SvgElement(SvgShapes, 360, 150) },

            new SlotElement { Content = Subheading("Polyline + line — chart-style path") },
            new SlotElement { Content = new SvgElement(SvgPolylines, 360, 180) },
        },
    };

    // ----- section 6: Tables -----

    public static UIElement SectionTables() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(6, "Tables") },
            new SlotElement { Content = Caption(
                "TableElement auto-sizes columns from content (max of min and preferred across " +
                "rows), distributes them to fit the page width, draws per-cell borders, and " +
                "repeats the header on every page when the table paginates between rows.") },

            new SlotElement { Content = Subheading("Invoice table with 18 rows") },
            new SlotElement { Content = BuildInvoiceTable(18) },
        },
    };

    // ----- section 7: Header / Footer -----

    /// <summary>The page header used by the showcase engine (sample 41 onward).</summary>
    public static UIElement ShowcaseHeader() => new ColsElement
    {
        Background = Colors.DarkBlue,
        Padding = 8,
        ExtendHorizontal = true,
        Slots =
        {
            new SlotElement { Content = new TextElement("CSharpPdf Showcase", Bold, 12) { FontColor = Colors.White } },
            new SlotElement { Sizing = Sizing.Relative },
            new SlotElement { Content = new TextElement("Programmatic UI Layer", Body, 10) { FontColor = Colors.White } },
        },
    };

    /// <summary>The page footer used by the showcase engine (sample 41 onward).</summary>
    public static UIElement ShowcaseFooter() => new ColsElement
    {
        Padding = 6,
        BorderColor = Colors.LightGray,
        BorderThickness = 0.5,
        ExtendHorizontal = true,
        Slots =
        {
            new SlotElement { Content = new TextElement("github.com/itecho/CSharpPdf", Body, 9) { FontColor = Colors.Gray } },
            new SlotElement { Sizing = Sizing.Relative },
            new SlotElement { Content = new PageNumberElement(Body, 9) { Format = "Page {0}", FontColor = Colors.Gray } },
        },
    };

    public static UIElement SectionHeaderFooter() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(7, "Header & Footer") },
            new SlotElement { Content = Caption(
                "Set LayoutEngine.Header and LayoutEngine.Footer once; the engine measures " +
                "and re-renders them at the top and bottom of every new page and reserves " +
                "their height from the content area. The footer here carries a PageNumberElement, " +
                "which reads the current page from the PdfContext at render time, so every " +
                "page shows its own number.") },
        },
    };

    // ----- section 8: Multi-column layout (newspaper-style flow) -----

    private static string LongProse(int paragraphs)
    {
        const string para1 =
            "The layout engine flows a single block of content across several equal-width " +
            "columns side by side. The first column renders to the column height, and " +
            "whatever doesn't fit becomes overflow that the second column picks up — and so " +
            "on across every column.";
        const string para2 =
            "If the content is still not exhausted after the last column, the whole block " +
            "returns its own overflow and the engine continues the flow on the next page. " +
            "Long paragraphs, short paragraphs, even paragraphs that wrap mid-line at the " +
            "column boundary are all handled by the same machinery — the column doesn't " +
            "care what's inside, only that the inner element returns the right overflow.";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < paragraphs; i++)
        {
            sb.Append(i % 2 == 0 ? para1 : para2);
            sb.Append("\n\n");
        }
        return sb.ToString().TrimEnd();
    }

    public static UIElement SectionMultiColumn() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(8, "Multi-column layout") },
            new SlotElement { Content = Caption(
                "MultiColumnElement flows a single child across N equal-width columns: " +
                "column 1 fills to the height, its overflow flows into column 2, and so on. " +
                "Whatever doesn't fit in the last column overflows the whole block, so the " +
                "next page continues the flow.") },

            new SlotElement { Content = Subheading("Two columns, 220 pt tall") },
            new SlotElement
            {
                Content = new MultiColumnElement(
                    new TextElement(LongProse(2), Body, 10), columns: 2, height: 220, gap: 16),
            },

            new SlotElement { Content = Subheading("Three columns, 240 pt tall") },
            new SlotElement
            {
                Content = new MultiColumnElement(
                    new TextElement(LongProse(3), Body, 10), columns: 3, height: 240, gap: 14),
            },
        },
    };

    // ----- section 9: Borders (solid, dashed, rounded) -----

    public static UIElement SectionBorders() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(9, "Borders") },
            new SlotElement { Content = Caption(
                "Every UIElement exposes BorderColor, BorderThickness, BorderRadius (corner " +
                "radius in points), and BorderDash (a points-on/points-off pattern). Setting " +
                "BorderRadius switches background fill and border stroke to rounded paths.") },

            new SlotElement { Content = Subheading("Solid borders") },
            new SlotElement { Content = new ColsElement
            {
                Slots =
                {
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Solid 1 pt", Body, 11) {
                            Background = Colors.PaleGreen, BorderColor = Colors.Green, BorderThickness = 1, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Solid 2 pt", Body, 11) {
                            Background = Colors.PaleGreen, BorderColor = Colors.Green, BorderThickness = 2, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Solid 4 pt", Body, 11) {
                            Background = Colors.PaleGreen, BorderColor = Colors.Green, BorderThickness = 4, Padding = 10 } },
                },
            } },

            new SlotElement { Content = Subheading("Dashed borders (BorderDash patterns)") },
            new SlotElement { Content = new ColsElement
            {
                Slots =
                {
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Dash 4 / 2", Body, 11) {
                            Background = Colors.PaleBlue, BorderColor = Colors.Blue, BorderThickness = 1,
                            BorderDash = new[] { 4.0, 2.0 }, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Dash 6 / 3 / 2 / 3", Body, 11) {
                            Background = Colors.PaleBlue, BorderColor = Colors.Blue, BorderThickness = 1,
                            BorderDash = new[] { 6.0, 3.0, 2.0, 3.0 }, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Dotted (1 / 2)", Body, 11) {
                            Background = Colors.PaleBlue, BorderColor = Colors.Blue, BorderThickness = 1.5,
                            BorderDash = new[] { 1.0, 2.0 }, Padding = 10 } },
                },
            } },

            new SlotElement { Content = Subheading("Rounded borders (BorderRadius)") },
            new SlotElement { Content = new ColsElement
            {
                Slots =
                {
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Radius 4 pt", Body, 11) {
                            Background = Colors.PaleRed, BorderColor = Colors.Red, BorderThickness = 1,
                            BorderRadius = 4, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Radius 10 pt", Body, 11) {
                            Background = Colors.PaleRed, BorderColor = Colors.Red, BorderThickness = 1,
                            BorderRadius = 10, Padding = 10 } },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                    new SlotElement { Sizing = Sizing.Fixed, Length = 140,
                        Content = new TextElement("Pill (radius 16)", Body, 11) {
                            Background = Colors.PaleRed, BorderColor = Colors.Red, BorderThickness = 1,
                            BorderRadius = 16, Padding = 10 } },
                },
            } },

            new SlotElement { Content = Subheading("Rounded + dashed combined") },
            new SlotElement { Content = new TextElement(
                "A wide rounded-corner panel with a dashed gray border and a translucent " +
                "pale-yellow fill. Rounded and dashed are independent — combine freely.",
                Body, 11) {
                    Background = Colors.PaleYellow, BorderColor = Colors.Gray, BorderThickness = 1,
                    BorderDash = new[] { 5.0, 3.0 }, BorderRadius = 8, Padding = 12,
                    ExtendHorizontal = true } },
        },
    };

    // ----- section 10: Layer overlays -----

    private const string SvgBadge = """
        <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
          <circle cx="50" cy="50" r="42" fill="rgba(0,0,0,0)" stroke="#FFFFFF" stroke-width="3"/>
          <polygon points="50,18 59,42 84,42 64,57 72,82 50,67 28,82 36,57 16,42 41,42"
                   fill="#FFC107" stroke="#FFFFFF" stroke-width="1.5"/>
        </svg>
        """;

    public static UIElement SectionLayers() => new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = SectionHeading(10, "Layer overlays") },
            new SlotElement { Content = Caption(
                "LayersElement draws every child at the same origin and the same size, in " +
                "z-order — index 0 is the bottom layer, the next child paints on top, and so " +
                "on. PDF naturally composites by content-stream order, so the same machinery " +
                "stacks images, SVG, text, and any other UIElement without special cases.") },

            new SlotElement { Content = Subheading("Image background + SVG badge + text overlay") },
            new SlotElement
            {
                Content = new LayersElement(180,
                    // Layer 1: the gradient image fills the whole block.
                    new ImageElement(GradientRgb(128, 128), 128, 128, 0, 0)
                        { ExtendHorizontal = true },
                    // Layer 2: an SVG badge floats on the right, vertically centered-ish.
                    new ColsElement
                    {
                        Slots =
                        {
                            new SlotElement { Sizing = Sizing.Relative },
                            new SlotElement { VAlign = VerticalAlignment.Middle,
                                Content = new SvgElement(SvgBadge, 90, 90) },
                            new SlotElement { Sizing = Sizing.Fixed, Length = 20 },
                        },
                    },
                    // Layer 3: bottom text caption with a translucent-looking dark band.
                    new RowsElement
                    {
                        Slots =
                        {
                            new SlotElement { Sizing = Sizing.Relative },
                            new SlotElement
                            {
                                Background = Colors.DarkBlue,
                                ExtendHorizontal = true,
                                Padding = 8,
                                Content = new TextElement("Featured — top story of the week", Bold, 14)
                                    { FontColor = Colors.White },
                            },
                        },
                    }
                ),
            },

            new SlotElement { Content = Subheading("Plain Cols beneath a rounded ribbon overlay") },
            new SlotElement
            {
                Content = new LayersElement(100,
                    // Bottom: a content row.
                    new ColsElement
                    {
                        ExtendHorizontal = true,
                        Padding = 14,
                        Background = Colors.PaleGreen,
                        Slots =
                        {
                            new SlotElement { Content = new TextElement("Long form content sits underneath…", Body, 11) },
                            new SlotElement { Sizing = Sizing.Relative },
                            new SlotElement { Content = new TextElement("Read more →", Body, 11) },
                        },
                    },
                    // Top: a rounded ribbon labelling the block, sized to its content
                    // and positioned via Cols/Rows (relative spacers + alignment).
                    new RowsElement
                    {
                        Slots =
                        {
                            new SlotElement { Sizing = Sizing.Fixed, Length = 10 },
                            new SlotElement
                            {
                                Content = new ColsElement
                                {
                                    Slots =
                                    {
                                        new SlotElement { Sizing = Sizing.Fixed, Length = 14 },
                                        new SlotElement
                                        {
                                            Content = new TextElement("NEW", Bold, 10)
                                            {
                                                FontColor = Colors.White,
                                                Background = Colors.Red,
                                                BorderRadius = 9,
                                                Padding = 7,
                                            },
                                        },
                                        new SlotElement { Sizing = Sizing.Relative },
                                    },
                                },
                            },
                            new SlotElement { Sizing = Sizing.Relative },
                        },
                    }
                ),
            },
        },
    };

    private static TableElement BuildInvoiceTable(int itemCount)
    {
        var table = new TableElement
        {
            CellBorderColor = Colors.Gray,
            CellBorderThickness = 0.5,
            HeaderBackground = Colors.DarkBlue,
            CellPadding = 5,
            Header = new UIElement[]
            {
                new TextElement("#", Bold, 11) { FontColor = Colors.White },
                new TextElement("Item", Bold, 11) { FontColor = Colors.White },
                new TextElement("Description", Bold, 11) { FontColor = Colors.White },
                new TextElement("Qty", Bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
                new TextElement("Unit", Bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
            },
        };

        string[] items = { "Widget", "Gadget", "Sprocket", "Cog", "Flange", "Bracket", "Bushing", "Gasket" };
        for (int i = 1; i <= itemCount; i++)
        {
            string item = items[i % items.Length];
            table.Rows.Add(new UIElement[]
            {
                new TextElement(i.ToString(), Body, 10),
                new TextElement(item, Body, 10),
                new TextElement($"A high-quality {item.ToLower()} for the assembly line.", Body, 10),
                new TextElement((i * 3 % 9 + 1).ToString(), Body, 10) { HAlign = HorizontalAlignment.Right },
                new TextElement(System.FormattableString.Invariant($"${(i * 1.49 % 30 + 0.5):0.00}"), Body, 10) { HAlign = HorizontalAlignment.Right },
            });
        }
        return table;
    }
}
