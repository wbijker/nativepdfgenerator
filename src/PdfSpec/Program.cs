using PdfSpec.Samples;

namespace PdfSpec;

internal static class Program
{
    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);

        // PdfDoc.Create()
        //     .Info(title: "PdfSpec Combined Showcase", creator: "PdfSpec", producer: "PdfSpec")
        //     .DefaultFont(StandardFont.Helvetica, 11)
        //     .DefaultPageSize(PageSizes.A4)
        //     .AddPage(p => p
        //         .Header(BuildHeader())
        //         .Footer(BuildFooter())
        //         .AddBody(
        //             CoverBody(),
        //             Page1_DocumentBasics(),
        //             Page2_Imaging(),
        //             Page3_Text(),
        //             Page4_NavStructureMetadata(p.Document)))
        //     .Save(path);
        //
        // One PDF — every sample lives as a section inside SampleCombined,
        // separated by PageBreak sentinels. The individual Sample0N classes
        // are kept as building blocks but are no longer emitted as
        // standalone files.
        ISample sample = new SampleCombined();
        var path = Path.Combine(samplesDir, sample.FileName);
        sample.Build(path);
        Console.WriteLine($"Wrote {path}");
    }
}
