using CSharpPdf;
using CSharpPdf.Annotations;
using CSharpPdf.ColorSpaces;
using CSharpPdf.Content;
using CSharpPdf.Files;
using CSharpPdf.Forms;
using CSharpPdf.Geometry;
using CSharpPdf.Images;
using CSharpPdf.Layers;
using CSharpPdf.Layout;
using CSharpPdf.Metadata;
using CSharpPdf.Multimedia;
using CSharpPdf.Navigation;
using CSharpPdf.Objects;
using CSharpPdf.Tagging;
using CSharpPdf.Text;

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
RunWithTimeout("35", () => BuildShowcase35(Path.Combine(samplesDir, "35-showcase-rows.pdf")), 2.0);
RunWithTimeout("36", () => BuildShowcase36(Path.Combine(samplesDir, "36-showcase-rows-cols.pdf")), 2.0);
RunWithTimeout("37", () => BuildShowcase37(Path.Combine(samplesDir, "37-showcase-extends.pdf")), 2.0);

Console.WriteLine($"Wrote samples to {samplesDir}");

// A minimal valid PDF: catalog -> page tree -> a single blank US Letter page.
static void BuildBlankPage(string path)
{
    var doc = new PdfDocument();
    doc.AddPage(PageSizes.Letter);
    doc.Save(path);
    Report(path);
}

// A single page that draws "Hello, World!" using the standard Helvetica font.
static void BuildHelloWorld(string path)
{
    var doc = new PdfDocument();
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
    var doc = new PdfDocument();

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
    var doc = new PdfDocument();

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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    var c = page.Content;

    // Painter's model: later shapes paint over earlier ones (top-left).
    c.SetRgbFill(1, 0, 0).Rectangle(60, 660, 110, 90).Fill();
    c.SetRgbFill(0, 1, 0).Rectangle(110, 635, 110, 90).Fill();
    c.SetRgbFill(0, 0, 1).Rectangle(160, 610, 110, 90).Fill();

    // A Bézier circle, both filled (orange) and stroked (dark blue, dashed).
    c.Save()
        .SetRgbFill(1, 0.6, 0).SetRgbStroke(0, 0, 0.5).SetLineWidth(2).SetDash(new double[] { 5, 2 })
        .Circle(470, 690, 55).FillStroke()
        .Restore();

    // The three device color spaces, as thick strokes.
    c.Save().SetLineWidth(10);
    c.SetGrayStroke(0.5).MoveTo(60, 560).LineTo(260, 560).Stroke();   // DeviceGray
    c.SetRgbStroke(1, 0, 0).MoveTo(60, 530).LineTo(260, 530).Stroke(); // DeviceRGB
    c.SetCmykStroke(1, 0, 0, 0).MoveTo(60, 500).LineTo(260, 500).Stroke(); // DeviceCMYK (cyan)
    c.Restore();

    // Line caps and joins on a zigzag (round) vs a closed shape (bevel).
    c.Save().SetRgbStroke(0, 0.6, 0).SetLineWidth(10).SetLineCap(1).SetLineJoin(1);
    c.MoveTo(330, 560).LineTo(380, 530).LineTo(430, 560).LineTo(480, 530).LineTo(530, 560).Stroke();
    c.Restore();

    // Transforms: a 50% scaled square, a translated square, and a rotated square.
    c.Save().Translate(60, 360).Scale(0.5, 0.5).SetRgbFill(0.8, 0, 0).Rectangle(0, 0, 100, 100).Fill().Restore();
    c.Save().Translate(180, 360).SetRgbFill(0, 0.7, 0).Rectangle(0, 0, 100, 100).Fill().Restore();
    c.Save().Translate(360, 410).Rotate(45).SetRgbFill(0, 0, 0.8).Rectangle(-50, -50, 100, 100).Fill().Restore();

    // Clipping: two rectangles clipped to a circular region (bottom).
    c.Save();
    c.Circle(200, 180, 90).Clip().EndPath();
    c.SetRgbFill(1, 0, 0).Rectangle(110, 90, 90, 180).Fill();
    c.SetRgbFill(0, 0, 1).Rectangle(200, 90, 90, 180).Fill();
    c.Restore();

    doc.Save(path);
    Report(path);
}

