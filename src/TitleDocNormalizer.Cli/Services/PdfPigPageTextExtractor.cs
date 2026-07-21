using PDFtoImage;
using SkiaSharp;
using TitleDocNormalizer.Cli.Models;
using UglyToad.PdfPig;


namespace TitleDocNormalizer.Cli.Services;

public interface IPageTextExtractor
{
    Task<IReadOnlyList<RawPage>> ExtractAsync(string pdfPath, CancellationToken cancellationToken = default);
}

public sealed class PdfPigPageTextExtractor(IOcrEngine? ocrEngine = null) : IPageTextExtractor
{
    public Task<IReadOnlyList<RawPage>> ExtractAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var pages = new List<RawPage>();
        using var document = PdfDocument.Open(pdfPath);
        byte[]? pdfBytes = null;

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

            if (string.IsNullOrWhiteSpace(text) && ocrEngine is not null)
            {
                pdfBytes ??= File.ReadAllBytes(pdfPath);
                text = RecognizeScannedPage(pdfBytes, page.Number - 1);
            }

            pages.Add(new RawPage(page.Number, text ?? string.Empty));
        }

        return Task.FromResult<IReadOnlyList<RawPage>>(pages);
    }

    private string RecognizeScannedPage(byte[] pdfBytes, int pageIndex)
    {
        try
        {
            using var bitmap = Conversion.ToImage(pdfBytes, page: (Index)pageIndex, options: new(Dpi: 300));
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return ocrEngine!.RecognizeText(png.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }
}
