using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

internal static class Program
{
    public static void Main(string[] args)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples/spec"));
        Directory.CreateDirectory(samplesDir);
        var path = Path.Combine(samplesDir, "samples.pdf");

        var doc = PdfDoc.Create()
            .Info(title: "PdfSpec Combined Showcase", creator: "PdfSpec", producer: "PdfSpec")
            .DefaultFont(StandardFont.Helvetica, 11)
            .DefaultPageSize(PageSizes.A5)
            .DefaultMargin(10, Unit.Mm);

        // Doc-level chrome inherited by every AddPage / Content below.
        doc.Header()
            .Background(PdfColors.Red(200))
            .Padding(10)
            .AlignCenter()
            .Text("Hap de pap").Bold().FontSize(13);

        doc.Footer()
            .Background(PdfColors.Blue(200))
            .Padding(10)
            .AlignRight()
            .PageNumber("Page {0} / {1}");

        doc.AddPage(PageSizes.A5, p =>
        {
            p.Body().Paragraph("Very good, Sire");

            p.Body()
                .Padding(20)
                .Background(PdfColors.Amber(100))
                .Border(1.5, PdfColors.Amber(700))
                .Rounded(12)
                .Anchor("amber-card")
                .Text("This card is anchored as \"amber-card\" — see the linked text on the next page.")
                .Italic()
                .Color(PdfColors.Amber(900));
        });

        doc.AddPage(PageSizes.A5, p =>
        {
            // Row with mixed slot sizing.
            p.Body().Row(r =>
            {
                r.FixedItem(80)
                    .Background(PdfColors.Sky(100))
                    .Padding(8)
                    .Text("Fixed 80pt").Bold();

                r.RelativeItem(2)
                    .Padding(8)
                    .Text("Relative 2× — this column grows to claim twice the remaining width.");

                r.RelativeItem()
                    .Padding(8)
                    .Text("Relative 1×").Underline();
            });

            // Component composition + link-to-anchor.
            p.Body().Component(new InfoCard(
                title: "Demo card",
                body: "Tap me to jump back to the anchored amber card on page 1."))
                ;
            p.Body().LinkToAnchor("amber-card", c =>
                c.Padding(8)
                 .Background(PdfColors.Gray(100))
                 .Text("Jump to amber card →").Color(PdfColors.Indigo(700)));

            // SVG: per-shape coverage in a single inline doc.
            p.Body().Padding(8).Row(r =>
            {
                r.RelativeItem().Height(110).Svg(@"
                    <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 120 120'>
                        <rect x='10' y='10' width='100' height='100' rx='14' ry='14'
                              fill='#fde68a' stroke='#92400e' stroke-width='3'/>
                        <circle cx='60' cy='60' r='28' fill='#ef4444' opacity='0.85'/>
                        <line x1='10' y1='110' x2='110' y2='10' stroke='#1f2937' stroke-width='2'/>
                        <polygon points='60,15 70,45 102,45 76,63 86,93 60,75 34,93 44,63 18,45 50,45'
                                 fill='none' stroke='#1e3a8a' stroke-width='1.5'/>
                    </svg>");

                r.RelativeItem().Height(110).Svg(@"
                    <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'>
                        <g transform='translate(50 50) rotate(15)'>
                            <path d='M -30 0 L 0 -30 L 30 0 L 0 30 Z' fill='#0ea5e9'/>
                            <path d='M -20 -20 Q 0 -40 20 -20 T 20 20 T -20 20 Z'
                                  fill='none' stroke='#fff' stroke-width='2'/>
                        </g>
                        <path d='M 10 80 A 30 30 0 0 1 90 80' fill='none' stroke='#10b981' stroke-width='4'/>
                    </svg>");
            });

            p.Body().PageBreak();

            // MultiColumn flow.
            p.Body().MultiColumn(columns: 2, gap: 12, build: col =>
            {
                for (int i = 1; i <= 6; i++)
                    col.Item().Padding(4).Text($"Column item {i} — short body text demonstrating newspaper-style flow across columns.");
            });
        });

        doc.Save(path);

        Console.WriteLine($"Wrote {path}");
    }

    private sealed class InfoCard : IComponent
    {
        private readonly string _title;
        private readonly string _body;
        public InfoCard(string title, string body) { _title = title; _body = body; }

        public void Compose(IContainer container)
        {
            container
                .Padding(12)
                .Background(PdfColors.Slate(50))
                .Border(1, PdfColors.Slate(300))
                .Rounded(6)
                .Column(col =>
                {
                    col.Item().Text(_title).Bold().FontSize(13);
                    col.Item().Text(_body);
                });
        }
    }
}
