﻿// <copyright file="SceneBase.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTesting.Core
{
    using System;
    using System.Collections.Generic;
    using Velaptor;
    using Velaptor.Content;
    using Velaptor.Graphics;
    using Velaptor.UI;

    /// <summary>
    /// A base scene to be used for creating new custom scenes.
    /// </summary>
    public abstract class SceneBase : IScene
    {
        private readonly List<IControl> controls = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneBase"/> class.
        /// </summary>
        /// <param name="contentLoader">Loads content for a scene.</param>
        protected SceneBase(IContentLoader contentLoader)
        {
            ContentLoader = contentLoader;
            IsActive = false;
        }

        /// <inheritdoc cref="IScene.Name"/>
        public string Name { get; set; } = string.Empty;

        /// <inheritdoc cref="IScene.Id"/>
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc cref="IScene.Name"/>
        public bool IsLoaded { get; private set; }

        /// <inheritdoc cref="IScene.IsActive"/>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the scene has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the content loader to load scene content.
        /// </summary>
        protected IContentLoader ContentLoader { get; }

        /// <inheritdoc cref="IScene.AddControl"/>
        public void AddControl(IControl control) => this.controls.Add(control);

        /// <inheritdoc cref="IScene.RemoveControl"/>
        public void RemoveControl(IControl control) => this.controls.Remove(control);

        /// <inheritdoc cref="IScene.LoadContent"/>
        public virtual void LoadContent() => IsLoaded = true;

        /// <inheritdoc cref="IScene.UnloadContent"/>
        public virtual void UnloadContent()
        {
            this.controls.Clear();
            IsLoaded = false;
        }

        /// <inheritdoc cref="IUpdatable.Update"/>
        public virtual void Update(FrameTime frameTime)
        {
            if (IsLoaded is false || IsActive)
            {
                return;
            }

            foreach (var control in this.controls)
            {
                control.Update(frameTime);
            }
        }

        /// <inheritdoc cref="IDrawable.Render"/>
        public virtual void Render(ISpriteBatch spriteBatch)
        {
            if (spriteBatch == null)
            {
                throw new ArgumentNullException(nameof(spriteBatch), "The parameter must not be null.");
            }

            if (IsLoaded is false)
            {
                return;
            }

            foreach (var control in this.controls)
            {
                control.Render(spriteBatch);
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>
        /// </summary>
        /// <param name="disposing">Disposes managed resources when <see langword="true"/></param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var control in this.controls)
                {
                    control.Dispose();
                }

                this.controls.Clear();
            }

            IsDisposed = true;
        }

        /// <summary>
        /// Throws an exception if the control is being loaded when it has already been disposed.
        /// </summary>
        /// <exception cref="Exception">Thrown when the control has been disposed.</exception>
        protected void ThrowExceptionIfLoadingWhenDisposed()
        {
            if (IsDisposed)
            {
                throw new Exception("Cannot load a scene that has been disposed.");
            }
        }
    }
}