// Chapter 2 "Basic Transparency" + "Marked Content": named ExtGState resources
// carrying fill/stroke alpha (ca/CA), and content bracketed by marked-content
// operators (BMC/EMC and a BDC with an inline property list).
static void BuildTransparency(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);

    // ExtGState resources holding constant alpha for fill (ca) and stroke (CA).
    page.AddExtGState("GSopaque", new PdfDictionary { ["ca"] = new PdfNumber(1.0), ["CA"] = new PdfNumber(1.0) });
    page.AddExtGState("GShalf", new PdfDictionary { ["ca"] = new PdfNumber(0.5), ["CA"] = new PdfNumber(0.5) });

    var c = page.Content;

    // Three overlapping rectangles: opaque red, then 50%-alpha green and blue
    // that blend with whatever lies beneath them.
    c.Save().SetExtGState("GSopaque").SetRgbFill(1, 0, 0).Rectangle(150, 520, 170, 170).Fill().Restore();
    c.Save().SetExtGState("GShalf").SetRgbFill(0, 1, 0).Rectangle(230, 460, 170, 170).Fill().Restore();
    c.Save().SetExtGState("GShalf").SetRgbFill(0, 0, 1).Rectangle(310, 400, 170, 170).Fill().Restore();

    // Marked content: a bracketed sequence with a plain tag (BMC/EMC).
    c.BeginMarkedContent("Demo");
    c.Save().SetRgbFill(0.4, 0.4, 0.4).Rectangle(150, 250, 120, 90).Fill().Restore();
    c.EndMarkedContent();

    // Marked content with an inline property list (BDC/EMC).
    var props = new PdfDictionary { ["Label"] = new PdfString("Translucent overlay"), ["Index"] = new PdfNumber(1) };
    c.BeginMarkedContent("Demo", props);
    c.Save().SetExtGState("GShalf").SetRgbFill(1, 0.5, 0).Rectangle(310, 250, 120, 90).Fill().Restore();
    c.EndMarkedContent();

    doc.Save(path);
    Report(path);
}

// Chapter 3 "Raster Images": a procedurally generated DeviceRGB image embedded
// as an Image XObject (Flate-compressed) and painted at two different sizes,
// showing that one resource can be reused with different transforms.
static void BuildRasterImage(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);

    const int w = 128, h = 128;
    var image = PdfImage.Rgb(MakeGradient(w, h), w, h);
    var imageRef = doc.AddObject(image);
    page.AddXObject("Im1", imageRef);

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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    var c = page.Content;
    const int w = 128, h = 128;

    // 1) Soft mask: a solid image with a radial alpha mask fades out at the edges.
    var soft = PdfImage.Rgb(MakeSolid(w, h, 220, 30, 140), w, h);
    var softAlpha = doc.AddObject(PdfImage.SoftMask(MakeRadialAlpha(w, h), w, h));
    soft.Dictionary["SMask"] = softAlpha;
    page.AddXObject("ImSoft", doc.AddObject(soft));
    c.Save().SetRgbFill(1, 0.95, 0.4).Rectangle(60, 560, 200, 160).Fill().Restore(); // yellow bg
    c.DrawImage("ImSoft", 60, 560, 200, 160);

    // 2) Color-key mask: white pixels are dropped, leaving only the blue disc.
    var keyed = PdfImage.Rgb(MakeDiscOnWhite(w, h), w, h);
    keyed.Dictionary["Mask"] = new PdfArray(
        new PdfNumber(255), new PdfNumber(255), new PdfNumber(255),
        new PdfNumber(255), new PdfNumber(255), new PdfNumber(255));
    page.AddXObject("ImKey", doc.AddObject(keyed));
    c.Save().SetRgbFill(0.3, 0.8, 0.3).Rectangle(320, 560, 200, 160).Fill().Restore(); // green bg
    c.DrawImage("ImKey", 320, 560, 200, 160);

    // 3) Stencil mask: a 1-bit ImageMask painted in the current fill color (red).
    page.AddXObject("ImStencil", doc.AddObject(PdfImage.StencilMask(MakeCheckerBits(w, h), w, h)));
    c.Save().SetRgbFill(0.85, 0.85, 0.85).Rectangle(60, 340, 200, 160).Fill().Restore(); // gray bg
    c.Save().SetRgbFill(0.85, 0.1, 0.1).DrawImage("ImStencil", 60, 340, 200, 160).Restore();

    doc.Save(path);
    Report(path);
}

// Chapter 8 "Embedded Files": attach files to the document via the EmbeddedFiles
// name tree, and bind one to the page with a FileAttachment annotation.
static void BuildEmbeddedFiles(string path)
{
    var doc = new PdfDocument();
    doc.SetPageMode("UseAttachments");
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "Embedded Files")
        .DrawText("F1", 12, 100, 690, "Two files are attached. Click the paperclip or open the attachments panel.");

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

// Showcase v1 — Rows with Fixed / Auto / Relative sizing variants. Each successive
// showcase sample (35 → 44) re-renders the previous content plus one new section.
static void BuildShowcase35(string path)
{
    var doc = new PdfDocument();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    engine.Add(Showcase.SectionRows());
    doc.Save(path);
    Report(path);
}

// Showcase v2 — adds the Cols section (Fixed / Auto / Relative widths + mixed).
static void BuildShowcase36(string path)
{
    var doc = new PdfDocument();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    engine.Add(Showcase.SectionRows());
    engine.Add(Showcase.SectionCols());
    doc.Save(path);
    Report(path);
}

