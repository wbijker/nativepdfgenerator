using CSharpPdf;
using CSharpPdf.Annotations;
using CSharpPdf.Content;
using CSharpPdf.Fluent;
using CSharpPdf.Forms;
using CSharpPdf.Layers;
using CSharpPdf.Layout;
using CSharpPdf.Multimedia;
using CSharpPdf.Tagging;
using PdfSpec;
using Element = CSharpPdf.Layout.Element;
using RenderResult = CSharpPdf.Layout.RenderResult;
using PdfSpec.ColorSpaces;
using PdfSpec.Content;
using PdfSpec.Files;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Navigation;
using PdfSpec.Objects;
using PdfSpec.Structure;

string samplesDir = Path.Combine(FindRepoRoot(), "samples");
Directory.CreateDirectory(samplesDir);

BuildBlankPage(Path.Combine(samplesDir, "01-blank.pdf"));
BuildHelloWorld(Path.Combine(samplesDir, "02-hello.pdf"));
BuildDocumentStructure(Path.Combine(samplesDir, "03-document-structure.pdf"));
BuildNameTree(Path.Combine(samplesDir, "04-name-tree.pdf"));
BuildImagingModel(Path.Combine(samplesDir, "05-imaging-model.pdf"));
BuildTransparency(Path.Combine(samplesDir, "06-transparency.pdf"));
BuildRasterImage(Path.Combine(samplesDir, "07-raster-image.pdf"));
BuildImageMasks(Path.Combine(samplesDir, "08-image-masks.pdf"));
BuildFormXObject(Path.Combine(samplesDir, "09-form-xobject.pdf"));
BuildTextFonts(Path.Combine(samplesDir, "10-text-fonts.pdf"));
BuildTextState(Path.Combine(samplesDir, "11-text-state.pdf"));
BuildNavigation(Path.Combine(samplesDir, "12-navigation.pdf"));
BuildOutline(Path.Combine(samplesDir, "13-outline.pdf"));
BuildMarkupAnnotations(Path.Combine(samplesDir, "14-markup-annotations.pdf"));
BuildStampAndNotes(Path.Combine(samplesDir, "15-stamp-and-notes.pdf"));
BuildFormBasics(Path.Combine(samplesDir, "16-form-basics.pdf"));
BuildFormChoices(Path.Combine(samplesDir, "17-form-choices.pdf"));
BuildEmbeddedFiles(Path.Combine(samplesDir, "18-embedded-files.pdf"));
BuildCollection(Path.Combine(samplesDir, "19-collection.pdf"));
BuildGoToEmbedded(Path.Combine(samplesDir, "20-goto-embedded.pdf"));
BuildSimpleMedia(Path.Combine(samplesDir, "21-simple-media.pdf"));
BuildMultimedia3D(Path.Combine(samplesDir, "22-multimedia-3d.pdf"));
BuildOptionalContent(Path.Combine(samplesDir, "23-optional-content.pdf"));
BuildOptionalContentAdvanced(Path.Combine(samplesDir, "24-optional-content-advanced.pdf"));
BuildTaggedStructure(Path.Combine(samplesDir, "25-tagged-structure.pdf"));
BuildMetadata(Path.Combine(samplesDir, "26-metadata.pdf"));
BuildPdfAStyle(Path.Combine(samplesDir, "27-pdfa-style.pdf"));
BuildOperators(Path.Combine(samplesDir, "28-operators.pdf"));
BuildShadings(Path.Combine(samplesDir, "29-shadings.pdf"));
BuildTextMeasurement(Path.Combine(samplesDir, "30-text-measurement.pdf"));
BuildTrueTypeEmbedding(Path.Combine(samplesDir, "31-truetype-embedding.pdf"));
BuildLayoutEngine(Path.Combine(samplesDir, "32-layout-text.pdf"));
BuildLayoutAlignment(Path.Combine(samplesDir, "33-layout-alignment.pdf"));
BuildLayoutTable(Path.Combine(samplesDir, "34-layout-table.pdf"));
RunWithTimeout("35", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "35-showcase-rows.pdf"), 1), 5.0);
RunWithTimeout("36", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "36-showcase-rows-cols.pdf"), 2), 5.0);
RunWithTimeout("37", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "37-showcase-extends.pdf"), 3), 5.0);
RunWithTimeout("38", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "38-showcase-image.pdf"), 4), 5.0);
RunWithTimeout("39", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "39-showcase-svg.pdf"), 5), 5.0);
RunWithTimeout("40", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "40-showcase-tables.pdf"), 6), 5.0);
RunWithTimeout("41", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "41-showcase-header-footer.pdf"), 7), 5.0);
RunWithTimeout("42", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "42-showcase-multi-column.pdf"), 8), 5.0);
RunWithTimeout("43", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "43-showcase-borders.pdf"), 9), 5.0);
RunWithTimeout("44", () => BuildShowcaseUpTo(Path.Combine(samplesDir, "44-showcase-layers.pdf"), 10), 5.0);
RunWithTimeout("45", () => BuildShowcase(Path.Combine(samplesDir, "45-programmatic.pdf")), 5.0);
RunWithTimeout("46", () => BuildFluentDemo(Path.Combine(samplesDir, "46-fluent.pdf")), 3.0);
RunWithTimeout("47", () => BuildSample47(Path.Combine(samplesDir, "47-table-side-by-side.pdf")), 5.0);
RunWithTimeout("48", () => BuildFluentShowcase(Path.Combine(samplesDir, "48-fluent-showcase.pdf")), 5.0);
RunWithTimeout("49", () => BuildRenderedHooksSample(Path.Combine(samplesDir, "49-rendered-hooks.pdf")), 5.0);
RunWithTimeout("50", () => BuildDynamicContentSample(Path.Combine(samplesDir, "50-dynamic-content.pdf")), 5.0);
RunWithTimeout("51", () => BuildElementComponentRoundTrip(Path.Combine(samplesDir, "51-element-component.pdf")), 5.0);
RunWithTimeout("52", () => BuildCanvasShowcase(Path.Combine(samplesDir, "52-canvas-showcase.pdf")), 5.0);

Console.WriteLine($"Wrote samples to {samplesDir}");

// A minimal valid PDF: catalog -> page tree -> a single blank US Letter page.
static void BuildBlankPage(string path)
{
    var doc = new PdfDoc();
    doc.AddPage(PageSizes.Letter);
    doc.Save(path);
    Report(path);
}

// A single page that draws "Hello, World!" using the standard Helvetica font.
static void BuildHelloWorld(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    AddTextLabel(doc, page, 72, 720, 24, "Hello, World!");
    doc.Save(path);
    Report(path);
}

// Chapter 1 "Document Structure": a multi-page document that exercises the page
// tree, attribute inheritance, page-layout / viewer preferences, rotation, and
// the UserUnit key.
static void BuildDocumentStructure(string path)
{
    var doc = new PdfDoc();

    // Catalog-level viewing options.
    doc.SetPageLayout("SinglePage");
    doc.SetPageMode("UseThumbs");
    doc.SetDisplayDocTitle(true);

    // A default page size on the page-tree root; pages below inherit it.
    doc.SetDefaultMediaBox(PageSizes.Letter);

    // Page 1: inherits MediaBox from the page tree (no MediaBox of its own).
    var p1 = doc.AddPage();
    AddTextLabel(doc, p1, 72, 720, 18, "Page 1: inherits Letter MediaBox from the page tree");

    // Page 2: still inherits the size, but doubles the user unit.
    var p2 = doc.AddPage();
    p2.SetUserUnit(2.0);
    AddTextLabel(doc, p2, 72, 720, 18, "Page 2: UserUnit 2.0 (144 units/inch)");

    // Page 3: overrides the inherited size with A4 and rotates 90 degrees.
    var p3 = doc.AddPage(PageSizes.A4);
    p3.SetRotation(90);
    AddTextLabel(doc, p3, 72, 720, 18, "Page 3: A4 override, rotated 90 degrees");

    doc.Save(path);
    Report(path);
}

// Chapter 1 "The Name Dictionary": register named destinations in a name tree
// under the catalog's /Names dictionary so pages can be referenced by name.
static void BuildNameTree(string path)
{
    var doc = new PdfDoc();

    var intro = doc.AddPage(PageSizes.Letter);
    AddTextLabel(doc, intro, 72, 720, 18, "Intro page (named destination: intro)");

    var summary = doc.AddPage(PageSizes.Letter);
    AddTextLabel(doc, summary, 72, 720, 18, "Summary page (named destination: summary)");

    // Each destination is an explicit destination array [page /Fit].
    var dests = new PdfNameTree();
    dests.Add("intro", new PdfArray(intro.Reference, new PdfName("Fit")));
    dests.Add("summary", new PdfArray(summary.Reference, new PdfName("Fit")));

    doc.SetNameTree("Dests", dests.Build());

    doc.Save(path);
    Report(path);
}

// Chapter 2 "PDF Imaging Model": vector graphics using the content-stream API —
// the painter's model, paths and curves, the three device color spaces,
// coordinate transforms, and clipping.
static void BuildImagingModel(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    var c = page.Content;

    // Painter's model: later shapes paint over earlier ones (top-left).
    c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(60, 660, 110, 90).Fill();
    c.SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(110, 635, 110, 90).Fill();
    c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(160, 610, 110, 90).Fill();

    // A Bézier circle, both filled (orange) and stroked (dark blue, dashed).
    c.Save()
        .SetRgbFill(PdfColor.Rgb(1, 0.6, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0.5)).SetLineWidth(2).SetDash(new double[] { 5, 2 })
        .Circle(470, 690, 55).FillStroke()
        .Restore();

    // The three device color spaces, as thick strokes.
    c.Save().SetLineWidth(10);
    c.SetGrayStroke(0.5).MoveTo(60, 560).LineTo(260, 560).Stroke();   // DeviceGray
    c.SetRgbStroke(PdfColor.Rgb(1, 0, 0)).MoveTo(60, 530).LineTo(260, 530).Stroke(); // DeviceRGB
    c.SetCmykStroke(PdfColor.Cmyk(1, 0, 0, 0)).MoveTo(60, 500).LineTo(260, 500).Stroke(); // DeviceCMYK (cyan)
    c.Restore();

    // Line caps and joins on a zigzag (round) vs a closed shape (bevel).
    c.Save().SetRgbStroke(PdfColor.Rgb(0, 0.6, 0)).SetLineWidth(10).SetLineCap(1).SetLineJoin(1);
    c.MoveTo(330, 560).LineTo(380, 530).LineTo(430, 560).LineTo(480, 530).LineTo(530, 560).Stroke();
    c.Restore();

    // Transforms: a 50% scaled square, a translated square, and a rotated square.
    c.Save().Translate(60, 360).Scale(0.5, 0.5).SetRgbFill(PdfColor.Rgb(0.8, 0, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
    c.Save().Translate(180, 360).SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(0, 0, 100, 100).Fill().Restore();
    c.Save().Translate(360, 410).Rotate(45).SetRgbFill(PdfColor.Rgb(0, 0, 0.8)).Rectangle(-50, -50, 100, 100).Fill().Restore();

    // Clipping: two rectangles clipped to a circular region (bottom).
    c.Save();
    c.Circle(200, 180, 90).Clip().EndPath();
    c.SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(110, 90, 90, 180).Fill();
    c.SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(200, 90, 90, 180).Fill();
    c.Restore();

    doc.Save(path);
    Report(path);
}

// Chapter 2 "Basic Transparency" + "Marked Content": named ExtGState resources
// carrying fill/stroke alpha (ca/CA), and content bracketed by marked-content
// operators (BMC/EMC and a BDC with an inline property list).
static void BuildTransparency(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);

    // ExtGState resources holding constant alpha for fill (ca) and stroke (CA).
    page.AddExtGState("GSopaque", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
    page.AddExtGState("GShalf", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

    var c = page.Content;

    // Three overlapping rectangles: opaque red, then 50%-alpha green and blue
    // that blend with whatever lies beneath them.
    c.Save().SetExtGState("GSopaque").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(150, 520, 170, 170).Fill().Restore();
    c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(0, 1, 0)).Rectangle(230, 460, 170, 170).Fill().Restore();
    c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(310, 400, 170, 170).Fill().Restore();

    // Marked content: a bracketed sequence with a plain tag (BMC/EMC).
    c.BeginMarkedContent("Demo");
    c.Save().SetRgbFill(PdfColor.Rgb(0.4, 0.4, 0.4)).Rectangle(150, 250, 120, 90).Fill().Restore();
    c.EndMarkedContent();

    // Marked content with an inline property list (BDC/EMC).
    var props = new PdfDictionary { ["Label"] = new PdfString("Translucent overlay"), ["Index"] = new PdfNumber(1) };
    c.BeginMarkedContent("Demo", props);
    c.Save().SetExtGState("GShalf").SetRgbFill(PdfColor.Rgb(1, 0.5, 0)).Rectangle(310, 250, 120, 90).Fill().Restore();
    c.EndMarkedContent();

    doc.Save(path);
    Report(path);
}

// Chapter 3 "Raster Images": a procedurally generated DeviceRGB image embedded
// as an Image XObject (Flate-compressed) and painted at two different sizes,
// showing that one resource can be reused with different transforms.
static void BuildRasterImage(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);

    const int w = 128, h = 128;
    var image = PdfImage.Rgb(MakeGradient(w, h), w, h);
    page.AddXObject("Im1", image.EmbedIn(doc));

    // Large, then a smaller copy of the same XObject.
    page.Content.DrawImage("Im1", 80, 430, 280, 280);
    page.Content.DrawImage("Im1", 380, 430, 120, 120);

    doc.Save(path);
    Report(path);
}

// Chapter 3 "Transparency and Images": the three masking techniques, each drawn
// over a colored background so the see-through areas are obvious.
static void BuildImageMasks(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    var c = page.Content;
    const int w = 128, h = 128;

    // 1) Soft mask: a solid image with a radial alpha mask fades out at the edges.
    var soft = PdfImage.Rgb(MakeSolid(w, h, 220, 30, 140), w, h);
    soft.SoftMask = PdfImage.Alpha(MakeRadialAlpha(w, h), w, h);
    page.AddXObject("ImSoft", soft.EmbedIn(doc));
    c.Save().SetRgbFill(PdfColor.Rgb(1, 0.95, 0.4)).Rectangle(60, 560, 200, 160).Fill().Restore(); // yellow bg
    c.DrawImage("ImSoft", 60, 560, 200, 160);

    // 2) Color-key mask: white pixels are dropped, leaving only the blue disc.
    var keyed = PdfImage.Rgb(MakeDiscOnWhite(w, h), w, h);
    keyed.ColorKeyMask = new PdfArray(
        new PdfNumber(255), new PdfNumber(255), new PdfNumber(255),
        new PdfNumber(255), new PdfNumber(255), new PdfNumber(255));
    page.AddXObject("ImKey", keyed.EmbedIn(doc));
    c.Save().SetRgbFill(PdfColor.Rgb(0.3, 0.8, 0.3)).Rectangle(320, 560, 200, 160).Fill().Restore(); // green bg
    c.DrawImage("ImKey", 320, 560, 200, 160);

    // 3) Stencil mask: a 1-bit ImageMask painted in the current fill color (red).
    page.AddXObject("ImStencil", PdfImage.Stencil(MakeCheckerBits(w, h), w, h).EmbedIn(doc));
    c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.85, 0.85)).Rectangle(60, 340, 200, 160).Fill().Restore(); // gray bg
    c.Save().SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).DrawImage("ImStencil", 60, 340, 200, 160).Restore();

    doc.Save(path);
    Report(path);
}

