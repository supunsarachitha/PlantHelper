using System.Globalization;
using PlantHelper.Core.Services;

namespace PlantHelper.Controls;

/// <summary>One line series with an optional "ideal range" band.</summary>
public sealed record ChartSeries(
    IReadOnlyList<DataPoint> Points,
    Color Color,
    string Unit,
    int Decimals = 1,
    double? BandLow = null,
    double? BandHigh = null);

/// <summary>Lightweight line/area chart drawn with MAUI Graphics (no third-party dependency).</summary>
public class ChartView : GraphicsView
{
    public static readonly BindableProperty SeriesProperty =
        BindableProperty.Create(nameof(Series), typeof(ChartSeries), typeof(ChartView), null, propertyChanged: (b, _, _) => ((ChartView)b).Invalidate());

    public static readonly BindableProperty LabelColorProperty =
        BindableProperty.Create(nameof(LabelColor), typeof(Color), typeof(ChartView), Colors.Gray, propertyChanged: (b, _, _) => ((ChartView)b).Invalidate());

    public static readonly BindableProperty GridColorProperty =
        BindableProperty.Create(nameof(GridColor), typeof(Color), typeof(ChartView), Color.FromArgb("#22000000"), propertyChanged: (b, _, _) => ((ChartView)b).Invalidate());

    public ChartView()
    {
        Drawable = new LineChartDrawable(this);
        HeightRequest = 190;
        BackgroundColor = Colors.Transparent;
    }

    public ChartSeries? Series { get => (ChartSeries?)GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }
    public Color LabelColor { get => (Color)GetValue(LabelColorProperty); set => SetValue(LabelColorProperty, value); }
    public Color GridColor { get => (Color)GetValue(GridColorProperty); set => SetValue(GridColorProperty, value); }

    private sealed class LineChartDrawable(ChartView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            var series = owner.Series;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            if (series is null || series.Points.Count < 2)
            {
                canvas.FontColor = owner.LabelColor;
                canvas.FontSize = 13;
                canvas.DrawString("Add at least two readings to see a trend", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
                return;
            }

            var pts = series.Points;
            const float left = 44, right = 16, top = 22, bottom = 26;
            var plot = new RectF(rect.X + left, rect.Y + top, rect.Width - left - right, rect.Height - top - bottom);

            // Y range (include the ideal band so it is always visible)
            var values = pts.Select(p => p.Value).ToList();
            if (series.BandLow is { } bl) values.Add(bl);
            if (series.BandHigh is { } bh) values.Add(bh);
            double min = values.Min(), max = values.Max();
            if (Math.Abs(max - min) < 1e-9)
            {
                var pad0 = Math.Abs(max) < 1e-9 ? 1 : Math.Abs(max) * 0.1;
                min -= pad0;
                max += pad0;
            }
            var pad = (max - min) * 0.12;
            min -= pad;
            max += pad;
            if (pts.All(p => p.Value >= 0) && min < 0 && (series.BandLow ?? 0) >= 0)
                min = 0;

            // X by time (fallback to index if all on one instant)
            var t0 = pts[0].Date.Ticks;
            var t1 = pts[^1].Date.Ticks;
            float X(int i) => t1 == t0
                ? plot.Left + plot.Width * i / (pts.Count - 1)
                : plot.Left + (float)((pts[i].Date.Ticks - t0) / (double)(t1 - t0)) * plot.Width;
            float Y(double v) => plot.Bottom - (float)((v - min) / (max - min)) * plot.Height;

            // Ideal band
            if (series.BandLow is { } lo && series.BandHigh is { } hi)
            {
                canvas.FillColor = Color.FromArgb("#2210B981");
                canvas.FillRectangle(plot.Left, Y(hi), plot.Width, Y(lo) - Y(hi));
            }

            // Grid + Y labels
            canvas.StrokeColor = owner.GridColor;
            canvas.StrokeSize = 1;
            canvas.FontColor = owner.LabelColor;
            canvas.FontSize = 10;
            // Enough decimals that the four grid labels are all different.
            var step = (max - min) / 3;
            var axisDecimals = Math.Max(series.Decimals, step >= 1 ? 0 : step >= 0.1 ? 1 : 2);
            for (var g = 0; g <= 3; g++)
            {
                var v = min + step * g;
                var y = Y(v);
                canvas.DrawLine(plot.Left, y, plot.Right, y);
                canvas.DrawString(Format(v, axisDecimals), rect.X, y - 7, left - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // Area fill
            var area = new PathF();
            area.MoveTo(X(0), plot.Bottom);
            for (var i = 0; i < pts.Count; i++)
                area.LineTo(X(i), Y(pts[i].Value));
            area.LineTo(X(pts.Count - 1), plot.Bottom);
            area.Close();
            // Save/restore: on Android the gradient shader would otherwise stick to later fills and strokes.
            canvas.SaveState();
            canvas.SetFillPaint(new LinearGradientPaint
            {
                StartColor = series.Color.WithAlpha(0.35f),
                EndColor = series.Color.WithAlpha(0.02f),
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
            }, plot);
            canvas.FillPath(area);
            canvas.RestoreState();

            // Line
            var line = new PathF();
            line.MoveTo(X(0), Y(pts[0].Value));
            for (var i = 1; i < pts.Count; i++)
                line.LineTo(X(i), Y(pts[i].Value));
            canvas.StrokeColor = series.Color;
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawPath(line);

            // Points
            for (var i = 0; i < pts.Count; i++)
            {
                var last = i == pts.Count - 1;
                canvas.FillColor = Colors.White;
                canvas.FillCircle(X(i), Y(pts[i].Value), last ? 6 : 4);
                canvas.FillColor = series.Color;
                canvas.FillCircle(X(i), Y(pts[i].Value), last ? 4 : 2.5f);
            }

            // Latest value bubble
            var lx = X(pts.Count - 1);
            var ly = Y(pts[^1].Value);
            var label = $"{Format(pts[^1].Value, series.Decimals)} {series.Unit}".Trim();
            var w = 12 + label.Length * 6.5f;
            var bx = Math.Min(lx - w / 2, rect.Right - w - 2);
            var by = Math.Max(rect.Y, ly - 26);
            canvas.FillColor = series.Color;
            canvas.FillRoundedRectangle(bx, by, w, 18, 9);
            canvas.FontColor = Colors.White;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = 11;
            canvas.DrawString(label, bx, by, w, 18, HorizontalAlignment.Center, VerticalAlignment.Center);

            // X labels
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontColor = owner.LabelColor;
            canvas.FontSize = 10;
            var fy = plot.Bottom + 6;
            canvas.DrawString(pts[0].Date.ToString("d MMM", CultureInfo.CurrentCulture), plot.Left - 20, fy, 60, 14, HorizontalAlignment.Left, VerticalAlignment.Top);
            canvas.DrawString(pts[^1].Date.ToString("d MMM", CultureInfo.CurrentCulture), plot.Right - 40, fy, 56, 14, HorizontalAlignment.Right, VerticalAlignment.Top);
            if (pts.Count > 2 && plot.Width > 200)
            {
                var mid = pts.Count / 2;
                canvas.DrawString(pts[mid].Date.ToString("d MMM", CultureInfo.CurrentCulture), X(mid) - 30, fy, 60, 14, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        private static string Format(double v, int decimals) =>
            Math.Round(v, decimals).ToString(decimals == 0 ? "0" : "0." + new string('#', decimals), CultureInfo.CurrentCulture);
    }
}
