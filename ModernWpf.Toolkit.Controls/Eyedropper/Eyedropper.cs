// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        private readonly Grid _rootGrid;
        private readonly Grid _targetGrid;

        private Window _overlayWindow;
        private BitmapFrame _appScreenshot;
        private Action _lazyTask;
        private InputDevice _inputDevice;
        private TaskCompletionSource<Color> _taskSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="Eyedropper"/> class.
        /// </summary>
        public Eyedropper()
        {
            DefaultStyleKey = typeof(Eyedropper);
            _rootGrid = new Grid();
            _targetGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00))
            };

            OwnerWindow ??= Application.Current?.MainWindow;

            RenderTransform = _layoutTransform;
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
                    UpdateAppScreenshot();
                    UpdateEyedropper(startPoint.Value);
                    Opacity = 1;
                };
            }

            _rootGrid.Children.Add(_targetGrid);
            _rootGrid.Children.Add(this);

            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
            }

            if (OwnerWindow == null)
            {
                throw new NullReferenceException(nameof(OwnerWindow) + " must not be null!");
            }

            _overlayWindow = new Window()
            {
                WindowStyle = WindowStyle.None,
                Style = null,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Content = _rootGrid,
                Owner = OwnerWindow,
                ShowInTaskbar = false,
                ShowActivated = false
            };

            FrameworkElement content = (FrameworkElement)OwnerWindow.Content;

            _overlayWindow.Width = content.ActualWidth;
            _overlayWindow.Height = content.ActualHeight;

            var point = content.PointToScreen(default);
            _overlayWindow.Left = point.X; _overlayWindow.Top = point.Y;

            _overlayWindow.SourceInitialized += OverlayWindow_SourceInitialized;

            UpdateWorkArea();
            _overlayWindow.Show();

            var result = await _taskSource.Task;
            _taskSource = null;

            UnhookOverlayWndProc();
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

            OwnerWindow.SizeChanged -= Window_SizeChanged;
            OwnerWindow.SizeChanged += Window_SizeChanged;
            OwnerWindow.LocationChanged -= Window_LocationChanged;
            OwnerWindow.LocationChanged += Window_LocationChanged;
            OwnerWindow.DpiChanged -= Window_DpiChanged;
            OwnerWindow.DpiChanged += Window_DpiChanged;

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

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            FrameworkElement content = (FrameworkElement)OwnerWindow.Content;

            var point = content.PointToScreen(default);
            _overlayWindow.Left = point.X; _overlayWindow.Top = point.Y;
        }

        private void UnhookEvents()
        {
            UnhookWindowEvents(OwnerWindow);

            if (_targetGrid != null)
            {
                _targetGrid.MouseEnter -= TargetGrid_MouseEnter;
                _targetGrid.MouseLeave -= TargetGrid_MouseLeave;
                _targetGrid.MouseDown -= TargetGrid_MouseDown;
                _targetGrid.MouseMove -= TargetGrid_MouseMove;
                _targetGrid.MouseUp -= TargetGrid_MouseUp;
            }
        }

        private void UnhookWindowEvents(Window window)
        {
            window.SizeChanged -= Window_SizeChanged;
            window.LocationChanged -= Window_LocationChanged;
            window.DpiChanged -= Window_DpiChanged;
        }

        private void Eyedropper_Loaded(object sender, RoutedEventArgs e)
        {
            _lazyTask?.Invoke();
            _lazyTask = null;
        }

        private void TargetGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Cursor = DefaultCursor;
            }

            if (_inputDevice != null)
            {
                _inputDevice = null;
                Opacity = 0;
            }
        }

        private void TargetGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Cursor = MoveCursor;
            }
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            UpdateAppScreenshot();
        }

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
                    UpdateAppScreenshot();
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
            UpdateAppScreenshot();
            UpdateEyedropper(position);

            if (Opacity < 1)
            {
                Opacity = 1;
            }
        }

        private void Eyedropper_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookEvents();
            if (_overlayWindow != null)
            {
                _overlayWindow.Cursor = DefaultCursor;
                UnhookOverlayWndProc();
                _overlayWindow.Close();
                _overlayWindow = null;
            }

            _appScreenshot = null;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_overlayWindow != null)
            {
                FrameworkElement content = (FrameworkElement)OwnerWindow.Content;

                _overlayWindow.Width = content.ActualWidth;
                _overlayWindow.Height = content.ActualHeight;
            }
        }

        private void UpdateOwnerWindow(Window oldWindow)
        {
            if (oldWindow != null)
            {
                Close();
                UnhookWindowEvents(oldWindow);
            }
        }

        #region Overlay Window

        private void OverlayWindow_SourceInitialized(object sender, EventArgs e)
        {
            _overlayWindow.SourceInitialized -= OverlayWindow_SourceInitialized;
            HookOverlayWndProc();
        }

        private void HookOverlayWndProc()
        {
            var wih = new WindowInteropHelper(_overlayWindow);
            var hWnd = wih.Handle;
            var source = HwndSource.FromHwnd(hWnd);
            source.AddHook(WndProc);
        }

        private void UnhookOverlayWndProc()
        {
            var wih = new WindowInteropHelper(_overlayWindow);
            var hWnd = wih.Handle;
            var source = HwndSource.FromHwnd(hWnd);
            source.RemoveHook(WndProc);
        }

        private const int MA_NOACTIVATE = 0x3;
        private const int WM_MOUSEACTIVATE = 0x21;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        }

        #endregion
    }
}
