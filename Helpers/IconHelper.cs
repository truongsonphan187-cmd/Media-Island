using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace DynamicIslandMedia.Helpers
{
    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private static Icon? _cachedIcon;
        private static Icon? _cachedIconOff;
        private static ImageSource? _cachedFallbackArt;

        /// <summary>
        /// Returns the application icon. First checks Assets/app_icon.ico, and if missing or invalid,
        /// generates a crisp procedural music note Icon.
        /// </summary>
        public static Icon GetAppIcon()
        {
            if (_cachedIcon != null)
                return _cachedIcon;

            try
            {
                // Check relative paths
                string[] potentialPaths =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app_icon.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "app_icon.ico"),
                    "Assets\\app_icon.ico"
                };

                foreach (var path in potentialPaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            _cachedIcon = new Icon(fs);
                            return _cachedIcon;
                        }
                        catch
                        {
                            // If file cannot be read, continue to next or fallback
                        }
                    }
                }
            }
            catch
            {
                // Fall through to procedural generation
            }

            // Procedural generation fallback
            _cachedIcon = GenerateProceduralMusicIcon(32);
            return _cachedIcon;
        }

        /// <summary>
        /// Returns the dynamic tray icon based on master enabled state.
        /// State 1 (ON): Standard clean music note icon.
        /// State 2 (OFF): The same music note icon overlaid with a crisp white diagonal strike-through slash.
        /// </summary>
        public static Icon GetTrayIcon(bool isEnabled)
        {
            if (isEnabled)
            {
                return GetAppIcon();
            }

            if (_cachedIconOff != null)
                return _cachedIconOff;

            try
            {
                var baseIcon = GetAppIcon();
                _cachedIconOff = CreateStrikeThroughIcon(baseIcon);
                return _cachedIconOff;
            }
            catch
            {
                return GetAppIcon();
            }
        }

        /// <summary>
        /// Overlays a crisp white diagonal strike-through / slash line over the base icon to indicate disabled state.
        /// </summary>
        public static Icon CreateStrikeThroughIcon(Icon baseIcon, int size = 32)
        {
            using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Draw base icon
                g.DrawIcon(baseIcon, new Rectangle(0, 0, size, size));

                // Diagonal strike-through line
                float x1 = size * 0.16f;
                float y1 = size * 0.16f;
                float x2 = size * 0.84f;
                float y2 = size * 0.84f;

                // Subtle dark shadow outline behind slash for high-contrast on any Windows theme
                using (var shadowPen = new Pen(Color.FromArgb(220, 15, 15, 20), 4.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawLine(shadowPen, x1, y1, x2, y2);
                }

                // Crisp bright white slash line
                using (var whitePen = new Pen(Color.FromArgb(255, 255, 255, 255), 2.4f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawLine(whitePen, x1, y1, x2, y2);
                }
            }

            IntPtr hIcon = bmp.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(hIcon).Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        /// <summary>
        /// Procedurally generates a Windows 11 style musical note icon with a completely transparent
        /// background and a scaled-up, bold glyph for crisp visibility on the Windows taskbar.
        /// </summary>
        public static Icon GenerateProceduralMusicIcon(int size = 32)
        {
            try
            {
                using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    // Vibrant high-contrast gradient for the note
                    using var noteBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new PointF(0, 0),
                        new PointF(size, size),
                        Color.FromArgb(255, 56, 189, 248), // Bright sky blue
                        Color.FromArgb(255, 192, 132, 252) // Vibrant violet
                    );

                    float s = size / 100f;

                    // Bold tilted note heads (occupying bottom area, scaled up)
                    // Left note head
                    var leftHeadRect = new RectangleF(10f * s, 64f * s, 32f * s, 26f * s);
                    var state = g.Save();
                    g.TranslateTransform(leftHeadRect.X + leftHeadRect.Width / 2, leftHeadRect.Y + leftHeadRect.Height / 2);
                    g.RotateTransform(-20f);
                    g.FillEllipse(noteBrush, -leftHeadRect.Width / 2, -leftHeadRect.Height / 2, leftHeadRect.Width, leftHeadRect.Height);
                    g.Restore(state);

                    // Right note head
                    var rightHeadRect = new RectangleF(58f * s, 50f * s, 32f * s, 26f * s);
                    state = g.Save();
                    g.TranslateTransform(rightHeadRect.X + rightHeadRect.Width / 2, rightHeadRect.Y + rightHeadRect.Height / 2);
                    g.RotateTransform(-20f);
                    g.FillEllipse(noteBrush, -rightHeadRect.Width / 2, -rightHeadRect.Height / 2, rightHeadRect.Width, rightHeadRect.Height);
                    g.Restore(state);

                    // Bold stems
                    float stemWidth = Math.Max(2f, 9.5f * s);
                    using var stemPen = new Pen(noteBrush, stemWidth)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Flat
                    };
                    // Left stem
                    g.DrawLine(stemPen, 38f * s, 72f * s, 38f * s, 16f * s);
                    // Right stem
                    g.DrawLine(stemPen, 86f * s, 58f * s, 86f * s, 6f * s);

                    // Bold connecting beam
                    float beamWidth = Math.Max(3f, 13f * s);
                    using var beamPen = new Pen(noteBrush, beamWidth)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(beamPen, 35f * s, 17f * s, 88f * s, 7f * s);
                }

                IntPtr hIcon = bmp.GetHicon();
                return (Icon)Icon.FromHandle(hIcon).Clone();
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// Provides a clean vector WPF ImageSource used as fallback when media has no album artwork.
        /// </summary>
        public static ImageSource GetFallbackAlbumArt()
        {
            if (_cachedFallbackArt != null)
                return _cachedFallbackArt;

            var group = new DrawingGroup();

            // Background subtle rounded rect
            var bgBrush = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(28, 28, 36),
                System.Windows.Media.Color.FromRgb(16, 16, 22),
                45.0
            );
            group.Children.Add(new GeometryDrawing(
                bgBrush,
                new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)), 1.0),
                new RectangleGeometry(new System.Windows.Rect(0, 0, 100, 100), 16, 16)
            ));

            // Musical note path geometry
            var noteBrush = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(96, 165, 250),
                System.Windows.Media.Color.FromRgb(192, 132, 252),
                45.0
            );

            var pathGeometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new System.Windows.Point(38, 30), IsClosed = true };
            figure.Segments.Add(new LineSegment(new System.Windows.Point(70, 22), true));
            figure.Segments.Add(new LineSegment(new System.Windows.Point(70, 58), true));
            figure.Segments.Add(new ArcSegment(new System.Windows.Point(54, 66), new System.Windows.Size(12, 9), 0, false, SweepDirection.Counterclockwise, true));
            figure.Segments.Add(new ArcSegment(new System.Windows.Point(66, 56), new System.Windows.Size(12, 9), 0, false, SweepDirection.Counterclockwise, true));
            figure.Segments.Add(new LineSegment(new System.Windows.Point(66, 30), true));
            figure.Segments.Add(new LineSegment(new System.Windows.Point(42, 36), true));
            figure.Segments.Add(new LineSegment(new System.Windows.Point(42, 66), true));
            figure.Segments.Add(new ArcSegment(new System.Windows.Point(26, 74), new System.Windows.Size(12, 9), 0, false, SweepDirection.Counterclockwise, true));
            figure.Segments.Add(new ArcSegment(new System.Windows.Point(38, 64), new System.Windows.Size(12, 9), 0, false, SweepDirection.Counterclockwise, true));
            pathGeometry.Figures.Add(figure);

            group.Children.Add(new GeometryDrawing(noteBrush, null, pathGeometry));

            var drawingImage = new DrawingImage(group);
            drawingImage.Freeze();
            _cachedFallbackArt = drawingImage;
            return _cachedFallbackArt;
        }
    }
}
