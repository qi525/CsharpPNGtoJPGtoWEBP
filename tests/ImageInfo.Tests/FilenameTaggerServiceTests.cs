using Xunit;
using ImageInfo.Services;
using System.Collections.Generic;

namespace ImageInfo.Tests;

public class FilenameTaggerServiceTests
{
    private static readonly List<string> DefaultKeywords = new()
    {
        "anime",
        "girl",
        "cute",
        "blue_archive",
        "nsfw",
        "pasties"
    };

    [Fact]
    public void ExtractKeywords_WithMatchingKeywords_ShouldReturnMatchedList()
    {
        // Arrange
        var prompt = "beautiful anime girl, very cute, from blue_archive";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        Assert.True(result.HasTags);
        Assert.Equal(4, result.MatchCount);
        Assert.Contains("anime", result.MatchedKeywords);
        Assert.Contains("girl", result.MatchedKeywords);
        Assert.Contains("cute", result.MatchedKeywords);
        Assert.Contains("blue_archive", result.MatchedKeywords);
        Assert.Equal("___anime___girl___cute___blue_archive", result.TagSuffix);
    }

    [Fact]
    public void ExtractKeywords_WithPartialMatching_ShouldReturnOnlyMatched()
    {
        // Arrange
        var prompt = "anime character, very cute";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        Assert.True(result.HasTags);
        Assert.Equal(2, result.MatchCount);
        Assert.Equal("___anime___cute", result.TagSuffix);
    }

    [Fact]
    public void ExtractKeywords_WithNoMatching_ShouldReturnEmpty()
    {
        // Arrange
        var prompt = "landscape, mountain, nature";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        Assert.False(result.HasTags);
        Assert.Empty(result.MatchedKeywords);
        Assert.Empty(result.TagSuffix);
    }

    [Fact]
    public void ExtractKeywords_CaseSensitivity_ShouldBeInsensitiveByDefault()
    {
        // Arrange
        var prompt = "Beautiful ANIME Girl, CUTE style";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords, caseSensitive: false);

