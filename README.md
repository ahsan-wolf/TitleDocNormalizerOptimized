# TitleDocNormalizerOptimized

A production-oriented .NET 10 console pipeline for normalizing title-industry PDF documents before sending them to a local LLM such as Qwen/Phi through Ollama.

This regenerated version adds the optimizations discussed:

- PDF text extraction with PdfPig
- page-level normalization
- repeated header/footer removal
- title-policy page classification
- Schedule A / Schedule B / Legal Description / Exceptions section detection
- page relevance scoring
- field-aware routing
- section-based chunks, not blind token chunks
- manifest.json generation
- LLM-ready routed input
- strict JSON extraction prompt generation
- optional Ollama extraction call
- unit-testable services

## Why this is faster on CPU-only laptops

Instead of sending all 14 pages to Qwen/Phi, the pipeline classifies pages first and routes each field to only the most relevant pages/sections. This reduces prompt size and gives your i7 CPU less work.

## Build

```bash
dotnet restore
dotnet build
```

If your environment has package restrictions, install PdfPig explicitly:

```bash
dotnet add src/TitleDocNormalizer.Cli/TitleDocNormalizer.Cli.csproj package UglyToad.PdfPig --version 1.7.0-custom-5
```

## Run normalization only

```bash
dotnet run --project src/TitleDocNormalizer.Cli -- normalize "C:\docs\policy.pdf" -o "C:\out\policy-normalized"
```

## Run normalization and generate prompt files

```bash
dotnet run --project src/TitleDocNormalizer.Cli -- normalize "C:\docs\policy.pdf" -o "C:\out\policy-normalized" --prompt
```

## Optional local LLM extraction through Ollama

Make sure Ollama is running:

```bash
ollama serve
ollama pull qwen2.5:7b
```

Then run:

```bash
dotnet run --project src/TitleDocNormalizer.Cli -- normalize "C:\docs\policy.pdf" -o "C:\out\policy-normalized" --prompt --extract --model qwen2.5:7b
```

## Output files

```text
output/
  manifest.json
  normalized_document.md
  routed_llm_input.md
  pages/
    page_001.md
    page_002.md
  sections/
    schedule_a.md
    schedule_b.md
    legal_description.md
    exceptions.md
  chunks/
    policy_number.md
    effective_date.md
    insured_name.md
    property_address.md
    legal_description.md
  prompts/
    field_extraction_prompt.md
  extraction_result.json       (only when --extract is used)
```

## Important note

This pipeline is designed for digital/text-layer PDFs first. For scanned PDFs, add OCR before this pipeline or plug an OCR implementation into `IPageTextExtractor`.
