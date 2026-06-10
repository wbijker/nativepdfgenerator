using PdfSpec.Samples;

namespace PdfSpec;

internal static class Program
{
    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);

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