// Showcase v3 — adds the ExtendHorizontal section (full-width bands).
static void BuildShowcase37(string path)
{
    var doc = new PdfDocument();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    engine.Add(Showcase.SectionRows());
    engine.Add(Showcase.SectionCols());
    engine.Add(Showcase.SectionExtends());
    doc.Save(path);
    Report(path);
}

// Layout: a Table with shared auto-sized columns, a header that repeats on every
// page, per-cell borders, and pagination across many rows.
static void BuildLayoutTable(string path)
{
    var doc = new PdfDocument();
    var engine = new LayoutEngine(doc) { PageSize = PageSizes.Letter, Margin = 54 };
    var body = Standard14Font.Helvetica;
    var bold = Standard14Font.HelveticaBold;

    var table = new TableElement
    {
        CellBorderColor = Colors.Gray,
        CellBorderThickness = 0.5,
        HeaderBackground = Colors.DarkBlue,
        CellPadding = 5,
        Header = new UIElement[]
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
        table.Rows.Add(new UIElement[]
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
    var doc = new PdfDocument();
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
    var doc = new PdfDocument();
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    var heading = Standard14Font.Helvetica; // also goes through the Font API
    page.DrawText(heading, 22, 60, 740, "Embedded TrueType Font");

    string? fontPath = FindTrueTypeFont();
    if (fontPath is null)
    {
        page.DrawText(heading, 12, 60, 710, "(no TrueType font found on this system to embed)");
        doc.Save(path);
        Report(path);
        return;
    }

    var ttf = TrueTypeFont.FromFile(fontPath);
    page.DrawText(heading, 11, 60, 712,
        $"Loaded {Path.GetFileName(fontPath)}  ->  BaseFont /{ttf.BaseFont}");

    page.DrawText(ttf, 30, 60, 660, "The quick brown fox jumps");
    page.DrawText(ttf, 16, 60, 624, "Big quartz jugs (pdfHQ) - embedded glyph outlines");
    page.DrawText(ttf, 14, 60, 596, "Accented: cafe, naive, Dusseldorf -> café, naïve, Düsseldorf");

    // Measurement works for the embedded font too: overlay a guide line for each
    // vertical metric (read from the font's OS/2 / hhea tables), like sample 30.
    DrawFontMetricGuides(page, heading, ttf, 28, 60, 540, "Measured TrueType");

    // Reusing the same font does not embed it twice.
    page.DrawText(heading, 11, 60, 450, "The font is embedded once even when reused across the document.");

    doc.Save(path);
    Report(path);
}

// Draw text in textFont and overlay a horizontal guide line for each of its
// vertical metrics, plus the line-height box and a color-coded legend (labels in
// labelFont). Works for any Font, including embedded TrueType.
static void DrawFontMetricGuides(PdfPage page, Font labelFont, Font textFont, double size, double bx, double by, string text)
{
    page.DrawText(textFont, size, bx, by, text);
    var vm = textFont.GetVerticalMetrics(size);
    double width = textFont.MeasureText(text, size);
    var c = page.Content;

    // Line-height box (gray) spans descent..ascent.
    c.Save().SetRgbStroke(0.6, 0.6, 0.6).SetLineWidth(0.75)
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
        c.Save().SetRgbStroke(gr, gg, gb).SetLineWidth(0.6)
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
    page.Content.Save().SetRgbStroke(r, g, b).SetLineWidth(2).MoveTo(x, y + 3).LineTo(x + 16, y + 3).Stroke().Restore();
    page.DrawText(font, 9, x + 22, y, label);
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.AddFont("F2", doc.AddObject(StandardFonts.Create(StandardFonts.TimesRoman)));
    var c = page.Content;
    c.DrawText("F1", 22, 60, 740, "Text Measurement");

    // Alignment around a guide line at x = 320.
    const double anchor = 320;
    c.Save().SetRgbStroke(0.7, 0.7, 0.7).SetLineWidth(0.5).MoveTo(anchor, 700).LineTo(anchor, 615).Stroke().Restore();
    c.DrawText("F1", 14, anchor, 685, "Left-aligned at 320");
    c.DrawTextCentered("F1", StandardFonts.Helvetica, 14, anchor, 655, "Centered at 320");
    c.DrawTextRight("F1", StandardFonts.Helvetica, 14, anchor, 625, "Right-aligned at 320");

    // Measure a phrase with caps, ascenders, x-height letters and descenders
    // (g, j, p, q, y) and overlay a horizontal guide line for each font vertical
    // metric, plus the line-height box. Baseline is at 'by'.
    const string sample = "Big quartz jugs (pdfHQ)";
    const double size = 32;
    const string sampleFont = StandardFonts.TimesRoman;
    double width = TextMeasurer.MeasureText(sampleFont, size, sample);
    var vm = FontMetrics.GetVerticalMetrics(sampleFont, size);
    const double bx = 60, by = 580;
    c.DrawText("F2", size, bx, by, sample);

    // The line-height box (gray) spans descent..ascent.
    c.Save().SetRgbStroke(0.6, 0.6, 0.6).SetLineWidth(0.75)
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
        c.Save().SetRgbStroke(gr, gg, gb).SetLineWidth(0.6)
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

    c.Save().SetRgbStroke(0.4, 0.4, 0.85).SetLineWidth(1)
        .Rectangle(boxX, boxBottom, boxWidth, boxTop - boxBottom).Stroke().Restore();
    c.BeginText().SetFont("F2", psize).SetLeading(leading).SetTextMatrix(1, 0, 0, 1, boxX + pad, firstBaseline);
    for (int i = 0; i < lines.Count; i++)
    {
        if (i > 0) c.NextLine();
        c.ShowText(lines[i]);
    }
    c.EndText();

    doc.Save(path);
    Report(path);
}

// Spec gap-fill (book skipped these, deferring to ISO 32000): PDF functions,
// axial/radial shadings, the sh operator, and shading patterns via the Pattern
// colour space.
static void BuildShadings(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;
    c.DrawText("F1", 22, 60, 740, "Color Spaces & Shadings");

    var rgb = new CSharpPdf.Objects.PdfName("DeviceRGB");

    // 1) Axial (linear) two-colour gradient, painted with sh inside a clip.
    c.DrawText("F1", 11, 60, 705, "Axial gradient (sh, red -> blue)");
    var axialFn = PdfFunction.Exponential(new double[] { 1, 0, 0 }, new double[] { 0, 0, 1 });
    page.AddShading("Sh1", doc.AddObject(Shading.Axial(rgb, 60, 560, 300, 560, axialFn)));
    c.Save().Rectangle(60, 560, 240, 120).Clip().EndPath().PaintShading("Sh1").Restore();

    // 2) Three-stop axial gradient via a stitching function.
    c.DrawText("F1", 11, 60, 540, "Axial gradient (3-stop stitching: red -> green -> blue)");
    var f01 = PdfFunction.Exponential(new double[] { 1, 0, 0 }, new double[] { 0, 1, 0 });
    var f12 = PdfFunction.Exponential(new double[] { 0, 1, 0 }, new double[] { 0, 0, 1 });
    var stitch = PdfFunction.Stitching(new[] { f01, f12 }, new double[] { 0.5 }, new double[] { 0, 1, 0, 1 });
    page.AddShading("Sh2", doc.AddObject(Shading.Axial(rgb, 60, 440, 300, 440, stitch)));
    c.Save().Rectangle(60, 440, 240, 80).Clip().EndPath().PaintShading("Sh2").Restore();

    // 3) Radial gradient via a shading pattern (Pattern colour space + scn).
    c.DrawText("F1", 11, 360, 705, "Radial gradient (shading pattern)");
    var radialFn = PdfFunction.Exponential(new double[] { 1, 1, 1 }, new double[] { 0.1, 0.2, 0.7 });
    var radial = Shading.Radial(rgb, 440, 600, 5, 440, 600, 75, radialFn);
    page.AddPattern("P1", doc.AddObject(Shading.Pattern(radial)));
    c.Save().SetFillColorSpace("Pattern").SetFillPattern("P1").Circle(440, 600, 75).Fill().Restore();

    // 4) Gradient-filled text: clip to the glyph outlines (Tr 7), then paint a shading.
    c.DrawText("F1", 11, 360, 470, "Gradient-filled text (text clip + sh)");
    var textFn = PdfFunction.Exponential(new double[] { 0.8, 0, 0.4 }, new double[] { 0, 0.4, 0.9 });
    page.AddShading("Sh3", doc.AddObject(Shading.Axial(rgb, 360, 0, 560, 0, textFn)));
    c.Save();
    c.BeginText().SetFont("F1", 54).SetTextRenderMode(7).SetTextMatrix(1, 0, 0, 1, 360, 400).ShowText("PDF").EndText();
    c.PaintShading("Sh3").Restore();

    doc.Save(path);
    Report(path);
}

// Spec gap-fill: operators from Annex A not covered by the book — v/y Bézier
// variants, even-odd close-fill-stroke (b*), the " text operator, and an inline
// image (BI/ID/EI).
static void BuildOperators(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;
    c.DrawText("F1", 22, 60, 740, "Additional Operators");

    // Nonzero (b) vs even-odd (b*) fill on a self-intersecting pentagram.
    c.DrawText("F1", 11, 60, 700, "Pentagram fill: nonzero (b) vs even-odd (b*)");
    c.Save().SetRgbFill(1, 0.75, 0).SetRgbStroke(0.6, 0.4, 0).SetLineWidth(2);
    AppendPentagram(c, 140, 620, 55);
    c.CloseFillStroke().Restore();
    c.Save().SetRgbFill(1, 0.75, 0).SetRgbStroke(0.6, 0.4, 0).SetLineWidth(2);
    AppendPentagram(c, 300, 620, 55);
    c.CloseFillStrokeEvenOdd().Restore();

    // v / y Bézier curve variants forming a leaf.
    c.DrawText("F1", 11, 420, 700, "v / y Bézier curves");
    c.Save().SetRgbFill(0.2, 0.6, 0.9);
    c.MoveTo(440, 590).CurveToV(440, 660, 520, 660).CurveToY(520, 590, 440, 590).Fill().Restore();

    // The " operator: set word + char spacing, next line, then show.
    c.BeginText().SetFont("F1", 14).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 540)
        .ShowText("The quote operator sets spacing and shows a line:")
        .NextLineShowText(wordSpacing: 6, charSpacing: 1, text: "spaced out via the quote operator")
        .EndText();

    // Inline image (BI/ID/EI): a 4x4 RGB checker scaled up.
    c.DrawText("F1", 11, 60, 470, "Inline image (BI/ID/EI):");
    c.DrawInlineImageRgb(MakeTinyChecker(), 4, 4, 60, 380, 80, 80);

    doc.Save(path);
    Report(path);
}

// Draw one legend row: a short colored swatch line and a black label, then move down.
static void LegendRow(CSharpPdf.Content.ContentStream c, double x, ref double y,
    double r, double g, double b, string label)
{
    c.Save().SetRgbStroke(r, g, b).SetLineWidth(2).MoveTo(x, y + 3).LineTo(x + 16, y + 3).Stroke().Restore();
    c.DrawText("F1", 9, x + 22, y, label);
    y -= 13;
}

static void AppendPentagram(CSharpPdf.Content.ContentStream c, double cx, double cy, double r)
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

    var tree = new StructureTreeBuilder(doc);
    var tagger = tree.TagPage(page);
    tagger.Begin("H1");
    tagger.Content.DrawText("F1", 22, 60, 740, "PDF Standards");
    tagger.End();
    tagger.Begin("P");
    tagger.Content.DrawText("F1", 12, 60, 710, "PDF/A-style identification: XMP pdfaid, an OutputIntent, tagging, metadata.");
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "Document Metadata")
        .DrawText("F1", 12, 60, 712, "Title/Author/Subject/Keywords in both the Info dict and XMP.");

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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    page.AddFont("F2", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));

    const int w = 96, h = 96;
    page.AddXObject("Im1", doc.AddObject(PdfImage.Rgb(MakeGradient(w, h), w, h)));

    var tree = new StructureTreeBuilder(doc);
    tree.MapRole("Chap", "Sect"); // custom type mapped to a standard one
    var tagger = tree.TagPage(page);

    tagger.Begin("H1");
    tagger.Content.DrawText("F1", 24, 60, 720, "Tagged PDF Demo");
    tagger.End();

    tagger.Begin("P");
    tagger.Content.DrawText("F2", 13, 60, 690, "This paragraph is a tagged structure element (P).");
    tagger.End();

    tagger.Begin("Chap");
    tagger.Content.DrawText("F2", 13, 60, 665, "A custom 'Chap' element, role-mapped to Sect.");
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
    var doc = new PdfDocument();
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
    c.DrawText("F1", 22, 60, 740, "Optional Content (Layers)");
    c.DrawText("F1", 12, 60, 712, "Red and Green are ON by default; Blue is OFF.");

    c.BeginOptionalContent("OCR").SetRgbFill(1, 0, 0).Rectangle(80, 560, 160, 120).Fill().EndMarkedContent();
    c.BeginOptionalContent("OCG").SetRgbFill(0, 0.7, 0).Rectangle(180, 560, 160, 120).Fill().EndMarkedContent();
    c.BeginOptionalContent("OCB").SetRgbFill(0, 0, 1).Rectangle(280, 560, 160, 120).Fill().EndMarkedContent();

    doc.Save(path);
    Report(path);
}

