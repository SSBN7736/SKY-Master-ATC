using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SkyMasterATC.Core.Models;
using SkyMasterATC.Core.Services;

namespace SkyMasterATC.App.Controls
{
    /// <summary>
    /// WPF radar display.  Renders range rings, cardinal labels, and per-aircraft
    /// data tags on a dark canvas.  Supports mouse-wheel zoom and mouse-drag pan.
    /// </summary>
    public partial class RadarControl : UserControl
    {
        // ── Dependency properties ─────────────────────────────────────────────────

        public static readonly DependencyProperty CenterLatitudeProperty =
            DependencyProperty.Register(nameof(CenterLatitude), typeof(double), typeof(RadarControl),
                new PropertyMetadata(51.4775, OnViewChanged));

        public static readonly DependencyProperty CenterLongitudeProperty =
            DependencyProperty.Register(nameof(CenterLongitude), typeof(double), typeof(RadarControl),
                new PropertyMetadata(-0.4614, OnViewChanged));

        public static readonly DependencyProperty RangeNmProperty =
            DependencyProperty.Register(nameof(RangeNm), typeof(double), typeof(RadarControl),
                new PropertyMetadata(80.0, OnViewChanged));

        public double CenterLatitude
        {
            get => (double)GetValue(CenterLatitudeProperty);
            set => SetValue(CenterLatitudeProperty, value);
        }

        public double CenterLongitude
        {
            get => (double)GetValue(CenterLongitudeProperty);
            set => SetValue(CenterLongitudeProperty, value);
        }

        public double RangeNm
        {
            get => (double)GetValue(RangeNmProperty);
            set => SetValue(RangeNmProperty, Math.Max(5, Math.Min(500, value)));
        }

        private static void OnViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((RadarControl)d).Redraw();

        // ── State ─────────────────────────────────────────────────────────────────

        private IReadOnlyList<RadarTrack> _tracks = [];
        private uint _selectedObjectId;
        private Point _dragStart;
        private bool _isDragging;
        private double _dragStartLat, _dragStartLon;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the user clicks a radar track data tag.</summary>
        public event EventHandler<SimAircraft>? TrackSelected;

        // ── Construction ──────────────────────────────────────────────────────────

        public RadarControl()
        {
            InitializeComponent();
            SizeChanged += (_, _) => Redraw();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Replaces the current track list and redraws.</summary>
        public void UpdateTracks(IReadOnlyList<RadarTrack> tracks)
        {
            _tracks = tracks;
            Dispatcher.Invoke(Redraw);
        }

        /// <summary>Marks the given object as selected and redraws.</summary>
        public void SelectTrack(uint objectId)
        {
            _selectedObjectId = objectId;
            Dispatcher.Invoke(Redraw);
        }

        // ── Mouse interaction ─────────────────────────────────────────────────────

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            double factor = e.Delta > 0 ? 0.85 : 1.15;
            RangeNm = Math.Max(5, Math.Min(500, RangeNm * factor));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _dragStart    = e.GetPosition(this);
            _dragStartLat = CenterLatitude;
            _dragStartLon = CenterLongitude;
            _isDragging   = true;
            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isDragging) return;

            var pos   = e.GetPosition(this);
            var dx    = pos.X - _dragStart.X;
            var dy    = pos.Y - _dragStart.Y;
            double scale = 1.0 / NmScale;  // degrees-per-pixel equivalent via NmScale
            double cosLat = Math.Cos(_dragStartLat * Math.PI / 180.0);

            CenterLatitude  = _dragStartLat + (dy * scale) / 60.0;
            CenterLongitude = _dragStartLon - (dx * scale) / (60.0 * Math.Max(cosLat, 0.01));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _isDragging = false;
            ReleaseMouseCapture();
        }

        // ── Rendering ─────────────────────────────────────────────────────────────

        /// <summary>Pixels per nautical mile based on the current view size and range.</summary>
        private double NmScale => Math.Min(ActualWidth, ActualHeight) / 2.0 / RangeNm;

        private void Redraw()
        {
            RadarCanvas.Children.Clear();
            if (ActualWidth < 10 || ActualHeight < 10) return;

            DrawRings();
            DrawCardinalLabels();

            foreach (var track in _tracks)
                DrawTrack(track);
        }

