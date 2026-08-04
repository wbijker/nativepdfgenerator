# NativePdfGenerator

A PDF generator written from scratch in pure C#. No external packages — BCL
only (`System.IO.Compression`, `System.Xml.Linq`, `System.Globalization`).

This is a *writer*, not a parser: it builds PDF files directly, from the
low-level object model (indirect objects, content streams, cross-reference
tables) up to a layout engine with pages, rows, columns, text flow and SVG.

## Install

```
dotnet add package NativePdfGenerator
```

## Usage

```csharp
using PdfSpec;
using PdfSpec.Elements;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

var doc = PdfDoc.Create()
    .Info(title: "Hello", creator: "NativePdfGenerator")
    .DefaultFont(StandardFont.Helvetica, 11)
    .DefaultPageSize(PageSizes.A4)
    .DefaultMargin(10, Unit.Mm);

doc.AddPage(PageSizes.A4, p =>
{
    p.Body().Paragraph("Hello, PDF.");

    p.Body().Row(r =>
    {
        r.FixedItem(80).Padding(8).Text("Fixed 80pt").Bold();
        r.RelativeItem(2).Padding(8).Text("Grows to twice the remaining width.");
    });
});

doc.Save("hello.pdf");
```

## Repository layout

- `src/PdfSpec/` — the library, published as the `NativePdfGenerator` package.
- `src/PdfSpec.Samples/` — a console runner that writes sample PDFs into
  `samples/`. Run it with `dotnet run --project src/PdfSpec.Samples`.

## Releasing

Pushing a `v*` tag runs `.github/workflows/release.yml`, which packs
`src/PdfSpec` at the tag's version and publishes it to nuget.org using
[trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
(OIDC — no stored API key).

```
git tag v1.0.0
git push origin v1.0.0
```

Prerelease tags work the same way (`v0.1.0-preview.1`); NuGet hides those
from default search and `dotnet add package` unless `--prerelease` is passed.

## License

[MIT](LICENSE)
