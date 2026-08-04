using PdfSpec.Content;
using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Layout;
using PdfSpec.Objects;
using PdfSpec.Structure;

namespace PdfSpec.Samples;

/// <summary>
/// Combined showcase — every covered sample folded into four pages of
/// merged sections. Composition is pure fluent: <see cref="Element.VStack"/>
/// / <see cref="Element.HStack"/> / <see cref="Element.Container"/> +
/// fluent <see cref="PdfDoc"/> / <see cref="PdfPage"/>. Raw drawing
/// (paths, transforms, raster images, text-state operators) lives
/// inside <see cref="Element.Canvas"/> bodies whose <c>draw</c> delegate
/// receives an imperative <see cref="ContentStream"/>. PageBreaks
/// separate the four pages.
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
        PdfDoc.Create()
            .Info(title: "PdfSpec Combined Showcase", creator: "PdfSpec", producer: "PdfSpec")
            .DefaultFont(StandardFont.Helvetica, 11)
            .DefaultPageSize(PageSizes.A4)
            .AddPage(PageSizes.A4, p => p
                .Header(BuildHeader())
                .Footer(BuildFooter())
                .AddBody(
                    CoverBody(),
                    Page1_DocumentBasics(),
                    Page2_Imaging(),
                    Page3_Text(),
                    Page4_NavStructureMetadata(p.Document)))
            .Save(path);
    }

    // ===== shared header / footer ============================================

    /// <summary>Light-blue strip carrying the showcase title — full page width.</summary>
    private static Element BuildHeader() => Element.Container()
        .Background(PdfColor.Rgb(0.85, 0.92, 0.97))
        .Padding(vertical: 10, horizontal: 20)
        .Content(Element.Paragraph("PdfSpec — Combined Showcase", StandardFont.HelveticaBold, 14));

    /// <summary>
    /// Light-red strip with a centered "Page N of M" sourced from a
    /// <see cref="Element.Deferred"/>. The outer container centers the
    /// reservation horizontally; the deferred's render callback wraps
    /// its Paragraph in another centered container so the actual
    /// (shorter) "Page 1 of 5" text centers inside the
    /// "Page 999 of 999"-sized reservation rather than left-aligning.
    /// </summary>
    private static Element BuildFooter() => Element.Container()
        .Background(PdfColor.Rgb(0.98, 0.88, 0.88))
        .Padding(vertical: 8, horizontal: 20)
        .HAlign(HorizontalAlignment.Center)
        .Content(Element.Deferred(
            sizeHint: Element.Paragraph("Page 999 of 999", StandardFont.Helvetica, 10),
            render: data => Element.Container()
                .HAlign(HorizontalAlignment.Center)
                .Content(Element.Paragraph($"Page {data.PageNumber} of {data.TotalPages}",
                    StandardFont.Helvetica, 10))));

    // ===== cover ==============================================================

    /// <summary>
    /// Cover-page body — the shared header / footer already provide the
    /// outer chrome, so this only owns the in-page hero content (large
    /// title + subtitle + intro).
    /// </summary>
    private static Element CoverBody() => Element.VStack(v => v
        .Auto(Element.Container()
            .PaddingTop(60)
            .Content(Element.Paragraph("PdfSpec", StandardFont.HelveticaBold, 40)))
        .Auto(Element.Container()
            .PaddingTop(8)
            .Content(Element.Paragraph("Combined Showcase", StandardFont.Helvetica, 20)))
        .Auto(Element.Container()
            .PaddingTop(40)
            .Content(Element.Paragraph(
                "Four pages, sixteen samples — basics, imaging, text + font metrics, " +
                "and navigation/structure/metadata. Section composition is pure fluent " +
                "layout (VStack / HStack / Container); raw drawing lives inside Canvas " +
                "bodies. The footer at the bottom of every page is a Deferred — it " +
                "reserves space during layout and resolves the real page count once " +
                "every page has been laid out.",
                StandardFont.Helvetica, 11))));

    // ===== page 1 — document basics (samples 01-04) ===========================

    private static Element Page1_DocumentBasics() => Element.VStack(v => v
        .Auto(PageHeader("Page 1 — Document basics", "Samples 01 to 04"))
        .Auto(SubSection("01 — Blank page",
            "A minimal valid PDF: catalog → page tree → one blank Letter page. " +
            "No content streams, no fonts, no resources — the smallest output the " +
            "writer can produce."))
        .Auto(SubSection("02 — Hello world",
            "One page with “Hello, World!” drawn through the imperative AddText " +
            "builder at 24 pt Helvetica.",
            body: Element.Canvas(500, 40, (cs, _) =>
                cs.AddText(StandardFont.Helvetica, 24).Show(0, 26, "Hello, World!").Build())))
        .Auto(SubSection("03 — Document structure",
            "Three pages exercising page-tree attribute inheritance: a default " +
            "MediaBox on the root, UserUnit=2 on page 2, and an A4-override-plus-90°-" +
            "rotation on page 3. The catalog gets SinglePage layout and the UseThumbs " +
            "page mode."))
        .Auto(SubSection("04 — Name tree",
            "Two named destinations registered in a /Dests name tree under the " +
            "catalog. Each entry is an explicit [page /Fit] array so a link elsewhere " +
            "can jump by name.")));

    // ===== page 2 — imaging (samples 05-09 + 28) ==============================

    private static Element Page2_Imaging() => Element.VStack(v => v
        .Auto(PageHeader("Page 2 — Imaging", "Samples 05-09 + 28"))
        .Auto(SubSection("05 — Imaging model",
            "Painter's model, Bézier circle, three device colour spaces, line caps/joins, transforms.",
            body: ImagingCanvas(width: 500, height: 200)))
        .Auto(SubSection("06 — Transparency + 09 — Form XObject",
            "Constant alpha via ExtGState resources; a gold star defined once and painted with three transforms.",
            body: Element.HStack(h => h
                .Relative(1, TransparencyCanvas(width: 240, height: 140))
                .Relative(1, StarCanvas(width: 240, height: 140)))))
        .Auto(SubSection("07 — Raster image + 08 — Image masks",
            "One DeviceRGB gradient painted at two sizes; soft / colour-key / stencil masks over coloured plates.",
            body: Element.HStack(h => h
                .Relative(1, RasterCanvas(width: 240, height: 140))
                .Relative(1, ImageMasksCanvas(width: 240, height: 140)))))
        .Auto(SubSection("28 — Extra operators",
            "Nonzero vs even-odd fill on a pentagram; v / y Bézier variants; the quote operator + inline BI/ID/EI image.",
            body: OperatorsCanvas(width: 500, height: 130))));

    // ===== page 3 — text + font metrics (samples 10-11) =======================

    private static Element Page3_Text()
    {
        var painting = SampleFonts.PaintingWithChocolate();
        var quake = SampleFonts.Quake3d();

        return Element.VStack(v => v
            .Auto(PageHeader("Page 3 — Text", "Samples 10-11 + font metrics"))
            .Auto(SubSection("10 — Text fonts",
                "Seven of the standard 14 fonts at 16 pt, rendered as a stack of paragraphs.",
                body: TextFontsList()))
            .Auto(SubSection("11 — Text state",
                "Render modes (fill / stroke / fill+stroke), char + word spacing, horizontal scaling, text rise.",
                body: TextStateCanvas(width: 500, height: 180)))
            .Auto(SubSection("Font metrics",
                "For each font + size combination: five horizontal guides at the typographic and Windows-clip " +
                "ascenders, the baseline, and the typographic and Windows-clip descenders, with the text drawn so " +
                "its baseline lands on the green guide. Typographic = the designer's intended line-leading " +
                "(sTypoAscender/Descender on TTF, AFM Ascender/Descender on Standard-14). Windows-clip = the actual " +
                "visible reach (usWinAscent/Descent on TTF). On Standard-14 the two pairs coincide — Adobe's AFM " +
                "Ascender already matches visual reach — so the typo and win guides overlay. Labels on the right " +
                "show both pairs from FontVerticalMetrics with matching colour swatches.",
                body: FontMetricsBlock()))
            .Auto(SubSection("31 — Embedded TrueType",
                "Two TTF faces shipped alongside the assembly (Samples/Fonts/) and embedded as PDF /TrueType " +
                "fonts. Each is rendered at two sizes through the same metrics renderer the Standard-14 list above " +
                "uses — but on these decorative faces the typographic and Windows-clip ascenders/descenders " +
                "diverge visibly: the orange/purple Windows guides hug the actual glyph reach, while the red/blue " +
                "typographic guides report the designer's intended (tighter) body-text line-leading.",
                body: FontMetricsBlock(new (Font, double, string)[]
                {
                    (painting, 14, "Painting with Chocolate 14 pt - Hjgpy"),
                    (painting, 22, "Painting with Chocolate 22 pt - Hjgpy"),
                    (quake,    14, "Quake3d 14 pt - Hjgpy"),
                    (quake,    22, "Quake3d 22 pt - Hjgpy"),
                }))));
    }

    // ===== page 4 — nav / structure / metadata (12, 13, 23, 26) ===============

    private static Element Page4_NavStructureMetadata(PdfDoc doc) => Element.VStack(v => v
        .Auto(PageHeader("Page 4 — Navigation, structure, metadata", "Samples 12, 13, 23, 26"))
        .Auto(SubSection("12 — Navigation",
            "Link buttons covering every action type: GoTo Fit (jumps back to the cover), GoTo named (Dests name tree), URI (external), GoToR (remote PDF). Each whole blue block is the click target via Container.OnRendered.",
            body: NavButtonStack(doc)))
        .Auto(SubSection("13 — Outline",
            "A five-visible-items bookmark tree: an open Document root with Section 1, Section 2 (closed, hiding Subsection 1), and Section 3, plus a top-level Summary."))
        .Auto(SubSection("23 — Optional content (layers)",
            "Three OCG layers (Red, Green, Blue) marked in the content stream via BDC /OC. Blue is OFF in the default config so it stays hidden until the user toggles it.",
            body: OptionalContentCanvas(width: 500, height: 110)))
        .Auto(SubSection("26 — Document metadata",
            "Title / Author / Subject / Keywords / Creator / Producer / CreationDate / ModDate set in both the Information dictionary and a matching XMP metadata stream so every reader finds consistent values.")));

    // ===== shared layout helpers ==============================================

    private static Element PageHeader(string title, string subtitle) => Element.VStack(v => v
        .Auto(Element.Paragraph(title, StandardFont.HelveticaBold, 18))
        .Auto(Element.Container()
            .PaddingBottom(6)
            .BorderBottom(1, Heading)
            .Content(Element.Paragraph(subtitle, StandardFont.Helvetica, 9))));

    private static Element SubSection(string title, string description, Element? body = null) =>
        Element.VStack(v =>
        {
            v.Auto(Element.Container()
                .PaddingTop(6)
                .Content(Element.Paragraph(title, StandardFont.HelveticaBold, 11)));
            v.Auto(Element.Paragraph(description, StandardFont.Helvetica, 8));
            if (body is not null)
            {
                v.Auto(Element.Container()
                    .PaddingTop(4)
                    .Content(body));
            }
        });

    // ===== imaging canvases ===================================================

    private static Element ImagingCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
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
        });

    private static Element TransparencyCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
        {
            var page = c.RequirePage("TransparencyCanvas");
            page.AddExtGState("GSopaqueC", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
            page.AddExtGState("GShalfC", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

            c.Save().SetExtGState("GSopaqueC").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(10, 10, 100, 100).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(60, 30, 100, 100).Fill().Restore();
            c.Save().SetExtGState("GShalfC").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(110, 50, 100, 100).Fill().Restore();
        });

    private static Element RasterCanvas(double width, double height)
    {
        const int w = 128, h = 128;
        var image = PdfImage.Rgb(SampleImages.MakeGradient(w, h), w, h);
        return Element.Canvas(width, height, (c, _) =>
        {
            c.DrawImage(image, 0, 0, 130, 130);
            c.DrawImage(image, 145, 30, 80, 80);
        });
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

        return Element.Canvas(width, height, (c, _) =>
        {
            c.Save().SetRgbFill(PdfColor.Rgb(1, 0.95, 0.4)).Rectangle(0, 0, 70, 130).Fill().Restore();
            c.DrawImage(soft, 0, 0, 70, 130);

            c.Save().SetRgbFill(PdfColor.Rgb(0.3, 0.8, 0.3)).Rectangle(80, 0, 70, 130).Fill().Restore();
            c.DrawImage(keyed, 80, 0, 70, 130);

            c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.85, 0.85)).Rectangle(160, 0, 70, 130).Fill().Restore();
            c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).DrawImage(stencil, 160, 0, 70, 130).Restore();
        });
    }

    private static Element StarCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
        {
            // FormXObject creation is a doc-level operation, so it has
            // to happen against an imperative PdfDoc — and inside a
            // Canvas Draw delegate that's already imperative,
            // c.RequirePage().Document is the natural way to reach it.
            // Captures the FormXObject in a static field so successive
            // draws (multiple sample runs in the same process) reuse it.
            var doc = c.RequirePage("StarCanvas").Document;
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

            c.DrawForm(star, 5, 15);
            c.DrawForm(star, 100, 30, 0.6);
            c.DrawForm(star, 175, 25, 0.5);
        });

    private static Element OperatorsCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
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
            c.AddText(StandardFont.Helvetica, 11).SetLeading(15).SetTextMatrix(1, 0, 0, 1, 0, 110)
                .ShowText("Quote operator: spacing + next-line show on")
                .NextLineShowText(wordSpacing: 4, charSpacing: 1, text: "one chained operator (Tj / ’ / TJ).")
                .Build();
        });

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

    private static Element TextFontsList() => Element.VStack(v => v
        .Auto(Element.Paragraph("Helvetica — Pack my box.", StandardFont.Helvetica, 14))
        .Auto(Element.Paragraph("Helvetica-Bold — Pack my box.", StandardFont.HelveticaBold, 14))
        .Auto(Element.Paragraph("Times-Roman — Pack my box.", StandardFont.TimesRoman, 14))
        .Auto(Element.Paragraph("Times-Italic — Pack my box.", StandardFont.TimesItalic, 14))
        .Auto(Element.Paragraph("Times-Bold — Pack my box.", StandardFont.TimesBold, 14))
        .Auto(Element.Paragraph("Courier — Pack my box.", StandardFont.Courier, 14)));

    private static Element TextStateCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
        {
            c.AddText(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 24)
                .SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).SetTextRenderMode(TextRenderMode.Fill)
                .ShowText("Fill mode (Tr 0)").Build();
            c.AddText(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 56)
                .SetRgbStroke(PdfColor.Rgb(0.1, 0.1, 0.8)).SetLineWidth(0.7).SetTextRenderMode(TextRenderMode.Stroke)
                .ShowText("Stroke mode (Tr 1)").Build();
            c.AddText(StandardFont.HelveticaBold, 22).SetTextMatrix(1, 0, 0, 1, 0, 88)
                .SetRgbFill(PdfColor.Rgb(1, 0.8, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetTextRenderMode(TextRenderMode.FillStroke)
                .ShowText("Fill + Stroke (Tr 2)").Build();

            c.SetRgbFill(PdfColor.Rgb(0, 0, 0));
            c.AddText(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 116)
                .ShowText("Normal: the quick brown fox").Build();
            c.AddText(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 134)
                .SetCharSpacing(3).ShowText("Tc 3: the quick brown fox").Build();
            c.AddText(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 152)
                .SetWordSpacing(6).ShowText("Tw 6: the quick brown fox").Build();
            c.AddText(StandardFont.Helvetica, 12).SetTextMatrix(1, 0, 0, 1, 0, 170)
                .ShowText("Rise: H").SetTextRise(-3).SetFont(StandardFont.Helvetica, 9).ShowText("2")
                .SetTextRise(0).SetFont(StandardFont.Helvetica, 12).ShowText(",  E = mc")
                .SetTextRise(5).SetFont(StandardFont.Helvetica, 9).ShowText("2").Build();
        });

    // ===== font metrics demo ==================================================
    //
    // For each (font, size) example: an HStack with the demo canvas on the
    // left and a VStack of swatch+label rows on the right. The canvas draws
    // five horizontal guides — typographic and Windows-clip ascenders, the
    // baseline, and typographic and Windows-clip descenders — and the text
    // itself with Show(x, typoAscenderY, …) so the AABB-top semantic
    // SetTextMatrix uses lands the baseline on the green guide. For
    // Standard-14 the two ascender (and two descender) guides coincide; for
    // decorative TTFs the Windows guides sit noticeably further out and
    // match the actual glyph envelope. Composition on the right is pure
    // layout: an HStack of swatch + label-paragraph per metric.

    private static readonly PdfColor TypoAscenderColour  = PdfColor.Rgb(0.75, 0.10, 0.10);
    private static readonly PdfColor WinAscenderColour   = PdfColor.Rgb(0.95, 0.55, 0.05);
    private static readonly PdfColor BaselineColour      = PdfColor.Rgb(0.05, 0.55, 0.20);
    private static readonly PdfColor TypoDescenderColour = PdfColor.Rgb(0.10, 0.20, 0.75);
    private static readonly PdfColor WinDescenderColour  = PdfColor.Rgb(0.45, 0.10, 0.65);
    private static readonly PdfColor LineBoxColour       = PdfColors.Slate(400);

    // Sample strings stay ASCII — the writer's PdfString currently octal-
    // escapes any char > 0x7E rather than encoding through the active
    // font's encoding (WinAnsi). An em-dash in the sample would render
    // as the escaped control byte 0x14 (not the WinAnsi 0x97 the font
    // expects), and TrueTypeFont.GetGlyphWidth would fall through to
    // the .notdef advance — so the measured width over-reports by ~1
    // glyph's worth and the dash never actually shows in the PDF.
    // Fix tracked separately as a WinAnsi encoder on the Tj path.
    private static Element FontMetricsBlock() => FontMetricsBlock(new (Font, double, string)[]
    {
        (StandardFont.Helvetica,  10, "Helvetica 10 pt - Hjgpy"),
        (StandardFont.Helvetica,  18, "Helvetica 18 pt - Hjgpy"),
        (StandardFont.TimesRoman, 14, "Times-Roman 14 pt - Hjgpy"),
        (StandardFont.TimesBold,  22, "Times-Bold 22 pt - Hjgpy"),
        (StandardFont.Courier,    12, "Courier 12 pt - Hjgpy"),
    });

    private static Element FontMetricsBlock(IEnumerable<(Font Font, double Size, string Sample)> examples) =>
        Element.VStack(v =>
        {
            foreach (var (font, size, sample) in examples)
            {
                v.Auto(Element.Container()
                    .PaddingTop(6).PaddingBottom(6)
                    .Content(MetricRow(font, size, sample)));
            }
        });

    private static Element MetricRow(Font font, double size, string sample)
    {
        var m = font.GetVerticalMetrics(size);
        return Element.HStack(h => h
            .Relative(3, MetricCanvas(font, size, sample, m), verticalAlignment: VerticalAlignment.Middle)
            .Relative(2, MetricLabels(m), verticalAlignment: VerticalAlignment.Middle));
    }

    /// <summary>
    /// The text + five-guides canvas. Width hugs the actual rendered
    /// width via <see cref="Font.MeasureText"/>; height is the envelope
    /// of *both* ascent/descent pairs so the Windows-clip guides stay
    /// in-frame even when they extend past the typographic line box.
    /// </summary>
    private static Element MetricCanvas(Font font, double size, string sample, FontVerticalMetrics m)
    {
        double textWidth  = font.MeasureText(sample, size);
        double maxAscent  = Math.Max(m.Ascent,  m.WinAscent);
        double maxDescent = Math.Max(m.Descent, m.WinDescent);
        double canvasH    = maxAscent + maxDescent;
        return Element.Canvas(textWidth, canvasH, (c, _) =>
        {
            double baselineY      = maxAscent;
            double typoAscenderY  = baselineY - m.Ascent;
            double winAscenderY   = baselineY - m.WinAscent;
            double typoDescenderY = baselineY + m.Descent;
            double winDescenderY  = baselineY + m.WinDescent;

            Guide(WinAscenderColour,   winAscenderY);
            Guide(TypoAscenderColour,  typoAscenderY);
            Guide(TypoDescenderColour, typoDescenderY);
            Guide(WinDescenderColour,  winDescenderY);
            c.Save().SetRgbStroke(BaselineColour).SetLineWidth(0.5)
                .MoveTo(0, baselineY).LineTo(textWidth, baselineY).Stroke().Restore();

            // Show(e, f) treats f as the typographic AABB top (offset by
            // font typoAscent above the baseline). So f = baselineY -
            // m.Ascent lands the baseline on the green guide regardless
            // of where the win guides sit.
            c.AddText(font, size).Show(0, typoAscenderY, sample).Build();

            void Guide(PdfColor colour, double y) =>
                c.Save().SetRgbStroke(colour).SetLineWidth(0.4).SetDash(new double[] { 2, 1.5 })
                    .MoveTo(0, y).LineTo(textWidth, y).Stroke().Restore();
        });
    }

    private static Element MetricLabels(FontVerticalMetrics m) => Element.VStack(v => v
        .Auto(LabelRow(TypoAscenderColour,  "typo asc",    m.Ascent))
        .Auto(LabelRow(WinAscenderColour,   "win asc",     m.WinAscent))
        .Auto(LabelRow(BaselineColour,      "baseline",    0))
        .Auto(LabelRow(TypoDescenderColour, "typo desc",   m.Descent))
        .Auto(LabelRow(WinDescenderColour,  "win desc",    m.WinDescent))
        .Auto(LabelRow(LineBoxColour,       "line height", m.LineHeight)));

    private static Element LabelRow(PdfColor swatch, string label, double value) => Element.HStack(h => h
        .Fixed(18, Element.Container()
            .Width(14).Height(4)
            .Background(swatch), verticalAlignment: VerticalAlignment.Middle)
        .Auto(Element.Paragraph($"{label,-12} {value:F2}", StandardFont.Courier, 8),
            verticalAlignment: VerticalAlignment.Middle));

    // ===== nav-page components ================================================

    /// <summary>
    /// The navigation block — a VStack of link buttons. Each button is a
    /// fixed-height Container that subscribes to
    /// <see cref="Container.OnRendered"/> so the whole blue block becomes
    /// a Link annotation covering its rendered rectangle. The four
    /// actions exercise GoTo Fit, GoTo named, URI, and GoToR remote —
    /// all wired off the document the sample is currently building, so
    /// the in-document links resolve to real pages.
    /// </summary>
    private static Element NavButtonStack(PdfDoc doc)
    {
        // Register a named destination once. "chapter-3" resolves to the
        // first page of the document (the cover) at the Fit zoom level
        // — same surface area as a hand-written /Dests entry would
        // produce.
        doc.AddNamedDestination("chapter-3", pageIndex: 0);

        var buttons = new (string Label, PdfDictionary Action)[]
        {
            ("GoTo page 1 (Fit)",            Navigation.PdfAction.GoTo(doc.PageDestination(0))),
            ("Named destination: chapter-3", Navigation.PdfAction.GoToNamed("chapter-3")),
            ("Open oreilly.com (URI)",       Navigation.PdfAction.Uri("https://www.oreilly.com")),
            ("Open Chapter2.pdf (GoToR)",    Navigation.PdfAction.GoToRemote("Chapter2.pdf", 0)),
        };

        return Element.VStack(v =>
        {
            foreach (var (label, action) in buttons)
            {
                v.Auto(Element.Container()
                    .PaddingBottom(6)
                    .Content(NavButton(label, action)));
            }
        });
    }

    private static Element NavButton(string label, PdfDictionary action) => Element.Container()
        .Width(260).Height(30)
        .Background(PdfColor.Rgb(0.90, 0.93, 1.0))
        .Border(1, PdfColor.Rgb(0.2, 0.3, 0.7))
        .VAlign(VerticalAlignment.Middle)
        // The full blue block is the click target — the Link annotation
        // is added at render time with the box's actual on-page Rect, so
        // the layout engine retains complete control of where the button
        // sits. There are no coordinates anywhere in the composition.
        .OnRendered(info => info.Page.AddLinkAnnotation(info.Bounds, action))
        .Content(Element.Container()
            .PaddingLeft(10)
            .Content(Element.Paragraph(label, StandardFont.Helvetica, 11)));

    private static Element OptionalContentCanvas(double width, double height) =>
        Element.Canvas(width, height, (c, _) =>
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
        });
}
