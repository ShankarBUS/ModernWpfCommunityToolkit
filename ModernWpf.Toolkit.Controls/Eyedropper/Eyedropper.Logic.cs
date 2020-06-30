// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
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

            if (WorkArea == default(Rect))
            {
                _targetGrid.Margin = default(Thickness);
            }
            else
            {
                var left = WorkArea.Left;
                var top = WorkArea.Top;
                double right = _window.ActualWidth - WorkArea.Right;
                double bottom = _window.ActualHeight - WorkArea.Bottom;

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
            var right = (int)Math.Min(centerX + halfPixelCountPerRow, _appScreenshot.PixelWidth - 1);
            var bottom = (int)Math.Min(centerY + halfPixelCountPerRow, _appScreenshot.PixelHeight - 1);
            var width = right - left + 1;
            var height = bottom - top + 1;
            //var colors = _appScreenshot.GetPixelColors(left, top, width, height);
            var colorStartX = left - (centerX - halfPixelCountPerRow);
            var colorStartY = top - (centerY - halfPixelCountPerRow);
            var colorEndX = colorStartX + width;
            var colorEndY = colorStartY + height;

            //var size = new Size(PreviewPixelsPerRawPixel, PreviewPixelsPerRawPixel);
            var startPoint = new Point(0, PreviewPixelsPerRawPixel * colorStartY);

            for (var i = colorStartY; i < colorEndY; i++)
            {
                startPoint.X = colorStartX * PreviewPixelsPerRawPixel;
                for (var j = colorStartX; j < colorEndX; j++)
                {
                    var color = _appScreenshot.GetPixelColor(i, j);//colors[((i - colorStartY) * width) + (j - colorStartX)];
                    //drawingSession.FillRectangle(new Rect(startPoint, size), color);
                    var pixelColor = new PixelColor[1,1];
                    pixelColor[0, 0] = new PixelColor() { Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B };
                    _previewImageSource.SetPixels(pixelColor, i, j);
                    startPoint.X += PreviewPixelsPerRawPixel;
                }

                startPoint.Y += PreviewPixelsPerRawPixel;
            }

            //using (var drawingSession = _previewImageSource.CreateDrawingSession(Colors.White))
            //{
            //    for (var i = colorStartY; i < colorEndY; i++)
            //    {
            //        startPoint.X = colorStartX * PreviewPixelsPerRawPixel;
            //        for (var j = colorStartX; j < colorEndX; j++)
            //        {
            //            var color = colors[((i - colorStartY) * width) + (j - colorStartX)];
            //            drawingSession.FillRectangle(new Rect(startPoint, size), color);
            //            startPoint.X += PreviewPixelsPerRawPixel;
            //        }

            //        startPoint.Y += PreviewPixelsPerRawPixel;
            //    }
            //}
        }

        internal void UpdateAppScreenshotAsync()
        {
            //var displayInfo = DisplayInformation.GetForCurrentView();
            double dpiX = 96;
            double dpiY = 96;
            double scale = 1;//displayInfo.RawPixelsPerViewPixel;
            double width = _window.ActualWidth;
            double height = _window.ActualHeight;
            FrameworkElement content = (FrameworkElement)_window.Content;

            try
            {
                var scaleWidth = (int)Math.Ceiling(width / scale);
                var scaleHeight = (int)Math.Ceiling(height / scale);
                var renderTargetBitmap = new RenderTargetBitmap(
                    (int)content.ActualWidth,
                    (int)content.ActualHeight,
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

    public static class GraphicsHelpers
    {
        public static Color GetPixelColor(this BitmapFrame bitmapFrame, double x, double y)
        {
            //var renderTargetBitmap = new RenderTargetBitmap(
            //    (int)AssociatedObject.ActualWidth,
            //    (int)AssociatedObject.ActualHeight,
            //    DpiX, DpiY, PixelFormats.Default);
            //renderTargetBitmap.Render(AssociatedObject);
            if (x <= bitmapFrame.PixelWidth && y <= bitmapFrame.PixelHeight)
            {
                var croppedBitmap = new CroppedBitmap(bitmapFrame, new Int32Rect((int)x, (int)y, 1, 1));
                var pixels = new byte[4];
                croppedBitmap.CopyPixels(pixels, 4, 0);
                return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
            }
            return Colors.Transparent;
        }

        public static void SetPixels(this WriteableBitmap bitmap, PixelColor[,] pixels, int x, int y)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, x, y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelColor
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Alpha;
    }
}
