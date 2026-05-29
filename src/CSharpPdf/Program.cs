using CSharpPdf;
using CSharpPdf.Objects;

string samplesDir = Path.Combine(FindRepoRoot(), "samples");
Directory.CreateDirectory(samplesDir);

BuildBlankPage(Path.Combine(samplesDir, "01-blank.pdf"));
BuildHelloWorld(Path.Combine(samplesDir, "02-hello.pdf"));

Console.WriteLine($"Wrote samples to {samplesDir}");

// A minimal valid PDF: catalog -> page tree -> a single blank US Letter page.
static void BuildBlankPage(string path)
{
    var doc = new PdfDocument();

    var catalog = new PdfDictionary();
    var pages = new PdfDictionary();
    var page = new PdfDictionary();

    // Reserve object numbers first so we can wire up the circular references
    // (Pages -> Kids -> Page, and Page -> Parent -> Pages).
    var catalogRef = doc.Add(catalog);
    var pagesRef = doc.Add(pages);
    var pageRef = doc.Add(page);

    catalog["Type"] = new PdfName("Catalog");
    catalog["Pages"] = pagesRef;

    pages["Type"] = new PdfName("Pages");
    pages["Kids"] = new PdfArray(pageRef);
    pages["Count"] = new PdfNumber(1);

    page["Type"] = new PdfName("Page");
    page["Parent"] = pagesRef;
    page["MediaBox"] = new PdfArray(
        new PdfNumber(0), new PdfNumber(0), new PdfNumber(612), new PdfNumber(792));

    doc.Root = catalogRef;
    doc.Save(path);

    Console.WriteLine($"  {Path.GetFileName(path)}");
}

// A single page that draws "Hello, World!" using the standard Helvetica font.
static void BuildHelloWorld(string path)
{
    var doc = new PdfDocument();

    var catalog = new PdfDictionary();
    var pages = new PdfDictionary();
    var page = new PdfDictionary();
    var font = new PdfDictionary();

    var content = new PdfStream(
        "BT\n" +
        "/F1 24 Tf\n" +
        "72 720 Td\n" +
        "(Hello, World!) Tj\n" +
        "ET\n");

    var catalogRef = doc.Add(catalog);
    var pagesRef = doc.Add(pages);
    var pageRef = doc.Add(page);
    var contentRef = doc.Add(content);
    var fontRef = doc.Add(font);

    catalog["Type"] = new PdfName("Catalog");
    catalog["Pages"] = pagesRef;

    pages["Type"] = new PdfName("Pages");
    pages["Kids"] = new PdfArray(pageRef);
    pages["Count"] = new PdfNumber(1);

    var fonts = new PdfDictionary();
    fonts["F1"] = fontRef;
    var resources = new PdfDictionary();
    resources["Font"] = fonts;

    page["Type"] = new PdfName("Page");
    page["Parent"] = pagesRef;
    page["MediaBox"] = new PdfArray(
        new PdfNumber(0), new PdfNumber(0), new PdfNumber(612), new PdfNumber(792));
    page["Resources"] = resources;
    page["Contents"] = contentRef;

    font["Type"] = new PdfName("Font");
    font["Subtype"] = new PdfName("Type1");
    font["BaseFont"] = new PdfName("Helvetica");

    doc.Root = catalogRef;
    doc.Save(path);

    Console.WriteLine($"  {Path.GetFileName(path)}");
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