// Chapter 8 "Embedded Files": attach files to the document via the EmbeddedFiles
// name tree, and bind one to the page with a FileAttachment annotation.
static void BuildEmbeddedFiles(string path)
{
    var doc = new PdfDoc();
    doc.SetPageMode("UseAttachments");
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Embedded Files").Build()
        .AddText().SetFont("F1", 12).Show(100, 690, "Two files are attached. Click the paperclip or open the attachments panel.").Build();

    byte[] readme = System.Text.Encoding.UTF8.GetBytes(
        "Hello from CSharpPdf!\nThis text file is embedded inside the PDF.\n");
    byte[] csv = System.Text.Encoding.UTF8.GetBytes(
        "name,role\nAda,pioneer\nGrace,admiral\n");

    // Way 1: document-global, via the EmbeddedFiles name tree.
    doc.AddEmbeddedFile("people.csv", "people.csv", csv, "text/csv", "Sample data");

    // Way 2: page-specific, via a FileAttachment annotation referencing its own
    // file specification (built directly, not registered in the name tree).
    var readmeStream = doc.AddObject(EmbeddedFile.Stream(readme, "text/plain"));
    var readmeSpec = doc.AddObject(EmbeddedFile.FileSpec("readme.txt", readmeStream, "A small readme"));
    page.AddAnnotation(Annotation.FileAttachment(
        new PdfRectangle(62, 686, 80, 704), readmeSpec, "readme.txt", "Paperclip"));

    doc.Save(path);
    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 37 — Skeleton for the fluent (CSharpPdf.Fluent) API.
//
//  Entry point: Pdf.Create() returns a Document. The Document exposes page-
//  level chained setters (PageSize, Margin), Header / Footer / Content lambdas
//  that hand you a Container, and Save(path).
//
//  Reference (see src/CSharpPdf/Fluent/Container.cs for the full list):
//
//    Styling (any container)            .Padding(v)  .Background(c)  .Border(c, w)
//                                       .BorderRadius(r)  .BorderDash(...pattern)
//                                       .ExtendHorizontal()
//                                       .AlignLeft|Center|Right()
//                                       .AlignTop|Middle|Bottom()
//
//    Leaf content                       .Text("…")          → .Font(f).Bold().Italic()
//                                                              .FontSize(s).FontColor(c)
//                                                              .AlignLeft|Center|Right()
//                                       .Image(rgb, pw, ph) → .Size(w, h).Border(c, w)
//                                       .Svg(xml, w, h)
//                                       .PageNumber("Page {0} of {1}")
//                                       .PageReference("anchor", "p. {0}")
//
//    Composite content (lambda)         .Column(col => col.Item().Text(…)
//                                                         .ConstantItem(40).…
//                                                         .RelativeItem(2).…)
//                                       .Row(r => r.AutoItem().… .ConstantItem(60).…
//                                                  .RelativeItem(1).…)
//                                       .Layers(h, l => { l.Layer().…; l.Layer().… })
//                                       .Table(t => t.Header(h => h.Cell().Text(…))
//                                                    .Row(r => r.Cell().Text(…)))
//                                       .Transform(t => t.Rotate(deg).Scale(s)
//                                                        .Content(c => …))
//
//    Flow / sentinels                   .PageBreak()
//                                       .ShowAll(c => …)
//
//    Interactive                        .Link("https://…", c => …)
//                                       .LinkInternal("anchor-name", c => …)
//                                       .Note("popup text", icon: "Comment")
//                                       .Stamp("Approved", width, height)
//                                       .Bookmark("Section title")
//                                       .Anchor("name")
//
//    Page-number / anchor-page values are filled in during the single layout
//    pass via deferred regions — no measure phase to think about.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildFluentDemo(string path)
{
    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.A4)
        .Margin(0)
        .Content(c =>
        {
            c.Column(col =>
            {
                col.RelativeItem()
                    .Background(Colors.Red)
                    .Text("Relative");

                col.Item()
                    .Background(Colors.Blue)
                    // M = capital (CapSafety gap from top); jpqy = descenders (touch bottom).
                    .Text("Mg jpqy");

                col.Item()
                    .Background(Colors.LightGray)
                    .Element(new TestComponent
                    {
                        Title = "TestComponent",
                        Body = "This is a custom Element subclass plugged in directly.",
                        Accent = Colors.DarkBlue,
                        Surface = Colors.PaleYellow,
                    });

                col.RelativeItem()
                    .Background(Colors.Gray)
                    .Text("Relative");
            });
        })
        .Save(path);

    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 48 — comprehensive fluent-API showcase.
//
//  Walks through every public method on the Pdf.Create() / Document /
//  Container / Column / Row / Layers / Table / Transform / Cells surfaces,
//  plus every styling and content descriptor. Each section is a small
//  example you can copy-paste; sections are separated by PageBreak so the
//  PDF reads like a manual.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildFluentShowcase(string path)
{
    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.A4)
        .Margin(36)
        // ----- Page header -----
        .Header(h => h
            .ExtendHorizontal()
            .Background(Colors.DarkBlue)
            .Padding(8)
            .AlignCenter()
            .Text("CSharpPdf Fluent API Showcase")
            .FontColor(Colors.White)
            .FontSize(14)
            .Bold())
        // ----- Page footer (uses PageNumber to demonstrate dynamic content) -----
        .Footer(f => f
            .Padding(6)
            .AlignCenter()
            .PageNumber("Page {0} of {1}")
            .FontSize(9)
            .FontColor(Colors.Gray))
        // ----- Content -----
        .Content(c => c.Column(col =>
        {
            // ===== Title page =====
            col.Item().Padding(40);
            col.Item().AlignCenter().Text("Fluent API Showcase")
                .Bold().FontSize(36).FontColor(Colors.DarkBlue);
            col.Item().AlignCenter().Padding(8)
                .Text("Every public Container method in one document")
                .Italic().FontSize(12).FontColor(Colors.Gray);
            col.Item().Padding(8);

            // ===== Section 1 — Styling primitives =====
            col.Item().Bookmark("1. Styling primitives");
            col.Item().Anchor("styling-section");
            col.Item().Text("1. Styling — Padding, Background, Border, BorderRadius, BorderDash")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Row(r =>
            {
                r.RelativeItem().Padding(8).Background(Colors.PaleBlue).Text("Padding + Background");
                r.RelativeItem().Padding(8).Border(Colors.DarkBlue, 1).Text("Padded Border");
                r.RelativeItem().Padding(8).BorderRadius(6).Border(Colors.Red, 2).Text("Rounded");
                r.RelativeItem().Padding(8).BorderDash(4, 2).Border(Colors.Green, 1).Text("Dashed");
            });
            col.Item().Padding(12);

            col.Item().Text("Horizontal alignment — AlignLeft / AlignCenter / AlignRight")
                .Bold().FontSize(12);
            col.Item().Padding(4);
            col.Item().Row(r =>
            {
                r.RelativeItem().Background(Colors.PaleGreen).Padding(6).AlignLeft().Text("Left");
                r.RelativeItem().Background(Colors.PaleGreen).Padding(6).AlignCenter().Text("Center");
                r.RelativeItem().Background(Colors.PaleGreen).Padding(6).AlignRight().Text("Right");
            });
            col.Item().Padding(4);
            col.Item().Text("Vertical alignment — AlignTop / AlignMiddle / AlignBottom (in 60pt rows)")
                .Bold().FontSize(12);
            col.Item().Padding(4);
            col.ConstantItem(60).Row(r =>
            {
                r.RelativeItem().Background(Colors.PaleYellow).AlignTop().Padding(4).Text("Top");
                r.RelativeItem().Background(Colors.PaleYellow).AlignMiddle().Padding(4).Text("Middle");
                r.RelativeItem().Background(Colors.PaleYellow).AlignBottom().Padding(4).Text("Bottom");
            });

            col.Item().PageBreak();

            // ===== Section 2 — Text descriptor =====
            col.Item().Bookmark("2. Text styling");
            col.Item().Text("2. Text — Font / FontSize / FontColor / Bold / Italic / LineHeight")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Text("8pt regular").FontSize(8);
            col.Item().Text("12pt regular").FontSize(12);
            col.Item().Text("18pt regular").FontSize(18);
            col.Item().Text("24pt bold").FontSize(24).Bold();
            col.Item().Text("18pt italic").FontSize(18).Italic();
            col.Item().Text("Red text").FontColor(Colors.Red);
            col.Item().Text("Times Roman font").Font(Standard14Font.TimesRoman).FontSize(14);
            col.Item().Text("Tight leading (10pt over a 12pt font)").LineHeight(10);
            col.Item().Padding(6);
            col.Item().Text("Stretched & padded text").ExtendHorizontal()
                .Background(Colors.PaleYellow).Padding(4).AlignCenter();
            col.Item().Padding(4);
            col.Item().Text("Text on top of a bordered, rounded box")
                .Border(Colors.DarkBlue, 1).BorderRadius(4).Padding(6).AlignCenter();
            col.Item().Padding(4);
            col.Item().Text("With SaveMetric() — per-word widths cached on the canvas")
                .SaveMetric().FontSize(11).FontColor(Colors.Gray);

            col.Item().PageBreak();

            // ===== Section 3 — Column / Row sizing =====
            col.Item().Bookmark("3. Column & Row layouts");
            col.Item().Text("3. Column / Row — Item / ConstantItem / RelativeItem / AutoItem")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Text("Row sizing (AutoItem / ConstantItem(80) / RelativeItem / RelativeItem(2)):")
                .FontSize(11);
            col.Item().Padding(4);
            col.Item().Row(r =>
            {
                r.AutoItem().Background(Colors.PaleRed).Padding(4).Text("Auto");
                r.ConstantItem(80).Background(Colors.PaleGreen).Padding(4).Text("Constant 80");
                r.RelativeItem().Background(Colors.PaleBlue).Padding(4).Text("Relative 1");
                r.RelativeItem(2).Background(Colors.PaleYellow).Padding(4).Text("Relative 2");
            });
            col.Item().Padding(12);

            col.Item().Text("Column sizing — Item (content-sized) / ConstantItem(h) / AutoItem (alias for Item):")
                .FontSize(11);
            col.Item().Padding(4);
            col.Item().Background(Colors.PaleRed).Padding(4).Text("Item — natural text height");
            col.ConstantItem(40).Background(Colors.PaleGreen).Padding(4).Text("ConstantItem(40) — fixed 40pt tall");
            col.Item().Background(Colors.PaleBlue).Padding(4).Text("Item again — auto height");
            col.AutoItem().Background(Colors.PaleYellow).Padding(4).Text("AutoItem (alias for Item)");
            col.Item().Padding(4);
            col.Item().Text("RelativeItem(weight) is most useful inside a sized container — a Row distributes leftover "
                + "width by weight (see above), and a Column inside a fixed-height wrapper distributes leftover height.")
                .FontSize(10).FontColor(Colors.Gray);

            col.Item().PageBreak();

            // ===== Section 4 — Table =====
            col.Item().Bookmark("4. Table");
            col.Item().Text("4. Table — CellBorder / HeaderBackground / CellPadding / Header / Row / Cell")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Table(t => t
                .CellBorder(Colors.Gray, 0.5)
                .HeaderBackground(Colors.DarkBlue)
                .CellPadding(6)
                .Header(h =>
                {
                    h.Cell().Text("#").FontColor(Colors.White).Bold();
                    h.Cell().Text("Item").FontColor(Colors.White).Bold();
                    h.Cell().Text("Description").FontColor(Colors.White).Bold();
                    h.Cell().Text("Price").FontColor(Colors.White).Bold();
                })
                .Row(r =>
                {
                    r.Cell().Text("1");
                    r.Cell().Text("Widget");
                    r.Cell().Text("A high-quality widget for the assembly line.");
                    r.Cell().AlignRight().Text("$9.99");
                })
                .Row(r =>
                {
                    r.Cell().Text("2");
                    r.Cell().Text("Gadget");
                    r.Cell().Text("Premium-grade gadget — ships in a velvet box.");
                    r.Cell().AlignRight().Text("$14.50");
                })
                .Row(r =>
                {
                    r.Cell().Text("3");
                    r.Cell().Text("Gizmo");
                    r.Cell().Text("Industrial gizmo, certified to ISO 9001.");
                    r.Cell().AlignRight().Text("$22.99");
                }));

            col.Item().PageBreak();

            // ===== Section 5 — Image & SVG =====
            col.Item().Bookmark("5. Image & SVG");
            col.Item().Text("5. Image / Svg — raster + vector").Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Text("Image(rgb, pixelW, pixelH) — programmatically built gradient, displayed at 160×80:")
                .FontSize(11);
            col.Item().Padding(4);
            col.Item().Image(BuildGradientRgb(64, 32), 64, 32).Size(160, 80).Border(Colors.Gray, 1);
            col.Item().Padding(12);
            col.Item().Text("Svg(xml, w, h) — inline SVG fragment at 120×120:").FontSize(11);
            col.Item().Padding(4);
            col.Item().Svg(
                "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'>" +
                "  <rect x='10' y='10' width='80' height='80' fill='lightblue' stroke='navy' stroke-width='2'/>" +
                "  <circle cx='50' cy='50' r='30' fill='red' stroke='black' stroke-width='2'/>" +
                "</svg>",
                120, 120);

            col.Item().PageBreak();

            // ===== Section 6 — Transform =====
            col.Item().Bookmark("6. Transform");
            col.Item().Text("6. Transform — Rotate / Scale / Pivot")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.ConstantItem(140).Row(r =>
            {
                r.RelativeItem().Transform(t => t
                    .Rotate(15)
                    .Content(cc => cc.Padding(16).Background(Colors.PaleBlue).Text("Rotate 15°")));
                r.RelativeItem().Transform(t => t
                    .Scale(1.4)
                    .Content(cc => cc.Padding(6).Background(Colors.PaleGreen).Text("Scale 1.4×")));
                r.RelativeItem().Transform(t => t
                    .Scale(0.8, 1.3)
                    .Pivot(0.5, 0.5)
                    .Content(cc => cc.Padding(6).Background(Colors.PaleRed).Text("Scale 0.8 × 1.3, centred pivot")));
            });

            col.Item().PageBreak();

            // ===== Section 7 — Layers =====
            col.Item().Bookmark("7. Layers");
            col.Item().Text("7. Layers — bottom-to-top z-order overlays")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Layers(140, l =>
            {
                l.Layer().Background(Colors.PaleGray);
                l.Layer().AlignCenter().AlignMiddle().Text("background").FontColor(Colors.Gray).FontSize(36);
                l.Layer().AlignCenter().AlignMiddle().Text("foreground").FontColor(Colors.Red).FontSize(14).Bold();
            });

            col.Item().PageBreak();

            // ===== Section 8 — Interactive =====
            col.Item().Bookmark("8. Interactive");
            col.Item().Text("8. Interactive — Link / LinkInternal / Note / Stamp")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Link("https://example.com", lc => lc
                .Padding(4).Background(Colors.PaleBlue)
                .Text("External Link → https://example.com").FontColor(Colors.DarkBlue));
            col.Item().Padding(8);
            col.Item().LinkInternal("styling-section", lc => lc
                .Padding(4).Background(Colors.PaleGreen)
                .Text("Internal Link → jump to the Styling section anchor")
                .FontColor(Colors.DarkBlue));
            col.Item().Padding(12);
            col.Item().Text("Sticky-note annotation (hover in a PDF viewer):").FontSize(11);
            col.Item().Padding(4);
            col.Item().Note("This is the note's popup text. Icons: Comment, Note, Key, Help, …", icon: "Comment");
            col.Item().Padding(12);
            col.Item().Text("Stamp annotation:").FontSize(11);
            col.Item().Padding(4);
            col.Item().Stamp("Approved", width: 120, height: 40, contents: "Approved by the Showcase");

            col.Item().PageBreak();

            // ===== Section 9 — Cross-references =====
            col.Item().Bookmark("9. Cross-references");
            col.Item().Text("9. Cross-references — PageNumber / PageReference (deferred render)")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Row(r =>
            {
                r.AutoItem().Padding(4).Text("This is page ");
                r.AutoItem().Padding(4).PageNumber("{0}").Bold();
                r.AutoItem().Padding(4).Text(" of ");
                r.AutoItem().Padding(4).PageNumber("{1}").Bold();
                r.AutoItem().Padding(4).Text(".");
            });
            col.Item().Padding(4);
            col.Item().Row(r =>
            {
                r.AutoItem().Padding(4).Text("The Styling section is on page ");
                r.AutoItem().Padding(4).PageReference("styling-section").Bold().FontColor(Colors.DarkBlue);
                r.AutoItem().Padding(4).Text(".");
            });

            col.Item().Padding(12);
            col.Item().Text("Both values are resolved during the single-pass save via PdfCanvas.Defer — "
                + "the layout reserves space for them in this pass and patches in the real numbers once "
                + "every Anchor has been visited and TotalPages is final.")
                .FontSize(10).FontColor(Colors.Gray);

            col.Item().PageBreak();

            // ===== Section 10 — ShowAll & Element =====
            col.Item().Bookmark("10. ShowAll & Element");
            col.Item().Text("10. ShowAll — atomic block + Element() escape hatch")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(6);

            col.Item().Text("ShowAll wraps content so it can't be split across pages:").FontSize(11);
            col.Item().Padding(4);
            col.Item().ShowAll(sc => sc
                .Background(Colors.PaleGreen).Padding(12)
                .Text("This block is atomic. If it doesn't fit on the current page, it reflows whole onto the next."));
            col.Item().Padding(12);
            col.Item().Text("Element() drops in any raw Element subclass (escape hatch to the programmatic layer):")
                .FontSize(11);
            col.Item().Padding(4);
            col.Item().Element(new TestComponent
            {
                Title = "Custom Element",
                Body = "Plugged in via Container.Element() — a TestComponent rendered just like any other content.",
                Accent = Colors.DarkBlue,
                Surface = Colors.PaleYellow,
            });
        }))
        .Save(path);

    Report(path);
}