        // Assert
        Assert.Equal(3, result.MatchCount);
        Assert.Contains("anime", result.MatchedKeywords);
        Assert.Contains("girl", result.MatchedKeywords);
        Assert.Contains("cute", result.MatchedKeywords);
    }

    [Fact]
    public void ExtractKeywords_EmptyPrompt_ShouldReturnEmpty()
    {
        // Arrange
        var prompt = "";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        Assert.False(result.HasTags);
    }

    [Fact]
    public void ExtractKeywords_NullKeywordList_ShouldReturnEmpty()
    {
        // Arrange
        var prompt = "anime girl";

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, null);

        // Assert
        Assert.False(result.HasTags);
    }

    [Fact]
    public void ExtractKeywordsFromPrompts_CombinePositiveAndNegative_ShouldMatchBoth()
    {
        // Arrange
        var positivePrompt = "anime girl, cute";
        var negativePrompt = "ugly, nsfw content";
        var keywords = DefaultKeywords;

        // Act
        var result = FilenameTaggerService.ExtractKeywordsFromPrompts(
            positivePrompt, negativePrompt, keywords);

        // Assert
        Assert.True(result.HasTags);
        Assert.Contains("anime", result.MatchedKeywords);
        Assert.Contains("girl", result.MatchedKeywords);
        Assert.Contains("cute", result.MatchedKeywords);
        // nsfw 在合并后的文本中存在
        Assert.Contains("nsfw", result.MatchedKeywords);
    }

    [Fact]
    public void GenerateFilenameTagSuffix_ShouldIncludeLeadingUnderscore()
    {
        // Arrange
        var positivePrompt = "anime girl, blue_archive";
        var negativePrompt = "";
        var keywords = DefaultKeywords;

        // Act
        var suffix = FilenameTaggerService.GenerateFilenameTagSuffix(
            positivePrompt, negativePrompt, keywords, includeLeadingUnderscore: true);

        // Assert
        Assert.StartsWith("___", suffix);
        Assert.Equal("___anime___girl___blue_archive", suffix);
    }

    [Fact]
    public void GenerateFilenameTagSuffix_WithoutLeadingUnderscore_ShouldNotInclude()
    {
        // Arrange
        var positivePrompt = "anime girl, cute";
        var negativePrompt = "";
        var keywords = DefaultKeywords;

        // Act
        var suffix = FilenameTaggerService.GenerateFilenameTagSuffix(
            positivePrompt, negativePrompt, keywords, includeLeadingUnderscore: false);

        // Assert
        Assert.False(suffix.StartsWith("___"));
        Assert.StartsWith("anime", suffix);
        Assert.Contains("girl", suffix);
    }

    [Fact]
    public void ValidateKeywordList_WithValidList_ShouldReturnTrue()
    {
        // Arrange
        var keywords = new List<string> { "tag1", "tag2", "tag3" };

        // Act
        var (isValid, errors) = FilenameTaggerService.ValidateKeywordList(keywords);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateKeywordList_WithDuplicates_ShouldReturnError()
    {
        // Arrange
        var keywords = new List<string> { "tag1", "tag2", "tag1", "tag3" };

        // Act
        var (isValid, errors) = FilenameTaggerService.ValidateKeywordList(keywords);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateKeywordList_WithEmptyKeywords_ShouldReturnError()
    {
        // Arrange
        var keywords = new List<string> { "tag1", "", "tag3" };

        // Act
        var (isValid, errors) = FilenameTaggerService.ValidateKeywordList(keywords);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void GetDefaultKeywordList_ShouldReturnNonEmpty()
    {
        // Act
        var keywords = FilenameTaggerService.GetDefaultKeywordList();

        // Assert
        Assert.NotEmpty(keywords);
        Assert.Contains("blue_archive", keywords);
        Assert.Contains("pasties", keywords);
    }

    [Fact]
    public void ExtractKeywords_WithUnderscoreInKeyword_ShouldMatchCorrectly()
    {
        // Arrange
        var prompt = "from blue_archive, rio_(blue_archive) character";
        var keywords = new List<string> { "blue_archive", "rio_(blue_archive)" };

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        Assert.Equal(2, result.MatchCount);
        Assert.Contains("blue_archive", result.MatchedKeywords);
        Assert.Contains("rio_(blue_archive)", result.MatchedKeywords);
    }

    [Fact]
    public void ExtractKeywords_OrderingPreserved_ShouldMaintainKeywordListOrder()
    {
        // Arrange
        var prompt = "cute girl anime with nsfw elements";
        var keywords = new List<string> { "anime", "girl", "cute", "nsfw" };

        // Act
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Assert
        // 验证顺序与关键词列表一致，而不是出现顺序
        Assert.Equal(new[] { "anime", "girl", "cute", "nsfw" }, result.MatchedKeywords);
    }

    [Fact]
    public void ExtractKeywordsBatch_ShouldProcessMultipleRecords()
    {
        // Arrange
        var records = new List<Dictionary<string, string>>
        {
            new() { { "正面提示词", "anime girl" }, { "负面提示词", "ugly" } },
            new() { { "正面提示词", "cute blue_archive" }, { "负面提示词", "" } }
        };
        var keywords = DefaultKeywords;

        // Act
        var results = FilenameTaggerService.ExtractKeywordsBatch(records, keywords);

        // Assert
        Assert.Equal(2, results.Count);
        var firstResult = results[records[0]];
        Assert.True(firstResult.HasTags);
        var secondResult = results[records[1]];
        Assert.True(secondResult.HasTags);
    }

    [Fact]
    public void ToStringTest_ShouldFormatResultNicely()
    {
        // Arrange
        var prompt = "anime girl";
        var keywords = DefaultKeywords;
        var result = FilenameTaggerService.ExtractKeywords(prompt, keywords);

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("2个关键词", str);
        Assert.Contains("___anime___girl", str);
    }
}
