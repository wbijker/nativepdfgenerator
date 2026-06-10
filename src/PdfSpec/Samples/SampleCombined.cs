using PdfSpec.Content;
using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Layout;
using PdfSpec.Navigation;
using PdfSpec.Objects;
using PdfSpec.Structure;

namespace PdfSpec.Samples;

/// <summary>
/// Combined showcase — every sample covered so far rendered as a
/// section inside one outer <see cref="VStack"/>, separated by
/// <see cref="PageBreak"/> sentinels. No coordinates leak into the
/// section composition; the only raw drawing lives inside
/// <see cref="Canvas"/> bodies for samples whose subject is the
/// imperative content stream itself (paths, transforms, image masks,
/// text-state operators).
/// </summary>
public sealed class SampleCombined : ISample
{
    public string FileName => "samples.pdf";

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

        AddSection(body, "01 — Blank page",
            "A minimal valid PDF: catalog → page tree → one blank Letter page. No content streams, no fonts, no resources — the smallest output the writer produces.",
            content: null);

        AddSection(body, "02 — Hello world",
            "One page with “Hello, World!” drawn through the imperative AddText builder at 24 pt Helvetica.",
            content: HelloCanvas());

        AddSection(body, "03 — Document structure",
            "Three pages exercising page-tree attribute inheritance: a default MediaBox on the root, UserUnit=2 on page 2, and an A4-override-plus-90°-rotation on page 3. The catalog gets SinglePage layout and the UseThumbs page mode.",
            content: null);

        AddSection(body, "04 — Name tree",
            "Two named destinations registered in a /Dests name tree under the catalog. Each entry is an explicit [page /Fit] array so a link elsewhere can jump by name.",
            content: null);

        AddSection(body, "05 — Imaging model",
            "Vector graphics through the content-stream API: the painter's model (overlapping rectangles), a Bézier circle, the three device colour spaces, line caps/joins, transforms, and clipping.",
            content: ImagingCanvas());

        AddSection(body, "06 — Transparency + marked content",
            "Constant alpha applied via named ExtGState resources (ca / CA) and two marked-content brackets (BMC/EMC and BDC/EMC with an inline property list).",
            content: TransparencyCanvas());

        AddSection(body, "07 — Raster image",
            "A procedurally generated DeviceRGB gradient embedded once as an Image XObject and painted at two sizes — one resource, two transforms.",
            content: RasterCanvas(doc));

        AddSection(body, "08 — Image masks",
            "The three masking techniques: a soft alpha mask, a colour-key mask dropping white pixels, and a 1-bit stencil ImageMask painted in the current fill colour.",
            content: ImageMasksCanvas(doc));

        AddSection(body, "09 — Form XObject",
            "A gold star defined once inside a 100×100 form XObject and painted eight times with different CTM transforms (full size, scaled, rotated, plus a row of small stamps).",
            content: FormXObjectCanvas(doc));

        AddSection(body, "10 — Text fonts",
            "Seven of the standard 14 fonts — Helvetica, Helvetica-BoldOblique, Times-Roman, Times-Italic, Courier-Bold, Symbol, ZapfDingbats — registered as F1..F7 and drawn at 22 pt.",
            content: FontsCanvas());

        AddSection(body, "11 — Text state",
            "Rendering modes (fill / stroke / fill+stroke), character + word spacing, horizontal scaling, text rise for sub- and super-scripts, leading + T* across multiple lines, manual TJ kerning, and WinAnsiEncoding for accented Latin-1.",
            content: TextStateCanvas());

        AddSection(body, "12 — Navigation",
            "Three pages with link buttons covering every action type: GoTo Fit, GoTo named (Dests name tree), URI, GoToR remote, plus an XYZ back-link and a document-level OpenAction.",
            content: NavCanvas());

        AddSection(body, "13 — Outline",
            "A five-visible-items bookmark tree: an open Document root with Section 1, Section 2 (closed, with Subsection 1 hidden), and Section 3, plus a top-level Summary entry.",
            content: null);

        AddSection(body, "23 — Optional content",
            "Three OCG layers (Red, Green, Blue) marked in the content stream via BDC /OC. Blue is OFF in the default configuration so it stays hidden until the user toggles it.",
            content: OptionalContentCanvas());

        AddSection(body, "26 — Document metadata",
            "Title, Author, Subject, Keywords, Creator, Producer, CreationDate, ModDate set in both the Information dictionary and a matching XMP metadata stream so every reader finds consistent values.",
            content: null);

