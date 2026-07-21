using Tesseract;

namespace TitleDocNormalizer.Cli.Services;

public interface IOcrEngine : IDisposable
{
    string RecognizeText(byte[] imageBytes);
}

public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine _engine;

    public TesseractOcrEngine(string tessDataPath, string language = "eng")
    {
        if (!Directory.Exists(tessDataPath) || !File.Exists(Path.Combine(tessDataPath, $"{language}.traineddata")))
        {
            throw new FileNotFoundException(
                $"Tesseract trained data '{language}.traineddata' was not found in '{Path.GetFullPath(tessDataPath)}'. " +
                "Download it from https://github.com/tesseract-ocr/tessdata and place it in the tessdata folder, " +
                "or pass --tessdata-path pointing at a folder that contains it.");
        }

        _engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
    }

    public string RecognizeText(byte[] imageBytes)
    {
        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = _engine.Process(pix);
        return page.GetText() ?? string.Empty;
    }

    public void Dispose() => _engine.Dispose();
}
