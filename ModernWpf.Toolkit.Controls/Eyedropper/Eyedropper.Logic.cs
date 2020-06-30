// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModernWpf.Toolkit.Controls
{
    /// <summary>
    /// The <see cref="Eyedropper"/> control can pick up a color from anywhere in your application.
    /// </summary>
    public partial class Eyedropper
    {
        private void UpdateEyedropper(Point position)
        {
            if (_appScreenshot == null)
            {
                return;
            }

            _layoutTransform.X = position.X - (ActualWidth / 2);
            _layoutTransform.Y = position.Y - ActualHeight;

            var x = (int)Math.Ceiling(Math.Min(_appScreenshot.PixelWidth - 1, Math.Max(position.X, 0)));
            var y = (int)Math.Ceiling(Math.Min(_appScreenshot.PixelHeight - 1, Math.Max(position.Y, 0)));
            Color = _appScreenshot.GetPixelColor(x, y);
            UpdatePreview(x, y);
        }

        private void UpdateWorkArea()
        {
            if (_targetGrid == null)
            {
                return;
            }

            var content = (FrameworkElement)_window.Content;
            if (WorkArea == default)
            {
                _targetGrid.Margin = default;
            }
            else
            {
                var left = WorkArea.Left;
                var top = WorkArea.Top;
                double right = content.ActualWidth - WorkArea.Right;
                double bottom = content.ActualHeight - WorkArea.Bottom;

                _targetGrid.Margin = new Thickness(left, top, right, bottom);
            }
        }

        private void UpdatePreview(int centerX, int centerY)
        {
            var halfPixelCountPerRow = (PixelCountPerRow - 1) / 2;
            var left = Math.Min(
                _appScreenshot.PixelWidth - 1,
                Math.Max(centerX - halfPixelCountPerRow, 0));
            var top = Math.Min(
                _appScreenshot.PixelHeight - 1,
                Math.Max(centerY - halfPixelCountPerRow, 0));
            var right = Math.Min(centerX + halfPixelCountPerRow, _appScreenshot.PixelWidth - 1);
            var bottom = Math.Min(centerY + halfPixelCountPerRow, _appScreenshot.PixelHeight - 1);
            var width = right - left + 1;
            var height = bottom - top + 1;

            var startX = centerX - halfPixelCountPerRow;
            var startY = centerY - halfPixelCountPerRow;

            var colorStartX = left - startX;
            var colorStartY = top - startY;
            var colorEndX = colorStartX + width;
            var colorEndY = colorStartY + height;

            var size = new Size(PreviewPixelsPerRawPixel, PreviewPixelsPerRawPixel);
            var startPoint = new Point(0, PreviewPixelsPerRawPixel * colorStartY);

            var drawingGroup = new DrawingGroup();

            for (var i = colorStartY; i < colorEndY; i++)
            {
                startPoint.X = colorStartX * PreviewPixelsPerRawPixel;
                for (var j = colorStartX; j < colorEndX; j++)
                {
                    var color = _appScreenshot.GetPixelColor(startX + j, startY + i);

                    var rectangleGeometry = new RectangleGeometry() { Rect = new Rect(startPoint, size) };

                    var geometryDrawing = new GeometryDrawing(new SolidColorBrush(color), new Pen(Brushes.Transparent, 0.0), rectangleGeometry);
                    drawingGroup.Children.Add(geometryDrawing);
                    startPoint.X += PreviewPixelsPerRawPixel;
                }

                startPoint.Y += PreviewPixelsPerRawPixel;
            }

            var drawingImage = new DrawingImage(drawingGroup);
            drawingImage.Freeze();
            Preview = drawingImage;
        }

        internal void UpdateAppScreenshot()
        {
            //var displayInfo = DisplayInformation.GetForCurrentView();
            double dpiX = 96;
            double dpiY = 96;
            double scale = 1;//displayInfo.RawPixelsPerViewPixel;
            
            FrameworkElement content = (FrameworkElement)_window.Content;
            double width = content.ActualWidth;
            double height = content.ActualHeight;

            try
            {
                var scaleWidth = (int)Math.Ceiling(width / scale);
                var scaleHeight = (int)Math.Ceiling(height / scale);
                var renderTargetBitmap = new RenderTargetBitmap(
                    scaleWidth,
                    scaleHeight,
                    dpiX, dpiY, PixelFormats.Default);
                renderTargetBitmap.Render(content);

                _appScreenshot = BitmapFrame.Create(renderTargetBitmap);
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    internal static class GraphicsHelpers
    {
        public static Color GetPixelColor(this BitmapFrame bitmapFrame, double x, double y)
        {
            if (x <= bitmapFrame.PixelWidth && y <= bitmapFrame.PixelHeight)
            {
                var croppedBitmap = new CroppedBitmap(bitmapFrame, new Int32Rect((int)x, (int)y, 1, 1));
                var pixels = new byte[4];
                croppedBitmap.CopyPixels(pixels, 4, 0);
                return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
            }
            return Colors.Transparent;
        }
    }
}
