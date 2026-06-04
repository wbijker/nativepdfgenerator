# CSharpPdf — agent notes

## Conventions

- **Nullable reference types are enabled** across the codebase
  (`<Nullable>enable</Nullable>` in each `.csproj`). Treat reference types
  as non-nullable by default and use `?` only where null is a meaningful
  value. Do not write defensive null checks against parameters typed as
  non-nullable — rely on the type system. If a field becomes effectively
  non-null by construction, type it that way and delete the `Require…`
  helpers that exist only to throw on null.
- **No external dependencies.** Pure C# / BCL only (per
  `feedback_pdf_constraints.md`). System.IO.Compression, System.Xml.Linq,
  System.Globalization, etc. are fine.
- **PDF native concepts at the higher level.** Typed wrappers in
  `Structure/`, `Content/`, `Annotations/`, `Actions/`, `Layers/`,
  `Images/`, `Fonts/`, `ColorSpaces/`, `Geometry/` should not leak raw
  primitive PDF object construction (`new PdfName(...)`, `new PdfNumber(...)`,
  etc.) into their public surface — they use
  `PdfDictionary.Set/SetName/SetString/SetNumber/SetInteger/SetBoolean`.
- **Enums for closed PDF-name sets.** Anywhere a string parameter has a
  closed set of valid PDF spec values (PageLayout, PageMode, BlendMode,
  RenderingIntent, TextRenderMode, ColorSpace, OutputIntentSubtype,
  TextAnnotationIcon, OptionalContentIntent, PdfAConformance) — use the
  enum, not a string. Enum identifiers should match the PDF name objects
  emitted to the file when possible (so `.ToString()` is the spec name).
- **`PdfReference` is the canonical handle to a registered indirect
  object.** Resource registration methods (`PdfPage.UseFont`,
  `FormXObject.UseFont`, `…UseExtGState`) return `PdfReference`. The
  per-host resource name string is recovered via
  `FontNameOf(PdfReference)` / `ExtGStateNameOf(PdfReference)`.
- **Solution layout.** `src/PdfSpec/` is the low-level library (this is
  the focus); `src/CSharpPdf/` is a parallel/older project that does not
  reference PdfSpec.

## Building / running

`dotnet build src/PdfSpec/PdfSpec.csproj` or `dotnet run --project src/PdfSpec`
(writes the sample PDF to `samples/spec-text-operators.pdf`).