// Chapter 10 advanced: OCMD visibility policies, radio-button layer groups
// (RBGroups), and the OC key on a form XObject and on an annotation.
static void BuildOptionalContentAdvanced(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    var font = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica));
    page.AddFont("F1", font);
    var c = page.Content;
    c.DrawText("F1", 22, 60, 740, "Optional Content — Advanced");

    // Language layers as a radio group: only one visible at a time (English on).
    var en = doc.AddOptionalContentGroup("English");
    var fr = doc.AddOptionalContentGroup("French");
    page.AddProperty("OCen", en);
    page.AddProperty("OCfr", fr);
    c.BeginOptionalContent("OCen").DrawText("F1", 16, 60, 690, "Hello! (English layer)").EndMarkedContent();
    c.BeginOptionalContent("OCfr").DrawText("F1", 16, 60, 690, "Bonjour! (French layer)").EndMarkedContent();

    // OCMD with an AllOn policy: visible only when both detail groups are on.
    var detailA = doc.AddOptionalContentGroup("Detail A");
    var detailB = doc.AddOptionalContentGroup("Detail B");
    var ocmd = doc.AddObject(OptionalContent.Membership(new[] { detailA, detailB }, "AllOn"));
    page.AddProperty("OCMD1", ocmd);
    c.BeginOptionalContent("OCMD1").SetRgbFill(0.9, 0.5, 0).Rectangle(60, 620, 220, 36).Fill().EndMarkedContent();
    c.DrawText("F1", 10, 60, 606, "(orange bar shows only when Detail A AND Detail B are on)");

    // DRAFT watermark: a form XObject carrying its own OC key (a toggleable layer).
    var watermark = doc.AddOptionalContentGroup("Watermark");
    page.AddProperty("OCwm", watermark);
    var wm = new FormXObject(PdfRectangle.FromSize(380, 90));
    wm.AddResource("Font", "F1", font);
    wm.Content.SetRgbFill(0.95, 0.6, 0.6).BeginText().SetFont("F1", 64)
        .SetTextMatrix(1, 0, 0, 1, 6, 12).ShowText("DRAFT").EndText();
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    var fontRef = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica));
    page.AddFont("F1", fontRef);
    page.Content.DrawText("F1", 22, 60, 740, "Multimedia & 3D");

    // Screen annotation playing a video via a rendition action.
    page.Content.DrawText("F1", 12, 60, 700, "Screen + rendition (video region):");
    var screenRect = new PdfRectangle(60, 540, 380, 680);
    page.Content.Save().SetRgbStroke(0, 0, 1).SetLineWidth(1).Rectangle(60, 540, 320, 140).Stroke().Restore();
    var screen = Media.ScreenAnnotation(screenRect, "A Movie", new double[] { 0, 0, 1 });
    var screenRef = page.AddAnnotation(screen);
    var rendition = doc.AddObject(Media.MediaRendition("video/mp4", "https://example.com/clip.mp4"));
    screen["A"] = PdfAction.Rendition(screenRef, rendition);

    // 3D annotation with a default view and a poster (fallback) appearance.
    page.Content.DrawText("F1", 12, 60, 470, "3D annotation (with poster fallback):");
    var view = doc.AddObject(Media.ThreeDView("Default",
        new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, -200 }));
    var threeD = doc.AddObject(Media.ThreeDStream(new byte[512], "U3D", new PdfArray(view), 0));

    var poster = new FormXObject(PdfRectangle.FromSize(320, 200));
    poster.AddResource("Font", "F1", fontRef);
    poster.Content.SetRgbFill(0.90, 0.90, 0.96).Rectangle(0, 0, 320, 200).Fill();
    poster.Content.SetRgbStroke(0.4, 0.4, 0.4).SetLineWidth(1).Rectangle(0.5, 0.5, 319, 199).Stroke();
    poster.Content.SetRgbFill(0.2, 0.2, 0.2).DrawText("F1", 16, 80, 95, "3D model (poster)");
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "Simple Media — Sound & Movie")
        .DrawText("F1", 12, 90, 690, "Speaker icon: sound annotation");

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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "Document Collection (Portfolio)")
        .DrawText("F1", 12, 60, 712, "Open in a portfolio-aware viewer to browse the embedded files.");

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
        var target = new PdfDocument();
        var tp = target.AddPage(PageSizes.Letter);
        tp.AddFont("F1", target.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
        tp.Content.DrawText("F1", 24, 72, 700, "I am the embedded target PDF!");
        using var ms = new MemoryStream();
        target.Save(ms);
        targetBytes = ms.ToArray();
    }

    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "GoToE — Embedded Go-To");

    doc.AddEmbeddedFile("target.pdf", "target.pdf", targetBytes, "application/pdf", "Embedded target PDF");
    LinkButton(page, 60, 690, 280, 28, "Open embedded target.pdf", PdfAction.GoToEmbedded("target.pdf"));

    doc.Save(path);
    Report(path);
}

