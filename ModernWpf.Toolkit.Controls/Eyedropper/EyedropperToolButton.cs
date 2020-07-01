// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ModernWpf.Toolkit.Controls
{
    /// <summary>
    /// The <see cref="EyedropperToolButton"/> control helps use <see cref="Eyedropper"/> in view.
    /// </summary>
    public partial class EyedropperToolButton : ButtonBase
    {
        private readonly Eyedropper _eyedropper;

        /// <summary>
        /// Initializes a new instance of the <see cref="EyedropperToolButton"/> class.
        /// </summary>
        public EyedropperToolButton()
        {
            DefaultStyleKey = typeof(EyedropperToolButton);
            _eyedropper = new Eyedropper();

            OwnerWindow = Window.GetWindow(this);

            Loaded += EyedropperToolButton_Loaded;
        }

        /// <summary>
        /// Occurs when the Color property has changed.
        /// </summary>
        public event TypedEventHandler<EyedropperToolButton, EyedropperColorChangedEventArgs> ColorChanged;

        /// <summary>
        /// Occurs when the eyedropper begins to take color.
        /// </summary>
        public event TypedEventHandler<EyedropperToolButton, EventArgs> PickStarted;

        /// <summary>
        /// Occurs when the eyedropper stops to take color.
        /// </summary>
        public event TypedEventHandler<EyedropperToolButton, EventArgs> PickCompleted;

        private void HookUpEvents()
        {
            Click -= EyedropperToolButton_Click;
            Click += EyedropperToolButton_Click;
            Unloaded -= EyedropperToolButton_Unloaded;
            Unloaded += EyedropperToolButton_Unloaded;
            ThemeManager.RemoveActualThemeChangedHandler(this, EyedropperToolButton_ActualThemeChanged);
            ThemeManager.AddActualThemeChangedHandler(this, EyedropperToolButton_ActualThemeChanged);

            _eyedropper.ColorChanged -= Eyedropper_ColorChanged;
            _eyedropper.ColorChanged += Eyedropper_ColorChanged;
            _eyedropper.PickStarted -= Eyedropper_PickStarted;
            _eyedropper.PickStarted += Eyedropper_PickStarted;
            _eyedropper.PickCompleted -= Eyedropper_PickCompleted;
            _eyedropper.PickCompleted += Eyedropper_PickCompleted;
        }

        private void UnhookEvents()
        {
            Click -= EyedropperToolButton_Click;
            Unloaded -= EyedropperToolButton_Unloaded;

            ThemeManager.RemoveActualThemeChangedHandler(this, EyedropperToolButton_ActualThemeChanged);

            _eyedropper.ColorChanged -= Eyedropper_ColorChanged;
            _eyedropper.PickStarted -= Eyedropper_PickStarted;
            _eyedropper.PickCompleted -= Eyedropper_PickCompleted;
            if (TargetElement != null)
            {
                TargetElement = null;
            }

            if (EyedropperEnabled)
            {
                EyedropperEnabled = false;
            }
        }

        private void EyedropperToolButton_Loaded(object sender, RoutedEventArgs e)
        {
            HookUpEvents();
        }

        private void EyedropperToolButton_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookEvents();
        }

        private void EyedropperToolButton_ActualThemeChanged(object sender, RoutedEventArgs e)
        {
            ThemeManager.SetRequestedTheme(_eyedropper, ThemeManager.GetActualTheme(this));
        }

        private void Eyedropper_PickStarted(Eyedropper sender, EventArgs args)
        {
            PickStarted?.Invoke(this, args);
        }

        private void Eyedropper_PickCompleted(Eyedropper sender, EventArgs args)
        {
            EyedropperEnabled = false;
            PickCompleted?.Invoke(this, args);
        }

        private void Eyedropper_ColorChanged(Eyedropper sender, EyedropperColorChangedEventArgs args)
        {
            Color = args.NewColor;
            ColorChanged?.Invoke(this, args);
        }

        private void EyedropperToolButton_Click(object sender, RoutedEventArgs e)
        {
            EyedropperEnabled = !EyedropperEnabled;
        }

        private void UpdateEyedropperWorkAreaAsync()
        {
            OwnerWindow ??= Window.GetWindow(this);
            if (TargetElement != null && OwnerWindow != null)
            {
                UIElement content = (UIElement)OwnerWindow.Content;

                var transform = TargetElement.TransformToVisual(content);
                var position = transform.Transform(default);
                _eyedropper.WorkArea = new Rect(position, new Size(TargetElement.ActualWidth, TargetElement.ActualHeight));

                _eyedropper.UpdateAppScreenshot();
            }
        }
    }
}