        private void DrawRings()
        {
            double cx = ActualWidth  / 2.0;
            double cy = ActualHeight / 2.0;
            double scale = NmScale;

            var ringBrush = new SolidColorBrush(Color.FromArgb(80, 100, 100, 120));

            for (int i = 1; i <= 4; i++)
            {
                double radiusPx = (RangeNm / 4.0 * i) * scale;
                var ellipse = new Ellipse
                {
                    Width           = radiusPx * 2,
                    Height          = radiusPx * 2,
                    Stroke          = ringBrush,
                    StrokeThickness = 1,
                };
                Canvas.SetLeft(ellipse, cx - radiusPx);
                Canvas.SetTop(ellipse, cy - radiusPx);
                RadarCanvas.Children.Add(ellipse);

                // Range label
                var lbl = MakeText($"{RangeNm / 4 * i:F0} NM", 9, Brushes.Gray);
                Canvas.SetLeft(lbl, cx + 4);
                Canvas.SetTop(lbl, cy - radiusPx - 14);
                RadarCanvas.Children.Add(lbl);
            }

            // Cross-hair lines
            var lineBrush = new SolidColorBrush(Color.FromArgb(50, 100, 100, 120));
            AddLine(cx, 0, cx, ActualHeight, lineBrush);
            AddLine(0, cy, ActualWidth, cy, lineBrush);
        }

        private void DrawCardinalLabels()
        {
            double cx = ActualWidth  / 2.0;
            double cy = ActualHeight / 2.0;
            double edge = Math.Min(cx, cy) - 12;

            AddCardinal("N", cx, cy - edge);
            AddCardinal("S", cx, cy + edge - 14);
            AddCardinal("E", cx + edge - 10, cy);
            AddCardinal("W", cx - edge, cy);
        }

        private void AddCardinal(string text, double x, double y)
        {
            var tb = MakeText(text, 11, Brushes.DimGray);
            Canvas.SetLeft(tb, x - 5);
            Canvas.SetTop(tb, y);
            RadarCanvas.Children.Add(tb);
        }

        private void DrawTrack(RadarTrack track)
        {
            var (sx, sy) = RadarRendererService.LatLonToScreen(
                track.Aircraft.Latitude, track.Aircraft.Longitude,
                CenterLatitude, CenterLongitude,
                RangeNm, ActualWidth, ActualHeight);

            bool selected = track.Aircraft.ObjectId == _selectedObjectId;
            var dotColor  = selected
                ? Color.FromRgb(0xA6, 0xE3, 0xA1)   // green
                : Color.FromRgb(0xCD, 0xD6, 0xF4);   // white-ish

            // Dot
            const double dotR = 4;
            var dot = new Ellipse
            {
                Width  = dotR * 2,
                Height = dotR * 2,
                Fill   = new SolidColorBrush(dotColor),
            };
            Canvas.SetLeft(dot, sx - dotR);
            Canvas.SetTop(dot, sy - dotR);
            RadarCanvas.Children.Add(dot);

            // Data tag line
            double tagX = sx + 12;
            double tagY = sy - 20;
            AddLine(sx, sy, tagX, tagY, new SolidColorBrush(Color.FromArgb(160, 150, 150, 170)));

            // Data tag text
            var ac  = track.Aircraft;
            int alt = (int)(ac.AltitudeFt / 100);
            int spd = (int)ac.SpeedKnots;
            string tagText = $"{ac.Callsign}\n{alt:D3} {spd:D3}\n{ac.Squawk}";

            var border = new Border
            {
                Background      = new SolidColorBrush(Color.FromArgb(180, 17, 17, 27)),
                BorderBrush     = new SolidColorBrush(selected
                    ? Color.FromRgb(0xA6, 0xE3, 0xA1)
                    : Color.FromArgb(80, 100, 100, 120)),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(3, 1, 3, 1),
                Child = new TextBlock
                {
                    Text       = tagText,
                    Foreground = new SolidColorBrush(dotColor),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 10,
                },
                Cursor = Cursors.Hand,
                Tag    = track.Aircraft,
            };
            border.MouseLeftButtonDown += OnTagClicked;

            Canvas.SetLeft(border, tagX);
            Canvas.SetTop(border, tagY);
            RadarCanvas.Children.Add(border);
        }

        private void OnTagClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border { Tag: SimAircraft ac })
            {
                _selectedObjectId = ac.ObjectId;
                TrackSelected?.Invoke(this, ac);
                Redraw();
            }
            e.Handled = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void AddLine(double x1, double y1, double x2, double y2, Brush brush)
        {
            var line = new Line
            {
                X1              = x1,
                Y1              = y1,
                X2              = x2,
                Y2              = y2,
                Stroke          = brush,
                StrokeThickness = 1,
            };
            RadarCanvas.Children.Add(line);
        }

        private static TextBlock MakeText(string text, double size, Brush brush) =>
            new()
            {
                Text       = text,
                Foreground = brush,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize   = size,
            };
    }
}