// Tiny helper used by the showcase: builds a horizontal red→blue gradient
// as a raw-RGB byte array (3 bytes per pixel, row-major).
static byte[] BuildGradientRgb(int w, int h)
{
    var data = new byte[w * h * 3];
    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 3;
            data[i + 0] = (byte)(255 * (w - 1 - x) / (w - 1));
            data[i + 1] = 64;
            data[i + 2] = (byte)(255 * x / (w - 1));
        }
    }
    return data;
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 49 — rendered-hook capture + skeleton overlay.
//
//  Three pages:
//    1. Page 1 — varied content; each element's OnRendered hook records its
//       boundary into a shared list.
//    2. Page 2 — a SkeletonOverlay element draws the captured page-1 boundaries
//       as stroked rectangles, with a small label per box, at the same absolute
//       positions they were placed on page 1.
//    3. Page 3 — different content, demonstrating the document continues
//       normally after the skeleton page.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildRenderedHooksSample(string path)
{
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;
    var italic = Standard14Font.HelveticaOblique;

    // Bucket every element's RenderedInfo into this list, tagged with a label
    // so the skeleton page can annotate each box.
    var captured = new System.Collections.Generic.List<(string Label, RenderedInfo Info)>();
    System.Action<RenderedInfo> Capture(string label) => info => captured.Add((label, info));

    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.Letter)
        .Margin(54)
        .Header(h => h
            .ExtendHorizontal().Background(Colors.DarkBlue).Padding(6).AlignCenter()
            .Text("Rendered-Hook Capture Demo").FontColor(Colors.White).FontSize(12).Bold())
        .Footer(f => f
            .Padding(6).AlignCenter()
            .PageNumber("Page {0} of {1}").FontSize(9).FontColor(Colors.Gray))
        .Content(c => c.Column(col =>
        {
            // ===== Page 1 — real content, each element hooks its RenderedInfo =====

            col.Item().OnRendered(Capture("title-block"))
                .Padding(4).AlignCenter()
                .Text("Annotated Layout").Bold().FontSize(22).FontColor(Colors.DarkBlue)
                .OnRendered(Capture("title-text"));

            col.Item().OnRendered(Capture("subtitle"))
                .Padding(4).AlignCenter()
                .Text("Every component below registers its bounding box via OnRendered.")
                .Italic().FontSize(11).FontColor(Colors.Gray);

            col.Item().Padding(8);

            col.Item().OnRendered(Capture("paragraph-1"))
                .Padding(8).Background(Colors.PaleYellow).Border(Colors.DarkBlue, 0.5)
                .Text("A paragraph with a yellow background and a thin blue border. " +
                      "The slot captures the outer bounding rectangle including its padding.")
                .FontSize(11);

            col.Item().Padding(6);

            col.Item().OnRendered(Capture("two-col-row"))
                .Row(row =>
                {
                    row.RelativeItem().OnRendered(Capture("left-cell"))
                        .Padding(6).Background(Colors.PaleBlue)
                        .Text("Left cell — RelativeItem(1)").FontSize(10);
                    row.ConstantItem(140).OnRendered(Capture("right-cell"))
                        .Padding(6).Background(Colors.PaleGreen)
                        .Text("Right cell — ConstantItem(140)").FontSize(10);
                });

            col.Item().Padding(6);

            col.Item().OnRendered(Capture("image-block"))
                .AlignCenter()
                .Image(BuildGradientRgb(64, 32), 64, 32).Size(180, 50).Border(Colors.Gray, 1)
                .OnRendered(Capture("image"));

            col.Item().Padding(6);

            col.Item().OnRendered(Capture("table-block"))
                .Table(t => t
                    .OnRendered(Capture("table"))
                    .CellBorder(Colors.Gray, 0.5)
                    .HeaderBackground(Colors.DarkBlue)
                    .CellPadding(4)
                    .Header(h =>
                    {
                        h.Cell().Text("Region").FontColor(Colors.White).Bold();
                        h.Cell().Text("Units").FontColor(Colors.White).Bold();
                    })
                    .Row(r => { r.Cell().Text("North"); r.Cell().AlignRight().Text("128"); })
                    .Row(r => { r.Cell().Text("South"); r.Cell().AlignRight().Text("96"); })
                    .Row(r => { r.Cell().Text("East");  r.Cell().AlignRight().Text("142"); }));

            col.Item().Padding(6);

            col.Item().OnRendered(Capture("closing"))
                .Padding(4).AlignCenter()
                .Text("End of page 1.").FontSize(10).FontColor(Colors.Gray);

            // ===== Page 2 — skeleton overlay built from the captured infos ====

            col.Item().PageBreak();

            col.Item().Padding(4).AlignCenter()
                .Text("Layout Skeleton — boundaries captured on page 1")
                .Bold().FontSize(14).FontColor(Colors.DarkBlue);
            col.Item().Padding(4);

            // The overlay reads `captured` at render-time; by then page 1 has
            // already fired every hook so the list is fully populated.
            col.Item().Element(new SkeletonOverlay(captured, targetPage: 1)
            {
                Stroke = Colors.Red,
                LineWidth = 0.6,
                LabelFont = italic,
                LabelSize = 7,
            });

            col.Item().Padding(8);

            col.Item().Padding(4).AlignCenter()
                .Text("Each red rectangle marks where the corresponding element landed "
                    + "on page 1 in PDF absolute coordinates.")
                .FontSize(9).FontColor(Colors.Gray);

            // ===== Page 3 — different content ===============================

            col.Item().PageBreak();

            col.Item().Padding(4).AlignCenter()
                .Text("Page 3 — life after the skeleton").Bold().FontSize(18).FontColor(Colors.DarkBlue);
            col.Item().Padding(8);

            col.Item().Padding(6)
                .Text("The hook-and-replay machinery is composable: the SkeletonOverlay above is "
                    + "just a small Element that reads the captured list during its render call. "
                    + "Any post-layout overlay — accessibility-tag tracing, hit-test regions, debug "
                    + "rulers — can be built the same way.")
                .FontSize(11);

            col.Item().Padding(8);

            col.Item().Padding(6).Background(Colors.PaleGreen)
                .Text("Try it: filter `captured` by Page or by element type, and overlay "
                    + "anything you like.")
                .FontSize(10);
        }))
        .Save(path);

    // Echo what was captured to stdout — useful for spot-checking values.
    Console.WriteLine($"  captured {captured.Count} entries from sample 49:");
    foreach (var (label, info) in captured)
    {
        Console.WriteLine($"    {label,-14} page={info.Page} pos=({info.AbsolutePos.X:F1},{info.AbsolutePos.Y:F1}) " +
                          $"box=({info.Boundary.Width:F1}×{info.Boundary.Height:F1})");
    }

    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 50 — DynamicContent: footer that knows the last quote on its page.
//
//  Each quote in the body uses OnRendered to record itself as "the last quote
//  seen on page N" into a shared dictionary. The footer is built with
//  DynamicContent: the initial block reserves room for a long placeholder
//  string, then a deferred callback runs once per page (after every
//  OnRendered has fired and TotalPages is final) to draw the actual line
//  using the dictionary indexed by ctx.Page.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildDynamicContentSample(string path)
{
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;
    var italic = Standard14Font.HelveticaOblique;

    // Per-page state populated during the main pass by each quote's OnRendered.
    // Mapped to ctx.Page by the footer's deferred callback below.
    var lastQuotePerPage = new System.Collections.Generic.Dictionary<int, string>();

    string[] quotes =
    {
        "The only true wisdom is in knowing you know nothing. — Socrates",
        "I think, therefore I am. — Descartes",
        "Imagination is more important than knowledge. — Einstein",
        "Whereof one cannot speak, thereof one must be silent. — Wittgenstein",
        "Man is condemned to be free. — Sartre",
        "The unexamined life is not worth living. — Socrates",
        "We are what we repeatedly do. Excellence is a habit. — Aristotle",
        "Hell is other people. — Sartre",
        "The owl of Minerva spreads its wings only with the falling of the dusk. — Hegel",
        "God is dead. God remains dead. And we have killed him. — Nietzsche",
        "He who has a why to live can bear almost any how. — Nietzsche",
        "One cannot step twice into the same river. — Heraclitus",
        "The map is not the territory. — Korzybski",
        "I can't go on. I'll go on. — Beckett",
        "Be the change you wish to see in the world. — Gandhi",
        "Cogito, ergo sum.",
        "The road of excess leads to the palace of wisdom. — Blake",
        "There is nothing either good or bad, but thinking makes it so. — Shakespeare",
        "A foolish consistency is the hobgoblin of little minds. — Emerson",
        "Time is a flat circle. — Nietzsche, paraphrased",
    };

    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.Letter)
        .Margin(5)
        .Header(h => h
            .ExtendHorizontal().Background(Colors.DarkBlue).Padding(6).AlignCenter()
            .Text("Dynamic Content — footer reads per-page state")
            .FontColor(Colors.White).FontSize(12).Bold())
        .Footer(f => f
            .ExtendHorizontal().Background(Colors.PaleGray).Padding(6)
            .Row(r =>
            {
                r.AutoItem().Padding(2)
                    .PageNumber("Page {0} of {1}")
                    .FontSize(9).FontColor(Colors.Gray);

                r.RelativeItem().AlignRight().Padding(2).DynamicContent(
                    // Initial: long worst-case placeholder so the deferred draw
                    // never exceeds the reserved width / height.
                    init => init
                        .Text("Last on this page: a sufficiently long placeholder line")
                        .Italic().FontSize(9).FontColor(Colors.Gray),
                    // Deferred: ctx.Page is the page this footer sits on; pull
                    // the matching entry from the dictionary populated during
                    // the main pass.
                    (c, ctx) =>
                    {
                        string text = lastQuotePerPage.TryGetValue(ctx.Page, out var v) ? v : "(none)";
                        string preview = text.Length > 56 ? text[..53] + "…" : text;
                        c.Text("Last on this page: " + preview)
                            .Italic().FontSize(9).FontColor(Colors.Gray);
                    });
            }))
        .Content(c => c.Column(col =>
        {
            // Reusable IComponent — composed via .Component(...). Same effect
            // as inline fluent calls, but bottled up as a typed class with
            // parameters (Title / Subtitle).
            col.Item().Component(new TitleHeader
            {
                Title = "Aphorisms",
                Subtitle = "Each item's OnRendered records the page it landed on. "
                         + "The footer's DynamicContent reads that dictionary at deferred-render time.",
            });

            int n = 1;
            foreach (var quote in quotes)
            {
                int idx = n++;
                string label = $"#{idx:00} {quote}";
                col.Item()
                    .Padding(12)
                    .Background(Colors.PaleYellow)
                    .Border(Colors.LightGray, 0.5)
                    .BorderRadius(3)
                    .Text(label)
                    .FontSize(13)
                    // Record this quote against whichever page it actually
                    // ended up on — the footer will read this back later.
                    .OnRendered(info => lastQuotePerPage[info.Page] = label);
                col.Item().Padding(2);
            }
        }))
        .Save(path);

    // Diagnostic: show what the footer will end up reading per page.
    Console.WriteLine($"  per-page state for sample 50:");
    foreach (var kv in lastQuotePerPage)
    {
        string preview = kv.Value.Length > 60 ? kv.Value[..57] + "…" : kv.Value;
        Console.WriteLine($"    page {kv.Key}: {preview}");
    }

    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 51 — Elements and Components, both directions.
//
//   A. A Component contains a custom Element.
//      ProductCard (IComponent) composes a fluent card and plugs in
//      StarRatingElement (a custom Element) via col.Element(...).
//
//   B. An Element renders a Component.
//      FramedSection (custom Element) calls canvas.Draw(x, y, IComponent)
//      from inside its RenderCore to place a ProductCard inside its frame.
//
//   C. An Element renders an inline fluent block.
//      Same FramedSection, this time fed an Action<Container> — uses
//      canvas.Draw(x, y, Action<Container>) so no IComponent class is needed.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildElementComponentRoundTrip(string path)
{
    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.Letter)
        .Margin(30)
        .Header(h => h
            .ExtendHorizontal().Background(Colors.DarkBlue).Padding(8).AlignCenter()
            .Text("Sample 51 — Elements and Components, in both directions")
            .Bold().FontSize(13).FontColor(Colors.White))
        .Content(c => c.Column(col =>
        {
            col.Item().Padding(6);

            // ── A. Component contains a custom Element ─────────────────
            col.Item().Text("A.  Component containing a custom Element")
                .Bold().FontSize(12).FontColor(Colors.DarkBlue);
            col.Item().Padding(2);
            col.Item().Text(
                    "ProductCard's Compose() builds a fluent column. The rating row inside "
                  + "is StarRatingElement — a custom Element subclass — plugged in via "
                  + "col.Element(new StarRatingElement { ... }).")
                .FontSize(9).FontColor(Colors.Gray);
            col.Item().Padding(8);

            col.Item().Component(new ProductCard
            {
                Title = "Vintage Notebook",
                Tagline = "Hand-bound, recycled paper. 200 lined pages.",
                Price = "$24.95",
                Rating = 5,
                Accent = Colors.DarkBlue,
            });
            col.Item().Padding(6);
            col.Item().Component(new ProductCard
            {
                Title = "Compact Pen",
                Tagline = "Aluminium body, refillable. Just-right weight.",
                Price = "$12.00",
                Rating = 3,
                Accent = Colors.Red,
                Surface = Colors.PaleRed,
            });

            col.Item().Padding(14);

            // ── B. Element renders a Component ─────────────────────────
            col.Item().Text("B.  Custom Element rendering a Component")
                .Bold().FontSize(12).FontColor(Colors.DarkBlue);
            col.Item().Padding(2);
            col.Item().Text(
                    "FramedSection is a custom Element. Inside its RenderCore it strokes the "
                  + "decorative frame and then calls canvas.Draw(x, y, IComponent) — the new "
                  + "PdfCanvas overload — to render an inner ProductCard.")
                .FontSize(9).FontColor(Colors.Gray);
            col.Item().Padding(8);

            col.Element(new FramedSection
            {
                Title = "Featured Today",
                FrameColor = Colors.Green,
                FrameHeight = 170,
                Content = new ProductCard
                {
                    Title = "Pocket Diary",
                    Tagline = "Sturdy hardcover. 365 dated pages.",
                    Price = "$29.95",
                    Rating = 4,
                    Accent = Colors.Green,
                    Surface = Colors.PaleGreen,
                },
            });

            col.Item().Padding(14);

            // ── C. Element renders an inline fluent block ──────────────
            col.Item().Text("C.  Custom Element rendering an inline fluent block")
                .Bold().FontSize(12).FontColor(Colors.DarkBlue);
            col.Item().Padding(2);
            col.Item().Text(
                    "Same FramedSection, but the content is an Action<Container> instead of "
                  + "an IComponent — uses canvas.Draw(x, y, Action<Container>) so no "
                  + "IComponent class is needed for one-shot content.")
                .FontSize(9).FontColor(Colors.Gray);
            col.Item().Padding(8);

            col.Element(new FramedSection
            {
                Title = "Quick Notice",
                FrameColor = Colors.Orange,
                FrameHeight = 90,
                Build = inner => inner.Padding(6).Column(ic =>
                {
                    ic.Item().Text("Inline composition, no IComponent class.")
                        .Bold().FontSize(11);
                    ic.Item().Padding(2);
                    ic.Item().Text("Useful for one-off content drawn from a custom Element's RenderCore — "
                                 + "you get the fluent surface without committing to a named class.")
                        .FontSize(9).FontColor(Colors.Gray);
                }),
            });
        }))
        .Save(path);

    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 52 — fluent .Canvas(w, h, draw) inline drawing showcase.
