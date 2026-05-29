using CSharpPdf;
using CSharpPdf.Geometry;
using CSharpPdf.Images;
using CSharpPdf.Objects;

string samplesDir = Path.Combine(FindRepoRoot(), "samples");
Directory.CreateDirectory(samplesDir);

BuildBlankPage(Path.Combine(samplesDir, "01-blank.pdf"));
BuildHelloWorld(Path.Combine(samplesDir, "02-hello.pdf"));
BuildDocumentStructure(Path.Combine(samplesDir, "03-document-structure.pdf"));
BuildNameTree(Path.Combine(samplesDir, "04-name-tree.pdf"));
BuildImagingModel(Path.Combine(samplesDir, "05-imaging-model.pdf"));
BuildTransparency(Path.Combine(samplesDir, "06-transparency.pdf"));
BuildRasterImage(Path.Combine(samplesDir, "07-raster-image.pdf"));

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
    var font = new PdfDictionary
    {
        ["Type"] = new PdfName("Font"),
        ["Subtype"] = new PdfName("Type1"),
        ["BaseFont"] = new PdfName("Helvetica"),
    };
    page.AddResource("Font", "F1", doc.AddObject(font));

    var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    page.SetContent(System.FormattableString.Invariant(
        $"BT\n/F1 {size:0.##} Tf\n{x:0.##} {y:0.##} Td\n({escaped}) Tj\nET\n"));
}

static void Report(string path) => Console.WriteLine($"  {Path.GetFileName(path)}");

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CSharpPdf.slnx")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
