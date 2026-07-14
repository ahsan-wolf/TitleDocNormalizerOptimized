using TitleDocNormalizer.Cli.Models;
using UglyToad.PdfPig;


namespace TitleDocNormalizer.Cli.Services;

public interface IPageTextExtractor
{
    Task<IReadOnlyList<RawPage>> ExtractAsync(string pdfPath, CancellationToken cancellationToken = default);
}

public sealed class PdfPigPageTextExtractor : IPageTextExtractor
{
    public Task<IReadOnlyList<RawPage>> ExtractAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var pages = new List<RawPage>();
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text;
            try
            {
                text = page.Text;
            }
            catch
            {
                text = page.Text;
            }

            pages.Add(new RawPage(page.Number, text ?? string.Empty));
        }

        return Task.FromResult<IReadOnlyList<RawPage>>(pages);
    }
}
