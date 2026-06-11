using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec;

internal static class Program
{
    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);
        var path = Path.Combine(samplesDir, "samples.pdf");

        PdfDoc.Create()
            .Info(title: "PdfSpec Combined Showcase", creator: "PdfSpec", producer: "PdfSpec")
            .DefaultFont(StandardFont.Helvetica, 11)
            .DefaultPageSize(PageSizes.A5) 
            .AddPage(p =>
            {
                p.Header().Paragraph("Hap de pap");
                p.Footer().Paragraph("Copyright 2024, PdfSpec");
                
                p.Body().Paragraph("Very good, Sire");
            })
            .Save(path);

        Console.WriteLine($"Wrote {path}");
    }
}