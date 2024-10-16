// <copyright file="FontStatsServiceTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTests.Content.Fonts.Services;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using Velaptor.Content;
using Velaptor.Content.Fonts;
using Velaptor.Content.Fonts.Services;
using Velaptor.NativeInterop.Services;
using Xunit;

/// <summary>
/// Tests the <see cref="FontStatsService"/> class.
/// </summary>
public class FontStatsServiceTests
{
    private const string RootContentDirPath = @"C:\content-dir\";
    private const string DirNameForContentPath = "content-fonts";
    private const string FullContentFontDirPath = $@"{RootContentDirPath}{DirNameForContentPath}\";
    private readonly IFreeTypeService mockFreeTypeService;
    private readonly IContentPathResolver mockFontPathResolver;
    private readonly IDirectory mockDirectory;
    private readonly IPath mockPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontStatsServiceTests"/> class.
    /// </summary>
    public FontStatsServiceTests()
    {
        this.mockFreeTypeService = Substitute.For<IFreeTypeService>();
        this.mockFontPathResolver = Substitute.For<IContentPathResolver>();
        this.mockFontPathResolver.ResolveDirPath()
            .Returns(FullContentFontDirPath);
        this.mockFontPathResolver.ContentDirectoryName.Returns(DirNameForContentPath);

        this.mockDirectory = Substitute.For<IDirectory>();
        this.mockPath = Substitute.For<IPath>();
    }

    #region Constructor Tests

    [Fact]
    public void Ctor_WithNullFontServiceParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new FontStatsService(
                null,
                this.mockFontPathResolver,
                this.mockDirectory,
                this.mockPath);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'freeTypeService')");
    }

    [Fact]
    public void Ctor_WithNullContentFontPathResolverParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new FontStatsService(
                this.mockFreeTypeService,
                null,
                this.mockDirectory,
                this.mockPath);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'contentPathResolver')");
    }

    [Fact]
    public void Ctor_WithNullDirectoryParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new FontStatsService(
                this.mockFreeTypeService,
                this.mockFontPathResolver,
                null,
                this.mockPath);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'directory')");
    }

    [Fact]
    public void Ctor_WithNullPathParam_ThrowsException()
    {
        // Arrange & Act
        var act = () =>
        {
            _ = new FontStatsService(
                this.mockFreeTypeService,
                this.mockFontPathResolver,
                this.mockDirectory,
                null);
        };

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'path')");
    }
    #endregion

    #region Method Tests
    [Fact]
    public void GetContentStatsForFontFamily_WhenInvoked_ReturnsCorrectListOfFontStyles()
    {
        // Arrange
        const string fontFamily = "Times New Roman";
        var fontTimesRegular = BuildContentFontPath("TimesNewRoman-Regular.ttf");
        var fontTimesBold = BuildContentFontPath("TimesNewRoman-Bold.ttf");
        var fontTimesItalic = BuildContentFontPath("TimesNewRoman-Italic.ttf");
        var fontTimesBoldItalic = BuildContentFontPath("TimesNewRoman-BoldItalic.ttf");

        this.mockPath.GetDirectoryName(fontTimesRegular).Returns(FullContentFontDirPath);
        this.mockPath.GetDirectoryName(fontTimesBold).Returns(FullContentFontDirPath);
        this.mockPath.GetDirectoryName(fontTimesItalic).Returns(FullContentFontDirPath);
        this.mockPath.GetDirectoryName(fontTimesBoldItalic).Returns(FullContentFontDirPath);

        var fontFiles = new[]
        {
            fontTimesRegular, fontTimesBold, fontTimesItalic, fontTimesBoldItalic,
        };

        MockAllFontFamilies(fontFiles, fontFamily);

        MockFontStyle(fontTimesRegular, FontStyle.Regular);
        MockFontStyle(fontTimesBold, FontStyle.Bold);
        MockFontStyle(fontTimesItalic, FontStyle.Italic);
        MockFontStyle(fontTimesBoldItalic, FontStyle.Bold | FontStyle.Italic);

        this.mockDirectory.GetFiles(FullContentFontDirPath, "*.ttf").Returns(fontFiles);

        var expected = new[]
        {
            new FontStats
            {
                FontFilePath = fontTimesRegular, FamilyName = fontFamily, Style = FontStyle.Regular, Source = FontSource.AppContent,
            },
            new FontStats
            {
                FontFilePath = fontTimesBold, FamilyName = fontFamily, Style = FontStyle.Bold, Source = FontSource.AppContent,
            },
            new FontStats
            {
                FontFilePath = fontTimesItalic, FamilyName = fontFamily, Style = FontStyle.Italic, Source = FontSource.AppContent,
            },
            new FontStats
            {
                FontFilePath = fontTimesBoldItalic, FamilyName = fontFamily, Style = FontStyle.Bold | FontStyle.Italic, Source = FontSource.AppContent,
            },
        };

        var sut = CreateSystemUnderTest();

        // Act
        sut.GetContentStatsForFontFamily(fontFamily); // Executed 2 times to check for caching
        var actual = sut.GetContentStatsForFontFamily(fontFamily);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }
    #endregion

    /// <summary>
    /// Builds a path to an application font directory location.
    /// </summary>
    /// <param name="fileName">The file name to include in the path.</param>
    /// <returns>The application file path.</returns>
    private static string BuildContentFontPath(string fileName) => $"{FullContentFontDirPath}{fileName}";

    /// <summary>
    /// Creates a new service for the purpose of testing.
    /// </summary>
    /// <returns>The instance to test.</returns>
    private FontStatsService CreateSystemUnderTest() => new (
        this.mockFreeTypeService,
        this.mockFontPathResolver,
        this.mockDirectory,
        this.mockPath);

    /// <summary>
    /// Mocks the font at the given <paramref name="filePath"/> with the given font <paramref name="familyName"/>.
    /// </summary>
    /// <param name="filePath">The file path to the font file.</param>
    /// <param name="familyName">The family of the font to mock.</param>
    private void MockFontFamilyName(string filePath, string familyName)
    {
        this.mockFreeTypeService.GetFamilyName(filePath).Returns(familyName);
    }

    /// <summary>
    /// Mocks all the font families at the given <paramref name="filePaths"/> with the given font <paramref name="familyName"/>.
    /// </summary>
    /// <param name="filePaths">All the file paths to the fonts.</param>
    /// <param name="familyName">The family of the font to mock.</param>
    private void MockAllFontFamilies(IEnumerable<string> filePaths, string familyName)
    {
        foreach (var path in filePaths)
        {
            MockFontFamilyName(path, familyName);
        }
    }

    /// <summary>
    /// Mocks the given font style of the font at the given <paramref name="filePath"/> using the given font <paramref name="style"/>.
    /// </summary>
    /// <param name="filePath">The path to the font file.</param>
    /// <param name="style">The style to mock.</param>
    private void MockFontStyle(string filePath, FontStyle style)
    {
        this.mockFreeTypeService.GetFontStyle(filePath).Returns(style);
    }
}