//
//   The Container.Canvas(...) method reserves a fixed rectangle and hands a
//   sub-PdfCanvas to a draw callback. Local (0,0) is bottom-left, Y-up;
//   high-level helpers (FillRectangle, DrawText, StrokeRoundedRectangle, …)
//   take local coords; path-based drawing through canvas.Graphics() uses raw
//   PDF coords, so the sample uses canvas.ToAbsoluteX/Y to translate.
//
//   Four mini-visualisations, all inline — no custom Element subclass:
//     A. Bar chart (FillRoundedRectangle + DrawText)
//     B. Sparkline (Graphics.DrawPolyline)
//     C. Star polygon (Graphics.DrawPolygon)
//     D. Flow diagram — three labelled boxes joined by arrows
// ─────────────────────────────────────────────────────────────────────────────
static void BuildCanvasShowcase(string path)
{
    var rng = new System.Random(7);
    var spark = new double[24];
    for (int i = 0; i < spark.Length; i++) spark[i] = 25 + rng.NextDouble() * 70;

    string[] cats = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    double[] bars = { 42, 67, 53, 78, 91, 88, 35 };

    CSharpPdf.Fluent.Pdf.Create()
        .PageSize(PageSizes.Letter)
        .Margin(30)
        .Header(h => h
            .ExtendHorizontal().Background(Colors.DarkBlue).Padding(8).AlignCenter()
            .Text("Sample 52 — Inline Canvas Drawing")
            .Bold().FontSize(13).FontColor(Colors.White))
        .Content(c => c.Column(col =>
        {
            col.Item().Padding(6);

            // A. Bar chart ───────────────────────────────────────────────
            col.Item().Text("A.  Bar chart")
                .Bold().FontSize(11).FontColor(Colors.DarkBlue);
            col.Item().Padding(4);

            col.Item().AlignCenter().Canvas(480, 140, (canvas, size) =>
            {
                double w = size.Width, h = size.Height;
                double padL = 28, padR = 6, padT = 14, padB = 18;
                double chartW = w - padL - padR, chartH = h - padT - padB;
                double maxV = 100, gap = 8;
                int n = bars.Length;
                double barW = (chartW - gap * (n - 1)) / n;

                // Horizontal grid + Y-axis labels at 0/25/50/75/100.
                for (int i = 0; i <= 4; i++)
                {
                    double y = padB + chartH * i / 4;
                    canvas.FillRectangle(padL, y + 0.25, chartW, 0.5, Colors.LightGray);
                    canvas.DrawText(Standard14Font.Helvetica, 7,
                        2, y - 2, (maxV * i / 4).ToString("0"), Colors.Gray);
                }

                // Bars + per-bar labels (category below, value above).
                for (int i = 0; i < n; i++)
                {
                    double v = bars[i];
                    double bh = chartH * v / maxV;
                    double bx = padL + i * (barW + gap);
                    double by = padB + bh;
                    canvas.FillRoundedRectangle(bx, by, barW, bh, Colors.Blue, 3);
                    canvas.DrawText(Standard14Font.Helvetica, 8,
                        bx + 4, padB - 11, cats[i], Colors.Gray);
                    canvas.DrawText(Standard14Font.HelveticaBold, 9,
                        bx + 4, by + 10, v.ToString("0"), Colors.DarkBlue);
                }
            });

            col.Item().Padding(12);

            // B. Sparkline ───────────────────────────────────────────────
            col.Item().Text("B.  Sparkline — 24 values via canvas.Graphics().DrawPolyline")
                .Bold().FontSize(11).FontColor(Colors.DarkBlue);
            col.Item().Padding(4);

            col.Item().AlignCenter().Canvas(480, 60, (canvas, size) =>
            {
                double w = size.Width, h = size.Height;
                double minV = 0, maxV = 100;

                // Bottom baseline.
                canvas.FillRectangle(0, 1, w, 0.5, Colors.LightGray);

                // Build polyline in ABSOLUTE coords (path API doesn't translate).
                var pts = new Point[spark.Length];
                for (int i = 0; i < spark.Length; i++)
                {
                    double x = w * i / (spark.Length - 1.0);
                    double yLocal = (spark[i] - minV) / (maxV - minV) * (h - 8) + 4;
                    pts[i] = new Point(canvas.ToAbsoluteX(x), canvas.ToAbsoluteY(yLocal));
                }
                using (var g = canvas.Graphics())
                {
                    g.DrawPolyline(pts, Colors.Red, 1.4);
                }

                // Last-value marker.
                double last = spark[spark.Length - 1];
                double lastY = (last - minV) / (maxV - minV) * (h - 8) + 4;
                canvas.DrawText(Standard14Font.HelveticaBold, 9,
                    w - 24, lastY + 8, last.ToString("0"), Colors.Red);
            });

            col.Item().Padding(12);

            // C. Star polygon ────────────────────────────────────────────
            col.Item().Text("C.  Polygon — a 5-point star")
                .Bold().FontSize(11).FontColor(Colors.DarkBlue);
            col.Item().Padding(4);

            col.Item().AlignCenter().Canvas(140, 120, (canvas, size) =>
            {
                double cx = size.Width / 2, cy = size.Height / 2;
                double outerR = 50, innerR = 20;
                const int corners = 5;
                var verts = new Point[corners * 2];
                for (int i = 0; i < corners * 2; i++)
                {
                    double r = (i % 2 == 0) ? outerR : innerR;
                    double angle = Math.PI / 2 + i * Math.PI / corners;
                    double x = cx + r * Math.Cos(angle);
                    double y = cy + r * Math.Sin(angle);
                    verts[i] = new Point(canvas.ToAbsoluteX(x), canvas.ToAbsoluteY(y));
                }
                using (var g = canvas.Graphics())
                {
                    g.DrawPolygon(verts, Colors.Yellow, Colors.Orange, 1.5);
                }
            });

            col.Item().Padding(12);

            // D. Mini flow diagram ───────────────────────────────────────
            col.Item().Text("D.  Mini flow diagram — boxes + arrows")
                .Bold().FontSize(11).FontColor(Colors.DarkBlue);
            col.Item().Padding(4);

            col.Item().AlignCenter().Canvas(480, 80, (canvas, size) =>
            {
                double w = size.Width, h = size.Height;
                double boxW = 100, boxH = 38;
                double boxTopY = (h + boxH) / 2;          // top edge in local Y-up coords
                double centerY = boxTopY - boxH / 2;

                double[] xs = { 14, (w - boxW) / 2, w - 14 - boxW };
                string[] labels = { "Input", "Process", "Output" };
                Color[] fills = { Colors.PaleBlue, Colors.PaleYellow, Colors.PaleGreen };

                // Arrows first (under the boxes).
                using (var g = canvas.Graphics())
                {
                    for (int i = 0; i < xs.Length - 1; i++)
                    {
                        double ax1 = canvas.ToAbsoluteX(xs[i] + boxW);
                        double ax2 = canvas.ToAbsoluteX(xs[i + 1]);
                        double ay = canvas.ToAbsoluteY(centerY);
                        g.DrawLine(ax1, ay, ax2, ay, Colors.Gray, 1.2);
                        var head = new Point[]
                        {
                            new Point(ax2, ay),
                            new Point(ax2 - 6, ay + 3.5),
                            new Point(ax2 - 6, ay - 3.5),
                        };
                        g.DrawPolygon(head, Colors.Gray, Colors.Gray, 1);
                    }
                }

                // Boxes + labels.
                for (int i = 0; i < xs.Length; i++)
                {
                    canvas.FillRoundedRectangle(xs[i], boxTopY, boxW, boxH, fills[i], 4);
                    canvas.StrokeRoundedRectangle(xs[i], boxTopY, boxW, boxH, Colors.DarkBlue, 1, 4);
                    var titleFont = Standard14Font.HelveticaBold;
                    double textW = titleFont.MeasureText(labels[i], 11);
                    canvas.DrawText(titleFont, 11,
                        xs[i] + (boxW - textW) / 2, centerY - 4, labels[i], Colors.DarkBlue);
                }
            });
        }))
        .Save(path);

    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 47 — no margin, light header / footer bands, one row with two
//  relative columns where the left holds an invoice table and the right is
//  empty. Built directly against the programmatic Element layer.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildSample47(string path)
{
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 0 };

    engine.SaveTwoPhase(path, eng =>
    {
        // Header: pale-gray band, big centered "Sample 47" with padding.
        eng.Header = new ColsElement
        {
            Background = Colors.PaleGray,
            Padding = 16,
            ExtendHorizontal = true,
            Slots =
            {
                new SlotElement { Sizing = Sizing.Relative },
                new SlotElement { Content = new TextElement("Sample 47", bold, 28) { FontColor = Colors.Black } },
                new SlotElement { Sizing = Sizing.Relative },
            },
        };

        // Footer: pale-gray band, "Page X of Y" right-aligned with padding.
        eng.Footer = new ColsElement
        {
            Background = Colors.PaleGray,
            Padding = 16,
            ExtendHorizontal = true,
            Slots =
            {
                new SlotElement { Sizing = Sizing.Relative },
                new SlotElement { Content = new PageNumberElement(body, 11)
                    { Format = "Page {0} of {1}", FontColor = Colors.Black } },
            },
        };

        // Build an invoice table similar to the §6 Tables showcase.
        var table = new TableElement
        {
            CellBorderColor = Colors.Gray,
            CellBorderThickness = 0.5,
            HeaderBackground = Colors.DarkBlue,
            CellPadding = 5,
            Header = new Element[]
            {
                new TextElement("#", bold, 11) { FontColor = Colors.White },
                new TextElement("Item", bold, 11) { FontColor = Colors.White },
                new TextElement("Short description", bold, 11) { FontColor = Colors.White },
                new TextElement("Long description", bold, 11) { FontColor = Colors.White },
                new TextElement("Qty", bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
                new TextElement("Price", bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
            },
        };
        string[] items = { "Widget", "Gadget", "Sprocket", "Cog", "Flange", "Bracket", "Bushing", "Gasket" };
        string[] grades = { "Standard", "Compact", "Premium", "Heavy-duty", "Lite", "OEM", "Eco", "Pro" };
        // Bumped to 20 rows so the table is intrinsically taller than one page,
        // to demonstrate what happens when ShowAllElement wraps content that
        // cannot fit even on a fresh page.
        for (int i = 1; i <= 20; i++)
        {
            string item = items[i % items.Length];
            string grade = grades[i % grades.Length];
            table.Rows.Add(new Element[]
            {
                new TextElement(i.ToString(), body, 10),
                new TextElement(item, body, 10),
                new TextElement(grade, body, 10),
                new TextElement($"A high-quality {item.ToLower()} for the assembly line.", body, 10),
                new TextElement((i * 3 % 9 + 1).ToString(), body, 10) { HAlign = HorizontalAlignment.Right },
                new TextElement(System.FormattableString.Invariant($"${(i * 1.49 % 30 + 0.5):0.00}"), body, 10) { HAlign = HorizontalAlignment.Right },
            });
        }

        // Main content: an outer Rows where each slot is a 2-column band
        // (content on the left, empty on the right). Putting the visual
        // "two columns" at the row level lets the Rows paginate between
        // bands — when the four paragraphs use enough page room that the
        // table band doesn't fit, the outer Rows moves the (ShowAllElement-
        // wrapped) table band onto the next page whole.
        static ColsElement TwoCol(Element left) => new()
        {
            Slots =
            {
                new SlotElement { Sizing = Sizing.Relative, Content = left },
                new SlotElement { Sizing = Sizing.Relative },
            },
        };

        eng.Add(new RowsElement
        {
            Slots =
            {
                new SlotElement { Content = TwoCol(new TextElement(
                    "The table below lists eight items recently picked from the assembly line. " +
                    "Each row carries an item number, a short name, a longer description, and a unit price. " +
                    "Item numbers are zero-padded only when the count grows past a hundred — at this " +
                    "scale the natural decimal width is comfortable enough to scan straight down the " +
                    "leftmost column without further treatment.",
                    body, 11) { Padding = 8, FontColor = Colors.Black }) },

                new SlotElement { Content = TwoCol(new TextElement(
                    "Quantities are right-aligned for readability, and the header band repeats on every " +
                    "page if the table paginates. The right column on this layout is intentionally left " +
                    "blank — it exists as a structural placeholder so the visual rhythm of the two-column " +
                    "rows above carries down to the table itself, and so a future revision can drop a " +
                    "sidebar in without rebuilding the layout.",
                    body, 11) { Padding = 8, FontColor = Colors.Gray }) },

                new SlotElement { Content = TwoCol(new TextElement(
                    "These two extra paragraphs use enough vertical space that the engine cannot fit the " +
                    "table on the same page. Because the table is wrapped in ShowAllElement, the engine " +
                    "treats it as atomic: rather than letting the first few rows render at the bottom of " +
                    "page one and the remainder on page two, the whole block is held back and rendered " +
                    "from the top of the next page in one stretch.",
                    body, 11) { Padding = 8, FontColor = Colors.Black }) },

                new SlotElement { Content = TwoCol(new TextElement(
                    "Each band here is itself a small two-column block — content on the left, " +
                    "intentionally empty on the right. The outer RowsElement is what actually paginates: " +
                    "when the next band won't fit on the current page it moves wholesale to the next page, " +
                    "and the engine carries the page header and footer over automatically so the new page " +
                    "looks like a continuation rather than a fresh start.",
                    body, 11) { Padding = 8, FontColor = Colors.Gray }) },

                // Table band: half-width Cols on the left, empty on the right.
                // ColsElement now propagates per-slot overflow as a continuation
                // Cols, and the engine force-renders any element that defers on
                // a fresh empty page — so the 20-row table paginates across pages
                // while staying pinned to the left half of every page it lands on.
                new SlotElement { Content = TwoCol(table) },
            },
        });
    });
    Report(path);
}


// Samples 35–44 — the progressive showcase, one section per sample.
// Each call adds one more Showcase section to the previous output:
//   35 = §1 Rows
//   36 = §1–2 (+ Cols)
//   37 = §1–3 (+ ExtendHorizontal)
//   38 = §1–4 (+ Image)
//   39 = §1–5 (+ SVG)
//   40 = §1–6 (+ Tables)
//   41 = §1–7 (+ Header/Footer descriptor + engine-level header/footer)
//   42 = §1–8 (+ Multi-column flow)
//   43 = §1–9 (+ Borders)
//   44 = §1–10 (+ Layer overlays)  ← complete showcase.
// The engine-level Header/Footer is wired in from sample 41 onward, matching
// the original progressive commits.
static void BuildShowcaseUpTo(string path, int sectionCount)
{
    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };

    bool withHeaderFooter = sectionCount >= 7;
    if (withHeaderFooter)
    {
        engine.Header = Showcase.ShowcaseHeader();
        engine.Footer = Showcase.ShowcaseFooter();
    }

    if (sectionCount >= 1)  engine.Add(Showcase.SectionRows());
    if (sectionCount >= 2)  engine.Add(Showcase.SectionCols());
    if (sectionCount >= 3)  engine.Add(Showcase.SectionExtends());
    if (sectionCount >= 4)  engine.Add(Showcase.SectionImage());
    if (sectionCount >= 5)  engine.Add(Showcase.SectionSvg());
    if (sectionCount >= 6)  engine.Add(Showcase.SectionTables());
    if (sectionCount >= 7)  engine.Add(Showcase.SectionHeaderFooter());
    if (sectionCount >= 8)  engine.Add(Showcase.SectionMultiColumn());
    if (sectionCount >= 9)  engine.Add(Showcase.SectionBorders());
    if (sectionCount >= 10) engine.Add(Showcase.SectionLayers());

    engine.Finish();
    doc.Save(path);
    Report(path);
}


// ─────────────────────────────────────────────────────────────────────────────
//  Sample 45 — Programmatic mirror of sample 46.
//
//  Same content as BuildFluentDemo above, written directly against the
//  Element classes (no CSharpPdf.Fluent layer). Compare the two side-by-side
//  to see what the fluent wrapper expands to: every fluent call is just an
//  object initialiser + Slot.Content = X under the hood.
//
//  Two-phase render works the same way — engine.SaveTwoPhase(path, build)
//  runs the build delegate twice (measure → render). Build everything inside
//  the delegate so element trees are constructed fresh per phase; that keeps
//  per-instance caches (ImageElement._imageRef, etc.) free of cross-phase
//  contamination.
// ─────────────────────────────────────────────────────────────────────────────
static void BuildShowcase(string path)
{
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };

    engine.SaveTwoPhase(path, eng =>
    {
        // Header: dark-blue band with two text slots and a relative spacer.
        eng.Header = new ColsElement
        {
            Background = Colors.DarkBlue,
            Padding = 8,
            ExtendHorizontal = true,
            Slots =
            {
                new SlotElement { Content = new TextElement("Sample 36", bold, 12)
                    { FontColor = Colors.White } },
                new SlotElement { Sizing = Sizing.Relative },
                new SlotElement { Content = new TextElement("Programmatic API skeleton", body, 10)
                    { FontColor = Colors.White } },
            },
        };

        // Footer: thin bordered band with "Page X of Y" pulled from PdfContext.
        eng.Footer = new ColsElement
        {
            Padding = 6,
            BorderColor = Colors.LightGray,
            BorderThickness = 0.5,
            ExtendHorizontal = true,
            Slots =
            {
                new SlotElement { Content = new TextElement("CSharpPdf", body, 9)
                    { FontColor = Colors.Gray } },
                new SlotElement { Sizing = Sizing.Relative },
                new SlotElement { Content = new PageNumberElement(body, 9)
                    { Format = "Page {0} of {1}", FontColor = Colors.Gray } },
            },
        };

        // Content: a single RowsElement that mirrors the sample-37 skeleton.
        eng.Add(new RowsElement
        {
            Slots =
            {
                new SlotElement { Padding = 4,
                    Content = new TextElement("Hello from the programmatic API", bold, 22)
                        { FontColor = Colors.DarkBlue } },

                new SlotElement { Padding = 4,
                    Content = new TextElement("Replace this content with whatever you want to try.",
                        body, 11) { FontColor = Colors.Gray } },

                // A custom Element plugged in directly — no Slot.Content wrapping
                // helpers needed; just set Content.
                new SlotElement { Padding = 4, Content = new TestComponent
                {
                    Title = "TestComponent",
                    Body = "Rendered by a custom Element subclass.",
                    Accent = Colors.DarkBlue,
                    Surface = Colors.PaleYellow,
                } },

                new SlotElement { Padding = 4, Content = new TestComponent
                {
                    Title = "Edit me",
                    Body = "Properties (Title, Body, Accent, Surface, Height) are plain setters.",
                    Accent = Colors.Red,
                    Surface = Colors.PaleRed,
                    Height = 70,
                } },
            },
        });
    });
    Report(path);
}