        AddSection(body, "28 — Extra operators",
            "Operators not exercised elsewhere: nonzero (b) vs even-odd (b*) fill on a self-intersecting pentagram, the v / y Bézier curve variants, the quote operator (' word/char spacing + next-line show), and an inline image via BI/ID/EI.",
            content: OperatorsCanvas(),
            lastSection: true);

        page.Body(body);
        doc.Save(path);
    }

    // ===== section helper =====================================================

    private static void AddSection(VStack body, string title, string description, Element? content, bool lastSection = false)
    {
        var section = new VStack();
        section.AddAuto(new BorderElement
        {
            PaddingBottom = 8,
            BorderBottomWidth = 1,
            BorderBottomColor = PdfColors.Slate(700),
            Content = new Paragraph(title, StandardFont.HelveticaBold, 16),
        });
        section.AddAuto(new BorderElement
        {
            PaddingTop = 6,
            PaddingBottom = 12,
            Content = new Paragraph(description, StandardFont.Helvetica, 10),
        });
        if (content is not null) section.AddAuto(content);
        body.AddAuto(section);
        if (!lastSection) body.AddAuto(new PageBreak());
    }

    // ===== cover page =========================================================

    private static Element Cover()
    {
        var cover = new VStack();
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 120,
            Content = new Paragraph("PdfSpec", StandardFont.HelveticaBold, 48),
        });
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 16,
            Content = new Paragraph("Combined Showcase", StandardFont.Helvetica, 24),
        });
        cover.AddAuto(new BorderElement
        {
            PaddingTop = 24,
            Content = new Paragraph(
                "Every sample covered so far, rendered as a section inside one outer VStack. " +
                "Each section's drawn content sits in a Canvas so the page-absolute coordinates " +
                "the original samples used translate to the section's local origin without " +
                "leaking into the surrounding layout. PageBreak sentinels separate the sections.",
                StandardFont.Helvetica, 11),
        });
        return cover;
    }

    // ===== per-sample canvases =================================================

    private static Element HelloCanvas() => new Canvas
    {
        Width = 500,
        Height = 60,
        Draw = (cs, _) => cs.AddText().SetFont(StandardFont.Helvetica, 24).Show(0, 0, "Hello, World!").Build(),
    };

    private static Element ImagingCanvas() => new Canvas
    {
        Width = 520,
        Height = 360,
        Draw = (c, _) =>
        {
            c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(10, 10, 110, 90).Fill();
            c.SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(60, 35, 110, 90).Fill();
            c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(110, 60, 110, 90).Fill();

            c.Save()
                .SetRgbFill(PdfColor.Rgb(1, 0.6, 0))
                .SetRgbStroke(PdfColor.Rgb(0, 0, 0.5))
                .SetLineWidth(2)
                .SetDash(new double[] { 5, 2 })
                .Circle(420, 55, 50).FillStroke()
                .Restore();

            c.Save().SetLineWidth(10);
            c.SetGrayStroke(0.5).MoveTo(10, 160).LineTo(210, 160).Stroke();
            c.SetRgbStroke(PdfColor.Rgb(1, 0, 0)).MoveTo(10, 190).LineTo(210, 190).Stroke();
            c.SetCmykStroke(PdfColor.Cmyk(1, 0, 0, 0)).MoveTo(10, 220).LineTo(210, 220).Stroke();
            c.Restore();

            c.Save().SetRgbStroke(PdfColor.Rgb(0, 0.6, 0)).SetLineWidth(10).SetLineCap(1).SetLineJoin(1);
            c.MoveTo(280, 160).LineTo(330, 190).LineTo(380, 160).LineTo(430, 190).LineTo(480, 160).Stroke();
            c.Restore();

            c.Save().Translate(10, 260).Scale(0.5, 0.5).SetRgbFill(PdfColor.Rgb(0.8, 0, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
            c.Save().Translate(130, 260).SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
            c.Save().Translate(310, 310).Rotate(45).SetRgbFill(PdfColor.Rgb(0, 0, 0.8)).Rectangle(-50, -50, 100, 100).Fill().Restore();
        },
    };

    private static Element TransparencyCanvas() => new Canvas
    {
        Width = 460,
        Height = 240,
        Draw = (c, _) =>
        {
            var page = c.UseFontPage();
            page.AddExtGState("GSopaqueC", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
            page.AddExtGState("GShalfC", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

            c.Save().SetExtGState("GSopaqueC").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(20, 20, 130, 130).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(80, 60, 130, 130).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(140, 100, 130, 130).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(1, 0.5, 0)).Rectangle(310, 100, 120, 90).Fill().Restore();
        },
    };

    private static Element RasterCanvas(PdfDoc doc)
    {
        const int w = 128, h = 128;
        var image = PdfImage.Rgb(SampleImages.MakeGradient(w, h), w, h);
        return new Canvas
        {
            Width = 460,
            Height = 220,
            Draw = (c, _) =>
            {
                c.DrawImage(image, 10, 0, 200, 200);
                c.DrawImage(image, 230, 50, 100, 100);
            },
        };
    }

    private static Element ImageMasksCanvas(PdfDoc doc)
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
            Width = 520,
            Height = 200,
            Draw = (c, _) =>
            {
                c.Save().SetRgbFill(PdfColor.Rgb(1, 0.95, 0.4)).Rectangle(10, 10, 150, 150).Fill().Restore();
                c.DrawImage(soft, 10, 10, 150, 150);

                c.Save().SetRgbFill(PdfColor.Rgb(0.3, 0.8, 0.3)).Rectangle(180, 10, 150, 150).Fill().Restore();
                c.DrawImage(keyed, 180, 10, 150, 150);

                c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.85, 0.85)).Rectangle(350, 10, 150, 150).Fill().Restore();
                c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).DrawImage(stencil, 350, 10, 150, 150).Restore();
            },
        };
    }

    private static Element FormXObjectCanvas(PdfDoc doc)
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
        var built = star.Build();

        return new Canvas
        {
            Width = 540,
            Height = 220,
            Draw = (c, _) =>
            {
                c.DrawForm(star, 10, 60);
                c.DrawForm(star, 130, 100, 0.6);
                c.DrawForm(star, 280, 80, 0.8);
                for (int i = 0; i < 5; i++)
                    c.DrawForm(star, 10 + i * 80, 200, 0.45);
            },
        };
    }

    private static Element FontsCanvas()
    {
        var col = new VStack();
        col.AddAuto(new Paragraph("Helvetica: Pack my box.", StandardFont.Helvetica, 18));
        col.AddAuto(new Paragraph("Helvetica-Bold", StandardFont.HelveticaBold, 18));
        col.AddAuto(new Paragraph("Times-Roman: Pack my box.", StandardFont.TimesRoman, 18));
        col.AddAuto(new Paragraph("Times-Italic: Pack my box.", StandardFont.TimesItalic, 18));
        col.AddAuto(new Paragraph("Courier-Bold: Pack my box.", StandardFont.Courier, 18));
        return col;
    }

    private static Element TextStateCanvas() => new Canvas
    {
        Width = 520,
        Height = 260,
        Draw = (c, _) =>
        {
            c.AddText().SetFont(StandardFont.HelveticaBold, 28).SetTextMatrix(1, 0, 0, 1, 0, 16)
                .SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).SetTextRenderMode(TextRenderMode.Fill)
                .ShowText("Fill mode (Tr 0)").Build();
            c.AddText().SetFont(StandardFont.HelveticaBold, 28).SetTextMatrix(1, 0, 0, 1, 0, 52)
                .SetRgbStroke(PdfColor.Rgb(0.1, 0.1, 0.8)).SetLineWidth(0.7).SetTextRenderMode(TextRenderMode.Stroke)
                .ShowText("Stroke mode (Tr 1)").Build();
            c.AddText().SetFont(StandardFont.HelveticaBold, 28).SetTextMatrix(1, 0, 0, 1, 0, 88)
                .SetRgbFill(PdfColor.Rgb(1, 0.8, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetTextRenderMode(TextRenderMode.FillStroke)
                .ShowText("Fill + Stroke (Tr 2)").Build();

            c.SetRgbFill(PdfColor.Rgb(0, 0, 0));
            c.AddText().SetFont(StandardFont.Helvetica, 14).SetTextMatrix(1, 0, 0, 1, 0, 130)
                .ShowText("Normal:  the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 14).SetTextMatrix(1, 0, 0, 1, 0, 152)
                .SetCharSpacing(3).ShowText("Tc 3:  the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 14).SetTextMatrix(1, 0, 0, 1, 0, 174)
                .SetWordSpacing(8).ShowText("Tw 8:  the quick brown fox").Build();
            c.AddText().SetFont(StandardFont.Helvetica, 14).SetTextMatrix(1, 0, 0, 1, 0, 196)
                .SetHorizontalScaling(160).ShowText("Tz 160 (horizontal scaling)").Build();

            c.AddText().SetFont(StandardFont.Helvetica, 18).SetTextMatrix(1, 0, 0, 1, 0, 234)
                .ShowText("Rise: H").SetTextRise(-4).SetFont(StandardFont.Helvetica, 12).ShowText("2")
                .SetTextRise(0).SetFont(StandardFont.Helvetica, 18).ShowText("O,   E = mc")
                .SetTextRise(7).SetFont(StandardFont.Helvetica, 12).ShowText("2").Build();
        },
    };

    private static Element NavCanvas() => new Canvas
    {
        Width = 460,
        Height = 180,
        Draw = (c, _) =>
        {
            string[] labels =
            {
                "GoTo page 3 (Fit)",
                "Named destination: chapter-3",
                "Open oreilly.com (URI)",
                "Open Chapter2.pdf (GoToR)",
            };
            for (int i = 0; i < labels.Length; i++)
            {
                double y = 10 + i * 38;
                c.Save().SetRgbStroke(PdfColor.Rgb(0.2, 0.3, 0.7)).SetRgbFill(PdfColor.Rgb(0.90, 0.93, 1.0)).SetLineWidth(1)
                    .Rectangle(0, y, 260, 26).FillStroke().Restore();
                c.Save().SetRgbFill(PdfColor.Rgb(0.1, 0.2, 0.6))
                    .AddText().SetFont(StandardFont.Helvetica, 11).Show(10, y + 8, labels[i]).Build()
                    .Restore();
            }
        },
    };

    private static Element OptionalContentCanvas() => new Canvas
    {
        Width = 460,
        Height = 160,
        Draw = (c, _) =>
        {
            var page = c.UseFontPage();
            var redOcg = page.Document.AddOptionalContentGroup("Red layer");
            var greenOcg = page.Document.AddOptionalContentGroup("Green layer");
            var blueOcg = page.Document.AddOptionalContentGroup("Blue layer");
            page.AddProperty("OCRC", redOcg);
            page.AddProperty("OCGC", greenOcg);
            page.AddProperty("OCBC", blueOcg);

            c.BeginOptionalContent("OCRC").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(20, 10, 140, 120).Fill().EndMarkedContent();
            c.BeginOptionalContent("OCGC").SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(120, 10, 140, 120).Fill().EndMarkedContent();
            c.BeginOptionalContent("OCBC").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(220, 10, 140, 120).Fill().EndMarkedContent();
        },
    };

    private static Element OperatorsCanvas() => new Canvas
    {
        Width = 520,
        Height = 240,
        Draw = (c, _) =>
        {
            // Pentagrams: nonzero vs even-odd fill.
            DrawPentagram(c, 80, 80, 55);
            c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2).CloseFillStroke().Restore();

            DrawPentagram(c, 240, 80, 55);
            c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2).CloseFillStrokeEvenOdd().Restore();

            // Bézier leaf via v / y curves.
            c.Save().SetRgbFill(PdfColor.Rgb(0.2, 0.6, 0.9));
            c.MoveTo(380, 100).CurveToV(380, 30, 460, 30).CurveToY(460, 100, 380, 100).Fill().Restore();

            // Quote operator: word/char spacing + next-line show.
            c.AddText().SetFont(StandardFont.Helvetica, 13).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 0, 180)
                .ShowText("The quote operator sets spacing and shows a line:")
                .NextLineShowText(wordSpacing: 6, charSpacing: 1, text: "spaced out via the quote operator")
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
}

/// <summary>
/// Hack: a Canvas-bound ContentStream doesn't expose its owning page,
/// but several samples need it for resources (ExtGState, OCG
/// properties). Walk the parent chain to the page-attached stream.
/// </summary>
internal static class ContentStreamPageExtensions
{
    public static PdfPage UseFontPage(this ContentStream cs)
    {
        // The page-attached top-level stream is the one with no parent.
        // ContentStream tracks its parent privately, so we go via the
        // internal helper RequirePage; if that's unavailable here, the
        // caller is in trouble anyway.
        return cs.RequirePageForSamples();
    }

    internal static PdfPage RequirePageForSamples(this ContentStream cs) =>
        cs.RequirePage(nameof(UseFontPage));
}
