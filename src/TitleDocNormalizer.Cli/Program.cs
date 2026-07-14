using System.Text.Json;
using TitleDocNormalizer.Cli.Pipeline;
using TitleDocNormalizer.Cli.Services;

namespace TitleDocNormalizer.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return 0;
        }

        if (!args[0].Equals("normalize", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Unknown command. Use: normalize <pdf-file> -o <output-folder>");
            return 2; 
        }

        var options = CliOptions.Parse(args.Skip(1).ToArray());
        if (options.InputPdf is null || !File.Exists(options.InputPdf))
        {
            Console.Error.WriteLine("PDF file not found. Use: normalize <pdf-file> -o <output-folder>");
            return 2;
        }

        Directory.CreateDirectory(options.OutputFolder);

        IPageTextExtractor extractor = new PdfPigPageTextExtractor();
        var pipeline = new NormalizationPipeline(
            extractor,
            new TextNormalizer(),
            new HeaderFooterDetector(),
            new TitlePageClassifier(),
            new SectionExtractor(),
            new FieldRouter(),
            new PromptBuilder(),
            new OutputWriter());

        var result = await pipeline.RunAsync(options);

        Console.WriteLine($"Done. Pages: {result.Manifest.Pages.Count}");
        Console.WriteLine($"Output: {Path.GetFullPath(options.OutputFolder)}");
        Console.WriteLine($"Manifest: {Path.Combine(options.OutputFolder, "manifest.json")}");

        if (options.Extract)
        {
            var promptPath = Path.Combine(options.OutputFolder, "prompts", "field_extraction_prompt.md");
            var prompt = await File.ReadAllTextAsync(promptPath);
            var ollama = new OllamaClient(options.OllamaUrl);
            var json = await ollama.GenerateAsync(options.Model, prompt, options.OllamaTimeoutSeconds);
            var output = Path.Combine(options.OutputFolder, "extraction_result.json");
            await File.WriteAllTextAsync(output, json);
            Console.WriteLine($"Extraction result: {output}");
        }

        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
TitleDocNormalizerOptimized

Usage:
  normalize <pdf-file> -o <output-folder> [--prompt] [--extract] [--model qwen2.5:7b]

Options:
  -o, --output           Output folder. Default: ./normalized-output
  --prompt              Generate LLM extraction prompt.
  --extract             Call Ollama and save extraction_result.json.
  --model               Ollama model name. Default: qwen2.5:7b
  --ollama-url          Ollama URL. Default: http://localhost:11434
  --max-pages-per-field Maximum routed pages per field. Default: 4

Examples:
  dotnet run --project src/TitleDocNormalizer.Cli -- normalize policy.pdf -o out --prompt
  dotnet run --project src/TitleDocNormalizer.Cli -- normalize policy.pdf -o out --prompt --extract --model phi4-mini
""");
    }
}

public sealed record CliOptions(
    string? InputPdf,
    string OutputFolder,
    bool GeneratePrompt,
    bool Extract,
    string Model,
    string OllamaUrl,
    int MaxPagesPerField,
    int OllamaTimeoutSeconds)
{
    public static CliOptions Parse(string[] args)
    {
        string? input = args.FirstOrDefault(a => !a.StartsWith("-", StringComparison.Ordinal));
        string output = "normalized-output";
        bool prompt = false;
        bool extract = false;
        string model = "qwen2.5:7b";
        string ollamaUrl = "http://localhost:11434";
        int maxPages = 4;
        int timeout = 300;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                case "--output":
                    output = args[++i];
                    break;
                case "--prompt":
                    prompt = true;
                    break;
                case "--extract":
                    extract = true;
                    prompt = true;
                    break;
                case "--model":
                    model = args[++i];
                    break;
                case "--ollama-url":
                    ollamaUrl = args[++i].TrimEnd('/');
                    break;
                case "--max-pages-per-field":
                    maxPages = int.Parse(args[++i]);
                    break;
                case "--ollama-timeout-seconds":
                    timeout = int.Parse(args[++i]);
                    break;
            }
        }

        return new CliOptions(input, output, prompt, extract, model, ollamaUrl, maxPages, timeout);
    }
}
