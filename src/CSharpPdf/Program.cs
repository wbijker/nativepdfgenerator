using CSharpPdf;
using CSharpPdf.Content;
using CSharpPdf.Geometry;
using CSharpPdf.Images;
using CSharpPdf.Navigation;
using CSharpPdf.Objects;
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

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CSharpPdf.slnx")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
