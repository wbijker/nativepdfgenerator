using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// Sample <see cref="IComponent"/> — a centred big title with a smaller grey
/// subtitle underneath and a bottom gutter. Used by sample 50 to demonstrate
/// component composition via <see cref="Container.Component"/>.
/// </summary>
public sealed class TitleHeader : IComponent
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public Color TitleColor { get; set; } = Colors.DarkBlue;
    public double TitleSize { get; set; } = 24;
    public double SubtitleSize { get; set; } = 10;

    public void Compose(Container container)
    {
        container.Column(col =>
        {
            col.Item().Padding(6).AlignCenter()
                .Text(Title).Bold().FontSize(TitleSize).FontColor(TitleColor);

            if (Subtitle.Length > 0)
            {
                col.Item().Padding(4).AlignCenter()
                    .Text(Subtitle).Italic().FontSize(SubtitleSize).FontColor(Colors.Gray);
            }

            col.Item().Padding(10);
        });
    }
}