// Layout: a Table with shared auto-sized columns, a header that repeats on every
// page, per-cell borders, and pagination across many rows.
static void BuildLayoutTable(string path)
{
    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    var table = new TableElement
    {
        CellBorderColor = Colors.Gray,
        CellBorderThickness = 0.5,
        HeaderBackground = Colors.DarkBlue,
        CellPadding = 5,
        Header = new Element[]
        {
            new TextElement("#", bold, 11) { FontColor = Colors.White },
            new TextElement("Item", bold, 11) { FontColor = Colors.White },
            new TextElement("Description", bold, 11) { FontColor = Colors.White },
            new TextElement("Qty", bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
            new TextElement("Price", bold, 11) { FontColor = Colors.White, HAlign = HorizontalAlignment.Right },
        },
    };

    string[] items = { "Widget", "Gadget", "Sprocket", "Cog", "Flange", "Bracket", "Bushing", "Gasket" };
    for (int i = 1; i <= 45; i++)
    {
        string item = items[i % items.Length];
        table.Rows.Add(new Element[]
        {
            new TextElement(i.ToString(), body, 11),
            new TextElement(item, body, 11),
            new TextElement($"A high-quality {item.ToLower()} suitable for assembly line use and rework.", body, 11),
            new TextElement((i * 3 % 17 + 1).ToString(), body, 11) { HAlign = HorizontalAlignment.Right },
            new TextElement(System.FormattableString.Invariant($"${(i * 1.49 % 50 + 0.5):0.00}"), body, 11) { HAlign = HorizontalAlignment.Right },
        });
    }

    engine.Add(new RowsElement
    {
        Slots =
        {
            new SlotElement { Padding = 6, Content = new TextElement("Table — shared columns, repeating header, per-cell borders", bold, 16) },
            new SlotElement { Content = table },
        },
    });

    doc.Save(path);
    Report(path);
}

// Layout: block alignment (left/center/right), a full-width band (ExtendHorizontal),
// and a width-distributing Row (columns sized by min+preferred) with per-cell
// vertical alignment.
static void BuildLayoutAlignment(string path)
{
    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    engine.Add(new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = new TextElement("Alignment & Width Distribution", bold, 22) { Padding = 4 } },

            // Block alignment: the bg is sized to content (on the TextElement) and the
            // TextElement aligns itself within the slot's full width.
            new SlotElement { Content = new TextElement("Left aligned", body, 13)
                { Background = Colors.LightGray, Padding = 4, HAlign = HorizontalAlignment.Left } },
            new SlotElement { Content = new TextElement("Center aligned", body, 13)
                { Background = Colors.LightGray, Padding = 4, HAlign = HorizontalAlignment.Center } },
            new SlotElement { Content = new TextElement("Right aligned", body, 13)
                { Background = Colors.LightGray, Padding = 4, HAlign = HorizontalAlignment.Right } },

            // A bordered, full-width band: ExtendHorizontal makes the bg + border span the row.
            new SlotElement
            {
                Content = new TextElement("Full-width band with border (ExtendHorizontal)", body, 13)
                {
                    FontColor = Colors.White, Background = Colors.DarkBlue,
                    BorderColor = Colors.Black, BorderThickness = 1, Padding = 8,
                    ExtendHorizontal = true,
                },
            },

            new SlotElement { Content = new TextElement("Width-distributing Row (3 columns sized by min + preferred):", body, 12) { Padding = 4 } },

            // Three paragraphs share the row width via min + preferred distribution.
            // Per-cell bg/padding live on the inner TextElement so they hug content;
            // VAlign lives on the slot because Cols reads it to position the cell.
            new SlotElement
            {
                Content = new ColsElement
                {
                    Slots =
                    {
                        new SlotElement { VAlign = VerticalAlignment.Top,
                            Content = new TextElement("Short column.", body, 11)
                                { Background = Colors.LightGray, Padding = 6 } },
                        new SlotElement { VAlign = VerticalAlignment.Middle,
                            Content = new TextElement("A medium column with a bit more text so it wraps onto a couple of lines.", body, 11)
                                { Background = Colors.PaleGreen, Padding = 6 } },
                        new SlotElement { VAlign = VerticalAlignment.Bottom,
                            Content = new TextElement(
                                "The widest column, carrying the most text of the three so it claims the " +
                                "largest share of the available width and wraps to the most lines here.",
                                body, 11) { Background = Colors.PaleBlue, Padding = 6 } },
                    },
                },
            },

            // An image element placed by the layout engine.
            new SlotElement { Content = new TextElement("Image element:", body, 12) { Padding = 4 } },
            new SlotElement
            {
                Content = new ImageElement(MakeGradient(96, 96), 96, 96, 120, 80)
                    { BorderColor = Colors.Gray, BorderThickness = 1 },
            },
        },
    });

    doc.Save(path);
    Report(path);
}

// Layout engine, fluent API: a Column with a title, a Row with a colored
// background + padding, and a long Paragraph that wraps and paginates. Exercises
// the partial-render / remainder path (the paragraph is split across pages).
static void BuildLayoutEngine(string path)
{
    var doc = new PdfDoc();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };

    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    string longText = string.Join(" ", Enumerable.Repeat(
        "This paragraph is laid out by the engine: it wraps to the column width and, when " +
        "the page runs out of room, the remainder flows onto the next page automatically.", 60));

    engine.Add(new RowsElement
    {
        Slots =
        {
            new SlotElement { Content = new TextElement("Programmatic Layout API", bold, 24) { Padding = 4 } },
            new SlotElement
            {
                Content = new ColsElement
                {
                    Background = Colors.DarkBlue, Padding = 8, ExtendHorizontal = true,
                    Slots =
                    {
                        new SlotElement { Content = new TextElement("A Cols band with a background and padding", body, 14)
                            { FontColor = Colors.White } },
                    },
                },
            },
            new SlotElement { Content = new TextElement("Below is a long paragraph that wraps and paginates:", body, 12)
                { FontColor = Colors.Gray, Padding = 4 } },
            new SlotElement { Content = new TextElement(longText, body, 12) },
        },
    });

    doc.Save(path);
    Report(path);
}

// Embedding a TrueType font: load it, draw text via the Font API (which tracks
// unique fonts and embeds them at save), and show that measurement works for the
// embedded font too.
static void BuildTrueTypeEmbedding(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    var heading = Standard14Font.Helvetica; // also goes through the Font API
    using (var g = page.Canvas().Graphics()) g.DrawText(heading, 22, 60, 740, "Embedded TrueType Font");

    string? fontPath = FindTrueTypeFont();
    if (fontPath is null)
    {
        using (var g = page.Canvas().Graphics()) g.DrawText(heading, 12, 60, 710, "(no TrueType font found on this system to embed)");
        doc.Save(path);
        Report(path);
        return;
    }

    var ttf = TrueTypeFont.FromFile(fontPath);
    using (var g = page.Canvas().Graphics()) g.DrawText(heading, 11, 60, 712, $"Loaded {Path.GetFileName(fontPath)}  ->  BaseFont /{ttf.BaseFont}");

    using (var g = page.Canvas().Graphics()) g.DrawText(ttf, 30, 60, 660, "The quick brown fox jumps");
    using (var g = page.Canvas().Graphics()) g.DrawText(ttf, 16, 60, 624, "Big quartz jugs (pdfHQ) - embedded glyph outlines");
    using (var g = page.Canvas().Graphics()) g.DrawText(ttf, 14, 60, 596, "Accented: cafe, naive, Dusseldorf -> café, naïve, Düsseldorf");

    // Measurement works for the embedded font too: overlay a guide line for each
    // vertical metric (read from the font's OS/2 / hhea tables), like sample 30.
    DrawFontMetricGuides(page, heading, ttf, 28, 60, 540, "Measured TrueType");

    // Reusing the same font does not embed it twice.
    using (var g = page.Canvas().Graphics()) g.DrawText(heading, 11, 60, 450, "The font is embedded once even when reused across the document.");

    doc.Save(path);
    Report(path);
}

// Draw text in textFont and overlay a horizontal guide line for each of its
// vertical metrics, plus the line-height box and a color-coded legend (labels in
// labelFont). Works for any Font, including embedded TrueType.
static void DrawFontMetricGuides(PdfPage page, Font labelFont, Font textFont, double size, double bx, double by, string text)
{
    using (var g = page.Canvas().Graphics()) g.DrawText(textFont, size, bx, by, text);
    var vm = textFont.GetVerticalMetrics(size);
    double width = textFont.MeasureText(text, size);
    var c = page.Content;

    // Line-height box (gray) spans descent..ascent.
    c.Save().SetRgbStroke(PdfColor.Rgb(0.6, 0.6, 0.6)).SetLineWidth(0.75)
        .Rectangle(bx, by - vm.Descent, width, vm.LineHeight).Stroke().Restore();

    (string Name, double Y, double R, double G, double B)[] guides =
    {
        ("ascent", by + vm.Ascent, 0.0, 0.6, 0.0),
        ("cap height", by + vm.CapHeight, 0.9, 0.5, 0.0),
        ("x-height", by + vm.XHeight, 0.7, 0.0, 0.6),
        ("baseline", by, 0.0, 0.0, 0.9),
        ("descent", by - vm.Descent, 0.0, 0.55, 0.55),
    };
    foreach (var (_, gy, gr, gg, gb) in guides)
    {
        c.Save().SetRgbStroke(PdfColor.Rgb(gr, gg, gb)).SetLineWidth(0.6)
            .MoveTo(bx, gy).LineTo(bx + width, gy).Stroke().Restore();
    }

    // Color-coded legend to the right (line height first, then each metric).
    double lx = bx + width + 24, ly = by + vm.Ascent;
    FontLegendRow(page, labelFont, lx, ref ly, 0.6, 0.6, 0.6,
        System.FormattableString.Invariant($"line height {vm.LineHeight:0.0}"));
    foreach (var (name, gy, gr, gg, gb) in guides)
    {
        double value = gy - by;
        string label = name == "baseline"
            ? "baseline 0.0"
            : System.FormattableString.Invariant($"{name} {Math.Abs(value):0.0}");
        FontLegendRow(page, labelFont, lx, ref ly, gr, gg, gb, label);
    }
}

static void FontLegendRow(PdfPage page, Font font, double x, ref double y, double r, double g, double b, string label)
{
    page.Content.Save().SetRgbStroke(PdfColor.Rgb(r, g, b)).SetLineWidth(2).MoveTo(x, y + 3).LineTo(x + 16, y + 3).Stroke().Restore();
    double baselineY = y;
    using (var gfx = page.Canvas().Graphics()) gfx.DrawText(font, 9, x + 22, baselineY, label);
    y -= 13;
}

static string? FindTrueTypeFont()
{
    string[] candidates =
    {
        "/Users/willembijker/Downloads/Quake3d.ttf",
        "/System/Library/Fonts/Geneva.ttf",
        "/System/Library/Fonts/NewYork.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/Library/Fonts/Arial.ttf",
    };
    foreach (string candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }
    return null;
}