// Chapter 7 "AcroForms" (choice and radio fields): a combo box, an editable
// combo box, a scrollable list box, and a radio button group.
static void BuildFormChoices(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var labels = page.Content;
    labels.DrawText("F1", 22, 60, 740, "Interactive Form — Choices");

    var form = new FormBuilder(doc);

    labels.DrawText("F1", 12, 60, 702, "State (combo):");
    form.ComboBox(page, "State", new PdfRectangle(180, 696, 380, 718),
        new[] { "Alabama", "Alaska", "Arizona", "California", "Colorado" }, "California");

    labels.DrawText("F1", 12, 60, 662, "Country (editable):");
    form.ComboBox(page, "Country", new PdfRectangle(180, 656, 380, 678),
        new[] { "France", "Belgium", "Germany", "Spain" }, "Slovakia", editable: true);

    labels.DrawText("F1", 12, 60, 622, "Fruit (list):");
    form.ListBox(page, "Fruit", new PdfRectangle(180, 540, 380, 632),
        new[] { "Orange", "Apple", "Banana", "Pear", "Melon", "Grape" }, selectedIndex: 2);

    labels.DrawText("F1", 12, 60, 500, "Shipping:");
    form.RadioGroup(page, "Shipping", new[]
    {
        ("standard", new PdfRectangle(180, 496, 198, 514)),
        ("express", new PdfRectangle(300, 496, 318, 514)),
        ("overnight", new PdfRectangle(430, 496, 448, 514)),
    }, selected: "express");
    labels.DrawText("F1", 11, 204, 500, "Standard").DrawText("F1", 11, 324, 500, "Express")
        .DrawText("F1", 11, 454, 500, "Overnight");

    doc.Save(path);
    Report(path);
}

