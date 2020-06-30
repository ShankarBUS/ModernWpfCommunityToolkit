// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ModernWpf.Controls.Primitives;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModernWpf.Toolkit.Controls
{
    /// <summary>
    /// The <see cref="Eyedropper"/> control can pick up a color from anywhere in your application.
    /// </summary>
    public partial class Eyedropper : Control
    {
        private const int PreviewPixelsPerRawPixel = 10;
        private const int PixelCountPerRow = 11;
        private static readonly Cursor DefaultCursor = Cursors.Arrow;
        private static readonly Cursor MoveCursor = Cursors.Cross;
        private readonly TranslateTransform _layoutTransform = new TranslateTransform();
        private readonly WriteableBitmap _previewImageSource;
        private readonly Grid _rootGrid;
        private readonly Grid _targetGrid;

        private Popup _popup;
        private Window _overlayWindow;
        private BitmapFrame _appScreenshot;
        private Action _lazyTask;
        private InputDevice _inputDevice;
        private TaskCompletionSource<Color> _taskSource;
        private double _currentDpi;
        private Window _window;

        /// <summary>
        /// Initializes a new instance of the <see cref="Eyedropper"/> class.
        /// </summary>
        public Eyedropper()
        {
            DefaultStyleKey = typeof(Eyedropper);
            _rootGrid = new Grid();
            _targetGrid = new Grid
            {
                Background = Brushes.Transparent
            };

            _window = Application.Current?.MainWindow;

            RenderTransform = _layoutTransform;
            _previewImageSource = new WriteableBitmap(
                PreviewPixelsPerRawPixel * PixelCountPerRow,
                PreviewPixelsPerRawPixel * PixelCountPerRow,
                96, 96,
                PixelFormats.Bgra32, null);
            Preview = _previewImageSource;
            Loaded += Eyedropper_Loaded;
        }

        /// <summary>
        /// Occurs when the Color property has changed.
        /// </summary>
        public event TypedEventHandler<Eyedropper, EyedropperColorChangedEventArgs> ColorChanged;

        /// <summary>
        /// Occurs when the eyedropper begins to take color.
        /// </summary>
        public event TypedEventHandler<Eyedropper, EventArgs> PickStarted;

        /// <summary>
        /// Occurs when the eyedropper stops to take color.
        /// </summary>
        public event TypedEventHandler<Eyedropper, EventArgs> PickCompleted;

        /// <summary>
        /// Open the eyedropper.
        /// </summary>
        /// <param name="startPoint">The initial eyedropper position</param>
        /// <returns>The picked color.</returns>
        public async Task<Color> Open(Point? startPoint = null)
        {
            _taskSource = new TaskCompletionSource<Color>();
            HookUpEvents();
            Opacity = 0;
            if (startPoint.HasValue)
            {
                _lazyTask = () =>
                {
                    UpdateAppScreenshotAsync();
                    UpdateEyedropper(startPoint.Value);
                    Opacity = 1;
                };
            }

            _rootGrid.Children.Add(_targetGrid);
            _rootGrid.Children.Add(this);

            //if (_popup != null)
            //{
            //    _popup.IsOpen = false;
            //}

            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
            }

            _overlayWindow = new Window()
            {
                WindowStyle = WindowStyle.None,
                Style = null,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Left = _window.Left,
                Top = _window.Top,
                Content = _rootGrid
            };

            //_popup = new Popup
            //{
            //    Child = _rootGrid,
            //    AllowsTransparency = false,
            //    PlacementTarget = _window,
            //    Placement = PlacementMode.Top
            //};

            _overlayWindow.Width = _window.ActualWidth;
            _overlayWindow.Height = _window.ActualHeight;

            UpdateWorkArea();
            _overlayWindow.Show();
            var result = await _taskSource.Task;
            _taskSource = null;
            _overlayWindow.Close();
            _overlayWindow = null;
            _rootGrid.Children.Clear();
            return result;
        }

        /// <summary>
        /// Close the eyedropper.
        /// </summary>
        public void Close()
        {
            if (_taskSource != null && !_taskSource.Task.IsCanceled)
            {
                _taskSource.TrySetCanceled();
                _rootGrid.Children.Clear();
            }
        }

        private void HookUpEvents()
        {
            Unloaded -= Eyedropper_Unloaded;
            Unloaded += Eyedropper_Unloaded;

            _window.SizeChanged -= Window_SizeChanged;
            _window.SizeChanged += Window_SizeChanged;
            //var displayInformation = DisplayInformation.GetForCurrentView();
            //displayInformation.DpiChanged -= Eyedropper_DpiChanged;
            //displayInformation.DpiChanged += Eyedropper_DpiChanged;
            //_currentDpi = displayInformation.LogicalDpi;

            _targetGrid.MouseEnter -= TargetGrid_MouseEnter;
            _targetGrid.MouseEnter += TargetGrid_MouseEnter;
            _targetGrid.MouseLeave -= TargetGrid_MouseLeave;
            _targetGrid.MouseLeave += TargetGrid_MouseLeave;
            _targetGrid.MouseDown -= TargetGrid_MouseDown;
            _targetGrid.MouseDown += TargetGrid_MouseDown;
            _targetGrid.MouseMove -= TargetGrid_MouseMove;
            _targetGrid.MouseMove += TargetGrid_MouseMove;
            _targetGrid.MouseUp -= TargetGrid_MouseUp;
            _targetGrid.MouseUp += TargetGrid_MouseUp;
        }

        private void UnhookEvents()
        {
            _window.SizeChanged -= Window_SizeChanged;
            //DisplayInformation.GetForCurrentView().DpiChanged -= Eyedropper_DpiChanged;

            if (_targetGrid != null)
            {
                _targetGrid.MouseEnter -= TargetGrid_MouseEnter;
                _targetGrid.MouseLeave -= TargetGrid_MouseLeave;
                _targetGrid.MouseDown -= TargetGrid_MouseDown;
                _targetGrid.MouseMove -= TargetGrid_MouseMove;
                _targetGrid.MouseUp -= TargetGrid_MouseUp;
            }
        }

        private void Eyedropper_Loaded(object sender, RoutedEventArgs e)
        {
            _lazyTask?.Invoke();
            _lazyTask = null;
        }

        private void TargetGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            _window.Cursor = DefaultCursor;
            if (_inputDevice != null)
            {
                _inputDevice = null;
                Opacity = 0;
            }
        }

        private void TargetGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            _window.Cursor = MoveCursor;
        }

        //private async void Eyedropper_DpiChanged(DisplayInformation sender, object args)
        //{
        //    _currentDpi = sender.LogicalDpi;
        //    await UpdateAppScreenshotAsync();
        //}

        private void TargetGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(_rootGrid);
            InternalMouseUp(e.Device, point);
        }

        internal void InternalMouseUp(InputDevice inputDevice, Point position)
        {
            if (inputDevice == _inputDevice)
            {
                if (_appScreenshot == null)
                {
                    UpdateAppScreenshotAsync();
                }

                UpdateEyedropper(position);
                _inputDevice = null;
                if (_taskSource != null && !_taskSource.Task.IsCanceled)
                {
                    _taskSource.TrySetResult(Color);
                }

                PickCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void TargetGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(_rootGrid);
            InternalMouseMove(e.Device, point);
        }

        internal void InternalMouseMove(InputDevice inputDevice, Point position)
        {
            if (inputDevice == _inputDevice)
            {
                UpdateEyedropper(position);
            }
        }

        private void TargetGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(_rootGrid);
            InternalMouseDownAsync(e.Device, point);
        }

        internal void InternalMouseDownAsync(InputDevice inputDevice, Point position)
        {
            _inputDevice = inputDevice;
            PickStarted?.Invoke(this, EventArgs.Empty);
            UpdateAppScreenshotAsync();
            UpdateEyedropper(position);

            if (Opacity < 1)
            {
                Opacity = 1;
            }
        }

        private void Eyedropper_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookEvents();
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }

            _appScreenshot = null;

            _window.Cursor = DefaultCursor;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRootGridSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void UpdateRootGridSize(double width, double height)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Width = width;
                _overlayWindow.Height = height;
            }
        }
    }
}
