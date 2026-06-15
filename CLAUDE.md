# CSharpPdf — agent notes

## Conventions

- **Nullable reference types are enabled** across the codebase
  (`<Nullable>enable</Nullable>` in each `.csproj`). Treat reference types
  as non-nullable by default and use `?` only where null is a meaningful
  value.
- **No runtime null checks against non-nullable types.** Do not write
  defensive `if (x is null) throw …`, `?? throw`, `ArgumentNullException`,
  or `Require…` helpers against parameters or fields typed as non-nullable
  — the compiler already enforces it at the call site. Errors that NRT and
  API design can catch at compile time must not be guarded at runtime.
- **Redesign the API rather than gate at runtime.** If a method is only
  valid in a particular state, model the state in the type system instead
  of throwing on misuse. Example: don't expose `Bold()` on a paragraph that
  may not have a font family and `throw` when it doesn't — split into a
  `Paragraph` base (no family) and a `FamilyParagraph : Paragraph`
  (carries the family), so `.Bold()` only exists on the derived type and
  the misuse is a compile error. Apply the same principle to other
  state-dependent operations.
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
