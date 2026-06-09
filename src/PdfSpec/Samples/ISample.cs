namespace PdfSpec.Samples;

/// <summary>
/// One sample. Implementations build a single PDF file at
/// <paramref name="path"/> when <see cref="Build"/> is invoked. The
/// runner in <see cref="Program"/> walks the registered samples in
/// order and writes each to <c>samples/spec/NN-name.pdf</c>.
/// </summary>
public interface ISample
{
    /// <summary>Output filename without the leading number (e.g. <c>blank.pdf</c>).</summary>
    string FileName { get; }

    void Build(string path);
}
