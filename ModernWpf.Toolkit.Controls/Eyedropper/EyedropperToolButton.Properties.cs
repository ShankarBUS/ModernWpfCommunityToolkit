// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ModernWpf.Controls.Primitives;
using System;
using System.Windows;
using System.Windows.Media;

namespace ModernWpf.Toolkit.Controls
{
    /// <summary>
    /// The <see cref="EyedropperToolButton"/> control helps use <see cref="Eyedropper"/> in view.
    /// </summary>
    public partial class EyedropperToolButton
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(EyedropperToolButton), new PropertyMetadata(default(Color)));

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            ControlHelper.CornerRadiusProperty.AddOwner(typeof(EyedropperToolButton));

        /// <summary>
        /// Identifies the <see cref="EyedropperEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EyedropperEnabledProperty =
            DependencyProperty.Register(nameof(EyedropperEnabled), typeof(bool), typeof(EyedropperToolButton), new PropertyMetadata(false, OnEyedropperEnabledChanged));

        /// <summary>
        /// Identifies the <see cref="EyedropperStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EyedropperStyleProperty =
            DependencyProperty.Register(nameof(EyedropperStyle), typeof(Style), typeof(EyedropperToolButton), new PropertyMetadata(default(Style), OnEyedropperStyleChanged));

        /// <summary>
        /// Identifies the <see cref="TargetElement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.Register(nameof(TargetElement), typeof(FrameworkElement), typeof(EyedropperToolButton), new PropertyMetadata(default(FrameworkElement), OnTargetElementChanged));

        /// <summary>
        /// Identifies the <see cref="OwnerWindow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OwnerWindowProperty =
            Eyedropper.OwnerWindowProperty.AddOwner(typeof(EyedropperToolButton), new PropertyMetadata(null, OnOwnerWindowChanged));

        /// <summary>
        /// Gets the current color value.
        /// </summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            private set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of this button.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether eyedropper is opened.
        /// </summary>
        public bool EyedropperEnabled
        {
            get => (bool)GetValue(EyedropperEnabledProperty);
            set => SetValue(EyedropperEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the eyedropper.
        /// </summary>
        public Style EyedropperStyle
        {
            get => (Style)GetValue(EyedropperStyleProperty);
            set => SetValue(EyedropperStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the working target element of the eyedropper.
        /// </summary>
        public FrameworkElement TargetElement
        {
            get => (FrameworkElement)GetValue(TargetElementProperty);
            set => SetValue(TargetElementProperty, value);
        }

        /// <summary>
        /// Gets or sets the owner window of the eyedropper.
        /// </summary>
        public Window OwnerWindow
        {
            get => (Window)GetValue(OwnerWindowProperty);
            set => SetValue(OwnerWindowProperty, value);
        }

        private static void OnEyedropperEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EyedropperToolButton eyedropperToolButton)
            {
                if (eyedropperToolButton.EyedropperEnabled)
                {
                    eyedropperToolButton._eyedropper.Open().ConfigureAwait(false);
                }
                else
                {
                    eyedropperToolButton._eyedropper.Close();
                }
            }
        }

        private static void OnEyedropperStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EyedropperToolButton eyedropperToolButton)
            {
                eyedropperToolButton._eyedropper.Style = eyedropperToolButton.EyedropperStyle;
            }
        }

        private static void OnTargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EyedropperToolButton eyedropperToolButton)
            {
                eyedropperToolButton.UnhookTargetElementEvents(e.OldValue as FrameworkElement);
                eyedropperToolButton.HookUpTargetElementEvents(e.NewValue as FrameworkElement);

                //if (e.NewValue == null)
                //{
                //    return;
                //}

                //if (eyedropperToolButton.OwnerWindow == Window.GetWindow(e.NewValue as FrameworkElement))
                //{
                //    eyedropperToolButton.HookUpTargetElementEvents(e.NewValue as FrameworkElement);
                //}
                //else
                //{
                //    eyedropperToolButton.TargetElement = null;
                //}
            }
        }

        private void UnhookTargetElementEvents(FrameworkElement target)
        {
            if (target != null)
            {
                target.SizeChanged -= Target_SizeChanged;
                target.MouseEnter -= Target_MouseEnter;
            }
        }

        private void HookUpTargetElementEvents(FrameworkElement target)
        {
            if (target != null)
            {
                target.SizeChanged += Target_SizeChanged;
                target.MouseEnter += Target_MouseEnter;
            }
        }

        private void Target_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateEyedropperWorkAreaAsync();
        }

        private void Target_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateEyedropperWorkAreaAsync();
        }

        private static void OnOwnerWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EyedropperToolButton eyedropperToolButton)
            {
                eyedropperToolButton._eyedropper.OwnerWindow = (Window)e.NewValue;
            }
        }
    }
}
