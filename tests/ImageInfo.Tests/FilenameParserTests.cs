using Xunit;
using ImageInfo.Services;
using System.Collections.Generic;

namespace ImageInfo.Tests;

public class FilenameParserTests
{
    [Fact]
    public void ParseFilename_WithFullFormat_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "00000-2365214977___blue_archive___whip___mari___track___commentaries___kit___aid___archive___milkshakework___highleg___dominatrix@@@评分88.jpg";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("00000-2365214977", result.OriginalName);
        Assert.Equal(".jpg", result.Extension);
        Assert.StartsWith("___blue_archive", result.Suffix);
        Assert.Contains("@@@评分88", result.Suffix);
    }

    [Fact]
    public void ParseFilename_WithoutSuffix_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "photo001.png";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("photo001", result.OriginalName);
        Assert.Equal(".png", result.Extension);
        Assert.Empty(result.Suffix);
    }

    [Fact]
    public void ParseFilename_WithTagsSuffix_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "image_001___landscape___nature___sunset.png";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("image_001", result.OriginalName);
        Assert.Equal(".png", result.Extension);
        Assert.Equal("___landscape___nature___sunset", result.Suffix);
    }

    [Fact]
    public void ParseFilename_WithScoreSuffix_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "image_001@@@评分95.webp";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("image_001", result.OriginalName);
        Assert.Equal(".webp", result.Extension);
        Assert.Equal("@@@评分95", result.Suffix);
    }

    [Fact]
    public void ParseFilename_SimpleFilename_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "simple_image.jpg";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("simple_image", result.OriginalName);
        Assert.Empty(result.Suffix);
        Assert.Equal(".jpg", result.Extension);
    }

    [Fact]
    public void ParseFilename_WithoutExtension_ShouldFail()
    {
        // Arrange
        var filename = "image_without_extension";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("扩展名", result.ErrorMessage);
    }

    [Fact]
    public void ParseFilename_WithEmptyString_ShouldFail()
    {
        // Arrange
        var filename = "";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("不能为空", result.ErrorMessage);
    }

    [Fact]
    public void GetOriginalName_WithValidFilename_ShouldReturnOriginalName()
    {
        // Arrange
        var filename = "photo_2025___tag1___tag2@@@评分85.jpg";

        // Act
        var originalName = FilenameParser.GetOriginalName(filename);

        // Assert
        Assert.NotNull(originalName);
        Assert.Equal("photo_2025", originalName);
    }

    [Fact]
    public void GetExtension_WithValidFilename_ShouldReturnExtension()
    {
        // Arrange
        var filename = "image___cat___cute___fluffy.png";

        // Act
        var extension = FilenameParser.GetExtension(filename);

        // Assert
        Assert.NotNull(extension);
        Assert.Equal(".png", extension);
    }

    [Fact]
    public void GetSuffix_WithSuffix_ShouldReturnSuffix()
    {
        // Arrange
        var filename = "photo___tag1___tag2@@@评分72.jpg";

        // Act
        var suffix = FilenameParser.GetSuffix(filename);

        // Assert
        Assert.Equal("___tag1___tag2@@@评分72", suffix);
    }

    [Fact]
    public void GetSuffix_WithoutSuffix_ShouldReturnEmpty()
    {
        // Arrange
        var filename = "photo_without_suffix.jpg";

        // Act
        var suffix = FilenameParser.GetSuffix(filename);

        // Assert
        Assert.Empty(suffix);
    }

    [Fact]
    public void RebuiltFilename_ShouldMatchOriginal()
    {
        // Arrange
        var originalFilename = "base001___tag1___tag2___tag3@@@评分88.jpg";
        var result = FilenameParser.ParseFilename(originalFilename);

        // Act
        var rebuilt = result.RebuiltFilename;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(originalFilename, rebuilt);
    }

    [Fact]
    public void ParseFilename_WithSpecialCharacters_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "00000-abc_123___tag-with-dash___tag_with_underscore@@@评分99.jpeg";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("00000-abc_123", result.OriginalName);
        Assert.Equal(".jpeg", result.Extension);
        Assert.Contains("tag-with-dash", result.Suffix);
        Assert.Contains("tag_with_underscore", result.Suffix);
    }

    [Fact]
    public void ParseFilename_WithChineseCharacters_ShouldParseCorrectly()
    {
        // Arrange
        var filename = "图片001___中文标签___english_tag___日本語@@@评分88.png";

        // Act
        var result = FilenameParser.ParseFilename(filename);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("图片001", result.OriginalName);
        Assert.Equal(".png", result.Extension);
        Assert.Contains("中文标签", result.Suffix);
        Assert.Contains("english_tag", result.Suffix);
        Assert.Contains("@@@评分88", result.Suffix);
    }

    [Fact]
    public void ToString_ShouldFormatResultNicely()
    {
        // Arrange
        var filename = "photo___tag1___tag2@@@评分75.jpg";
        var result = FilenameParser.ParseFilename(filename);

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("photo", str);
        Assert.Contains(".jpg", str);
        Assert.Contains("___tag1___tag2@@@评分75", str);
    }

    [Fact]
    public void ParseFilenamePath_ShouldExtractFilename()
    {
        // Arrange
        var filePath = "C:\\Users\\test\\Documents\\image___tag1___tag2.jpg";

        // Act
        var result = FilenameParser.ParseFilenamePath(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("image", result.OriginalName);
        Assert.Equal(".jpg", result.Extension);
    }
}