// Text measurement: alignment via measured widths, a verification box drawn to
// the measured width (it should hug the glyphs), and word-wrapped text fit to a
// fixed-width column using the Standard-14 metrics.
static void BuildTextMeasurement(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.AddFont("F2", doc.AddObject(StandardFonts.Create(StandardFonts.TimesRoman)));
    var c = page.Content;
    c.AddText().SetFont("F1", 22).Show(60, 740, "Text Measurement").Build();

    // Alignment around a guide line at x = 320.
    const double anchor = 320;
    c.Save().SetRgbStroke(PdfColor.Rgb(0.7, 0.7, 0.7)).SetLineWidth(0.5).MoveTo(anchor, 700).LineTo(anchor, 615).Stroke().Restore();
    c.AddText().SetFont("F1", 14).Show(anchor, 685, "Left-aligned at 320").Build();
    c.AddText().SetFont("F1", 14).Show(anchor - TextMeasurer.MeasureText(StandardFonts.Helvetica, 14, "Centered at 320") / 2, 655, "Centered at 320").Build();
    c.AddText().SetFont("F1", 14).Show(anchor - TextMeasurer.MeasureText(StandardFonts.Helvetica, 14, "Right-aligned at 320"), 625, "Right-aligned at 320").Build();

    // Measure a phrase with caps, ascenders, x-height letters and descenders
    // (g, j, p, q, y) and overlay a horizontal guide line for each font vertical
    // metric, plus the line-height box. Baseline is at 'by'.
    const string sample = "Big quartz jugs (pdfHQ)";
    const double size = 32;
    const string sampleFont = StandardFonts.TimesRoman;
    double width = TextMeasurer.MeasureText(sampleFont, size, sample);
    var vm = FontMetrics.GetVerticalMetrics(sampleFont, size);
    const double bx = 60, by = 580;
    c.AddText().SetFont("F2", size).Show(bx, by, sample).Build();

    // The line-height box (gray) spans descent..ascent.
    c.Save().SetRgbStroke(PdfColor.Rgb(0.6, 0.6, 0.6)).SetLineWidth(0.75)
        .Rectangle(bx, by - vm.Descent, width, vm.LineHeight).Stroke().Restore();

    // One horizontal guide line per metric, drawn across the text width.
    (string Name, double Y, double R, double G, double B)[] guides =
    {
        ("ascent", by + vm.Ascent, 0.0, 0.6, 0.0),
        ("cap height", by + vm.CapHeight, 0.9, 0.5, 0.0),
        ("x-height", by + vm.XHeight, 0.7, 0.0, 0.6),
        ("baseline", by, 0.0, 0.0, 0.9),
        ("descent", by - vm.Descent, 0.0, 0.55, 0.55),
    };
    foreach (var (_, gy, gr, gg, gb) in guides)
    {
        c.Save().SetRgbStroke(PdfColor.Rgb(gr, gg, gb)).SetLineWidth(0.6)
            .MoveTo(bx, gy).LineTo(bx + width, gy).Stroke().Restore();
    }

    // Color-coded legend to the right (line height first, then each metric).
    double legendX = bx + width + 24;
    double legendY = by + vm.Ascent;
    LegendRow(c, legendX, ref legendY, 0.6, 0.6, 0.6,
        System.FormattableString.Invariant($"line height {vm.LineHeight:0.0}"));
    foreach (var (name, gy, gr, gg, gb) in guides)
    {
        double value = gy - by; // signed distance from baseline
        string label = name == "baseline"
            ? "baseline 0.0"
            : System.FormattableString.Invariant($"{name} {System.Math.Abs(value):0.0}");
        LegendRow(c, legendX, ref legendY, gr, gg, gb, label);
    }

    // Word-wrapped paragraph in a box sized to fit the actual wrapped content.
    const string paragraphFont = StandardFonts.TimesRoman;
    const double psize = 13, leading = 17, pad = 8, wrapWidth = 250;
    const string paragraph =
        "This paragraph is wrapped, then the surrounding box is sized to fit the " +
        "actual content: its width matches the longest line and its height matches " +
        "the line count. Accented words like café and naïve are measured correctly too.";
    var lines = TextMeasurer.WrapText(paragraphFont, psize, paragraph, wrapWidth);
    double contentWidth = 0;
    foreach (string line in lines)
    {
        contentWidth = Math.Max(contentWidth, TextMeasurer.MeasureText(paragraphFont, psize, line));
    }
    var pvm = FontMetrics.GetVerticalMetrics(paragraphFont, psize);

    const double boxX = 60, boxTop = 470;
    double firstBaseline = boxTop - pad - pvm.Ascent;
    double lastBaseline = firstBaseline - (lines.Count - 1) * leading;
    double boxBottom = lastBaseline - pvm.Descent - pad;
    double boxWidth = contentWidth + 2 * pad;

    c.Save().SetRgbStroke(PdfColor.Rgb(0.4, 0.4, 0.85)).SetLineWidth(1)
        .Rectangle(boxX, boxBottom, boxWidth, boxTop - boxBottom).Stroke().Restore();
    var pt = c.AddText().SetFont("F2", psize).SetLeading(leading).SetTextMatrix(1, 0, 0, 1, boxX + pad, firstBaseline);
    for (int i = 0; i < lines.Count; i++)
    {
        if (i > 0) pt.NextLine();
        pt.ShowText(lines[i]);
    }
    pt.Build();

    doc.Save(path);
    Report(path);
}

// Spec gap-fill (book skipped these, deferring to ISO 32000): PDF functions,
// axial/radial shadings, the sh operator, and shading patterns via the Pattern
// colour space.
static void BuildShadings(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;
    c.AddText().SetFont("F1", 22).Show(60, 740, "Color Spaces & Shadings").Build();

    var rgb = new PdfSpec.Objects.PdfName("DeviceRGB");

    // 1) Axial (linear) two-colour gradient, painted with sh inside a clip.
    c.AddText().SetFont("F1", 11).Show(60, 705, "Axial gradient (sh, red -> blue)").Build();
    var axialFn = PdfFunction.Exponential(new double[] { 1, 0, 0 }, new double[] { 0, 0, 1 });
    page.AddShading("Sh1", doc.AddObject(Shading.Axial(rgb, 60, 560, 300, 560, axialFn)));
    c.Save().Rectangle(60, 560, 240, 120).Clip().EndPath().PaintShading("Sh1").Restore();

    // 2) Three-stop axial gradient via a stitching function.
    c.AddText().SetFont("F1", 11).Show(60, 540, "Axial gradient (3-stop stitching: red -> green -> blue)").Build();
    var f01 = PdfFunction.Exponential(new double[] { 1, 0, 0 }, new double[] { 0, 1, 0 });
    var f12 = PdfFunction.Exponential(new double[] { 0, 1, 0 }, new double[] { 0, 0, 1 });
    var stitch = PdfFunction.Stitching(new[] { f01, f12 }, new double[] { 0.5 }, new double[] { 0, 1, 0, 1 });
    page.AddShading("Sh2", doc.AddObject(Shading.Axial(rgb, 60, 440, 300, 440, stitch)));
    c.Save().Rectangle(60, 440, 240, 80).Clip().EndPath().PaintShading("Sh2").Restore();

    // 3) Radial gradient via a shading pattern (Pattern colour space + scn).
    c.AddText().SetFont("F1", 11).Show(360, 705, "Radial gradient (shading pattern)").Build();
    var radialFn = PdfFunction.Exponential(new double[] { 1, 1, 1 }, new double[] { 0.1, 0.2, 0.7 });
    var radial = Shading.Radial(rgb, 440, 600, 5, 440, 600, 75, radialFn);
    page.AddPattern("P1", doc.AddObject(Shading.Pattern(radial)));
    c.Save().SetFillColorSpace("Pattern").SetFillPattern("P1").Circle(440, 600, 75).Fill().Restore();

    // 4) Gradient-filled text: clip to the glyph outlines (Tr 7), then paint a shading.
    c.AddText().SetFont("F1", 11).Show(360, 470, "Gradient-filled text (text clip + sh)").Build();
    var textFn = PdfFunction.Exponential(new double[] { 0.8, 0, 0.4 }, new double[] { 0, 0.4, 0.9 });
    page.AddShading("Sh3", doc.AddObject(Shading.Axial(rgb, 360, 0, 560, 0, textFn)));
    c.Save();
    c.AddText().SetFont("F1", 54).SetTextRenderMode(TextRenderMode.Clip).SetTextMatrix(1, 0, 0, 1, 360, 400).ShowText("PDF").Build();
    c.PaintShading("Sh3").Restore();

    doc.Save(path);
    Report(path);
}

// Spec gap-fill: operators from Annex A not covered by the book — v/y Bézier
// variants, even-odd close-fill-stroke (b*), the " text operator, and an inline
// image (BI/ID/EI).
static void BuildOperators(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;
    c.AddText().SetFont("F1", 22).Show(60, 740, "Additional Operators").Build();

    // Nonzero (b) vs even-odd (b*) fill on a self-intersecting pentagram.
    c.AddText().SetFont("F1", 11).Show(60, 700, "Pentagram fill: nonzero (b) vs even-odd (b*)").Build();
    c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2);
    AppendPentagram(c, 140, 620, 55);
    c.CloseFillStroke().Restore();
    c.Save().SetRgbFill(PdfColor.Rgb(1, 0.75, 0)).SetRgbStroke(PdfColor.Rgb(0.6, 0.4, 0)).SetLineWidth(2);
    AppendPentagram(c, 300, 620, 55);
    c.CloseFillStrokeEvenOdd().Restore();

    // v / y Bézier curve variants forming a leaf.
    c.AddText().SetFont("F1", 11).Show(420, 700, "v / y Bézier curves").Build();
    c.Save().SetRgbFill(PdfColor.Rgb(0.2, 0.6, 0.9));
    c.MoveTo(440, 590).CurveToV(440, 660, 520, 660).CurveToY(520, 590, 440, 590).Fill().Restore();

    // The " operator: set word + char spacing, next line, then show.
    c.AddText().SetFont("F1", 14).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 540)
        .ShowText("The quote operator sets spacing and shows a line:")
        .NextLineShowText(wordSpacing: 6, charSpacing: 1, text: "spaced out via the quote operator")
        .Build();

    // Inline image (BI/ID/EI): a 4x4 RGB checker scaled up.
    c.AddText().SetFont("F1", 11).Show(60, 470, "Inline image (BI/ID/EI):").Build();
    c.DrawInlineImageRgb(MakeTinyChecker(), 4, 4, 60, 380, 80, 80);

    doc.Save(path);
    Report(path);
}

// Draw one legend row: a short colored swatch line and a black label, then move down.
static void LegendRow(PdfSpec.Content.ContentStream c, double x, ref double y,
    double r, double g, double b, string label)
{
    c.Save().SetRgbStroke(PdfColor.Rgb(r, g, b)).SetLineWidth(2).MoveTo(x, y + 3).LineTo(x + 16, y + 3).Stroke().Restore();
    c.AddText().SetFont("F1", 9).Show(x + 22, y, label).Build();
    y -= 13;
}

static void AppendPentagram(PdfSpec.Content.ContentStream c, double cx, double cy, double r)
{
    for (int i = 0; i < 5; i++)
    {
        int index = (i * 2) % 5; // connect every other vertex -> star polygon
        double a = -Math.PI / 2 + index * 2 * Math.PI / 5;
        double x = cx + r * Math.Cos(a), y = cy + r * Math.Sin(a);
        if (i == 0) c.MoveTo(x, y); else c.LineTo(x, y);
    }
    c.ClosePath();
}

static byte[] MakeTinyChecker()
{
    var rgb = new byte[4 * 4 * 3];
    int i = 0;
    for (int y = 0; y < 4; y++)
    {
        for (int x = 0; x < 4; x++)
        {
            bool on = ((x + y) & 1) == 0;
            rgb[i++] = on ? (byte)230 : (byte)40;
            rgb[i++] = on ? (byte)60 : (byte)120;
            rgb[i++] = on ? (byte)60 : (byte)200;
        }
    }
    return rgb;
}

// Chapter 13 "PDF Standards": the identification constructs a standards-aware
// writer adds — PDF/A identifiers in XMP, an OutputIntent, tagging, and metadata.
// (Full PDF/A conformance additionally needs embedded fonts and a real ICC
// profile, which are beyond the current scope.)
static void BuildPdfAStyle(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

    var tree = new StructureTreeBuilder(doc);
    var tagger = tree.TagPage(page);
    tagger.Begin("H1");
    tagger.Content.AddText().SetFont("F1", 22).Show(60, 740, "PDF Standards").Build();
    tagger.End();
    tagger.Begin("P");
    tagger.Content.AddText().SetFont("F1", 12).Show(60, 710, "PDF/A-style identification: XMP pdfaid, an OutputIntent, tagging, metadata.").Build();
    tagger.End();
    tagger.Finish();

    var created = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
    const string title = "PDF Standards Demo";
    const string author = "CSharpPdf";
    const string producer = "CSharpPdf (pure C#)";

    doc.SetDocumentInfo(title, author, "PDF/A-style output", "pdf/a, standards, conformance", "CSharpPdf", producer, created, created);
    doc.SetXmpMetadata(XmpMetadata.Build(title, author, "PDF/A-style output", "pdf/a, standards, conformance",
        "CSharpPdf", producer, created, created, pdfaPart: 3, pdfaConformance: "B"));
    doc.AddOutputIntent("GTS_PDFA1", "sRGB IEC61966-2.1", "sRGB IEC61966-2.1");

    doc.Save(path);
    Report(path);
}

// Chapter 12 "Metadata": set both the document information dictionary and an
// XMP metadata stream with consistent values.
static void BuildMetadata(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Document Metadata").Build()
        .AddText().SetFont("F1", 12).Show(60, 712, "Title/Author/Subject/Keywords in both the Info dict and XMP.").Build();

    var created = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
    const string title = "Developing with CSharpPdf";
    const string author = "Willem";
    const string subject = "A demonstration of PDF metadata";
    const string keywords = "pdf, metadata, xmp, csharp";
    const string creator = "CSharpPdf";
    const string producer = "CSharpPdf (pure C#)";

    doc.SetDocumentInfo(title, author, subject, keywords, creator, producer, created, created);
    doc.SetXmpMetadata(XmpMetadata.Build(title, author, subject, keywords, creator, producer, created, created));

    doc.Save(path);
    Report(path);
}

// Chapter 11 "Tagging and Structure": a tagged PDF with a structure tree
// (Document > H1, P, custom Chap via RoleMap, Figure), MCID-linked content, a
// ParentTree, and MarkInfo so the document reports as tagged.
static void BuildTaggedStructure(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    page.AddFont("F2", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

    const int w = 96, h = 96;
    page.AddXObject("Im1", PdfImage.Rgb(MakeGradient(w, h), w, h).EmbedIn(doc));

    var tree = new StructureTreeBuilder(doc);
    tree.MapRole("Chap", "Sect"); // custom type mapped to a standard one
    var tagger = tree.TagPage(page);

    tagger.Begin("H1");
    tagger.Content.AddText().SetFont("F1", 24).Show(60, 720, "Tagged PDF Demo").Build();
    tagger.End();

    tagger.Begin("P");
    tagger.Content.AddText().SetFont("F2", 13).Show(60, 690, "This paragraph is a tagged structure element (P).").Build();
    tagger.End();

    tagger.Begin("Chap");
    tagger.Content.AddText().SetFont("F2", 13).Show(60, 665, "A custom 'Chap' element, role-mapped to Sect.").Build();
    tagger.End();

    tagger.Begin("Figure");
    tagger.Content.DrawImage("Im1", 60, 520, 120, 120);
    tagger.End();

    tagger.Finish();

    doc.Save(path);
    Report(path);
}

// Chapter 10 "Optional Content": three layers (Red/Green/Blue) marked in the
// content stream via BDC /OC. Blue is OFF in the default configuration, so it is
// hidden until a user enables it; the others show.
static void BuildOptionalContent(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

    var redOcg = doc.AddOptionalContentGroup("Red layer");
    var greenOcg = doc.AddOptionalContentGroup("Green layer");
    var blueOcg = doc.AddOptionalContentGroup("Blue layer");
    page.AddProperty("OCR", redOcg);
    page.AddProperty("OCG", greenOcg);
    page.AddProperty("OCB", blueOcg);

    // Show the layer list in the viewer; start with Blue turned off.
    doc.OptionalContentConfig["Order"] = new PdfArray(redOcg, greenOcg, blueOcg);
    doc.OptionalContentConfig["OFF"] = new PdfArray(blueOcg);

    var c = page.Content;
    c.AddText().SetFont("F1", 22).Show(60, 740, "Optional Content (Layers)").Build();
    c.AddText().SetFont("F1", 12).Show(60, 712, "Red and Green are ON by default; Blue is OFF.").Build();

    c.BeginOptionalContent("OCR").SetRgbFill(PdfColor.Rgb(1, 0, 0)).Rectangle(80, 560, 160, 120).Fill().EndMarkedContent();
    c.BeginOptionalContent("OCG").SetRgbFill(PdfColor.Rgb(0, 0.7, 0)).Rectangle(180, 560, 160, 120).Fill().EndMarkedContent();
    c.BeginOptionalContent("OCB").SetRgbFill(PdfColor.Rgb(0, 0, 1)).Rectangle(280, 560, 160, 120).Fill().EndMarkedContent();

    doc.Save(path);
    Report(path);
}

// Chapter 10 advanced: OCMD visibility policies, radio-button layer groups
// (RBGroups), and the OC key on a form XObject and on an annotation.
static void BuildOptionalContentAdvanced(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    var font = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica));
    page.AddFont("F1", font);
    var c = page.Content;
    c.AddText().SetFont("F1", 22).Show(60, 740, "Optional Content — Advanced").Build();

    // Language layers as a radio group: only one visible at a time (English on).
    var en = doc.AddOptionalContentGroup("English");
    var fr = doc.AddOptionalContentGroup("French");
    page.AddProperty("OCen", en);
    page.AddProperty("OCfr", fr);
    c.BeginOptionalContent("OCen").AddText().SetFont("F1", 16).Show(60, 690, "Hello! (English layer)").Build().EndMarkedContent();
    c.BeginOptionalContent("OCfr").AddText().SetFont("F1", 16).Show(60, 690, "Bonjour! (French layer)").Build().EndMarkedContent();

    // OCMD with an AllOn policy: visible only when both detail groups are on.
    var detailA = doc.AddOptionalContentGroup("Detail A");
    var detailB = doc.AddOptionalContentGroup("Detail B");
    var ocmd = doc.AddObject(OptionalContent.Membership(new[] { detailA, detailB }, "AllOn"));
    page.AddProperty("OCMD1", ocmd);
    c.BeginOptionalContent("OCMD1").SetRgbFill(PdfColor.Rgb(0.9, 0.5, 0)).Rectangle(60, 620, 220, 36).Fill().EndMarkedContent();
    c.AddText().SetFont("F1", 10).Show(60, 606, "(orange bar shows only when Detail A AND Detail B are on)").Build();

    // DRAFT watermark: a form XObject carrying its own OC key (a toggleable layer).
    var watermark = doc.AddOptionalContentGroup("Watermark");
    page.AddProperty("OCwm", watermark);
    var wm = new FormXObject(doc, PdfRectangle.FromSize(380, 90));
    wm.AddResource("Font", "F1", font);
    wm.Content.SetRgbFill(PdfColor.Rgb(0.95, 0.6, 0.6)).AddText().SetFont("F1", 64)
        .SetTextMatrix(1, 0, 0, 1, 6, 12).ShowText("DRAFT").Build();
    var wmStream = wm.Build();
    wmStream.Dictionary["OC"] = watermark;
    page.AddXObject("WM", doc.AddObject(wmStream));
    c.Save().Translate(140, 320).Rotate(22).PaintXObject("WM").Restore();

    // An annotation whose whole visibility is governed by an OCG (the OC key).
    var noteLayer = doc.AddOptionalContentGroup("Note layer");
    page.AddProperty("OCnote", noteLayer);
    var square = Annotation.Square(new PdfRectangle(430, 560, 520, 650),
        new double[] { 0, 0, 1 }, new double[] { 0.8, 0.9, 1 }, 2);
    square["OC"] = noteLayer;
    page.AddAnnotation(square);

    // Show all layers in the panel; French off; English/French are a radio group.
    doc.OptionalContentConfig["Order"] = new PdfArray(en, fr, detailA, detailB, watermark, noteLayer);
    doc.OptionalContentConfig["OFF"] = new PdfArray(fr);
    doc.OptionalContentConfig["RBGroups"] = new PdfArray(new PdfArray(en, fr));

    doc.Save(path);
    Report(path);
}

