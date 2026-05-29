using Font = CSharpPdf.Text.Font;
using Standard14Font = CSharpPdf.Text.Standard14Font;

namespace CSharpPdf.Layout;

/// <summary>
/// Fluent factory for layout components, e.g.
/// <c>UI.Column().Children(UI.Text("Hi"), UI.Row().Background(Colors.Red))</c>.
/// </summary>
public static class UI
{
    /// <summary>Flowing, word-wrapped text in the default font (Helvetica 12).</summary>
    public static Paragraph Text(string text) => new(text, Standard14Font.Helvetica, 12);

    /// <summary>Flowing, word-wrapped text in a specific font and size.</summary>
    public static Paragraph Text(string text, Font font, double size) => new(text, font, size);

    /// <summary>A vertical stack of components.</summary>
    public static Column Column() => new();

    /// <summary>A horizontal run of components.</summary>
    public static Row Row() => new();
}