// Chapter 7 "AcroForms" (text/button fields): an interactive form with single-
// and multi-line text fields, checkboxes, and a push button bound to ResetForm.
static void BuildFormBasics(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var labels = page.Content;
    labels.DrawText("F1", 22, 60, 740, "Interactive Form — Fields");

    var form = new FormBuilder(doc);

    labels.DrawText("F1", 12, 60, 702, "Full name:");
    form.TextField(page, "FullName", new PdfRectangle(150, 696, 430, 718), "Ada Lovelace");

    labels.DrawText("F1", 12, 60, 662, "Comments:");
    form.TextField(page, "Comments", new PdfRectangle(150, 600, 430, 678),
        "Multi-line text field.\nType across several lines.", multiline: true);

    labels.DrawText("F1", 12, 90, 556, "Subscribe to newsletter");
    form.CheckBox(page, "Subscribe", new PdfRectangle(60, 552, 80, 572), isChecked: true);

    labels.DrawText("F1", 12, 90, 526, "Accept terms");
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", 22, 60, 740, "Stamp and Note Annotations");

    // Build the stamp's appearance as a form XObject (red "APPROVED" badge).
    var stampFont = doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold));
    var stamp = new FormXObject(PdfRectangle.FromSize(200, 70));
    stamp.AddResource("Font", "SF", stampFont);
    stamp.Content.SetRgbStroke(0.8, 0, 0).SetRgbFill(0.8, 0, 0).SetLineWidth(4)
        .Rectangle(4, 4, 192, 62).Stroke()
        .DrawText("SF", 32, 24, 24, "APPROVED");
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    var c = page.Content;

    c.DrawText("F1", 22, 60, 740, "Annotations");

    // Text markup over four drawn words.
    c.DrawText("F1", 18, 60, 700, "Highlight   Underline   StrikeOut   Squiggly");
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
    var doc = new PdfDocument();
    var p1 = doc.AddPage(PageSizes.Letter);
    var p2 = doc.AddPage(PageSizes.Letter);
    var p3 = doc.AddPage(PageSizes.Letter);
    foreach (var p in new[] { p1, p2, p3 })
    {
        p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    }

    p1.Content.DrawText("F1", 24, 60, 740, "Navigation — Page 1");
    LinkButton(p1, 60, 680, 240, 28, "GoTo page 3 (Fit)", PdfAction.GoTo(PdfDestination.Fit(p3.Reference)));
    LinkButton(p1, 60, 640, 240, 28, "Named destination: chapter-3", PdfAction.GoToNamed("chapter-3"));
    LinkButton(p1, 60, 600, 240, 28, "Open oreilly.com (URI)", PdfAction.Uri("https://www.oreilly.com"));
    LinkButton(p1, 60, 560, 240, 28, "Open Chapter2.pdf (GoToR)", PdfAction.GoToRemote("Chapter2.pdf", 0));

    p2.Content.DrawText("F1", 24, 60, 740, "Navigation — Page 2");
    LinkButton(p2, 60, 680, 240, 28, "Back to page 1 top (XYZ)",
        PdfAction.GoTo(PdfDestination.XYZ(p1.Reference, 0, 792, null)));

    p3.Content.DrawText("F1", 24, 60, 740, "Navigation — Page 3 (target)");
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
    var doc = new PdfDocument();
    doc.SetPageMode("UseOutlines");

    var page1 = doc.AddPage(PageSizes.Letter);
    var page2 = doc.AddPage(PageSizes.Letter);
    var page3 = doc.AddPage(PageSizes.Letter);
    foreach (var p in new[] { page1, page2, page3 })
    {
        p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    }
    page1.Content.DrawText("F1", 22, 60, 760, "Document").DrawText("F1", 16, 60, 701, "Section 1")
        .DrawText("F1", 16, 60, 600, "Section 2").DrawText("F1", 14, 80, 560, "Subsection 1");
    page2.Content.DrawText("F1", 16, 60, 500, "Section 3");
    page3.Content.DrawText("F1", 22, 60, 700, "Summary");

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
    c.Save().SetRgbStroke(0.2, 0.3, 0.7).SetRgbFill(0.90, 0.93, 1.0).SetLineWidth(1)
        .Rectangle(x, y, w, h).FillStroke().Restore();
    c.Save().SetRgbFill(0.1, 0.2, 0.6).DrawText("F1", 12, x + 10, y + h / 2 - 4, label).Restore();
    page.AddLinkAnnotation(new PdfRectangle(x, y, x + w, y + h), action);
}

