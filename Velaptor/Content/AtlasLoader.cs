﻿// <copyright file="AtlasLoader.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Content;

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Caching;
using Exceptions;
using Factories;
using Graphics;
using Services;

/// <summary>
/// Loads atlas data.
/// </summary>
internal sealed class AtlasLoader : ILoader<IAtlasData>
{
    private const char CrossPlatDirSeparatorChar = '/';
    private const string TextureExtension = ".png";
    private const string AtlasDataExtension = ".json";
    private readonly IItemCache<string, ITexture> textureCache;
    private readonly IAtlasDataFactory atlasDataFactory;
    private readonly IContentPathResolver atlasDataPathResolver;
    private readonly IJsonService jsonService;
    private readonly IDirectory directory;
    private readonly IFile file;
    private readonly IPath path;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtlasLoader"/> class.
    /// </summary>
    /// <param name="textureCache">Provides texture caching services.</param>
    /// <param name="atlasDataFactory">Generates <see cref="IAtlasData"/> instances.</param>
    /// <param name="atlasDataPathResolver">Resolves paths to JSON atlas data files.</param>
    /// <param name="jsonService">Provides JSON related services.</param>
    /// <param name="directory">Performs operations with directories.</param>
    /// <param name="file">Performs operations with files.</param>
    /// <param name="path">Processes directory and file paths.</param>
    /// <exception cref="ArgumentNullException">
    ///     Invoked when any of the parameters are null.
    /// </exception>
    public AtlasLoader(
        IItemCache<string, ITexture> textureCache,
        IAtlasDataFactory atlasDataFactory,
        IContentPathResolver atlasDataPathResolver,
        IJsonService jsonService,
        IDirectory directory,
        IFile file,
        IPath path)
    {
        ArgumentNullException.ThrowIfNull(textureCache);
        ArgumentNullException.ThrowIfNull(atlasDataFactory);
        ArgumentNullException.ThrowIfNull(atlasDataPathResolver);
        ArgumentNullException.ThrowIfNull(jsonService);
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(path);

        this.textureCache = textureCache;
        this.atlasDataFactory = atlasDataFactory;
        this.atlasDataPathResolver = atlasDataPathResolver;
        this.jsonService = jsonService;
        this.directory = directory;
        this.file = file;
        this.path = path;
    }

    /// <summary>
    /// Loads texture atlas data using the given <paramref name="contentPathOrName"/>.
    /// </summary>
    /// <param name="contentPathOrName">The content name or file path to the atlas data.</param>
    /// <returns>The loaded atlas data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="contentPathOrName"/> is null or empty.</exception>
    /// <exception cref="LoadTextureException">Thrown if the resulting texture content file path is invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the texture file does not exist.</exception>
    /// <exception cref="IOException">The directory specified a file or the network name is not known.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permissions.</exception>
    /// <exception cref="PathTooLongException">
    ///     The specified path, file name, or both exceed the system-defined maximum length.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
    /// <exception cref="NotSupportedException">The path contains a colon character <c>:</c> that is not part of a drive label.</exception>
    /// <remarks>
    /// Valid Values:
    /// <list type="bullet">
    ///     <item>MyAtlas</item>
    ///     <item>C:/Atlas/MyAtlas.png</item>
    ///     <item>C:/Atlas/MyAtlas.json</item>
    /// </list>
    ///
    /// Invalid Values:
    /// <list type="bullet">
    ///     <item>C:/Atlas/MyAtlas</item>
    ///     <item>C:/Atlas/MyAtlas.txt</item>
    /// </list>
    /// </remarks>
    public IAtlasData Load(string contentPathOrName)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentPathOrName);

        var isPathRooted = this.path.IsPathRooted(contentPathOrName);
        var contentDirPath = isPathRooted
            ? this.path.GetDirectoryName(contentPathOrName) ?? string.Empty
            : this.atlasDataPathResolver.ResolveDirPath();

        if (!isPathRooted && !this.directory.Exists(contentDirPath))
        {
            this.directory.CreateDirectory(contentDirPath);
        }

        var name = this.path.GetFileNameWithoutExtension(contentPathOrName);

        if (isPathRooted)
        {
            var validExtensions = new[] { TextureExtension, AtlasDataExtension };
            var extension = this.path.GetExtension(contentPathOrName);

            if (validExtensions.All(e => e != extension))
            {
                var exceptionMsg = "When loading atlas data with fully qualified paths,";
                exceptionMsg += $" the files must be a '{TextureExtension}' or '{AtlasDataExtension}' extension.";

                throw new LoadAtlasException(exceptionMsg);
            }
        }

        var atlasDataFilePath = $"{contentDirPath}{CrossPlatDirSeparatorChar}{name}{AtlasDataExtension}";

        if (!this.file.Exists(atlasDataFilePath))
        {
            var exceptionMsg = $"The atlas data directory '{contentDirPath}' does not contain the";
            exceptionMsg += $" required '{atlasDataFilePath}' atlas data file.";

            throw new LoadAtlasException(exceptionMsg);
        }

        var atlasImageFilePath = $"{contentDirPath}{CrossPlatDirSeparatorChar}{name}{TextureExtension}";

        if (!this.file.Exists(atlasImageFilePath))
        {
            var exceptionMsg = $"The atlas data directory '{contentDirPath}' does not contain the";
            exceptionMsg += $" required '{atlasImageFilePath}' atlas image file.";

            throw new LoadAtlasException(exceptionMsg);
        }

        var rawData = this.file.ReadAllText(atlasDataFilePath);
        var subTextureData = this.jsonService.Deserialize<AtlasSubTextureData[]>(rawData);

        if (subTextureData is null)
        {
            throw new LoadContentException($"There was an issue deserializing the JSON atlas data file at '{atlasDataFilePath}'.");
        }

        var atlasName = isPathRooted
            ? name
            : contentPathOrName;

        return this.atlasDataFactory.Create(subTextureData, contentDirPath, atlasName);
    }

    /// <inheritdoc/>
    public void Unload(string contentPathOrName) => this.textureCache.Unload(contentPathOrName);
}
