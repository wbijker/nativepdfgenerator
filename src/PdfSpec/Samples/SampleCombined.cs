using PdfSpec.Content;
using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Layout;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Combined showcase — every covered sample folded into four pages of
/// merged sections. Page composition is pure layout (VStack / HStack /
/// BorderElement); only imperative content streams (paths, transforms,
/// raster images, text-state operators) live inside <see cref="Canvas"/>
/// bodies. PageBreaks separate the four pages.
///
/// Pages:
/// <list type="bullet">
/// <item><description><b>1 — Document basics</b> — samples 01-04
/// (blank page, hello, document structure, name tree).</description></item>
/// <item><description><b>2 — Imaging</b> — samples 05-09 + 28
/// (imaging model, transparency, raster, image masks, form XObject,
/// extra operators).</description></item>
/// <item><description><b>3 — Text</b> — samples 10-11 plus a font
/// metrics rendering (baseline / ascender / descender / line-height
/// guides + border around the text line) for several font + size
/// combinations.</description></item>
/// <item><description><b>4 — Navigation, structure, metadata</b> —
/// samples 12, 13, 23, 26.</description></item>
/// </list>
/// </summary>
public sealed class SampleCombined : ISample
{
    public string FileName => "samples.pdf";

    private static readonly PdfColor Heading = PdfColors.Slate(800);
    private static readonly PdfColor SubHeading = PdfColors.Slate(700);
    private static readonly PdfColor BodyGrey = PdfColors.Slate(500);
    private static readonly PdfColor RuleColour = PdfColors.Slate(300);

    public void Build(string path)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Combined Showcase";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";
        doc.SetDefaultFont(StandardFont.Helvetica, 11);

        var page = doc.AddPage(PageSizes.A4);

        var body = new VStack();

        body.AddAuto(Cover());
        body.AddAuto(new PageBreak());

        body.AddAuto(Page1_DocumentBasics());
        body.AddAuto(new PageBreak());

        body.AddAuto(Page2_Imaging(doc));
        body.AddAuto(new PageBreak());

        body.AddAuto(Page3_Text());
        body.AddAuto(new PageBreak());

        body.AddAuto(Page4_NavStructureMetadata(doc));