// Chapter 4 "The Font Dictionary": the same phrase set in several of the
// Standard 14 fonts, showing different font programs and the symbol fonts.
static void BuildTextFonts(string path)
{
    var doc = new PdfDocument();
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
        c.DrawText(resource, 22, 60, y, sample);
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
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.AddFont("FB", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
    page.AddFont("FW", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica, StandardFonts.WinAnsiEncoding)));
    var c = page.Content;

    // Rendering modes: fill, stroke, fill+stroke.
    c.BeginText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 730)
        .SetRgbFill(0.85, 0.1, 0.1).SetTextRenderMode(0).ShowText("Fill mode (Tr 0)").EndText();
    c.BeginText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 690)
        .SetRgbStroke(0.1, 0.1, 0.8).SetLineWidth(0.7).SetTextRenderMode(1).ShowText("Stroke mode (Tr 1)").EndText();
    c.BeginText().SetFont("FB", 30).SetTextMatrix(1, 0, 0, 1, 60, 650)
        .SetRgbFill(1, 0.8, 0).SetRgbStroke(0, 0, 0).SetTextRenderMode(2).ShowText("Fill + Stroke (Tr 2)").EndText();

    // Back to plain black fill for the rest.
    c.SetRgbFill(0, 0, 0).SetTextRenderMode(0);

    // Character spacing, word spacing, horizontal scaling.
    c.BeginText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 600)
        .SetCharSpacing(0).SetWordSpacing(0).SetHorizontalScaling(100).ShowText("Normal: the quick brown fox").EndText();
    c.BeginText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 576)
        .SetCharSpacing(3).ShowText("Char spacing Tc 3: the quick brown fox").EndText();
    c.BeginText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 552)
        .SetCharSpacing(0).SetWordSpacing(8).ShowText("Word spacing Tw 8: the quick brown fox").EndText();
    c.BeginText().SetFont("F1", 15).SetTextMatrix(1, 0, 0, 1, 60, 528)
        .SetWordSpacing(0).SetHorizontalScaling(160).ShowText("Horizontal scaling Tz 160").EndText();
    c.SetHorizontalScaling(100);

    // Text rise for sub/superscripts (within one text object, pen auto-advances).
    c.BeginText().SetFont("F1", 18).SetTextMatrix(1, 0, 0, 1, 60, 488)
        .ShowText("Rise: H").SetTextRise(-4).SetFont("F1", 12).ShowText("2")
        .SetTextRise(0).SetFont("F1", 18).ShowText("O,  E = mc").SetTextRise(7).SetFont("F1", 12).ShowText("2")
        .SetTextRise(0).EndText();

    // Leading + T* for multiple lines.
    c.BeginText().SetFont("F1", 15).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 448)
        .ShowText("Leading + T*: line one").NextLine().ShowText("line two").NextLine().ShowText("line three").EndText();

    // Manual kerning: plain Tj vs TJ with adjustments.
    c.BeginText().SetFont("FB", 38).SetTextMatrix(1, 0, 0, 1, 60, 350).ShowText("AWAY  (plain Tj)").EndText();
    c.BeginText().SetFont("FB", 38).SetTextMatrix(1, 0, 0, 1, 60, 300)
        .ShowTextWithKerning("A", 120, "W", 120, "A", 95, "Y", "  (kerned TJ)").EndText();

    // WinAnsiEncoding: accented Latin-1 characters.
    c.BeginText().SetFont("FW", 18).SetTextMatrix(1, 0, 0, 1, 60, 250)
        .ShowText("WinAnsi: Français, Español, Düsseldorf, café, naïve").EndText();

    doc.Save(path);
    Report(path);
}

// Chapter 3 "Vector Images": a reusable form XObject (a gold star) defined once
// and painted many times with different transforms, demonstrating that vector
// content can be reused without duplicating its description.
static void BuildFormXObject(string path)
{
    var doc = new PdfDocument();
    var page = doc.AddPage(PageSizes.Letter);

    // Define the star once inside a 100x100 bounding box.
    var star = new FormXObject(PdfRectangle.FromSize(100, 100));
    star.Content.SetRgbFill(1, 0.78, 0).SetRgbStroke(0.5, 0.35, 0).SetLineWidth(3);
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
static void AppendStar(CSharpPdf.Content.ContentStream c, double cx, double cy, double outer, double inner)
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
static void AddTextLabel(PdfDocument doc, PdfPage page, double x, double y, double size, string text)
{
    page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
    page.Content.DrawText("F1", size, x, y, text);
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
