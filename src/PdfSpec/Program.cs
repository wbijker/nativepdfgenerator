using PdfSpec.Samples;

namespace PdfSpec;

internal static class Program
{
    /// <summary>
    /// Every sample registered in order. New samples are added here as
    /// they're written; the runner walks the list and writes each PDF
    /// to <c>samples/spec/</c>.
    /// </summary>
    private static readonly ISample[] Samples =
    {
        new Sample01_Blank(),
        new Sample02_Hello(),
        new Sample03_DocumentStructure(),
    };

    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);

        foreach (var sample in Samples)
        {
            var path = Path.Combine(samplesDir, sample.FileName);
            sample.Build(path);
            Console.WriteLine($"  {sample.FileName}");
        }

        Console.WriteLine($"Wrote {Samples.Length} sample(s) to {samplesDir}");
    }
}