// Chapter 9 "Multimedia" + "3D": a screen annotation driven by a rendition
// action (video), and a 3D annotation with a view and a poster appearance.
static void BuildMultimedia3D(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    var fontRef = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica));
    page.AddFont("F1", fontRef);
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Multimedia & 3D").Build();

    // Screen annotation playing a video via a rendition action.
    page.Content.AddText().SetFont("F1", 12).Show(60, 700, "Screen + rendition (video region):").Build();
    var screenRect = new PdfRectangle(60, 540, 380, 680);
    page.Content.Save().SetRgbStroke(PdfColor.Rgb(0, 0, 1)).SetLineWidth(1).Rectangle(60, 540, 320, 140).Stroke().Restore();
    var screen = Media.ScreenAnnotation(screenRect, "A Movie", new double[] { 0, 0, 1 });
    var screenRef = page.AddAnnotation(screen);
    var rendition = doc.AddObject(Media.MediaRendition("video/mp4", "https://example.com/clip.mp4"));
    screen["A"] = PdfAction.Rendition(screenRef, rendition);

    // 3D annotation with a default view and a poster (fallback) appearance.
    page.Content.AddText().SetFont("F1", 12).Show(60, 470, "3D annotation (with poster fallback):").Build();
    var view = doc.AddObject(Media.ThreeDView("Default",
        new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, -200 }));
    var threeD = doc.AddObject(Media.ThreeDStream(new byte[512], "U3D", new PdfArray(view), 0));

    var poster = new FormXObject(doc, PdfRectangle.FromSize(320, 200));
    poster.AddResource("Font", "F1", fontRef);
    poster.Content.SetRgbFill(PdfColor.Rgb(0.90, 0.90, 0.96)).Rectangle(0, 0, 320, 200).Fill();
    poster.Content.SetRgbStroke(PdfColor.Rgb(0.4, 0.4, 0.4)).SetLineWidth(1).Rectangle(0.5, 0.5, 319, 199).Stroke();
    poster.Content.SetRgbFill(PdfColor.Rgb(0.2, 0.2, 0.2)).AddText().SetFont("F1", 16).Show(80, 95, "3D model (poster)").Build();
    var posterRef = doc.AddObject(poster.Build());

    page.AddAnnotation(Media.ThreeDAnnotation(
        new PdfRectangle(60, 250, 380, 450), threeD, posterRef, "A 3D Model"));

    doc.Save(path);
    Report(path);
}

// Chapter 9 "Simple Media": a sound annotation (with an embedded Sound stream),
// a movie annotation referencing an external file, and a Sound action button.
static void BuildSimpleMedia(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Simple Media — Sound & Movie").Build()
        .AddText().SetFont("F1", 12).Show(90, 690, "Speaker icon: sound annotation").Build();

    // A sound annotation backed by a (placeholder) embedded Sound stream.
    var soundRef = doc.AddObject(Media.SoundStream(new byte[256], sampleRate: 11025));
    page.AddAnnotation(Media.SoundAnnotation(
        new PdfRectangle(62, 686, 82, 706), soundRef, "A short beep", "Speaker"));

    // A movie annotation referencing an external movie file.
    page.AddAnnotation(Media.MovieAnnotation(
        new PdfRectangle(60, 540, 360, 660), "SampleMovie.mov", new double[] { 308, 210 }, title: "Sample movie"));

    // A button that triggers the equivalent Sound action.
    LinkButton(page, 60, 500, 200, 28, "Play sound (action)", PdfAction.PlaySound(soundRef));

    doc.Save(path);
    Report(path);
}

// Chapter 8 "Collections": a portfolio of embedded files with a schema (Title/
// Year/Minutes), per-file collection items, and a default sort by year.
static void BuildCollection(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Document Collection (Portfolio)").Build()
        .AddText().SetFont("F1", 12).Show(60, 712, "Open in a portfolio-aware viewer to browse the embedded files.").Build();

    (string File, string Title, int Year, int Minutes)[] movies =
    {
        ("eyes-wide-shut.txt", "Eyes Wide Shut", 1999, 159),
        ("the-shining.txt", "The Shining", 1980, 146),
        ("2001.txt", "2001: A Space Odyssey", 1968, 149),
    };
    foreach (var m in movies)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes($"{m.Title} ({m.Year}) — {m.Minutes} min\n");
        var streamRef = doc.AddObject(EmbeddedFile.Stream(data, "text/plain"));
        var spec = EmbeddedFile.FileSpec(m.File, streamRef, m.Title);
        spec["CI"] = new PdfDictionary
        {
            ["Type"] = new PdfName("CollectionItem"),
            ["TITLE"] = new PdfString(m.Title),
            ["YEAR"] = new PdfNumber(m.Year),
            ["DURATION"] = new PdfNumber(m.Minutes),
        };
        doc.RegisterEmbeddedFile(m.File, doc.AddObject(spec));
    }

    var schema = new PdfDictionary
    {
        ["Type"] = new PdfName("CollectionSchema"),
        ["TITLE"] = EmbeddedFile.CollectionField("S", "Title", 0),
        ["YEAR"] = EmbeddedFile.CollectionField("N", "Year", 1),
        ["DURATION"] = EmbeddedFile.CollectionField("N", "Minutes", 2),
    };
    doc.SetCollection(new PdfDictionary
    {
        ["Type"] = new PdfName("Collection"),
        ["View"] = new PdfName("D"),
        ["Schema"] = schema,
        ["Sort"] = new PdfDictionary
        {
            ["Type"] = new PdfName("CollectionSort"),
            ["S"] = new PdfName("YEAR"),
            ["A"] = new PdfBoolean(false), // descending
        },
    });

    doc.Save(path);
    Report(path);
}

// Chapter 8 "GoToE Actions": embed a whole PDF and link to it with an embedded
// go-to action that opens the target's first page.
static void BuildGoToEmbedded(string path)
{
    // Build a small target PDF entirely in memory.
    byte[] targetBytes;
    {
        var target = new PdfDoc();
        var tp = target.AddPage(PageSizes.Letter);
        tp.AddFont("F1", target.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
        tp.Content.AddText().SetFont("F1", 24).Show(72, 700, "I am the embedded target PDF!").Build();
        using var ms = new MemoryStream();
        target.Save(ms);
        targetBytes = ms.ToArray();
    }

    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "GoToE — Embedded Go-To").Build();

    doc.AddEmbeddedFile("target.pdf", "target.pdf", targetBytes, "application/pdf", "Embedded target PDF");
    LinkButton(page, 60, 690, 280, 28, "Open embedded target.pdf", PdfAction.GoToEmbedded("target.pdf"));

    doc.Save(path);
    Report(path);
}

// Chapter 7 "AcroForms" (choice and radio fields): a combo box, an editable
// combo box, a scrollable list box, and a radio button group.
static void BuildFormChoices(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var labels = page.Content;
    labels.AddText().SetFont("F1", 22).Show(60, 740, "Interactive Form — Choices").Build();

    var form = new FormBuilder(doc);

    labels.AddText().SetFont("F1", 12).Show(60, 702, "State (combo):").Build();
    form.ComboBox(page, "State", new PdfRectangle(180, 696, 380, 718),
        new[] { "Alabama", "Alaska", "Arizona", "California", "Colorado" }, "California");

    labels.AddText().SetFont("F1", 12).Show(60, 662, "Country (editable):").Build();
    form.ComboBox(page, "Country", new PdfRectangle(180, 656, 380, 678),
        new[] { "France", "Belgium", "Germany", "Spain" }, "Slovakia", editable: true);

    labels.AddText().SetFont("F1", 12).Show(60, 622, "Fruit (list):").Build();
    form.ListBox(page, "Fruit", new PdfRectangle(180, 540, 380, 632),
        new[] { "Orange", "Apple", "Banana", "Pear", "Melon", "Grape" }, selectedIndex: 2);

    labels.AddText().SetFont("F1", 12).Show(60, 500, "Shipping:").Build();
    form.RadioGroup(page, "Shipping", new[]
    {
        ("standard", new PdfRectangle(180, 496, 198, 514)),
        ("express", new PdfRectangle(300, 496, 318, 514)),
        ("overnight", new PdfRectangle(430, 496, 448, 514)),
    }, selected: "express");
    labels.AddText().SetFont("F1", 11).Show(204, 500, "Standard").Build().AddText().SetFont("F1", 11).Show(324, 500, "Express").Build()
        .AddText().SetFont("F1", 11).Show(454, 500, "Overnight").Build();

    doc.Save(path);
    Report(path);
}

// Chapter 7 "AcroForms" (text/button fields): an interactive form with single-
// and multi-line text fields, checkboxes, and a push button bound to ResetForm.
static void BuildFormBasics(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var labels = page.Content;
    labels.AddText().SetFont("F1", 22).Show(60, 740, "Interactive Form — Fields").Build();

    var form = new FormBuilder(doc);

    labels.AddText().SetFont("F1", 12).Show(60, 702, "Full name:").Build();
    form.TextField(page, "FullName", new PdfRectangle(150, 696, 430, 718), "Ada Lovelace");

    labels.AddText().SetFont("F1", 12).Show(60, 662, "Comments:").Build();
    form.TextField(page, "Comments", new PdfRectangle(150, 600, 430, 678),
        "Multi-line text field.\nType across several lines.", multiline: true);

    labels.AddText().SetFont("F1", 12).Show(90, 556, "Subscribe to newsletter").Build();
    form.CheckBox(page, "Subscribe", new PdfRectangle(60, 552, 80, 572), isChecked: true);

    labels.AddText().SetFont("F1", 12).Show(90, 526, "Accept terms").Build();
    form.CheckBox(page, "AcceptTerms", new PdfRectangle(60, 522, 80, 542), isChecked: false);

    form.PushButton(page, "ResetBtn", new PdfRectangle(60, 470, 150, 496), "Reset form", PdfAction.ResetForm());

    doc.Save(path);
    Report(path);
}

// Chapter 6 "Stamps Markup" + "Text Annotations and Pop-ups": a Stamp annotation
// whose appearance is a form XObject, and sticky-note Text annotations each
// cross-linked to a Pop-up holding their text.
static void BuildStampAndNotes(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Stamp and Note Annotations").Build();

    // Build the stamp's appearance as a form XObject (red "APPROVED" badge).
    var stampFont = doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold));
    var stamp = new FormXObject(doc, PdfRectangle.FromSize(200, 70));
    stamp.AddResource("Font", "SF", stampFont);
    stamp.Content.SetRgbStroke(PdfColor.Rgb(0.8, 0, 0)).SetRgbFill(PdfColor.Rgb(0.8, 0, 0)).SetLineWidth(4)
        .Rectangle(4, 4, 192, 62).Stroke()
        .AddText().SetFont("SF", 32).Show(24, 24, "APPROVED").Build();
    var stampRef = doc.AddObject(stamp.Build());
    page.AddAnnotation(Annotation.Stamp(new PdfRectangle(80, 600, 280, 670), stampRef, 0.85));

    // Sticky notes with pop-ups (different icons).
    page.AddTextNote(new PdfRectangle(90, 540, 110, 560),
        "This is a Comment sticky note with an open pop-up.", "Comment",
        new PdfRectangle(120, 470, 340, 560), open: true);
    page.AddTextNote(new PdfRectangle(90, 430, 110, 450),
        "A Help note, shown closed by default.", "Help",
        new PdfRectangle(120, 360, 340, 450), open: false);

    doc.Save(path);
    Report(path);
}

// Chapter 6 "Markup Annotations": text markup (highlight/underline/strikeout/
// squiggly) over drawn words, plus drawing markup (square, circle, line with an
// arrowhead, polygon, polyline, and freehand ink).
static void BuildMarkupAnnotations(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;

    c.AddText().SetFont("F1", 22).Show(60, 740, "Annotations").Build();

    // Text markup over four drawn words.
    c.AddText().SetFont("F1", 18).Show(60, 700, "Highlight   Underline   StrikeOut   Squiggly").Build();
    page.AddAnnotation(Annotation.Highlight(new PdfRectangle(58, 697, 150, 718), new double[] { 1, 1, 0 }));
    page.AddAnnotation(Annotation.Underline(new PdfRectangle(157, 697, 250, 718), new double[] { 0, 0.6, 0 }));
    page.AddAnnotation(Annotation.StrikeOut(new PdfRectangle(258, 697, 345, 718), new double[] { 0.9, 0, 0 }));
    page.AddAnnotation(Annotation.Squiggly(new PdfRectangle(352, 697, 440, 718), new double[] { 0, 0, 0.9 }));

    // Square and circle (stroked + filled).
    page.AddAnnotation(Annotation.Square(new PdfRectangle(60, 560, 150, 640), new double[] { 0.9, 0, 0 }, null, 2));
    page.AddAnnotation(Annotation.Circle(new PdfRectangle(170, 560, 260, 640), new double[] { 0, 0.7, 0 }, new double[] { 0.85 }, 3));

    // Line with an open arrowhead at the end.
    page.AddAnnotation(Annotation.Line(290, 640, 430, 565, new double[] { 0, 0, 0.9 }, 3, endStyle: "OpenArrow"));

    // Polygon (triangle, green stroke / yellow fill) and an open polyline (red).
    page.AddAnnotation(Annotation.Polygon(
        new double[] { 60, 440, 150, 520, 30, 520 }, new double[] { 0, 0.6, 0 }, new double[] { 1, 1, 0 }, 3));
    page.AddAnnotation(Annotation.PolyLine(
        new double[] { 200, 440, 240, 520, 280, 450, 320, 520 }, new double[] { 0.9, 0, 0 }, null, 3));

    // Freehand ink: a curved squiggle expressed as a polyline of points.
    var ink = new List<double>();
    for (int i = 0; i <= 40; i++)
    {
        double x = 360 + i * 4;
        double y = 480 + 40 * Math.Sin(i * 0.5);
        ink.Add(x); ink.Add(y);
    }
    page.AddAnnotation(Annotation.Ink(new[] { ink.ToArray() }, new double[] { 0.8, 0, 0.8 }, 3));

    doc.Save(path);
    Report(path);
}

