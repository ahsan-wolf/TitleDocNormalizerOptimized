namespace TitleDocNormalizer.Cli.Models;

public sealed record RawPage(int PageNumber, string Text);
public sealed record NormalizedPage(int PageNumber, string Text, int OriginalCharCount, int NormalizedCharCount);

public enum PageKind
{
    Unknown,
    Cover,
    ScheduleA,
    ScheduleB,
    LegalDescription,
    Exceptions,
    Endorsements,
    Conditions,
    Signature,
    Miscellaneous
}

public sealed record ClassifiedPage(
    int PageNumber,
    PageKind Kind,
    double Confidence,
    string[] Signals,
    string Text);

public sealed record DocumentSection(
    string Name,
    PageKind Kind,
    IReadOnlyList<int> PageNumbers,
    string Text);

public sealed record FieldRoute(
    string FieldName,
    IReadOnlyList<int> PageNumbers,
    IReadOnlyList<string> SectionNames,
    double Score,
    string RoutedText);

public sealed record ManifestPage(
    int PageNumber,
    PageKind Kind,
    double Confidence,
    int OriginalCharCount,
    int NormalizedCharCount,
    string[] Signals);

public sealed record NormalizationManifest(
    string SourceFile,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ManifestPage> Pages,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> FieldRoutes,
    string Notes);

public sealed record PipelineResult(NormalizationManifest Manifest, IReadOnlyList<FieldRoute> Routes);

public sealed record RedactionMapping(
    string FieldName,
    string OriginalValue,
    string DummyValue,
    IReadOnlyList<int> PageNumbers);

public sealed record RedactionResult(
    IReadOnlyList<ClassifiedPage> Pages,
    IReadOnlyList<DocumentSection> Sections,
    IReadOnlyList<FieldRoute> Routes,
    IReadOnlyList<RedactionMapping> Mappings);
