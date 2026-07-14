using TitleDocNormalizer.Cli.Models;
using TitleDocNormalizer.Cli.Services;

namespace TitleDocNormalizer.Tests;

public sealed class PipelineServiceTests
{
    [Fact]
    public void ClassifierDetectsScheduleA()
    {
        var classifier = new TitlePageClassifier();
        var page = new NormalizedPage(1, "Schedule A\nPolicy Number: ABC123\nAmount of Insurance: $500,000", 80, 80);
        var result = classifier.Classify(page);
        Assert.Equal(PageKind.ScheduleA, result.Kind);
        Assert.True(result.Confidence > 0.5);
    }

    [Fact]
    public void ClassifierDetectsLegalDescription()
    {
        var classifier = new TitlePageClassifier();
        var page = new NormalizedPage(2, "Legal Description\nLot 4, Block 7, Sunny Acres Subdivision, according to the plat recorded in Book 5 Page 10", 150, 150);
        var result = classifier.Classify(page);
        Assert.Equal(PageKind.LegalDescription, result.Kind);
    }

    [Fact]
    public void FieldRouterSendsLegalDescriptionToLegalPages()
    {
        var pages = new List<ClassifiedPage>
        {
            new(1, PageKind.Cover, .8, ["cover"], "Policy Number: ABC"),
            new(2, PageKind.LegalDescription, .9, ["legal"], "Legal Description Lot 1 Block 2")
        };
        var sections = new SectionExtractor().ExtractSections(pages);
        var routes = new FieldRouter().BuildRoutes(pages, sections, 2);
        var legal = routes.Single(r => r.FieldName == "legal_description");
        Assert.Contains(2, legal.PageNumbers);
    }

    [Fact]
    public void HeaderFooterDetectorRemovesRepeatedLines()
    {
        var pages = Enumerable.Range(1, 5)
            .Select(i => new RawPage(i, $"Common Header\nUnique content {i}\nPage {i}"))
            .ToList();
        var repeated = new HeaderFooterDetector().DetectRepeatedLines(pages);
        Assert.Contains("common header", repeated);
    }
}