// Chapter 5 "Navigation": destinations, actions, link annotations, named
// destinations, and an OpenAction across a 3-page document.
static void BuildNavigation(string path)
{
    var doc = new PdfDoc();
    var p1 = doc.AddPage(PageSizes.Letter);
    var p2 = doc.AddPage(PageSizes.Letter);
    var p3 = doc.AddPage(PageSizes.Letter);
    foreach (var p in new[] { p1, p2, p3 })
    {
        p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    }

    p1.Content.AddText().SetFont("F1", 24).Show(60, 740, "Navigation — Page 1").Build();
    LinkButton(p1, 60, 680, 240, 28, "GoTo page 3 (Fit)", PdfAction.GoTo(PdfDestination.Fit(p3.Reference)));
    LinkButton(p1, 60, 640, 240, 28, "Named destination: chapter-3", PdfAction.GoToNamed("chapter-3"));
    LinkButton(p1, 60, 600, 240, 28, "Open oreilly.com (URI)", PdfAction.Uri("https://www.oreilly.com"));
    LinkButton(p1, 60, 560, 240, 28, "Open Chapter2.pdf (GoToR)", PdfAction.GoToRemote("Chapter2.pdf", 0));

    p2.Content.AddText().SetFont("F1", 24).Show(60, 740, "Navigation — Page 2").Build();
    LinkButton(p2, 60, 680, 240, 28, "Back to page 1 top (XYZ)",
        PdfAction.GoTo(PdfDestination.XYZ(p1.Reference, 0, 792, null)));

    p3.Content.AddText().SetFont("F1", 24).Show(60, 740, "Navigation — Page 3 (target)").Build();
    LinkButton(p3, 60, 680, 240, 28, "Back to page 1 (Fit)", PdfAction.GoTo(PdfDestination.Fit(p1.Reference)));

    // A named destination pointing at page 3, plus an OpenAction.
    doc.AddNamedDestination("chapter-3", PdfDestination.Fit(p3.Reference));
    doc.SetOpenAction(PdfAction.GoTo(PdfDestination.Fit(p1.Reference)));

    doc.Save(path);
    Report(path);
}

// Chapter 5 "Bookmarks or Outlines": a bookmark hierarchy with an open branch
// (Document) containing a closed sub-branch (Section 2 > Subsection 1), plus a
// top-level Summary — mirroring the book's five-visible-items example.
static void BuildOutline(string path)
{
    var doc = new PdfDoc();
    doc.SetPageMode("UseOutlines");

    var page1 = doc.AddPage(PageSizes.Letter);
    var page2 = doc.AddPage(PageSizes.Letter);
    var page3 = doc.AddPage(PageSizes.Letter);
    foreach (var p in new[] { page1, page2, page3 })
    {
        p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    }
    page1.Content.AddText().SetFont("F1", 22).Show(60, 760, "Document").Build().AddText().SetFont("F1", 16).Show(60, 701, "Section 1").Build()
        .AddText().SetFont("F1", 16).Show(60, 600, "Section 2").Build().AddText().SetFont("F1", 14).Show(80, 560, "Subsection 1").Build();
    page2.Content.AddText().SetFont("F1", 16).Show(60, 500, "Section 3").Build();
    page3.Content.AddText().SetFont("F1", 22).Show(60, 700, "Summary").Build();

    var document = new PdfOutlineItem("Document", PdfDestination.XYZ(page1.Reference, 0, 792, null));
    document.AddChild("Section 1", PdfDestination.XYZ(page1.Reference, null, 701, null));
    var section2 = document.AddChild("Section 2", PdfDestination.XYZ(page1.Reference, null, 600, null));
    section2.Open = false; // collapsed -> negative Count, child hidden
    section2.AddChild("Subsection 1", PdfDestination.XYZ(page1.Reference, null, 560, null));
    document.AddChild("Section 3", PdfDestination.XYZ(page2.Reference, null, 500, null));
    var summary = new PdfOutlineItem("Summary", PdfDestination.XYZ(page3.Reference, null, 700, null));

    doc.SetOutline(new[] { document, summary });

    doc.Save(path);
    Report(path);
}

// Draw a labeled, bordered button and bind a Link annotation over it.
static void LinkButton(PdfPage page, double x, double y, double w, double h, string label, PdfDictionary action)
{
    var c = page.Content;
    c.Save().SetRgbStroke(PdfColor.Rgb(0.2, 0.3, 0.7)).SetRgbFill(PdfColor.Rgb(0.90, 0.93, 1.0)).SetLineWidth(1)
        .Rectangle(x, y, w, h).FillStroke().Restore();
    c.Save().SetRgbFill(PdfColor.Rgb(0.1, 0.2, 0.6)).AddText().SetFont("F1", 12).Show(x + 10, y + h / 2 - 4, label).Build().Restore();
    page.AddLinkAnnotation(new PdfRectangle(x, y, x + w, y + h), action);
}

// Chapter 4 "The Font Dictionary": the same phrase set in several of the
// Standard 14 fonts, showing different font programs and the symbol fonts.
static void BuildTextFonts(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);

    (string resource, string baseFont, string sample)[] rows =
    {
        ("F1", StandardFonts.Helvetica, "Helvetica: Pack my box."),
        ("F2", StandardFonts.HelveticaBoldOblique, "Helvetica-BoldOblique"),
        ("F3", StandardFonts.TimesRoman, "Times-Roman: Pack my box."),
        ("F4", StandardFonts.TimesItalic, "Times-Italic: Pack my box."),
        ("F5", StandardFonts.CourierBold, "Courier-Bold: Pack my box."),
        ("F6", StandardFonts.Symbol, "abcdefghijklmnop"),
        ("F7", StandardFonts.ZapfDingbats, "abcdefghijklmnop"),
    };

    var c = page.Content;
    double y = 720;
    foreach (var (resource, baseFont, sample) in rows)
    {
        page.AddFont(resource, doc.AddObject(StandardFonts.Create(baseFont)));
        c.AddText().SetFont(resource, 22).Show(60, y, sample).Build();
        y -= 50;
    }

    doc.Save(path);
    Report(path);
}

// Chapter 4 "Text State", "Rendering Mode", "Drawing Text", "Positioning Text":
// rendering modes, character/word spacing, horizontal scaling, text rise,
// leading with T*, manual TJ kerning, and WinAnsiEncoding for accented text.
static void BuildTextState(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.AddFont("FB", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    page.AddFont("FW", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica, StandardFonts.WinAnsiEncoding)));
    var c = page.Content;

    // Rendering modes: fill, stroke, fill+stroke.
    c.AddText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 730)
        .SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).SetTextRenderMode(TextRenderMode.Fill).ShowText("Fill mode (Tr 0)").Build();
    c.AddText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 690)
        .SetRgbStroke(PdfColor.Rgb(0.1, 0.1, 0.8)).SetLineWidth(0.7).SetTextRenderMode(TextRenderMode.Stroke).ShowText("Stroke mode (Tr 1)").Build();
    c.AddText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 650)
        .SetRgbFill(PdfColor.Rgb(1, 0.8, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetTextRenderMode(TextRenderMode.FillStroke).ShowText("Fill + Stroke (Tr 2)").Build();

    // Back to plain black fill for the rest. The render mode is BT/ET-only so it
    // resets to Fill at the start of each subsequent text block automatically.
    c.SetRgbFill(PdfColor.Rgb(0, 0, 0));

    // Character spacing, word spacing, horizontal scaling.
    c.AddText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 600)
        .SetCharSpacing(0).SetWordSpacing(0).SetHorizontalScaling(100).ShowText("Normal: the quick brown fox").Build();
    c.AddText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 576)
        .SetCharSpacing(3).ShowText("Char spacing Tc 3: the quick brown fox").Build();
    c.AddText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 552)
        .SetCharSpacing(0).SetWordSpacing(8).ShowText("Word spacing Tw 8: the quick brown fox").Build();
    c.AddText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 528)
        .SetWordSpacing(0).SetHorizontalScaling(160).ShowText("Horizontal scaling Tz 160").Build();
    // Horizontal scaling is BT/ET-only and resets per text block, so no reset needed here.

    // Text rise for sub/superscripts (within one text object, pen auto-advances).
    c.AddText().SetFont("F1", 18).SetTextMatrix(1, 0, 0, 1, 60, 488)
        .ShowText("Rise: H").SetTextRise(-4).SetFont("F1", 12).ShowText("2")
        .SetTextRise(0).SetFont("F1", 18).ShowText("O,  E = mc").SetTextRise(7).SetFont("F1", 12).ShowText("2")
        .SetTextRise(0).Build();

    // Leading + T* for multiple lines.
    c.AddText().SetFont("F1", 15).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 448)
        .ShowText("Leading + T*: line one").NextLine().ShowText("line two").NextLine().ShowText("line three").Build();

    // Manual kerning: plain Tj vs TJ with adjustments.
    c.AddText().SetFont("FB", 38).SetTextMatrix(1, 0, 0, 1, 60, 350).ShowText("AWAY  (plain Tj)").Build();
    c.AddText().SetFont("FB", 38).SetTextMatrix(1, 0, 0, 1, 60, 300)
        .ShowTextWithKerning("A", 120, "W", 120, "A", 95, "Y", "  (kerned TJ)").Build();

    // WinAnsiEncoding: accented Latin-1 characters.
    c.AddText().SetFont("FW", 18).SetTextMatrix(1, 0, 0, 1, 60, 250)
        .ShowText("WinAnsi: Français, Español, Düsseldorf, café, naïve").Build();

    doc.Save(path);
    Report(path);
}

// Chapter 3 "Vector Images": a reusable form XObject (a gold star) defined once
// and painted many times with different transforms, demonstrating that vector
// content can be reused without duplicating its description.
static void BuildFormXObject(string path)
{
    var doc = new PdfDoc();
    var page = doc.AddPage(PageSizes.Letter);

    // Define the star once inside a 100x100 bounding box.
    var star = new FormXObject(doc, PdfRectangle.FromSize(100, 100));
    star.Content.SetRgbFill(PdfColor.Rgb(1, 0.78, 0)).SetRgbStroke(PdfColor.Rgb(0.5, 0.35, 0)).SetLineWidth(3);
    AppendStar(star.Content, 50, 50, 45, 18);
    star.Content.CloseFillStroke();
    page.AddXObject("Star", doc.AddObject(star.Build()));

    var c = page.Content;
    // Full size, half size, rotated, and a row of small stamps — all one resource.
    c.Save().Translate(70, 600).PaintXObject("Star").Restore();
    c.Save().Translate(250, 640).Scale(0.6, 0.6).PaintXObject("Star").Restore();
    c.Save().Translate(420, 650).Rotate(20).Scale(0.8, 0.8).PaintXObject("Star").Restore();
    for (int i = 0; i < 5; i++)
    {
        c.Save().Translate(70 + i * 90, 430).Scale(0.45, 0.45).PaintXObject("Star").Restore();
    }

    doc.Save(path);
    Report(path);
}

// Append a five-pointed star subpath centered at (cx, cy).
static void AppendStar(PdfSpec.Content.ContentStream c, double cx, double cy, double outer, double inner)
{
    for (int i = 0; i < 10; i++)
    {
        double r = (i % 2 == 0) ? outer : inner;
        double angle = -Math.PI / 2 + i * Math.PI / 5;
        double x = cx + r * Math.Cos(angle);
        double y = cy + r * Math.Sin(angle);
        if (i == 0) c.MoveTo(x, y); else c.LineTo(x, y);
    }
    c.ClosePath();
}

static byte[] MakeSolid(int w, int h, byte r, byte g, byte b)
{
    var rgb = new byte[w * h * 3];
    for (int i = 0; i < w * h; i++)
    {
        rgb[i * 3] = r; rgb[i * 3 + 1] = g; rgb[i * 3 + 2] = b;
    }
    return rgb;
}

// 8-bit alpha: opaque at the center, fading linearly to transparent past a radius.
static byte[] MakeRadialAlpha(int w, int h)
{
    var a = new byte[w * h];
    double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0, max = Math.Min(cx, cy);
    int i = 0;
    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            double t = 1.0 - d / max;
            a[i++] = (byte)Math.Clamp(t * 255.0, 0, 255);
        }
    }
    return a;
}

// RGB: white background with a centered solid blue disc.
static byte[] MakeDiscOnWhite(int w, int h)
{
    var rgb = new byte[w * h * 3];
    double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0, r = Math.Min(cx, cy) * 0.8;
    int i = 0;
    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            bool inside = (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
            rgb[i++] = inside ? (byte)30 : (byte)255;
            rgb[i++] = inside ? (byte)90 : (byte)255;
            rgb[i++] = inside ? (byte)220 : (byte)255;
        }
    }
    return rgb;
}

// 1-bit packed stencil (MSB first, rows byte-padded): 0 paints, 1 leaves alone.
static byte[] MakeCheckerBits(int w, int h)
{
    int rowBytes = (w + 7) / 8;
    var bits = new byte[rowBytes * h];
    for (int y = 0; y < h; y++)
    {
        for (int x = 0; x < w; x++)
        {
            bool paint = ((x / 16) + (y / 16)) % 2 == 0;
            if (!paint)
            {
                bits[y * rowBytes + (x >> 3)] |= (byte)(0x80 >> (x & 7));
            }
        }
    }
    return bits;
}

// Procedural 24-bit RGB: a smooth red(x)/green(y) gradient with a blue diagonal
// band, so the rendered output is unmistakably a raster image.
static byte[] MakeGradient(int width, int height)
{
    var rgb = new byte[width * height * 3];
    int i = 0;
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            rgb[i++] = (byte)(x * 255 / (width - 1));
            rgb[i++] = (byte)(y * 255 / (height - 1));
            rgb[i++] = (byte)(Math.Abs(x - y) < 12 ? 255 : 40);
        }
    }
    return rgb;
}

// Helper: register a Helvetica font on the page and draw a line of text at (x, y).
static void AddTextLabel(PdfDoc doc, PdfPage page, double x, double y, double size, string text)
{
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.AddText().SetFont("F1", size).Show(x, y, text).Build();
}

static void Report(string path) => Console.WriteLine($"  {Path.GetFileName(path)}");

// Run a sample with a watchdog. If it doesn't return within `seconds`, print the
// last LayoutTrace breadcrumb and terminate the process so the dev gets a precise
// pointer to where the layout got stuck instead of waiting forever.
static void RunWithTimeout(string name, Action action, double seconds)
{
    CSharpPdf.LayoutTrace.Reset($"start {name}");
    var task = System.Threading.Tasks.Task.Run(action);
    if (!task.Wait(System.TimeSpan.FromSeconds(seconds)))
    {
        Console.Error.WriteLine($"[HANG] {name}: no progress in {seconds}s — {CSharpPdf.LayoutTrace.Ticks} marks total. Last 30:\n{CSharpPdf.LayoutTrace.Tail()}");
        System.Environment.Exit(2);
    }
    if (task.IsFaulted)
    {
        Console.Error.WriteLine($"[ERR] {name}: {task.Exception?.GetBaseException()}");
    }
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CSharpPdf.slnx")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
