﻿// <copyright file="IoC.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RaptorTests", AllInternalsVisible = true)]

#pragma warning disable SA1116 // Split parameters should start on line after declaration
namespace Raptor
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using Raptor.Audio;
    using Raptor.Content;
    using Raptor.Graphics;
    using Raptor.OpenAL;
    using Raptor.OpenGL;
    using Raptor.Services;
    using SimpleInjector;

    /// <summary>
    /// Provides dependency injection for the application.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class IoC
    {
        private static readonly FileSystem FileSystem = new FileSystem();
        private static readonly Container IoCContainer = new Container();
        private static bool isInitialized;

        /// <summary>
        /// Gets the inversion of control container used to get instances of objects.
        /// </summary>
        public static Container Container
        {
            get
            {
                if (!isInitialized)
                {
                    SetupContainer();
                }

                return IoCContainer;
            }
        }

        /// <summary>
        /// Sets up the IoC container.
        /// </summary>
        private static void SetupContainer()
        {
            SetupOpenGL();

            SetupServices();

            SetupContent();

            isInitialized = true;
        }

        /// <summary>
        /// Setup container registration related to OpenTK.
        /// </summary>
        private static void SetupOpenGL()
        {
            IoCContainer.Register(() => FileSystem.File);
            IoCContainer.Register(() => FileSystem.Directory);
            IoCContainer.Register<IPlatform, Platform>(Lifestyle.Singleton);
            IoCContainer.Register<IGLInvoker, GLInvoker>(Lifestyle.Singleton);
            IoCContainer.Register<IALInvoker, ALInvoker>(Lifestyle.Singleton);
            IoCContainer.Register<IGLFWInvoker, GLFWInvoker>(Lifestyle.Singleton);

            IoCContainer.Register<GLFWMonitors>();

            IoCContainer.Register<IGPUBuffer, GPUBuffer<VertexData>>(Lifestyle.Singleton); // Suppressed Disposal

            IoCContainer.Register<IShaderProgram, ShaderProgram>(Lifestyle.Singleton); // Suppressed Disposal

            IoCContainer.Register<ISpriteBatch, SpriteBatch>(Lifestyle.Singleton); // Suppressed Disposal

            SetupAudio();
        }

        /// <summary>
        /// Setup container registration related to audio.
        /// </summary>
        private static void SetupAudio()
        {
            // Register the proper data stream to be the implementation if the consumer is a certain decoder
            IoCContainer.RegisterConditional<IAudioDataStream<float>, OggAudioDataStream>(context =>
            {
                return !context.HasConsumer || context.Consumer.ImplementationType == typeof(OggSoundDecoder);
            }, true);

            IoCContainer.RegisterConditional<IAudioDataStream<byte>, Mp3AudioDataStream>(context =>
            {
                return !context.HasConsumer || context.Consumer.ImplementationType == typeof(MP3SoundDecoder);
            }, true);

            IoCContainer.Register<ISoundDecoder<float>, OggSoundDecoder>(true);
            IoCContainer.Register<ISoundDecoder<byte>, MP3SoundDecoder>(true);
        }

        /// <summary>
        /// Setup container registration related to services.
        /// </summary>
        private static void SetupServices()
        {
            IoCContainer.Register<IImageFileService, ImageFileService>();
            IoCContainer.Register<IEmbeddedResourceLoaderService, EmbeddedResourceLoaderService>(Lifestyle.Singleton);
            IoCContainer.Register<ISystemMonitorService, SystemMonitorService>();
        }

        /// <summary>
        /// Setup container registration related to content.
        /// </summary>
        private static void SetupContent() => IoCContainer.Register<AtlasTexturePathResolver>();
    }
}