        page.Body(body);
        doc.Save(path);
    }

    // ===== cover ==============================================================

    private static Element Cover()
    {
        var cover = new VStack();
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 60,
            Content = new Paragraph("PdfSpec", StandardFont.HelveticaBold, 40),
        });
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 8,
            Content = new Paragraph("Combined Showcase", StandardFont.Helvetica, 20),
        });
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 16,
            Content = new Paragraph(
                "Four pages, sixteen samples — basics, imaging, text + font metrics, " +
                "and navigation/structure/metadata. Section composition is pure layout " +
                "(VStack / HStack / BorderElement); raw drawing lives inside Canvas bodies.",
                StandardFont.Helvetica, 11),
        });
        return cover;
    }

    // ===== page 1 — document basics (samples 01-04) ===========================

    private static Element Page1_DocumentBasics()
    {
        var v = new VStack();
        v.AddAuto(PageHeader("Page 1 — Document basics", "Samples 01 to 04"));

        v.AddAuto(SubSection("01 — Blank page",
            "A minimal valid PDF: catalog → page tree → one blank Letter page. " +
            "No content streams, no fonts, no resources — the smallest output the " +
            "writer can produce."));

        v.AddAuto(SubSection("02 — Hello world",
            "One page with “Hello, World!” drawn through the imperative AddText " +
            "builder at 24 pt Helvetica.",
            body: new Canvas
            {
                Width = 500, Height = 40,
                Draw = (cs, _) => cs.AddText().SetFont(StandardFont.Helvetica, 24).Show(0, 26, "Hello, World!").Build(),
            }));

        v.AddAuto(SubSection("03 — Document structure",
            "Three pages exercising page-tree attribute inheritance: a default " +
            "MediaBox on the root, UserUnit=2 on page 2, and an A4-override-plus-90°-" +
            "rotation on page 3. The catalog gets SinglePage layout and the UseThumbs " +
            "page mode."));

        v.AddAuto(SubSection("04 — Name tree",
            "Two named destinations registered in a /Dests name tree under the " +
            "catalog. Each entry is an explicit [page /Fit] array so a link elsewhere " +
            "can jump by name."));

        return v;
    }

    // ===== page 2 — imaging (samples 05-09 + 28) ==============================

    private static Element Page2_Imaging(PdfDoc doc)
    {
        var v = new VStack();
        v.AddAuto(PageHeader("Page 2 — Imaging", "Samples 05-09 + 28"));

        v.AddAuto(SubSection("05 — Imaging model",
            "Painter's model, Bézier circle, three device colour spaces, line caps/joins, transforms.",
            body: ImagingCanvas(width: 500, height: 200)));

        v.AddAuto(SubSection("06 — Transparency + 09 — Form XObject",
            "Constant alpha via ExtGState resources; a gold star defined once and painted with three transforms.",
            body: new HStack()
                .Add(AxisSize.Relative(1), TransparencyCanvas(width: 240, height: 140))
                .Add(AxisSize.Relative(1), StarCanvas(doc, width: 240, height: 140))));

        v.AddAuto(SubSection("07 — Raster image + 08 — Image masks",
            "One DeviceRGB gradient painted at two sizes; soft / colour-key / stencil masks over coloured plates.",
            body: new HStack()
                .Add(AxisSize.Relative(1), RasterCanvas(width: 240, height: 140))
                .Add(AxisSize.Relative(1), ImageMasksCanvas(width: 240, height: 140))));

        v.AddAuto(SubSection("28 — Extra operators",
            "Nonzero vs even-odd fill on a pentagram; v / y Bézier variants; the quote operator + inline BI/ID/EI image.",
            body: OperatorsCanvas(width: 500, height: 130)));

        return v;
    }

    // ===== page 3 — text + font metrics (samples 10-11) =======================

    private static Element Page3_Text()
    {
        var v = new VStack();
        v.AddAuto(PageHeader("Page 3 — Text", "Samples 10-11 + font metrics"));

        v.AddAuto(SubSection("10 — Text fonts",
            "Seven of the standard 14 fonts at 16 pt, rendered as a stack of paragraphs.",
            body: TextFontsList()));

        v.AddAuto(SubSection("11 — Text state",
            "Render modes (fill / stroke / fill+stroke), char + word spacing, horizontal scaling, text rise.",
            body: TextStateCanvas(width: 500, height: 180)));

        v.AddAuto(SubSection("Font metrics",
            "For each font + size combination: the line box outlined, three horizontal guides at the ascender, " +
            "baseline, and descender, and the text drawn with its baseline on the green guide. Labels on the right " +
            "show the actual numbers from FontVerticalMetrics with a matching colour swatch.",
            body: FontMetricsBlock()));

        // Embedded TrueType samples — the system TTFs that survived the
        // CSharpPdf project's BuildTrueTypeEmbedding probe. Each face
        // becomes a Paragraph rendered at a different size so the
        // glyph differences are obvious. Skipped silently when none of
        // the candidate paths exist.
        var ttfBlock = TrueTypeBlock();
        if (ttfBlock is not null)
        {
            v.AddAuto(SubSection("31 — TrueType embedding",
                "TrueType fonts loaded from the filesystem and embedded as PDF simple fonts. " +
                "Each face is rendered through the standard Paragraph element — the same one that drove " +
                "the standard-14 list above — proving the Paragraph / Font API is transparent to whether " +
                "the underlying face is a built-in or an embedded TTF.",
                body: ttfBlock));
        }

        return v;
    }

    private static Element? TrueTypeBlock()
    {
        var faces = new (string Path, string Display, double Size)[]
        {
            ("/System/Library/Fonts/NewYork.ttf",                "New York — serif body at 16 pt",       16),
            ("/System/Library/Fonts/Geneva.ttf",                 "Geneva — system sans-serif at 14 pt",  14),
            ("/System/Library/Fonts/Supplemental/Arial.ttf",     "Arial — supplemental sans at 18 pt",   18),
            ("/Users/willembijker/Downloads/Quake3d.ttf",        "Quake3d — decorative display at 24 pt", 24),
        };

        var stack = new VStack();
        bool any = false;
        foreach (var (path, display, size) in faces)
        {
            if (!System.IO.File.Exists(path)) continue;
            var ttf = TrueTypeFont.FromFile(path);
            stack.AddAuto(new BorderElement
            {
                PaddingTop = 4,
                Content = new Paragraph(display, ttf, size),
            });
            any = true;
        }
        return any ? stack : null;
    }

    // ===== page 4 — nav / structure / metadata (12, 13, 23, 26) ===============

    private static Element Page4_NavStructureMetadata(PdfDoc doc)
    {
        var v = new VStack();
        v.AddAuto(PageHeader("Page 4 — Navigation, structure, metadata", "Samples 12, 13, 23, 26"));

        v.AddAuto(SubSection("12 — Navigation",
            "Link buttons covering every action type: GoTo Fit (jumps back to the cover), GoTo named (Dests name tree), URI (external), GoToR (remote PDF). Each whole blue block is the click target via BoxElement.OnRendered.",
            body: NavButtonStack(doc)));

        v.AddAuto(SubSection("13 — Outline",
            "A five-visible-items bookmark tree: an open Document root with Section 1, Section 2 (closed, hiding Subsection 1), and Section 3, plus a top-level Summary."));

        v.AddAuto(SubSection("23 — Optional content (layers)",
            "Three OCG layers (Red, Green, Blue) marked in the content stream via BDC /OC. Blue is OFF in the default config so it stays hidden until the user toggles it.",
            body: OptionalContentCanvas(width: 500, height: 110)));

        v.AddAuto(SubSection("26 — Document metadata",
            "Title / Author / Subject / Keywords / Creator / Producer / CreationDate / ModDate set in both the Information dictionary and a matching XMP metadata stream so every reader finds consistent values."));

        return v;
    }

    // ===== shared layout helpers ==============================================

    private static Element PageHeader(string title, string subtitle)
    {
        var stack = new VStack();
        stack.AddAuto(new Paragraph(title, StandardFont.HelveticaBold, 18));
        stack.AddAuto(new BorderElement
        {
            PaddingBottom = 6,
            BorderBottomWidth = 1,
            BorderBottomColor = Heading,
            Content = new Paragraph(subtitle, StandardFont.Helvetica, 9),
        });
        return stack;
    }

    private static Element SubSection(string title, string description, Element? body = null)
    {
        var stack = new VStack();
        stack.AddAuto(new BorderElement
        {
            PaddingTop = 6,
            Content = new Paragraph(title, StandardFont.HelveticaBold, 11),
        });
        stack.AddAuto(new Paragraph(description, StandardFont.Helvetica, 8));
        if (body is not null)
        {
            stack.AddAuto(new BorderElement
            {
                PaddingTop = 4,
                Content = body,
            });
        }
        return stack;
    }

    // ===== imaging canvases ===================================================

    private static Element ImagingCanvas(double width, double height) => new Canvas
    {
        Width = width,
        Height = height,
        Draw = (c, _) =>
        {
            c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(0, 10, 70, 60).Fill();
            c.SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(35, 30, 70, 60).Fill();
            c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(70, 50, 70, 60).Fill();

            c.Save()
                .SetRgbFill(PdfColor.Rgb(1, 0.6, 0))
                .SetRgbStroke(PdfColor.Rgb(0, 0, 0.5))
                .SetLineWidth(2)
                .SetDash(new double[] { 5, 2 })
                .Circle(180, 40, 30).FillStroke()
                .Restore();

            c.Save().SetLineWidth(8);
            c.SetGrayStroke(0.5).MoveTo(0, 130).LineTo(140, 130).Stroke();
            c.SetRgbStroke(PdfColor.Rgb(1, 0, 0)).MoveTo(0, 150).LineTo(140, 150).Stroke();
            c.SetCmykStroke(PdfColor.Cmyk(1, 0, 0, 0)).MoveTo(0, 170).LineTo(140, 170).Stroke();
            c.Restore();

            c.Save().SetRgbStroke(PdfColor.Rgb(0, 0.6, 0)).SetLineWidth(8).SetLineCap(1).SetLineJoin(1);
            c.MoveTo(220, 130).LineTo(250, 160).LineTo(280, 130).LineTo(310, 160).LineTo(340, 130).Stroke();
            c.Restore();

            c.Save().Translate(380, 130).Scale(0.4, 0.4).SetRgbFill(PdfColor.Rgb(0.8, 0, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
            c.Save().Translate(440, 150).Rotate(20).SetRgbFill(PdfColor.Rgb(0, 0, 0.8)).Rectangle(-25, -25, 50, 50).Fill().Restore();
        },
    };

    private static Element TransparencyCanvas(double width, double height) => new Canvas
    {
        Width = width,
        Height = height,
        Draw = (c, _) =>
        {
            var page = c.RequirePage("TransparencyCanvas");
            page.AddExtGState("GSopaqueC", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
            page.AddExtGState("GShalfC", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

            c.Save().SetExtGState("GSopaqueC").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(10, 10, 100, 100).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(60, 30, 100, 100).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(110, 50, 100, 100).Fill().Restore();
        },
    };

    private static Element RasterCanvas(double width, double height)
    {
        const int w = 128, h = 128;
        var image = PdfImage.Rgb(SampleImages.MakeGradient(w, h), w, h);
        return new Canvas
        {
            Width = width,
            Height = height,
            Draw = (c, _) =>
            {
                c.DrawImage(image, 0, 0, 130, 130);
                c.DrawImage(image, 145, 30, 80, 80);
            },
        };
    }

    private static Element ImageMasksCanvas(double width, double height)
    {
        const int w = 128, h = 128;
        var soft = PdfImage.Rgb(SampleImages.MakeSolid(w, h, 220, 30, 140), w, h);
        soft.SoftMask = PdfImage.Alpha(SampleImages.MakeRadialAlpha(w, h), w, h);
        var keyed = PdfImage.Rgb(SampleImages.MakeDiscOnWhite(w, h), w, h);
        keyed.ColorKeyMask = new PdfArray(
            new PdfNumber(255), new PdfNumber(255), new PdfNumber(255),
            new PdfNumber(255), new PdfNumber(255), new PdfNumber(255));
        var stencil = PdfImage.Stencil(SampleImages.MakeCheckerBits(w, h), w, h);

        return new Canvas
        {
            Width = width,
            Height = height,
            Draw = (c, _) =>
            {
                c.Save().SetRgbFill(PdfColor.Rgb(1, 0.95, 0.4)).Rectangle(0, 0, 70, 130).Fill().Restore();
                c.DrawImage(soft, 0, 0, 70, 130);

                c.Save().SetRgbFill(PdfColor.Rgb(0.3, 0.8, 0.3)).Rectangle(80, 0, 70, 130).Fill().Restore();
                c.DrawImage(keyed, 80, 0, 70, 130);

                c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.85, 0.85)).Rectangle(160, 0, 70, 130).Fill().Restore();
                c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).DrawImage(stencil, 160, 0, 70, 130).Restore();
            },
        };
    }

    private static Element StarCanvas(PdfDoc doc, double width, double height)
    {
        var star = new FormXObject(doc, PdfRectangle.FromSize(100, 100));
        star.Content
            .SetRgbFill(PdfColor.Rgb(1, 0.78, 0))
            .SetRgbStroke(PdfColor.Rgb(0.5, 0.35, 0))
            .SetLineWidth(3);
        for (int i = 0; i < 10; i++)
        {
            double r = (i % 2 == 0) ? 45.0 : 18.0;
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            double x = 50 + r * Math.Cos(angle);
            double y = 50 + r * Math.Sin(angle);
            if (i == 0) star.Content.MoveTo(x, y); else star.Content.LineTo(x, y);
        }
        star.Content.ClosePath().CloseFillStroke();
        star.Build();

        return new Canvas
        {
            Width = width,
            Height = height,
            Draw = (c, _) =>
            {
                c.DrawForm(star, 5, 15);
                c.DrawForm(star, 100, 30, 0.6);
                c.DrawForm(star, 175, 25, 0.5);
            },
        };
    }

    private static Element OperatorsCanvas(double width, double height) => new Canvas
    {
        Width = width,
        Height = height,
        Draw = (c, _) =>
        {
            // Two pentagrams — non-zero vs even-odd fill.
            DrawPentagram(c, 60, 55, 40);
            c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2).CloseFillStroke().Restore();

            DrawPentagram(c, 180, 55, 40);
            c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2).CloseFillStrokeEvenOdd().Restore();

            // v / y Bézier leaf.
            c.Save().SetRgbFill(PdfColor.Rgb(0.2, 0.6, 0.9));
            c.MoveTo(290, 70).CurveToV(290, 20, 350, 20).CurveToY(350, 70, 290, 70).Fill().Restore();

            // Quote operator demo.
            c.AddText().SetFont(StandardFont.Helvetica, 11).SetLeading(15).SetTextMatrix(1, 0, 0, 1, 0, 110)
                .ShowText("Quote operator: spacing + next-line show on")
                .NextLineShowText(wordSpacing: 4, charSpacing: 1, text: "one chained operator (Tj / ’ / TJ).")
                .Build();
        },
    };

    private static void DrawPentagram(ContentStream c, double cx, double cy, double r)
    {
        for (int i = 0; i < 5; i++)
        {
            int index = (i * 2) % 5;
            double a = -Math.PI / 2 + index * 2 * Math.PI / 5;
            double x = cx + r * Math.Cos(a), y = cy + r * Math.Sin(a);
            if (i == 0) c.MoveTo(x, y); else c.LineTo(x, y);
        }
        c.ClosePath();
    }

    // ===== text page helpers ==================================================

    private static Element TextFontsList()
    {
        var col = new VStack();
        col.AddAuto(new Paragraph("Helvetica — Pack my box.", StandardFont.Helvetica, 14));
        col.AddAuto(new Paragraph("Helvetica-Bold — Pack my box.", StandardFont.HelveticaBold, 14));
        col.AddAuto(new Paragraph("Times-Roman — Pack my box.", StandardFont.TimesRoman, 14));
        col.AddAuto(new Paragraph("Times-Italic — Pack my box.", StandardFont.TimesItalic, 14));
        col.AddAuto(new Paragraph("Times-Bold — Pack my box.", StandardFont.TimesBold, 14));
        col.AddAuto(new Paragraph("Courier — Pack my box.", StandardFont.Courier, 14));
        return col;
    }

    private static Element TextStateCanvas(double width, double height) => new Canvas
    {
        Width = width,
        Height = height,
        Draw = (c, _) =>
        {
            c.AddText().SetFont(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 24)
                .SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).SetTextRenderMode(TextRenderMode.Fill)
                .ShowText("Fill mode (Tr 0)").Build();
            c.AddText().SetFont(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 56)
                .SetRgbStroke(PdfColor.Rgb(0.1, 0.1, 0.8)).SetLineWidth(0.7).SetTextRenderMode(TextRenderMode.Stroke)
                .ShowText("Stroke mode (Tr 1)").Build();
            c.AddText().SetFont(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 88)
                .SetRgbFill(PdfColor.Rgb(1, 0.8, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetTextRenderMode(TextRenderMode.FillStroke)
                .ShowText("Fill + Stroke (Tr 2)").Build();

            c.SetRgbFill(PdfColor.Rgb(0, 0, 0));
            c.AddText().SetFont(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 116)
                .ShowText("Normal: the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 134)
                .SetCharSpacing(3).ShowText("Tc 3: the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 152)
                .SetWordSpacing(6).ShowText("Tw 6: the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 170)
                .ShowText("Rise: H").SetTextRise(-3).SetFont(StandardFont.Helvetica, 9).ShowText("2")
                .SetTextRise(0).SetFont(StandardFont.Helvetica, 12).ShowText(",  E = mc")
                .SetTextRise(5).SetFont(StandardFont.Helvetica, 9).ShowText("2").Build();
        },
    };

    // ===== font metrics demo ==================================================
    //
    // For each (font, size) example: an HStack with the demo canvas on the
    // left and a VStack of swatch+label rows on the right. The canvas
    // draws the line box, three horizontal guides at the ascender, baseline
    // and descender, and the text itself — with Show(x, ascentLineY, …)
    // so the AABB-top semantic SetTextMatrix uses lands the actual
    // baseline on the green guide (the original bug used baselineY as
    // the y argument, which mis-translated by +ascent). Composition on
    // the right is pure layout: an HStack of swatch + label-paragraph
    // for each metric.

    private static readonly PdfColor AscenderColour  = PdfColor.Rgb(0.75, 0.10, 0.10);
    private static readonly PdfColor BaselineColour  = PdfColor.Rgb(0.05, 0.55, 0.20);
    private static readonly PdfColor DescenderColour = PdfColor.Rgb(0.10, 0.20, 0.75);
    private static readonly PdfColor LineBoxColour   = PdfColors.Slate(400);

    private static Element FontMetricsBlock()
    {
        var examples = new (Font Font, double Size, string Sample)[]
        {
            (StandardFont.Helvetica,  10, "Helvetica 10 pt — Hjgpy"),
            (StandardFont.Helvetica,  18, "Helvetica 18 pt — Hjgpy"),
            (StandardFont.TimesRoman, 14, "Times-Roman 14 pt — Hjgpy"),
            (StandardFont.TimesBold,  22, "Times-Bold 22 pt — Hjgpy"),
            (StandardFont.Courier,    12, "Courier 12 pt — Hjgpy"),
        };

        var stack = new VStack();
        foreach (var (font, size, sample) in examples)
        {
            stack.AddAuto(new BorderElement
            {
                PaddingTop = 6,
                PaddingBottom = 6,
                Content = MetricRow(font, size, sample),
            });
        }
        return stack;
    }

    private static Element MetricRow(Font font, double size, string sample)
    {
        var m = font.GetVerticalMetrics(size);
        return new HStack()
            .Add(AxisSize.Relative(3), MetricCanvas(font, size, sample, m), verticalAlignment: Alignment.Center)
            .Add(AxisSize.Relative(2), MetricLabels(m), verticalAlignment: Alignment.Center);
    }

    /// <summary>
    /// The text + three-guides canvas. The canvas height is the line box
    /// height so a VStack parent can size it exactly; the only imperative
    /// drawing here is the four ruled strokes and the AddText call —
    /// everything else (positioning relative to the section, alignment
    /// against the labels) is component-driven.
    /// </summary>
    private static Element MetricCanvas(Font font, double size, string sample, FontVerticalMetrics m) => new Canvas
    {
        Width = 280,
        Height = m.LineHeight,
        Draw = (c, sz) =>
        {
            double ascentLineY  = m.LineGap / 2;
            double baselineY    = m.BaseLine;
            double descentLineY = baselineY + m.Descent;

            // Line-box outline.
            c.Save().SetRgbStroke(LineBoxColour).SetLineWidth(0.4)
                .Rectangle(0, 0, sz.Width, sz.Height).Stroke().Restore();
            // Ascender (red, dashed).
            c.Save().SetRgbStroke(AscenderColour).SetLineWidth(0.4).SetDash(new double[] { 2, 1.5 })
                .MoveTo(0, ascentLineY).LineTo(sz.Width, ascentLineY).Stroke().Restore();
            // Baseline (green, solid).
            c.Save().SetRgbStroke(BaselineColour).SetLineWidth(0.5)
                .MoveTo(0, baselineY).LineTo(sz.Width, baselineY).Stroke().Restore();
            // Descender (blue, dashed).
            c.Save().SetRgbStroke(DescenderColour).SetLineWidth(0.4).SetDash(new double[] { 2, 1.5 })
                .MoveTo(0, descentLineY).LineTo(sz.Width, descentLineY).Stroke().Restore();

            // Text — Show treats the y arg as the AABB top (cap-top), so
            // the AABB top must sit on the ascender guide for the baseline
            // to land on the green guide.
            c.AddText().SetFont(font, size).Show(4, ascentLineY, sample).Build();
        },
    };

    private static Element MetricLabels(FontVerticalMetrics m)
    {
        var rows = new VStack();
        rows.AddAuto(LabelRow(AscenderColour,  "ascent",      m.Ascent));
        rows.AddAuto(LabelRow(BaselineColour,  "baseline",    m.BaseLine));
        rows.AddAuto(LabelRow(DescenderColour, "descent",     m.Descent));
        rows.AddAuto(LabelRow(LineBoxColour,   "line gap",    m.LineGap));
        rows.AddAuto(LabelRow(LineBoxColour,   "line height", m.LineHeight));
        return rows;
    }

    private static Element LabelRow(PdfColor swatch, string label, double value) =>
        new HStack()
            .Add(AxisSize.Fixed(18), new BorderElement
            {
                Width = 14,
                Height = 4,
                Background = swatch,
            }, verticalAlignment: Alignment.Center)
            .Add(AxisSize.Auto(), new Paragraph($"{label,-12} {value:F2}", StandardFont.Courier, 8),
                 verticalAlignment: Alignment.Center);

    // ===== nav-page components ================================================

    /// <summary>
    /// The navigation block — a VStack of link buttons. Each button is a
    /// fixed-height BorderElement that subscribes to <see cref="BoxElement.OnRendered"/>
    /// so the whole blue block becomes a Link annotation covering its
    /// rendered rectangle. The four actions exercise GoTo Fit, GoTo
    /// named, URI, and GoToR remote — all wired off the document the
    /// sample is currently building, so the in-document links resolve to
    /// real pages.
    /// </summary>
    private static Element NavButtonStack(PdfDoc doc)
    {
        // Register a named destination once. "chapter-3" resolves to the
        // first page of the document (the cover) at the Fit zoom level
        // — same surface area as a hand-written /Dests entry would
        // produce.
        doc.AddNamedDestination("chapter-3", new PdfArray(doc.Pages[0].Reference, new PdfName("Fit")));

        var coverFit = new PdfArray(doc.Pages[0].Reference, new PdfName("Fit"));

        var buttons = new (string Label, PdfDictionary Action)[]
        {
            ("GoTo page 1 (Fit)",            Navigation.PdfAction.GoTo(coverFit)),
            ("Named destination: chapter-3", Navigation.PdfAction.GoToNamed("chapter-3")),
            ("Open oreilly.com (URI)",       Navigation.PdfAction.Uri("https://www.oreilly.com")),
            ("Open Chapter2.pdf (GoToR)",    Navigation.PdfAction.GoToRemote("Chapter2.pdf", 0)),
        };

        var stack = new VStack();
        foreach (var (label, action) in buttons)
        {
            stack.AddAuto(new BorderElement
            {
                PaddingBottom = 6,
                Content = NavButton(label, action),
            });
        }
        return stack;
    }

    private static Element NavButton(string label, PdfDictionary action) => new BorderElement
    {
        Width = 260,
        Height = 30,
        Background = PdfColor.Rgb(0.90, 0.93, 1.0),
        BorderTopWidth = 1, BorderRightWidth = 1, BorderBottomWidth = 1, BorderLeftWidth = 1,
        BorderTopColor = PdfColor.Rgb(0.2, 0.3, 0.7),
        BorderRightColor = PdfColor.Rgb(0.2, 0.3, 0.7),
        BorderBottomColor = PdfColor.Rgb(0.2, 0.3, 0.7),
        BorderLeftColor = PdfColor.Rgb(0.2, 0.3, 0.7),
        VerticalAlignment = Alignment.Center,
        // The full blue block is the click target — the Link annotation
        // is added at render time with the box's actual on-page Rect, so
        // the layout engine retains complete control of where the button
        // sits. There are no coordinates anywhere in the composition.
        OnRendered = info => info.Page.AddLinkAnnotation(info.Bounds, action),
        Content = new BorderElement
        {
            PaddingLeft = 10,
            Content = new Paragraph(label, StandardFont.Helvetica, 11),
        },
    };

    private static Element OptionalContentCanvas(double width, double height) => new Canvas
    {
        Width = width,
        Height = height,
        Draw = (c, _) =>
        {
            var page = c.RequirePage("OptionalContentCanvas");
            var red = page.Document.AddOptionalContentGroup("Red layer");
            var green = page.Document.AddOptionalContentGroup("Green layer");
            var blue = page.Document.AddOptionalContentGroup("Blue layer");
            page.AddProperty("OCRC", red);
            page.AddProperty("OCGC", green);
            page.AddProperty("OCBC", blue);

            c.BeginOptionalContent("OCRC").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(20, 0, 130, 100).Fill().EndMarkedContent();
            c.BeginOptionalContent("OCGC").SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(100, 0, 130, 100).Fill().EndMarkedContent();
            c.BeginOptionalContent("OCBC").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(180, 0, 130, 100).Fill().EndMarkedContent();
        },
    };
}
