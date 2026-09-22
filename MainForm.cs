using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace A4ImageCutter
{
    internal sealed class PageMargins
    {
        public float Left = 10f;
        public float Top = 10f;
        public float Right = 10f;
        public float Bottom = 10f;

        public float ToPixels(float millimeters)
        {
            return millimeters / 25.4f * 300f;
        }

        public RectangleF GetPrintableRectangle()
        {
            float left = ToPixels(Left);
            float top = ToPixels(Top);
            float right = ToPixels(Right);
            float bottom = ToPixels(Bottom);
            return RectangleF.FromLTRB(left, top,
                Math.Max(left, ImageDocument.PageWidth - right),
                Math.Max(top, ImageDocument.PageHeight - bottom));
        }

        public float PrintableWidth
        {
            get { return GetPrintableRectangle().Width; }
        }

        public float PrintableHeight
        {
            get { return GetPrintableRectangle().Height; }
        }
    }

    internal sealed class MarginSettingsDialog : Form
    {
        private readonly NumericUpDown leftBox = CreateBox();
        private readonly NumericUpDown topBox = CreateBox();
        private readonly NumericUpDown rightBox = CreateBox();
        private readonly NumericUpDown bottomBox = CreateBox();

        public MarginSettingsDialog(PageMargins margins)
        {
            Text = "여백 설정";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(470, 350);

            Panel preview = new Panel
            {
                Location = new Point(160, 75),
                Size = new Size(150, 180),
                BackColor = Color.FromArgb(238, 238, 238)
            };
            preview.Paint += delegate(object sender, PaintEventArgs e)
            {
                const float pageWidth = 100f;
                const float pageHeight = pageWidth * 297f / 210f;
                RectangleF page = new RectangleF(
                    (preview.ClientSize.Width - pageWidth) / 2f,
                    (preview.ClientSize.Height - pageHeight) / 2f,
                    pageWidth, pageHeight);
                e.Graphics.FillRectangle(Brushes.White, page);
                e.Graphics.DrawRectangle(Pens.Black, page.X, page.Y, page.Width, page.Height);
                float left = Math.Min(page.Width, page.Width * (float)leftBox.Value / 210f);
                float right = Math.Min(page.Width, page.Width * (float)rightBox.Value / 210f);
                float top = Math.Min(page.Height, page.Height * (float)topBox.Value / 297f);
                float bottom = Math.Min(page.Height, page.Height * (float)bottomBox.Value / 297f);
                RectangleF printable = RectangleF.FromLTRB(page.Left + left, page.Top + top,
                    Math.Max(page.Left + left, page.Right - right),
                    Math.Max(page.Top + top, page.Bottom - bottom));
                using (Pen margin = new Pen(Color.IndianRed))
                {
                    margin.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(margin, printable.X, printable.Y,
                        printable.Width, printable.Height);
                }
                for (int line = 0; line < 8; line++)
                {
                    float y = page.Y + 32 + line * 10;
                    e.Graphics.DrawLine(Pens.Black, page.X + 18, y,
                        page.Right - (line % 3 == 0 ? 23 : 15), y);
                }
            };
            Controls.Add(preview);

            AddEditor(this, "위쪽", topBox, new Point(183, 20), new Point(220, 16));
            AddEditor(this, "아래쪽", bottomBox, new Point(175, 275), new Point(220, 271));
            AddEditor(this, "왼쪽", leftBox, new Point(30, 137), new Point(67, 133));
            AddEditor(this, "오른쪽", rightBox, new Point(320, 137), new Point(365, 133));

            Button ok = new Button { Text = "확인", DialogResult = DialogResult.OK,
                Location = new Point(300, 312), Size = new Size(72, 27) };
            Button cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel,
                Location = new Point(380, 312), Size = new Size(72, 27) };
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;

            leftBox.Value = (decimal)margins.Left;
            topBox.Value = (decimal)margins.Top;
            rightBox.Value = (decimal)margins.Right;
            bottomBox.Value = (decimal)margins.Bottom;
            leftBox.ValueChanged += delegate { preview.Invalidate(); };
            topBox.ValueChanged += delegate { preview.Invalidate(); };
            rightBox.ValueChanged += delegate { preview.Invalidate(); };
            bottomBox.ValueChanged += delegate { preview.Invalidate(); };
        }

        public void ApplyTo(PageMargins margins)
        {
            margins.Left = (float)leftBox.Value;
            margins.Top = (float)topBox.Value;
            margins.Right = (float)rightBox.Value;
            margins.Bottom = (float)bottomBox.Value;
        }

        private static NumericUpDown CreateBox()
        {
            return new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Increment = 1,
                Width = 68
            };
        }

        private static void AddEditor(Form form, string text, NumericUpDown box,
            Point labelLocation, Point boxLocation)
        {
            Label label = new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = labelLocation,
                AutoSize = true
            };
            box.Location = boxLocation;
            Label unit = new Label
            {
                Text = "mm",
                Location = new Point(boxLocation.X + 72, boxLocation.Y + 4),
                AutoSize = true
            };
            form.Controls.Add(label);
            form.Controls.Add(box);
            form.Controls.Add(unit);
        }
    }

    internal sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public BufferedFlowLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x02000000;
                return parameters;
            }
        }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x02000000;
                return parameters;
            }
        }
    }

    internal sealed class ImageDocumentState
    {
        public float Scale;
        public float OffsetX;
        public float OffsetY;
        public float Rotation;
        public bool FlipHorizontal;
        public bool FlipVertical;

        public bool SameAs(ImageDocumentState other)
        {
            return other != null && Scale == other.Scale && OffsetX == other.OffsetX &&
                OffsetY == other.OffsetY && Rotation == other.Rotation &&
                FlipHorizontal == other.FlipHorizontal && FlipVertical == other.FlipVertical;
        }
    }

    internal sealed class ImageDocument : IDisposable
    {
        public const float PageWidth = 2480f;   // A4 at 300 dpi
        public const float PageHeight = 3508f;

        public float TileWidth { get; private set; }
        public float TileHeight { get; private set; }

        public Bitmap Source { get; private set; }
        public string SourcePath { get; private set; }
        public float Scale { get; private set; }
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }
        public float Rotation { get; private set; }
        public bool FlipHorizontal { get; private set; }
        public bool FlipVertical { get; private set; }
        private bool ownsSource = true;
        private PointF[] includedSourcePolygon;
        private List<PointF[]> excludedSourcePolygons = new List<PointF[]>();

        public bool HasImage { get { return Source != null; } }

        public ImageDocument()
        {
            TileWidth = PageWidth;
            TileHeight = PageHeight;
        }

        public void SetTileSize(float width, float height)
        {
            TileWidth = Math.Max(1f, width);
            TileHeight = Math.Max(1f, height);
        }

        public ImageDocument CreateLinkedCopy()
        {
            ImageDocument copy = new ImageDocument();
            copy.Source = Source;
            copy.SourcePath = SourcePath;
            copy.ownsSource = false;
            copy.SetTileSize(TileWidth, TileHeight);
            copy.RestoreState(CaptureState());
            return copy;
        }

        public void SetIncludedSourcePolygon(PointF[] polygon)
        {
            includedSourcePolygon = polygon == null ? null : (PointF[])polygon.Clone();
        }

        public void SetExcludedSourcePolygons(IEnumerable<PointF[]> polygons)
        {
            excludedSourcePolygons = polygons == null
                ? new List<PointF[]>()
                : polygons.Select(p => (PointF[])p.Clone()).ToList();
        }

        public void Load(string path)
        {
            DisposeBitmap();
            using (Image loaded = Image.FromFile(path))
            {
                Source = new Bitmap(loaded);
            }
            SourcePath = path;
            NormalizeExifOrientation();
            FitForNaturalSplit();
            Rotation = 0;
            FlipHorizontal = false;
            FlipVertical = false;
        }

        public void FitForNaturalSplit()
        {
            if (!HasImage) return;
            float imageRatio = (float)Source.Width / Source.Height;
            float pageRatio = TileWidth / TileHeight;

            // Wide images fill A4's long edge and split horizontally;
            // tall images fill its short edge and split vertically.
            Scale = imageRatio >= pageRatio
                ? TileHeight / Source.Height
                : TileWidth / Source.Width;
            OffsetX = 0;
            OffsetY = 0;
        }

        public void ResetTransform()
        {
            if (!HasImage) return;
            FitForNaturalSplit();
            Rotation = 0;
            FlipHorizontal = false;
            FlipVertical = false;
        }

        public ImageDocumentState CaptureState()
        {
            return new ImageDocumentState
            {
                Scale = Scale,
                OffsetX = OffsetX,
                OffsetY = OffsetY,
                Rotation = Rotation,
                FlipHorizontal = FlipHorizontal,
                FlipVertical = FlipVertical
            };
        }

        public void RestoreState(ImageDocumentState state)
        {
            if (!HasImage || state == null) return;
            Scale = state.Scale;
            OffsetX = state.OffsetX;
            OffsetY = state.OffsetY;
            Rotation = state.Rotation;
            FlipHorizontal = state.FlipHorizontal;
            FlipVertical = state.FlipVertical;
        }

        public void Move(float dx, float dy)
        {
            OffsetX += dx;
            OffsetY += dy;
        }

        public void ScaleKeepingCenter(float multiplier)
        {
            if (!HasImage) return;
            ScaleKeepingSourcePoint(multiplier,
                new PointF(Source.Width / 2f, Source.Height / 2f));
        }

        public void ScaleKeepingSourcePoint(float multiplier, PointF sourceAnchor)
        {
            if (!HasImage) return;
            PointF fixedWorldPoint = MapSourcePoint(sourceAnchor.X, sourceAnchor.Y);
            float next = Math.Max(0.01f, Math.Min(100f, Scale * multiplier));
            Scale = next;
            PointF movedWorldPoint = MapSourcePoint(sourceAnchor.X, sourceAnchor.Y);
            OffsetX += fixedWorldPoint.X - movedWorldPoint.X;
            OffsetY += fixedWorldPoint.Y - movedWorldPoint.Y;
        }

        public void ScaleKeepingSourcePointWithin(float multiplier, PointF sourceAnchor,
            RectangleF boundary)
        {
            if (!HasImage) return;
            ImageDocumentState before = CaptureState();
            ScaleKeepingSourcePoint(multiplier, sourceAnchor);
            if (multiplier <= 1f || IsInsideBoundary(GetEditableBounds(), boundary)) return;

            RestoreState(before);
            float low = 1f;
            float high = multiplier;
            for (int iteration = 0; iteration < 24; iteration++)
            {
                float middle = (low + high) / 2f;
                RestoreState(before);
                ScaleKeepingSourcePoint(middle, sourceAnchor);
                if (IsInsideBoundary(GetEditableBounds(), boundary)) low = middle;
                else high = middle;
            }
            RestoreState(before);
            ScaleKeepingSourcePoint(low, sourceAnchor);
        }

        private static bool IsInsideBoundary(RectangleF bounds, RectangleF boundary)
        {
            const float tolerance = 0.05f;
            return bounds.Left >= boundary.Left - tolerance &&
                bounds.Top >= boundary.Top - tolerance &&
                bounds.Right <= boundary.Right + tolerance &&
                bounds.Bottom <= boundary.Bottom + tolerance;
        }

        public void Rotate(float degrees)
        {
            Rotation = NormalizeDegrees(Rotation + degrees);
        }

        public void SetRotation(float degrees)
        {
            Rotation = NormalizeDegrees(degrees);
        }

        public void ToggleHorizontalFlip()
        {
            FlipHorizontal = !FlipHorizontal;
        }

        public void ToggleVerticalFlip()
        {
            FlipVertical = !FlipVertical;
        }

        public RectangleF GetBounds()
        {
            if (!HasImage) return RectangleF.Empty;
            PointF[] points = new PointF[]
            {
                MapSourcePoint(0, 0),
                MapSourcePoint(Source.Width, 0),
                MapSourcePoint(Source.Width, Source.Height),
                MapSourcePoint(0, Source.Height)
            };
            float left = points.Min(p => p.X);
            float right = points.Max(p => p.X);
            float top = points.Min(p => p.Y);
            float bottom = points.Max(p => p.Y);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        public PointF[] GetCorners()
        {
            if (!HasImage) return new PointF[0];
            return new PointF[]
            {
                MapSourcePoint(0, 0),
                MapSourcePoint(Source.Width, 0),
                MapSourcePoint(Source.Width, Source.Height),
                MapSourcePoint(0, Source.Height)
            };
        }

        public PointF[] GetEditableCorners()
        {
            PointF[] sourcePolygon = GetVisibleSourceRectangle();
            PointF[] worldPolygon = sourcePolygon.Select(point =>
                MapSourcePoint(point.X, point.Y)).ToArray();
            if (excludedSourcePolygons.Count == 0) return worldPolygon;

            using (Region visible = CreateVisibleWorldRegion())
            using (Matrix identity = new Matrix())
            {
                RectangleF[] scans = visible.GetRegionScans(identity);
                if (scans.Length == 0) return worldPolygon;
                RectangleF bounds = scans[0];
                for (int index = 1; index < scans.Length; index++)
                    bounds = RectangleF.Union(bounds, scans[index]);
                return new PointF[]
                {
                    new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top),
                    new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Left, bounds.Bottom)
                };
            }
        }

        private PointF[] GetVisibleSourceRectangle()
        {
            float left = 0f;
            float top = 0f;
            float right = Source.Width;
            float bottom = Source.Height;
            if (includedSourcePolygon != null)
            {
                left = Math.Max(left, includedSourcePolygon.Min(point => point.X));
                top = Math.Max(top, includedSourcePolygon.Min(point => point.Y));
                right = Math.Min(right, includedSourcePolygon.Max(point => point.X));
                bottom = Math.Min(bottom, includedSourcePolygon.Max(point => point.Y));
            }
            return new PointF[]
            {
                new PointF(left, top), new PointF(right, top),
                new PointF(right, bottom), new PointF(left, bottom)
            };
        }

        private Region CreateVisibleWorldRegion()
        {
            PointF[] contentCorners = GetVisibleSourceRectangle().Select(point =>
                MapSourcePoint(point.X, point.Y)).ToArray();
            GraphicsPath contentPath = new GraphicsPath();
            contentPath.AddPolygon(contentCorners);
            Region visible = new Region(contentPath);
            contentPath.Dispose();
            foreach (PointF[] excludedSourcePolygon in excludedSourcePolygons)
            {
                PointF[] excludedWorldPolygon = excludedSourcePolygon.Select(point =>
                    MapSourcePoint(point.X, point.Y)).ToArray();
                using (GraphicsPath excludedPath = new GraphicsPath())
                {
                    excludedPath.AddPolygon(excludedWorldPolygon);
                    visible.Exclude(excludedPath);
                }
            }
            return visible;
        }

        public PointF[] GetEditableSourceCorners()
        {
            PointF[] world = GetEditableCorners();
            PointF[] source = new PointF[world.Length];
            for (int index = 0; index < world.Length; index++)
                TryMapWorldToSource(world[index], out source[index]);
            return source;
        }

        public RectangleF GetEditableBounds()
        {
            PointF[] corners = GetEditableCorners();
            return RectangleF.FromLTRB(corners.Min(p => p.X), corners.Min(p => p.Y),
                corners.Max(p => p.X), corners.Max(p => p.Y));
        }

        private static RectangleF GetPolygonBounds(PointF[] polygon)
        {
            return RectangleF.FromLTRB(polygon.Min(p => p.X), polygon.Min(p => p.Y),
                polygon.Max(p => p.X), polygon.Max(p => p.Y));
        }

        public bool TryGetColorAtWorld(float worldX, float worldY, out Color color)
        {
            color = Color.Transparent;
            if (!HasImage || Scale <= 0f) return false;

            float centerX = OffsetX + Source.Width * Scale / 2f;
            float centerY = OffsetY + Source.Height * Scale / 2f;
            float dx = worldX - centerX;
            float dy = worldY - centerY;
            double radians = -Rotation * Math.PI / 180d;
            float localX = (float)(dx * Math.Cos(radians) - dy * Math.Sin(radians));
            float localY = (float)(dx * Math.Sin(radians) + dy * Math.Cos(radians));
            if (FlipHorizontal) localX = -localX;
            if (FlipVertical) localY = -localY;

            int sourceX = (int)Math.Floor(localX / Scale + Source.Width / 2f);
            int sourceY = (int)Math.Floor(localY / Scale + Source.Height / 2f);
            if (sourceX < 0 || sourceY < 0 || sourceX >= Source.Width || sourceY >= Source.Height)
                return false;

            color = Source.GetPixel(sourceX, sourceY);
            return color.A > 24;
        }

        public bool ContainsVisibleImageAtWorld(PointF world)
        {
            PointF sourcePoint;
            if (!TryMapWorldToSource(world, out sourcePoint) ||
                sourcePoint.X < 0f || sourcePoint.Y < 0f ||
                sourcePoint.X >= Source.Width || sourcePoint.Y >= Source.Height)
                return false;
            if (includedSourcePolygon != null &&
                !PointInPolygon(sourcePoint, includedSourcePolygon)) return false;
            foreach (PointF[] excluded in excludedSourcePolygons)
                if (PointInPolygon(sourcePoint, excluded)) return false;
            int x = Math.Max(0, Math.Min(Source.Width - 1, (int)sourcePoint.X));
            int y = Math.Max(0, Math.Min(Source.Height - 1, (int)sourcePoint.Y));
            return Source.GetPixel(x, y).A > 24;
        }

        private static bool PointInPolygon(PointF point, PointF[] polygon)
        {
            if (polygon == null || polygon.Length < 3) return false;
            bool inside = false;
            for (int current = 0, previous = polygon.Length - 1;
                current < polygon.Length; previous = current++)
            {
                PointF first = polygon[current];
                PointF second = polygon[previous];
                if ((first.Y > point.Y) != (second.Y > point.Y) &&
                    point.X < (second.X - first.X) * (point.Y - first.Y) /
                    (second.Y - first.Y) + first.X)
                    inside = !inside;
            }
            return inside;
        }

        public bool TryMapWorldToSource(PointF world, out PointF source)
        {
            source = PointF.Empty;
            if (!HasImage || Scale <= 0f) return false;
            float centerX = OffsetX + Source.Width * Scale / 2f;
            float centerY = OffsetY + Source.Height * Scale / 2f;
            float dx = world.X - centerX;
            float dy = world.Y - centerY;
            double radians = -Rotation * Math.PI / 180d;
            float localX = (float)(dx * Math.Cos(radians) - dy * Math.Sin(radians));
            float localY = (float)(dx * Math.Sin(radians) + dy * Math.Cos(radians));
            if (FlipHorizontal) localX = -localX;
            if (FlipVertical) localY = -localY;
            source = new PointF(localX / Scale + Source.Width / 2f,
                localY / Scale + Source.Height / 2f);
            return true;
        }

        public List<PageTile> GetCoveredTiles()
        {
            List<PageTile> tiles = new List<PageTile>();
            if (!HasImage) return tiles;
            RectangleF bounds = GetEditableBounds();
            const float edgeTolerance = 0.01f;
            int left = (int)Math.Floor((bounds.Left + edgeTolerance) / TileWidth);
            int top = (int)Math.Floor((bounds.Top + edgeTolerance) / TileHeight);
            int right = (int)Math.Floor((bounds.Right - edgeTolerance) / TileWidth);
            int bottom = (int)Math.Floor((bounds.Bottom - edgeTolerance) / TileHeight);

            using (Matrix identity = new Matrix())
            using (Region visibleContent = CreateVisibleWorldRegion())
            {
                for (int row = top; row <= bottom; row++)
                {
                    for (int column = left; column <= right; column++)
                    {
                        using (Region tileContent = visibleContent.Clone())
                        {
                            tileContent.Intersect(new RectangleF(column * TileWidth,
                                row * TileHeight, TileWidth, TileHeight));
                            RectangleF[] scans = tileContent.GetRegionScans(identity);
                            if (scans.Any(scan => scan.Width > edgeTolerance &&
                                scan.Height > edgeTolerance))
                                tiles.Add(new PageTile(column, row));
                        }
                    }
                }
            }
            return tiles;
        }

        public void DrawWorld(Graphics graphics)
        {
            if (!HasImage) return;
            GraphicsState state = graphics.Save();
            float centerX = OffsetX + Source.Width * Scale / 2f;
            float centerY = OffsetY + Source.Height * Scale / 2f;
            // Prepend the image transforms so they are applied in document space
            // before the preview/page view transform already set on Graphics.
            graphics.TranslateTransform(centerX, centerY, MatrixOrder.Prepend);
            graphics.RotateTransform(Rotation, MatrixOrder.Prepend);
            graphics.ScaleTransform(FlipHorizontal ? -1f : 1f, FlipVertical ? -1f : 1f,
                MatrixOrder.Prepend);
            if (includedSourcePolygon != null)
            {
                using (GraphicsPath included = CreateLocalSourcePath(includedSourcePolygon))
                    graphics.SetClip(included, CombineMode.Intersect);
            }
            foreach (PointF[] polygon in excludedSourcePolygons)
            {
                using (GraphicsPath excluded = CreateLocalSourcePath(polygon))
                    graphics.SetClip(excluded, CombineMode.Exclude);
            }
            graphics.DrawImage(Source,
                new RectangleF(-Source.Width * Scale / 2f, -Source.Height * Scale / 2f,
                    Source.Width * Scale, Source.Height * Scale));
            graphics.Restore(state);
        }

        private GraphicsPath CreateLocalSourcePath(PointF[] sourcePolygon)
        {
            PointF[] local = sourcePolygon.Select(point => new PointF(
                (point.X - Source.Width / 2f) * Scale,
                (point.Y - Source.Height / 2f) * Scale)).ToArray();
            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(local);
            return path;
        }

        public void DrawPage(Graphics graphics, PageTile tile, bool clearBackground = true)
        {
            DrawPage(graphics, tile, null, clearBackground);
        }

        public void DrawPage(Graphics graphics, PageTile tile, PageMargins margins,
            bool clearBackground = true)
        {
            if (clearBackground) graphics.Clear(Color.White);
            GraphicsState state = graphics.Save();
            RectangleF printable = margins == null
                ? new RectangleF(0, 0, PageWidth, PageHeight)
                : margins.GetPrintableRectangle();
            graphics.SetClip(printable);
            graphics.TranslateTransform(printable.Left - tile.Column * TileWidth,
                printable.Top - tile.Row * TileHeight, MatrixOrder.Prepend);
            DrawWorld(graphics);
            graphics.Restore(state);
        }

        private PointF MapSourcePoint(float x, float y)
        {
            float dx = (x - Source.Width / 2f) * Scale;
            float dy = (y - Source.Height / 2f) * Scale;
            if (FlipHorizontal) dx = -dx;
            if (FlipVertical) dy = -dy;
            double radians = Rotation * Math.PI / 180d;
            float rotatedX = (float)(dx * Math.Cos(radians) - dy * Math.Sin(radians));
            float rotatedY = (float)(dx * Math.Sin(radians) + dy * Math.Cos(radians));
            return new PointF(OffsetX + Source.Width * Scale / 2f + rotatedX,
                OffsetY + Source.Height * Scale / 2f + rotatedY);
        }

        private void NormalizeExifOrientation()
        {
            const int orientationId = 0x0112;
            try
            {
                if (!Source.PropertyIdList.Contains(orientationId)) return;
                PropertyItem item = Source.GetPropertyItem(orientationId);
                if (item.Value == null || item.Value.Length == 0) return;
                RotateFlipType transform = RotateFlipType.RotateNoneFlipNone;
                switch (item.Value[0])
                {
                    case 2: transform = RotateFlipType.RotateNoneFlipX; break;
                    case 3: transform = RotateFlipType.Rotate180FlipNone; break;
                    case 4: transform = RotateFlipType.Rotate180FlipX; break;
                    case 5: transform = RotateFlipType.Rotate90FlipX; break;
                    case 6: transform = RotateFlipType.Rotate90FlipNone; break;
                    case 7: transform = RotateFlipType.Rotate270FlipX; break;
                    case 8: transform = RotateFlipType.Rotate270FlipNone; break;
                }
                if (transform != RotateFlipType.RotateNoneFlipNone)
                {
                    Source.RotateFlip(transform);
                }
            }
            catch (ArgumentException)
            {
                // Some formats expose no readable EXIF metadata. The bitmap is still usable.
            }
        }

        private static float NormalizeDegrees(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees <= -180f) degrees += 360f;
            return degrees;
        }

        private void DisposeBitmap()
        {
            if (Source != null)
            {
                if (ownsSource) Source.Dispose();
                Source = null;
            }
        }

        public void Dispose()
        {
            DisposeBitmap();
        }
    }

    internal sealed class PageTile : IEquatable<PageTile>
    {
        public int Column { get; private set; }
        public int Row { get; private set; }

        public PageTile(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(PageTile other)
        {
            return other != null && Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PageTile);
        }

        public override int GetHashCode()
        {
            return Column * 397 ^ Row;
        }

        public override string ToString()
        {
            return string.Format("열 {0}, 행 {1}", Column + 1, Row + 1);
        }
    }

    internal abstract class ImageInteractionControl : Control
    {
        protected readonly ImageDocument Document;
        private bool moving;
        private bool rotating;
        private bool interactionActive;
        private Point lastMouse;
        private readonly Timer wheelCommitTimer;

        protected ImageInteractionControl(ImageDocument document)
        {
            Document = document;
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            wheelCommitTimer = new Timer();
            wheelCommitTimer.Interval = 280;
            wheelCommitTimer.Tick += delegate
            {
                wheelCommitTimer.Stop();
                CompleteInteraction();
            };
        }

        protected abstract float GetDocumentScale();

        protected void NotifyChanged()
        {
            EventHandler handler = DocumentChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler DocumentChanged;
        public event EventHandler InteractionStarted;
        public event EventHandler InteractionCompleted;

        protected virtual void InteractionStarting() { }
        protected virtual void InteractionEnding() { }

        protected void BeginInteraction()
        {
            if (interactionActive) return;
            interactionActive = true;
            InteractionStarting();
            EventHandler handler = InteractionStarted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected void CompleteInteraction()
        {
            if (!interactionActive) return;
            interactionActive = false;
            InteractionEnding();
            EventHandler handler = InteractionCompleted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Document.HasImage) return;
            Focus();
            lastMouse = e.Location;
            moving = e.Button == MouseButtons.Left;
            rotating = e.Button == MouseButtons.Right;
            if (moving || rotating)
            {
                wheelCommitTimer.Stop();
                BeginInteraction();
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Document.HasImage) return;
            if (moving)
            {
                float scale = GetDocumentScale();
                if (scale > 0)
                {
                    ApplyMoveChange((e.X - lastMouse.X) / scale, (e.Y - lastMouse.Y) / scale);
                    lastMouse = e.Location;
                    NotifyChanged();
                }
            }
            else if (rotating)
            {
                Document.Rotate((lastMouse.X - e.X) * 0.35f);
                lastMouse = e.Location;
                NotifyChanged();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            moving = false;
            rotating = false;
            Capture = false;
            CompleteInteraction();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Document.HasImage) return;
            BeginInteraction();
            ApplyWheelChange(e);
            NotifyChanged();
            wheelCommitTimer.Stop();
            wheelCommitTimer.Start();
        }

        protected virtual void ApplyWheelChange(MouseEventArgs e)
        {
            Document.ScaleKeepingCenter(e.Delta > 0 ? 1.08f : 1f / 1.08f);
        }

        protected virtual void ApplyMoveChange(float dx, float dy)
        {
            Document.Move(dx, dy);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) wheelCommitTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class GridPreview : ImageInteractionControl
    {
        private enum ResizeTarget { None, Image, Grid }
        private enum BoundarySide { None, Left, Right, Top, Bottom }

        private float documentScale;
        private float originX;
        private float originY;
        private int leftColumn;
        private int topRow;
        private int columns;
        private int rows;
        private bool layoutFrozen;
        private bool viewCalculated;
        private bool resizingGrid;
        private float gridZoom = 1f;
        private float frozenImageScale;
        private float frozenImageOriginX;
        private float frozenImageOriginY;
        private int frozenImageLeftColumn;
        private int frozenImageTopRow;
        private int frozenImageColumns;
        private int frozenImageRows;
        private ResizeTarget resizeTarget;
        private PointF resizeCenter;
        private float resizeStartDistance;
        private float resizeStartImageScale;
        private float resizeStartGridZoom;
        private float resizeStartViewScale;
        private PointF resizeImageAnchor;
        private PointF resizeGridAnchorWorld;
        private ImageDocumentState resizeStartDocumentState;
        private BoundarySide blockedSide;
        private readonly PageMargins pageMargins;
        private float viewportZoom = 1f;
        private float viewportPanX;
        private float viewportPanY;

        public float GridZoom { get { return gridZoom; } }

        public GridPreview(ImageDocument document, PageMargins pageMargins) : base(document)
        {
            this.pageMargins = pageMargins;
            BackColor = Color.FromArgb(238, 241, 245);
            AllowDrop = true;
            Cursor = Cursors.SizeAll;
        }

        protected override float GetDocumentScale()
        {
            return documentScale;
        }

        public void SetGridZoom(float value)
        {
            gridZoom = Math.Max(0.25f, Math.Min(4f, value));
            viewCalculated = false;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control && Document.HasImage)
            {
                ZoomViewportAt(e.Location, e.Delta > 0 ? 1.16f : 1f / 1.16f);
                return;
            }
            base.OnMouseWheel(e);
        }

        private void ZoomViewportAt(Point cursor, float multiplier)
        {
            CalculateView();
            float nextZoom = Math.Max(0.25f, Math.Min(8f, viewportZoom * multiplier));
            float applied = nextZoom / viewportZoom;
            if (Math.Abs(applied - 1f) < 0.0001f) return;
            float worldX = leftColumn * Document.TileWidth + (cursor.X - originX) / documentScale;
            float worldY = topRow * Document.TileHeight + (cursor.Y - originY) / documentScale;
            viewportZoom = nextZoom;
            float nextScale = documentScale * applied;
            float nextOriginX = cursor.X -
                (worldX - leftColumn * Document.TileWidth) * nextScale;
            float nextOriginY = cursor.Y -
                (worldY - topRow * Document.TileHeight) * nextScale;
            float renderedWidth = columns * Document.TileWidth * nextScale;
            float renderedHeight = rows * Document.TileHeight * nextScale;
            viewportPanX = nextOriginX - (ClientSize.Width - renderedWidth) / 2f;
            viewportPanY = nextOriginY - ((ClientSize.Height - renderedHeight) / 2f + 14f);
            documentScale = nextScale;
            originX = nextOriginX;
            originY = nextOriginY;
            viewCalculated = true;
            Invalidate();
        }

        protected override void InteractionStarting()
        {
            CalculateView();
            blockedSide = BoundarySide.None;
            frozenImageScale = documentScale;
            frozenImageOriginX = originX;
            frozenImageOriginY = originY;
            frozenImageLeftColumn = leftColumn;
            frozenImageTopRow = topRow;
            frozenImageColumns = columns;
            frozenImageRows = rows;
            layoutFrozen = true;
        }

        protected override void InteractionEnding()
        {
            blockedSide = BoundarySide.None;
            resizingGrid = false;
            layoutFrozen = false;
            viewCalculated = false;
            Invalidate();
        }

        protected override void ApplyWheelChange(MouseEventArgs e)
        {
            float multiplier = e.Delta > 0 ? 1.08f : 1f / 1.08f;
            base.ApplyWheelChange(e);
        }

        private RectangleF GetPrintableGridBoundary()
        {
            return RectangleF.FromLTRB(
                leftColumn * Document.TileWidth,
                topRow * Document.TileHeight,
                (leftColumn + columns) * Document.TileWidth,
                (topRow + rows) * Document.TileHeight);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Document.HasImage)
            {
                CalculateView();
                ResizeTarget target = HitTestResizeTarget(e.Location);
                if (target != ResizeTarget.None)
                {
                    Focus();
                    resizeTarget = target;
                    if (target == ResizeTarget.Image)
                    {
                        PointF[] imageCorners = GetImageCornersOnScreen();
                        int draggedCorner = FindNearestCorner(e.Location, imageCorners);
                        int fixedCorner = (draggedCorner + 2) % 4;
                        resizeCenter = imageCorners[fixedCorner];
                        resizeImageAnchor = GetSourceCorner(fixedCorner);
                    }
                    else
                    {
                        RectangleF gridRectangle = GetGridRectangle();
                        PointF[] gridCorners = new PointF[]
                        {
                            new PointF(gridRectangle.Left, gridRectangle.Top),
                            new PointF(gridRectangle.Right, gridRectangle.Top),
                            new PointF(gridRectangle.Right, gridRectangle.Bottom),
                            new PointF(gridRectangle.Left, gridRectangle.Bottom)
                        };
                        int draggedCorner = FindNearestCorner(e.Location, gridCorners);
                        int fixedCorner = (draggedCorner + 2) % 4;
                        resizeCenter = gridCorners[fixedCorner];
                        resizeGridAnchorWorld = new PointF(
                            (fixedCorner == 0 || fixedCorner == 3)
                                ? leftColumn * Document.TileWidth
                                : (leftColumn + columns) * Document.TileWidth,
                            (fixedCorner == 0 || fixedCorner == 1)
                                ? topRow * Document.TileHeight
                                : (topRow + rows) * Document.TileHeight);
                    }
                    resizeStartDistance = Math.Max(1f, Distance(resizeCenter, e.Location));
                    resizeStartImageScale = Document.Scale;
                    resizeStartGridZoom = gridZoom;
                    resizeStartViewScale = documentScale;
                    resizeStartDocumentState = Document.CaptureState();
                    BeginInteraction();
                    resizingGrid = target == ResizeTarget.Grid;
                    Capture = true;
                    return;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (resizeTarget != ResizeTarget.None)
            {
                float ratio = Math.Max(0.05f, Distance(resizeCenter, e.Location) /
                    resizeStartDistance);
                if (resizeTarget == ResizeTarget.Image)
                {
                    float targetScale = resizeStartImageScale * ratio;
                    float multiplier = targetScale / Math.Max(Document.Scale, 0.0001f);
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                        Document.ScaleKeepingSourcePointWithin(multiplier, resizeImageAnchor,
                            GetPrintableGridBoundary());
                    else
                        Document.ScaleKeepingSourcePoint(multiplier, resizeImageAnchor);
                }
                else
                {
                    gridZoom = Math.Max(0.25f, Math.Min(4f, resizeStartGridZoom * ratio));
                    float appliedRatio = gridZoom / resizeStartGridZoom;
                    documentScale = resizeStartViewScale * appliedRatio;
                    originX = resizeCenter.X -
                        (resizeGridAnchorWorld.X - leftColumn * Document.TileWidth) * documentScale;
                    originY = resizeCenter.Y -
                        (resizeGridAnchorWorld.Y - topRow * Document.TileHeight) * documentScale;
                    viewCalculated = true;
                }
                NotifyChanged();
                return;
            }

            if (e.Button == MouseButtons.None && Document.HasImage)
            {
                ResizeTarget target = HitTestResizeTarget(e.Location);
                Cursor = target == ResizeTarget.None ? Cursors.SizeAll : Cursors.SizeNWSE;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (resizeTarget != ResizeTarget.None)
            {
                if (resizeTarget == ResizeTarget.Grid && resizeStartDocumentState != null)
                {
                    float gridRatio = gridZoom / Math.Max(resizeStartGridZoom, 0.0001f);
                    Document.RestoreState(resizeStartDocumentState);
                    Document.ScaleKeepingCenter(1f / Math.Max(gridRatio, 0.0001f));
                }
                resizeTarget = ResizeTarget.None;
                Capture = false;
                Cursor = Cursors.SizeAll;
                CompleteInteraction();
                return;
            }
            base.OnMouseUp(e);
        }

        protected override void ApplyMoveChange(float dx, float dy)
        {
            if ((ModifierKeys & Keys.Control) != Keys.Control)
            {
                Document.Move(dx, dy);
                return;
            }
            RectangleF imageBounds = Document.GetEditableBounds();
            RectangleF printableBoundary = GetPrintableGridBoundary();
            float gridLeft = printableBoundary.Left;
            float gridRight = printableBoundary.Right;
            float gridTop = printableBoundary.Top;
            float gridBottom = printableBoundary.Bottom;

            float allowedDx = dx;
            float allowedDy = dy;
            if (dx < 0f && imageBounds.Left + dx < gridLeft)
                allowedDx = Math.Min(0f, gridLeft - imageBounds.Left);
            else if (dx > 0f && imageBounds.Right + dx > gridRight)
                allowedDx = Math.Max(0f, gridRight - imageBounds.Right);
            if (dy < 0f && imageBounds.Top + dy < gridTop)
                allowedDy = Math.Min(0f, gridTop - imageBounds.Top);
            else if (dy > 0f && imageBounds.Bottom + dy > gridBottom)
                allowedDy = Math.Max(0f, gridBottom - imageBounds.Bottom);
            Document.Move(allowedDx, allowedDy);
        }

        private void ExpandGrid(BoundarySide side)
        {
            if (side == BoundarySide.Left)
            {
                leftColumn--;
                columns++;
                originX -= Document.TileWidth * documentScale;
            }
            else if (side == BoundarySide.Right) columns++;
            else if (side == BoundarySide.Top)
            {
                topRow--;
                rows++;
                originY -= Document.TileHeight * documentScale;
            }
            else if (side == BoundarySide.Bottom) rows++;
            viewCalculated = true;
        }

        private static bool IsMovingOutward(BoundarySide side, float dx, float dy)
        {
            return (side == BoundarySide.Left && dx < 0f) ||
                (side == BoundarySide.Right && dx > 0f) ||
                (side == BoundarySide.Top && dy < 0f) ||
                (side == BoundarySide.Bottom && dy > 0f);
        }

        private ResizeTarget HitTestResizeTarget(Point location)
        {
            const float tolerance = 9f;
            bool forceGrid = (ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!forceGrid)
            {
                PointF[] corners = GetImageCornersOnScreen();
                for (int index = 0; index < corners.Length; index++)
                {
                    if (DistanceToSegment(location, corners[index],
                        corners[(index + 1) % corners.Length]) <= tolerance)
                        return ResizeTarget.Image;
                }
            }

            RectangleF grid = GetGridRectangle();
            if (location.X >= grid.Left - tolerance && location.X <= grid.Right + tolerance &&
                location.Y >= grid.Top - tolerance && location.Y <= grid.Bottom + tolerance &&
                (Math.Abs(location.X - grid.Left) <= tolerance ||
                 Math.Abs(location.X - grid.Right) <= tolerance ||
                 Math.Abs(location.Y - grid.Top) <= tolerance ||
                 Math.Abs(location.Y - grid.Bottom) <= tolerance))
                return ResizeTarget.Grid;
            return ResizeTarget.None;
        }

        private PointF[] GetImageCornersOnScreen()
        {
            PointF[] world = Document.GetCorners();
            PointF[] screen = new PointF[world.Length];
            for (int index = 0; index < world.Length; index++)
                screen[index] = WorldToScreen(world[index]);
            return screen;
        }

        private PointF GetImageCenterOnScreen()
        {
            PointF[] corners = GetImageCornersOnScreen();
            float x = 0f;
            float y = 0f;
            foreach (PointF corner in corners) { x += corner.X; y += corner.Y; }
            return new PointF(x / corners.Length, y / corners.Length);
        }

        private RectangleF GetGridRectangle()
        {
            return new RectangleF(originX, originY,
                columns * Document.TileWidth * documentScale,
                rows * Document.TileHeight * documentScale);
        }

        private PointF WorldToScreen(PointF point)
        {
            return new PointF(originX + (point.X - leftColumn * Document.TileWidth) * documentScale,
                originY + (point.Y - topRow * Document.TileHeight) * documentScale);
        }

        private static float Distance(PointF first, PointF second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private int FindNearestCorner(PointF point, PointF[] corners)
        {
            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int index = 0; index < corners.Length; index++)
            {
                float distance = Distance(point, corners[index]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = index;
                }
            }
            return nearest;
        }

        private PointF GetSourceCorner(int index)
        {
            PointF[] corners = Document.GetEditableSourceCorners();
            return corners[Math.Max(0, Math.Min(corners.Length - 1, index))];
        }

        private static float DistanceToSegment(PointF point, PointF start, PointF end)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 0.001f) return Distance(point, start);
            float amount = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) /
                lengthSquared;
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Distance(point, new PointF(start.X + dx * amount, start.Y + dy * amount));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            CalculateView();

            using (SolidBrush pageBrush = new SolidBrush(Color.White))
            // 70% transparent yellow is the default. On yellow-like image areas,
            // the sampled segment switches to its blue complementary colour.
            using (Pen yellowGrid = new Pen(Color.FromArgb(77, 255, 193, 7),
                1.5f / Math.Max(documentScale, 0.01f)))
            using (Pen complementGrid = new Pen(Color.FromArgb(77, 0, 62, 248),
                1.5f / Math.Max(documentScale, 0.01f)))
            {
                RectangleF allPages = new RectangleF(leftColumn * Document.TileWidth,
                    topRow * Document.TileHeight,
                    columns * Document.TileWidth, rows * Document.TileHeight);

                GraphicsState pageState = e.Graphics.Save();
                ApplyViewTransform(e.Graphics, documentScale, originX, originY, leftColumn, topRow);
                e.Graphics.FillRectangle(pageBrush, allPages);
                e.Graphics.Restore(pageState);

                GraphicsState imageState = e.Graphics.Save();
                if (resizingGrid)
                {
                    ApplyViewTransform(e.Graphics, frozenImageScale, frozenImageOriginX,
                        frozenImageOriginY, frozenImageLeftColumn, frozenImageTopRow);
                    e.Graphics.SetClip(new RectangleF(
                        frozenImageLeftColumn * Document.TileWidth,
                        frozenImageTopRow * Document.TileHeight,
                        frozenImageColumns * Document.TileWidth,
                        frozenImageRows * Document.TileHeight));
                }
                else
                {
                    ApplyViewTransform(e.Graphics, documentScale, originX, originY, leftColumn, topRow);
                    e.Graphics.SetClip(allPages);
                }
                Document.DrawWorld(e.Graphics);
                e.Graphics.Restore(imageState);

                GraphicsState gridState = e.Graphics.Save();
                ApplyViewTransform(e.Graphics, documentScale, originX, originY, leftColumn, topRow);
                for (int row = 0; row <= rows; row++)
                {
                    float y = (topRow + row) * Document.TileHeight;
                    DrawAdaptiveGridLine(e.Graphics,
                        new PointF(leftColumn * Document.TileWidth, y),
                        new PointF((leftColumn + columns) * Document.TileWidth, y),
                        yellowGrid, complementGrid);
                }
                for (int column = 0; column <= columns; column++)
                {
                    float x = (leftColumn + column) * Document.TileWidth;
                    DrawAdaptiveGridLine(e.Graphics,
                        new PointF(x, topRow * Document.TileHeight),
                        new PointF(x, (topRow + rows) * Document.TileHeight),
                        yellowGrid, complementGrid);
                }
                e.Graphics.Restore(gridState);
            }

            if (Document.HasImage) DrawResizeHandles(e.Graphics);

            if (!Document.HasImage)
            {
                string text = "이미지를 이곳에 끌어 놓으세요\n또는 ‘이미지 열기’ 버튼을 누르세요";
                TextRenderer.DrawText(e.Graphics, text, Font,
                    new Rectangle(0, Height / 2 - 28, Width, 56), Color.FromArgb(78, 91, 105),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, "내부 드래그: 이동   ·   Ctrl+이동: 현재 격자 안에 고정   ·   파란 테두리: 이미지 크기   ·   노란 외곽: 격자 크기",
                    Font, new Point(16, 14), Color.FromArgb(70, 80, 90));
            }
        }

        private void DrawBlockedEdge(Graphics graphics)
        {
            if (blockedSide == BoundarySide.None) return;
            float left = leftColumn * Document.TileWidth;
            float right = (leftColumn + columns) * Document.TileWidth;
            float top = topRow * Document.TileHeight;
            float bottom = (topRow + rows) * Document.TileHeight;
            PointF start;
            PointF end;
            if (blockedSide == BoundarySide.Left)
            {
                start = new PointF(left, top); end = new PointF(left, bottom);
            }
            else if (blockedSide == BoundarySide.Right)
            {
                start = new PointF(right, top); end = new PointF(right, bottom);
            }
            else if (blockedSide == BoundarySide.Top)
            {
                start = new PointF(left, top); end = new PointF(right, top);
            }
            else
            {
                start = new PointF(left, bottom); end = new PointF(right, bottom);
            }
            using (Pen highlight = new Pen(Color.FromArgb(220, 255, 120, 0),
                7f / Math.Max(documentScale, 0.01f)))
            {
                graphics.DrawLine(highlight, start, end);
            }
        }

        private void DrawResizeHandles(Graphics graphics)
        {
            PointF[] imageCorners = GetImageCornersOnScreen();
            if (imageCorners.Length == 4)
            {
                using (Pen outline = new Pen(Color.FromArgb(190, 0, 145, 210), 1.5f))
                using (SolidBrush handle = new SolidBrush(Color.FromArgb(230, 0, 145, 210)))
                {
                    graphics.DrawPolygon(outline, imageCorners);
                    foreach (PointF point in imageCorners)
                        graphics.FillRectangle(handle, point.X - 4f, point.Y - 4f, 8f, 8f);
                }
            }

            RectangleF grid = GetGridRectangle();
            PointF[] gridCorners = new PointF[]
            {
                new PointF(grid.Left, grid.Top), new PointF(grid.Right, grid.Top),
                new PointF(grid.Right, grid.Bottom), new PointF(grid.Left, grid.Bottom)
            };
            using (SolidBrush handle = new SolidBrush(Color.FromArgb(230, 255, 193, 7)))
            {
                foreach (PointF point in gridCorners)
                    graphics.FillRectangle(handle, point.X - 4f, point.Y - 4f, 8f, 8f);
            }
        }

        private void ApplyViewTransform(Graphics graphics, float scale, float x, float y,
            int viewLeftColumn, int viewTopRow)
        {
            graphics.TranslateTransform(x, y);
            graphics.ScaleTransform(scale, scale);
            graphics.TranslateTransform(-viewLeftColumn * Document.TileWidth,
                -viewTopRow * Document.TileHeight);
        }

        private void DrawAdaptiveGridLine(Graphics graphics, PointF start, PointF end,
            Pen yellowGrid, Pen complementGrid)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            int pieces = Math.Max(1, Math.Min(500,
                (int)Math.Ceiling(length * Math.Max(documentScale, 0.01f) / 10f)));
            for (int index = 0; index < pieces; index++)
            {
                float from = (float)index / pieces;
                float to = (float)(index + 1) / pieces;
                PointF first = new PointF(start.X + dx * from, start.Y + dy * from);
                PointF second = new PointF(start.X + dx * to, start.Y + dy * to);
                Color sampled;
                bool useComplement = Document.TryGetColorAtWorld(
                    (first.X + second.X) / 2f, (first.Y + second.Y) / 2f, out sampled) &&
                    IsYellowLike(sampled);
                graphics.DrawLine(useComplement ? complementGrid : yellowGrid, first, second);
            }
        }

        private static bool IsYellowLike(Color color)
        {
            float max = Math.Max(color.R, Math.Max(color.G, color.B));
            float min = Math.Min(color.R, Math.Min(color.G, color.B));
            if (max < 115f || max - min < 35f) return false;
            float hue = color.GetHue();
            return hue >= 35f && hue <= 75f;
        }

        private void CalculateView()
        {
            if (layoutFrozen && viewCalculated) return;
            List<PageTile> tiles = Document.GetCoveredTiles();
            if (tiles.Count == 0)
            {
                leftColumn = 0;
                topRow = 0;
                columns = 1;
                rows = 1;
            }
            else
            {
                leftColumn = tiles.Min(t => t.Column);
                int rightColumn = tiles.Max(t => t.Column);
                topRow = tiles.Min(t => t.Row);
                int bottomRow = tiles.Max(t => t.Row);
                columns = rightColumn - leftColumn + 1;
                rows = bottomRow - topRow + 1;
            }
            float availableWidth = Math.Max(80, ClientSize.Width - 56);
            float availableHeight = Math.Max(80, ClientSize.Height - 72);
            // After an edit, always fit the complete grid (which also contains the
            // image bounds) back into the viewport. Grid enlargement is represented
            // by the inverse image/grid ratio rather than overflowing the screen.
            documentScale = Math.Min(availableWidth / (columns * Document.TileWidth),
                availableHeight / (rows * Document.TileHeight)) * Math.Min(gridZoom, 1f) *
                viewportZoom;
            documentScale = Math.Max(0.01f, documentScale);
            float renderedWidth = columns * Document.TileWidth * documentScale;
            float renderedHeight = rows * Document.TileHeight * documentScale;
            originX = (ClientSize.Width - renderedWidth) / 2f + viewportPanX;
            originY = (ClientSize.Height - renderedHeight) / 2f + 14f + viewportPanY;
            viewCalculated = true;
        }
    }

    internal sealed class SplitPageView : ImageInteractionControl
    {
        private enum BoundarySide { None, Left, Right, Top, Bottom }
        private readonly PageTile tile;
        private readonly RectangleF editingBoundary;
        private readonly bool ownsDocument;
        private readonly bool independentEditing;
        private readonly bool stopLeft;
        private readonly bool stopRight;
        private readonly bool stopTop;
        private readonly bool stopBottom;
        private readonly PageMargins pageMargins;
        private float documentScale;
        private BoundarySide blockedSide;
        private readonly Stack<ImageDocumentState> localUndo = new Stack<ImageDocumentState>();
        private readonly Stack<ImageDocumentState> localRedo = new Stack<ImageDocumentState>();
        private ImageDocumentState localPending;
        private bool resizingImage;
        private PointF resizeFixedScreen;
        private PointF resizeSourceAnchor;
        private float resizeStartDistance;
        private float resizeStartScale;

        public SplitPageView(ImageDocument document, PageTile tile, RectangleF boundary,
            bool independentEditing, bool ownsDocument, bool stopLeft, bool stopRight,
            bool stopTop, bool stopBottom, PageMargins pageMargins) : base(document)
        {
            this.tile = tile;
            editingBoundary = boundary;
            this.independentEditing = independentEditing;
            this.ownsDocument = ownsDocument;
            this.stopLeft = stopLeft;
            this.stopRight = stopRight;
            this.stopTop = stopTop;
            this.stopBottom = stopBottom;
            this.pageMargins = pageMargins;
            Size = new Size(255, 382);
            Margin = new Padding(14);
            BackColor = Color.FromArgb(228, 233, 239);
            Cursor = Cursors.SizeAll;
            ToolTipText = "왼쪽 드래그로 전체 이미지 위치를, 휠로 크기를, 오른쪽 드래그로 회전각을 조절합니다.";
        }

        public string ToolTipText { get; private set; }
        public PageTile Tile { get { return tile; } }
        public ImageDocument RenderDocument { get { return Document; } }
        public bool SaveExcluded { get; set; }
        public bool IsSelected { get; set; }
        public event MouseEventHandler ViewZoomRequested;
        public event EventHandler SelectionRequested;

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                MouseEventHandler handler = ViewZoomRequested;
                if (handler != null) handler(this, e);
                return;
            }
            base.OnMouseWheel(e);
        }

        public void ResetLocalTransform()
        {
            if (independentEditing) localUndo.Push(Document.CaptureState());
            Document.ResetTransform();
            localRedo.Clear();
            NotifyChanged();
            Invalidate();
        }

        public void ApplyLocalEdit(Action<ImageDocument> action)
        {
            if (!independentEditing || action == null) return;
            localUndo.Push(Document.CaptureState());
            action(Document);
            localRedo.Clear();
            Invalidate();
            Update();
        }

        public void UndoLocal()
        {
            if (!independentEditing || localUndo.Count == 0) return;
            localRedo.Push(Document.CaptureState());
            Document.RestoreState(localUndo.Pop());
            Invalidate();
        }

        public void RedoLocal()
        {
            if (!independentEditing || localRedo.Count == 0) return;
            localUndo.Push(Document.CaptureState());
            Document.RestoreState(localRedo.Pop());
            Invalidate();
        }

        protected override float GetDocumentScale()
        {
            return documentScale;
        }

        protected override void ApplyWheelChange(MouseEventArgs e)
        {
            float multiplier = e.Delta > 0 ? 1.08f : 1f / 1.08f;
            base.ApplyWheelChange(e);
        }

        protected override void InteractionEnding()
        {
            if (independentEditing && localPending != null)
            {
                ImageDocumentState current = Document.CaptureState();
                if (!localPending.SameAs(current))
                {
                    localUndo.Push(localPending);
                    localRedo.Clear();
                }
                localPending = null;
            }
            blockedSide = BoundarySide.None;
            Invalidate();
        }

        protected override void InteractionStarting()
        {
            if (independentEditing) localPending = Document.CaptureState();
        }

        protected override void ApplyMoveChange(float dx, float dy)
        {
            if ((ModifierKeys & Keys.Control) != Keys.Control)
            {
                Document.Move(dx, dy);
                return;
            }

            RectangleF bounds = GetEditableBounds();
            RectangleF printableBoundary = GetPrintableEditingBoundary();
            float allowedDx = dx;
            float allowedDy = dy;
            if (dx < 0f && bounds.Left + dx < printableBoundary.Left)
                allowedDx = Math.Min(0f, printableBoundary.Left - bounds.Left);
            else if (dx > 0f && bounds.Right + dx > printableBoundary.Right)
                allowedDx = Math.Max(0f, printableBoundary.Right - bounds.Right);
            if (dy < 0f && bounds.Top + dy < printableBoundary.Top)
                allowedDy = Math.Min(0f, printableBoundary.Top - bounds.Top);
            else if (dy > 0f && bounds.Bottom + dy > printableBoundary.Bottom)
                allowedDy = Math.Max(0f, printableBoundary.Bottom - bounds.Bottom);
            Document.Move(allowedDx, allowedDy);
        }

        private RectangleF GetPrintableEditingBoundary()
        {
            return editingBoundary;
        }

        private RectangleF GetEditableBounds()
        {
            PointF[] corners = Document.GetEditableCorners();
            return RectangleF.FromLTRB(corners.Min(p => p.X), corners.Min(p => p.Y),
                corners.Max(p => p.X), corners.Max(p => p.Y));
        }

        private static bool IsMovingOutward(BoundarySide side, float dx, float dy)
        {
            return (side == BoundarySide.Left && dx < 0f) ||
                (side == BoundarySide.Right && dx > 0f) ||
                (side == BoundarySide.Top && dy < 0f) ||
                (side == BoundarySide.Bottom && dy > 0f);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool imageEditingArea = IsPointOnVisibleImage(e.Location) ||
                    IsNearImageEdge(e.Location, GetImageCornersOnScreen());
                if (!imageEditingArea)
                {
                    EventHandler selectionHandler = SelectionRequested;
                    if (selectionHandler != null) selectionHandler(this, EventArgs.Empty);
                    return;
                }
            }
            if (e.Button == MouseButtons.Left && Document.HasImage)
            {
                PointF[] corners = GetImageCornersOnScreen();
                if (IsNearImageEdge(e.Location, corners))
                {
                    int dragged = FindNearestCorner(e.Location, corners);
                    int fixedCorner = (dragged + 2) % 4;
                    resizeFixedScreen = corners[fixedCorner];
                    resizeSourceAnchor = GetSourceCorner(fixedCorner);
                    resizeStartDistance = Math.Max(1f, Distance(e.Location, resizeFixedScreen));
                    resizeStartScale = Document.Scale;
                    resizingImage = true;
                    Focus();
                    BeginInteraction();
                    Capture = true;
                    return;
                }
            }
            base.OnMouseDown(e);
        }

        private bool IsPointOnVisibleImage(Point point)
        {
            if (!Document.HasImage) return false;
            float pageX, pageY, pageWidth, pageHeight;
            CalculatePageGeometry(out pageX, out pageY, out pageWidth, out pageHeight);
            RectangleF printable = pageMargins.GetPrintableRectangle();
            RectangleF visiblePage = new RectangleF(
                pageX + printable.Left * documentScale,
                pageY + printable.Top * documentScale,
                printable.Width * documentScale,
                printable.Height * documentScale);
            if (!visiblePage.Contains(point)) return false;
            PointF world = new PointF(
                tile.Column * Document.TileWidth +
                    (point.X - pageX) / documentScale - printable.Left,
                tile.Row * Document.TileHeight +
                    (point.Y - pageY) / documentScale - printable.Top);
            return Document.ContainsVisibleImageAtWorld(world);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (resizingImage)
            {
                float ratio = Math.Max(0.05f,
                    Distance(e.Location, resizeFixedScreen) / resizeStartDistance);
                float targetScale = resizeStartScale * ratio;
                float multiplier = targetScale / Math.Max(Document.Scale, 0.0001f);
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                    Document.ScaleKeepingSourcePointWithin(multiplier, resizeSourceAnchor,
                        GetPrintableEditingBoundary());
                else
                    Document.ScaleKeepingSourcePoint(multiplier, resizeSourceAnchor);
                NotifyChanged();
                return;
            }
            if (e.Button == MouseButtons.None && Document.HasImage)
                Cursor = IsNearImageEdge(e.Location, GetImageCornersOnScreen())
                    ? Cursors.SizeNWSE : Cursors.SizeAll;
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (resizingImage)
            {
                resizingImage = false;
                Capture = false;
                Cursor = Cursors.SizeAll;
                CompleteInteraction();
                return;
            }
            base.OnMouseUp(e);
        }

        private PointF[] GetImageCornersOnScreen()
        {
            float pageX, pageY, pageWidth, pageHeight;
            CalculatePageGeometry(out pageX, out pageY, out pageWidth, out pageHeight);
            PointF[] world = Document.GetEditableCorners();
            RectangleF printable = pageMargins.GetPrintableRectangle();
            for (int index = 0; index < world.Length; index++)
            {
                world[index] = new PointF(
                    pageX + (printable.Left + world[index].X -
                        tile.Column * Document.TileWidth) * documentScale,
                    pageY + (printable.Top + world[index].Y -
                        tile.Row * Document.TileHeight) * documentScale);
            }
            return world;
        }

        private void CalculatePageGeometry(out float x, out float y, out float width, out float height)
        {
            const float headingHeight = 28f;
            float availableWidth = Math.Max(1, ClientSize.Width - 18);
            float availableHeight = Math.Max(1, ClientSize.Height - headingHeight - 16);
            documentScale = Math.Min(availableWidth / ImageDocument.PageWidth,
                availableHeight / ImageDocument.PageHeight);
            width = ImageDocument.PageWidth * documentScale;
            height = ImageDocument.PageHeight * documentScale;
            x = (ClientSize.Width - width) / 2f;
            y = headingHeight + (availableHeight - height) / 2f;
        }

        private static bool IsNearImageEdge(PointF point, PointF[] corners)
        {
            for (int index = 0; index < corners.Length; index++)
                if (DistanceToSegment(point, corners[index], corners[(index + 1) % corners.Length]) <= 8f)
                    return true;
            return false;
        }

        private static int FindNearestCorner(PointF point, PointF[] corners)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int index = 0; index < corners.Length; index++)
            {
                float distance = Distance(point, corners[index]);
                if (distance < best) { best = distance; nearest = index; }
            }
            return nearest;
        }

        private PointF GetSourceCorner(int index)
        {
            PointF[] corners = Document.GetEditableSourceCorners();
            return corners[Math.Max(0, Math.Min(corners.Length - 1, index))];
        }

        private static float Distance(PointF first, PointF second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float DistanceToSegment(PointF point, PointF start, PointF end)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float length = dx * dx + dy * dy;
            if (length <= 0.001f) return Distance(point, start);
            float amount = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / length;
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Distance(point, new PointF(start.X + dx * amount, start.Y + dy * amount));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float x, y, width, height;
            CalculatePageGeometry(out x, out y, out width, out height);

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
            string heading = string.Format("A4 {0}{1}{2}", tile,
                independentEditing ? "  [고정]" : string.Empty,
                SaveExcluded ? "  [저장 제외]" : string.Empty);
            TextRenderer.DrawText(e.Graphics, heading, Font,
                new Rectangle(8, 5, Width - 16, 20),
                SaveExcluded ? Color.Firebrick : Color.FromArgb(45, 57, 70),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(shadow, x + 3, y + 3, width, height);
            }
            using (SolidBrush pageBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(pageBrush, x, y, width, height);
            }
            GraphicsState state = e.Graphics.Save();
            e.Graphics.TranslateTransform(x, y);
            e.Graphics.ScaleTransform(documentScale, documentScale);
            Document.DrawPage(e.Graphics, tile, pageMargins, false);
            e.Graphics.Restore(state);
            using (Pen border = new Pen(Color.FromArgb(121, 133, 146)))
            {
                e.Graphics.DrawRectangle(border, x, y, width, height);
            }
            RectangleF printable = pageMargins.GetPrintableRectangle();
            using (Pen marginPen = new Pen(Color.FromArgb(180, 220, 70, 55)))
            {
                marginPen.DashStyle = DashStyle.Dash;
                e.Graphics.DrawRectangle(marginPen,
                    x + printable.X * documentScale,
                    y + printable.Y * documentScale,
                    printable.Width * documentScale,
                    printable.Height * documentScale);
            }
            DrawImageResizeHandles(e.Graphics);
            if (IsSelected)
            {
                using (Pen selectedPen = new Pen(Color.FromArgb(245, 255, 232, 135), 3f))
                    e.Graphics.DrawRectangle(selectedPen, 2, 2, Width - 5, Height - 5);
            }
        }

        private void DrawImageResizeHandles(Graphics graphics)
        {
            PointF[] corners = GetImageCornersOnScreen();
            if (corners.Length != 4) return;
            using (Pen outline = new Pen(Color.FromArgb(190, 0, 145, 210), 1.5f))
            using (SolidBrush handle = new SolidBrush(Color.FromArgb(230, 0, 145, 210)))
            {
                graphics.DrawPolygon(outline, corners);
                foreach (PointF point in corners)
                    graphics.FillRectangle(handle, point.X - 4f, point.Y - 4f, 8f, 8f);
            }
        }

        private void DrawBlockedEdge(Graphics graphics, float x, float y, float width, float height)
        {
            if (blockedSide == BoundarySide.None) return;
            using (Pen highlight = new Pen(Color.FromArgb(230, 255, 120, 0), 6f))
            {
                if (blockedSide == BoundarySide.Left)
                    graphics.DrawLine(highlight, x, y, x, y + height);
                else if (blockedSide == BoundarySide.Right)
                    graphics.DrawLine(highlight, x + width, y, x + width, y + height);
                else if (blockedSide == BoundarySide.Top)
                    graphics.DrawLine(highlight, x, y, x + width, y);
                else if (blockedSide == BoundarySide.Bottom)
                    graphics.DrawLine(highlight, x, y + height, x + width, y + height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && ownsDocument) Document.Dispose();
        }
    }

    internal sealed class EditorState
    {
        public ImageDocumentState DocumentState;
        public float GridZoom;

        public bool SameAs(EditorState other)
        {
            return other != null && GridZoom == other.GridZoom &&
                DocumentState.SameAs(other.DocumentState);
        }
    }

    internal sealed class EnhancedPrintPreviewForm : Form
    {
        private readonly PrintPreviewControl preview = new PrintPreviewControl();
        private readonly ToolStripLabel pageLabel = new ToolStripLabel();
        private readonly PrintDocument document;
        private readonly int pageCount;

        public EnhancedPrintPreviewForm(PrintDocument document, int pageCount)
        {
            this.document = document;
            this.pageCount = Math.Max(1, pageCount);
            Text = "인쇄 미리보기";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 650);

            ToolStrip tools = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            AddButton(tools, "프린터 설정", ShowPrinterSettings);
            AddButton(tools, "인쇄", PrintNow);
            tools.Items.Add(new ToolStripSeparator());
            AddButton(tools, "◀ 이전", delegate { ChangePage(-1); });
            AddButton(tools, "다음 ▶", delegate { ChangePage(1); });
            tools.Items.Add(pageLabel);
            tools.Items.Add(new ToolStripSeparator());
            AddButton(tools, "한 페이지", delegate
            {
                preview.Rows = 1; preview.Columns = 1; preview.AutoZoom = true;
            });
            AddButton(tools, "두 페이지", delegate
            {
                preview.Rows = 1; preview.Columns = 2; preview.AutoZoom = true;
            });
            AddButton(tools, "화면 맞춤", delegate { preview.AutoZoom = true; });
            ToolStripComboBox zoom = new ToolStripComboBox { Width = 75 };
            zoom.Items.AddRange(new object[] { "50%", "75%", "100%", "125%", "150%", "200%" });
            zoom.Text = "100%";
            zoom.SelectedIndexChanged += delegate
            {
                int percent;
                if (int.TryParse(zoom.Text.Replace("%", ""), out percent))
                {
                    preview.AutoZoom = false;
                    preview.Zoom = percent / 100d;
                }
            };
            tools.Items.Add(new ToolStripLabel("배율"));
            tools.Items.Add(zoom);
            tools.Items.Add(new ToolStripSeparator());
            AddButton(tools, "닫기", delegate { Close(); });

            preview.Dock = DockStyle.Fill;
            preview.Document = document;
            preview.BackColor = Color.FromArgb(105, 105, 105);
            preview.UseAntiAlias = true;
            preview.AutoZoom = true;
            Controls.Add(preview);
            Controls.Add(tools);
            tools.Dock = DockStyle.Top;
            UpdatePageLabel();
        }

        private static ToolStripButton AddButton(ToolStrip tools, string text, EventHandler action)
        {
            ToolStripButton button = new ToolStripButton(text);
            button.Click += action;
            tools.Items.Add(button);
            return button;
        }

        private void ChangePage(int amount)
        {
            preview.StartPage = Math.Max(0, Math.Min(pageCount - 1,
                preview.StartPage + amount));
            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            pageLabel.Text = string.Format("  {0} / {1} 페이지  ",
                preview.StartPage + 1, pageCount);
        }

        private void ShowPrinterSettings(object sender, EventArgs e)
        {
            using (PrintDialog dialog = new PrintDialog())
            {
                dialog.Document = document;
                dialog.UseEXDialog = true;
                dialog.ShowDialog(this);
            }
        }

        private void PrintNow(object sender, EventArgs e)
        {
            using (PrintDialog dialog = new PrintDialog())
            {
                dialog.Document = document;
                dialog.UseEXDialog = true;
                if (dialog.ShowDialog(this) == DialogResult.OK) document.Print();
            }
        }
    }

    internal sealed class HelpForm : Form
    {
        public HelpForm()
        {
            Text = "도움말";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 620);
            MinimumSize = new Size(620, 500);
            TextBox contents = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Font = new Font("맑은 고딕", 10f),
                Text =
                    "A4 이미지 분할 편집기 도움말\r\n\r\n" +
                    "[상단 메뉴]\r\n" +
                    "○ 이미지 열기: 편집할 이미지를 불러옵니다.\r\n" +
                    "○ A4에 맞춤: 이미지를 A4 인쇄 가능 높이 또는 너비에 맞춥니다.\r\n" +
                    "○ 90°/180° 회전, 좌우/상하 반전: 선택된 이미지에 변환을 적용합니다.\r\n" +
                    "○ 회전각: 선택 이미지의 회전각을 직접 입력합니다.\r\n" +
                    "○ 임시 분할: 각 A4 페이지와 여백을 확인합니다.\r\n" +
                    "○ 격자 보기: 전체 이미지를 이어진 격자로 확인합니다.\r\n" +
                    "○ 저장: 저장 제외 페이지를 빼고 이미지 파일을 생성합니다.\r\n" +
                    "○ 인쇄: 확장 인쇄 미리보기에서 설정 후 순차 인쇄합니다.\r\n" +
                    "○ 여백 설정: 상하좌우 인쇄 여백을 mm 단위로 지정합니다.\r\n\r\n" +
                    "[선택과 편집]\r\n" +
                    "○ 이미지가 없는 페이지 공간 클릭: 한 격자를 선택합니다.\r\n" +
                    "○ Ctrl+빈 공간 클릭: 여러 격자를 추가 선택하거나 선택 해제합니다.\r\n" +
                    "○ 빈 배경 드래그: 사각형 안의 페이지를 다중 선택합니다.\r\n" +
                    "○ 선택된 고정 이미지는 맞춤, 회전, 반전, 회전각 기능을 사용할 수 있습니다.\r\n" +
                    "○ 이미지 위 왼쪽 드래그: 이미지 이동. 파란 경계 드래그: 크기 조절.\r\n" +
                    "○ 이미지 위 오른쪽 드래그: 이미지 회전.\r\n\r\n" +
                    "[Ctrl 조작]\r\n" +
                    "○ Ctrl+휠: 커서 위치를 중심으로 화면 확대/축소.\r\n" +
                    "○ Ctrl+이동/크기 조절: 현재 인쇄 가능 경계를 넘지 않도록 제한.\r\n" +
                    "○ Ctrl+Z: 이전 작업, Ctrl+X: 다음 작업."
            };
            Button close = new Button { Text = "닫기", Dock = DockStyle.Bottom, Height = 36 };
            close.Click += delegate { Close(); };
            Controls.Add(contents);
            Controls.Add(close);
            Shown += delegate
            {
                contents.SelectionLength = 0;
                close.Focus();
            };
        }
    }

    public sealed class MainForm : Form
    {
        private readonly ImageDocument document = new ImageDocument();
        private readonly GridPreview gridPreview;
        private readonly Panel splitPanel;
        private readonly Panel previewHost;
        private readonly NumericUpDown rotationBox;
        private readonly ToolStripButton temporarySplitButton;
        private readonly ToolStripButton finalizeButton;
        private readonly ToolStripButton printButton;
        private readonly ToolStripButton returnToGridButton;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly ToolTip toolTip = new ToolTip();
        private readonly PageMargins pageMargins = new PageMargins();
        private readonly ContextMenuStrip editMenu;
        private readonly ToolStripMenuItem gridViewMenuItem;
        private readonly ToolStripMenuItem lockSplitMenuItem;
        private readonly ToolStripMenuItem unlockSplitMenuItem;
        private readonly ToolStripMenuItem excludeSaveMenuItem;
        private readonly Stack<EditorState> undoHistory = new Stack<EditorState>();
        private readonly Stack<EditorState> redoHistory = new Stack<EditorState>();
        private List<PageTile> shownTiles = new List<PageTile>();
        private bool splitMode;
        private readonly Dictionary<PageTile, ImageDocument> lockedSplitDocuments =
            new Dictionary<PageTile, ImageDocument>();
        private readonly Dictionary<PageTile, PointF[]> lockedSourcePolygons =
            new Dictionary<PageTile, PointF[]>();
        private readonly HashSet<PageTile> excludedSaveTiles = new HashSet<PageTile>();
        private bool applyingRotation;
        private bool interactiveEdit;
        private EditorState pendingEdit;
        private SplitPageView activeIndependentPage;
        private float splitViewZoom = 1f;
        private readonly HashSet<PageTile> selectedTiles = new HashSet<PageTile>();
        private bool selectingPages;
        private bool selectionAdditive;
        private Point selectionStart;
        private Point selectionCurrent;

        public MainForm()
        {
            document.SetTileSize(pageMargins.PrintableWidth, pageMargins.PrintableHeight);
            Text = "A4 이미지 분할 편집기";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(920, 680);
            Size = new Size(1260, 850);
            AllowDrop = true;

            ToolStrip tools = new ToolStrip();
            tools.GripStyle = ToolStripGripStyle.Hidden;
            tools.Padding = new Padding(8, 6, 8, 6);
            ToolStripButton openButton = AddButton(tools, "이미지 열기", OpenImage);
            tools.Items.Add(new ToolStripSeparator());
            AddButton(tools, "A4에 맞춤", FitSelectedImages);
            AddButton(tools, "90° 회전", delegate { RotateSelectedImages(90f); });
            AddButton(tools, "180° 회전", delegate { RotateSelectedImages(180f); });
            AddButton(tools, "좌우 반전", delegate { FlipSelectedImages(true); });
            AddButton(tools, "상하 반전", delegate { FlipSelectedImages(false); });
            tools.Items.Add(new ToolStripLabel("회전각"));
            rotationBox = new NumericUpDown();
            rotationBox.Minimum = -180;
            rotationBox.Maximum = 180;
            rotationBox.DecimalPlaces = 1;
            rotationBox.Increment = 0.5m;
            rotationBox.Width = 70;
            rotationBox.ValueChanged += RotationBoxChanged;
            tools.Items.Add(new ToolStripControlHost(rotationBox));
            tools.Items.Add(new ToolStripLabel("°"));
            tools.Items.Add(new ToolStripSeparator());
            temporarySplitButton = AddButton(tools, "임시 분할", ShowTemporarySplit);
            returnToGridButton = AddButton(tools, "격자 보기", ShowGrid);
            finalizeButton = AddButton(tools, "저장", FinalizeSplit);
            printButton = AddButton(tools, "인쇄", PrintSplit);
            AddButton(tools, "여백 설정", ShowMarginSettings);
            returnToGridButton.Visible = false;
            temporarySplitButton.Visible = true;
            tools.Items.Add(new ToolStripSeparator());
            AddButton(tools, "도움말", ShowHelp);
            toolTip.SetToolTip(openButton.GetCurrentParent(), "지원 형식: JPG, PNG, GIF, BMP, TIFF, WEBP(Windows 코덱이 설치된 경우)");

            previewHost = new Panel();
            previewHost.Dock = DockStyle.Fill;
            gridPreview = new GridPreview(document, pageMargins);
            gridPreview.Dock = DockStyle.Fill;
            gridPreview.DragEnter += ImageDragEnter;
            gridPreview.DragDrop += ImageDragDrop;
            WireEditingControl(gridPreview);
            previewHost.Controls.Add(gridPreview);

            splitPanel = new BufferedPanel();
            splitPanel.Dock = DockStyle.Fill;
            splitPanel.AutoScroll = true;
            splitPanel.Padding = new Padding(18);
            splitPanel.Visible = false;
            splitPanel.BackColor = Color.FromArgb(238, 241, 245);
            splitPanel.Resize += delegate { ResizeSplitPages(); };
            splitPanel.MouseWheel += SplitPanelMouseWheel;
            splitPanel.MouseEnter += delegate { splitPanel.Focus(); };
            splitPanel.MouseDown += SplitPanelSelectionMouseDown;
            splitPanel.MouseMove += SplitPanelSelectionMouseMove;
            splitPanel.MouseUp += SplitPanelSelectionMouseUp;
            splitPanel.Paint += DrawSplitSelectionRectangle;

            editMenu = new ContextMenuStrip();
            editMenu.Items.Add("원래상태로", null, ResetToOriginal);
            editMenu.Items.Add("임시 분할", null, ShowTemporarySplit);
            gridViewMenuItem = new ToolStripMenuItem("격자 보기", null, ShowGrid);
            lockSplitMenuItem = new ToolStripMenuItem("이미지 분할 고정", null, LockSplitEditing);
            unlockSplitMenuItem = new ToolStripMenuItem("이미지 분할 고정 해제", null, UnlockSplitEditing);
            excludeSaveMenuItem = new ToolStripMenuItem("저장 제외", null, ToggleSaveExclusion);
            editMenu.Items.Add(gridViewMenuItem);
            editMenu.Items.Add(new ToolStripSeparator());
            editMenu.Items.Add(lockSplitMenuItem);
            editMenu.Items.Add(unlockSplitMenuItem);
            editMenu.Items.Add(new ToolStripSeparator());
            editMenu.Items.Add(excludeSaveMenuItem);
            editMenu.Items.Add("저장", null, FinalizeSplit);
            editMenu.Opening += delegate
            {
                bool hasImage = document.HasImage;
                editMenu.Items[0].Enabled = hasImage;
                editMenu.Items[1].Enabled = hasImage && !splitMode;
                gridViewMenuItem.Visible = splitMode;
                SplitPageView sourcePage = editMenu.SourceControl as SplitPageView;
                bool selectedLocked = sourcePage != null && lockedSplitDocuments.ContainsKey(sourcePage.Tile);
                lockSplitMenuItem.Visible = splitMode && sourcePage != null && !selectedLocked;
                unlockSplitMenuItem.Visible = splitMode && selectedLocked;
                excludeSaveMenuItem.Visible = splitMode && sourcePage != null;
                excludeSaveMenuItem.Checked = sourcePage != null && excludedSaveTiles.Contains(sourcePage.Tile);
                editMenu.Items[editMenu.Items.Count - 1].Enabled = hasImage &&
                    GetOutputTiles().Count > 0 && GetOutputTiles().Count <= 300;
            };
            gridPreview.ContextMenuStrip = editMenu;
            splitPanel.ContextMenuStrip = editMenu;

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(statusLabel);

            Controls.Add(previewHost);
            Controls.Add(splitPanel);
            Controls.Add(statusStrip);
            Controls.Add(tools);
            MainMenuStrip = new MenuStrip();
            DragEnter += ImageDragEnter;
            DragDrop += ImageDragDrop;
            FormClosed += delegate { document.Dispose(); };
            SetStatus("A4 빈 용지에 이미지를 놓아 시작하세요.");
            UpdateCommandState();
        }

        private static ToolStripButton AddButton(ToolStrip strip, string text, EventHandler action)
        {
            ToolStripButton button = new ToolStripButton(text);
            button.DisplayStyle = ToolStripItemDisplayStyle.Text;
            button.AutoToolTip = true;
            button.Click += action;
            strip.Items.Add(button);
            return button;
        }

        private void WireEditingControl(ImageInteractionControl control, bool independent = false)
        {
            if (independent)
            {
                control.DocumentChanged += delegate
                {
                    control.Invalidate();
                    control.Update();
                };
                control.InteractionStarted += delegate
                {
                    activeIndependentPage = control as SplitPageView;
                };
                control.InteractionCompleted += delegate { control.Invalidate(); };
                return;
            }
            control.DocumentChanged += delegate { NotifyDocumentChanged(); };
            control.InteractionStarted += delegate
            {
                interactiveEdit = true;
                BeginEdit();
            };
            control.InteractionCompleted += delegate
            {
                interactiveEdit = false;
                CommitEdit();
                NotifyDocumentChanged();
            };
        }

        private EditorState CaptureEditorState()
        {
            return new EditorState
            {
                DocumentState = document.CaptureState(),
                GridZoom = gridPreview.GridZoom
            };
        }

        private void BeginEdit()
        {
            if (!document.HasImage || pendingEdit != null) return;
            pendingEdit = CaptureEditorState();
        }

        private void CommitEdit()
        {
            if (pendingEdit == null || !document.HasImage) return;
            EditorState current = CaptureEditorState();
            if (!pendingEdit.SameAs(current))
            {
                undoHistory.Push(pendingEdit);
                redoHistory.Clear();
            }
            pendingEdit = null;
        }

        private void PerformEdit(Action action)
        {
            if (!document.HasImage) return;
            BeginEdit();
            action();
            CommitEdit();
            NotifyDocumentChanged();
        }

        private List<SplitPageView> GetSelectedLockedPages()
        {
            return splitPanel.Controls.OfType<SplitPageView>()
                .Where(page => selectedTiles.Contains(page.Tile) &&
                    lockedSplitDocuments.ContainsKey(page.Tile)).ToList();
        }

        private bool SelectionEditsMainDocument()
        {
            return selectedTiles.Count == 0 ||
                selectedTiles.Any(tile => !lockedSplitDocuments.ContainsKey(tile));
        }

        private void FitSelectedImages(object sender, EventArgs e)
        {
            if (!document.HasImage) return;
            foreach (SplitPageView page in GetSelectedLockedPages())
            {
                PageTile tile = page.Tile;
                page.ApplyLocalEdit(target => FitLockedDocumentToTile(target, tile));
            }
            if (SelectionEditsMainDocument()) PerformEdit(FitDocumentPreservingLocks);
            else
            {
                RefreshPageSelection();
                UpdateRotationForSelection();
            }
        }

        private void FitLockedDocumentToTile(ImageDocument target, PageTile tile)
        {
            PointF[] sourceCorners = target.GetEditableSourceCorners();
            float sourceLeft = sourceCorners.Min(point => point.X);
            float sourceRight = sourceCorners.Max(point => point.X);
            float sourceTop = sourceCorners.Min(point => point.Y);
            float sourceBottom = sourceCorners.Max(point => point.Y);
            const float inset = 2f;
            float targetScale = Math.Min(
                (target.TileWidth - inset * 2f) / Math.Max(1f, sourceRight - sourceLeft),
                (target.TileHeight - inset * 2f) / Math.Max(1f, sourceBottom - sourceTop));
            PointF sourceCenter = new PointF((sourceLeft + sourceRight) / 2f,
                (sourceTop + sourceBottom) / 2f);
            target.ScaleKeepingSourcePoint(targetScale / Math.Max(target.Scale, 0.0001f),
                sourceCenter);
            RectangleF bounds = target.GetEditableBounds();
            PointF tileCenter = new PointF((tile.Column + 0.5f) * target.TileWidth,
                (tile.Row + 0.5f) * target.TileHeight);
            target.Move(tileCenter.X - (bounds.Left + bounds.Width / 2f),
                tileCenter.Y - (bounds.Top + bounds.Height / 2f));
        }

        private void RotateSelectedImages(float degrees)
        {
            foreach (SplitPageView page in GetSelectedLockedPages())
                page.ApplyLocalEdit(target => TransformKeepingVisibleCenter(target,
                    delegate { target.Rotate(degrees); }));
            if (SelectionEditsMainDocument())
                PerformEdit(delegate
                {
                    TransformDocumentPreservingLockedPosition(delegate { document.Rotate(degrees); });
                });
            else UpdateRotationForSelection();
        }

        private void FlipSelectedImages(bool horizontal)
        {
            foreach (SplitPageView page in GetSelectedLockedPages())
                page.ApplyLocalEdit(target => TransformKeepingVisibleCenter(target, delegate
                {
                    if (horizontal) target.ToggleHorizontalFlip();
                    else target.ToggleVerticalFlip();
                }));
            if (SelectionEditsMainDocument())
                PerformEdit(delegate
                {
                    TransformDocumentPreservingLockedPosition(horizontal
                        ? (Action)document.ToggleHorizontalFlip
                        : document.ToggleVerticalFlip);
                });
            else UpdateRotationForSelection();
        }

        private static void TransformKeepingVisibleCenter(ImageDocument target, Action transform)
        {
            RectangleF before = target.GetEditableBounds();
            PointF center = new PointF(before.Left + before.Width / 2f,
                before.Top + before.Height / 2f);
            transform();
            RectangleF after = target.GetEditableBounds();
            target.Move(center.X - (after.Left + after.Width / 2f),
                center.Y - (after.Top + after.Height / 2f));
        }

        private void SetSelectedRotation(float degrees)
        {
            foreach (SplitPageView page in GetSelectedLockedPages())
                page.ApplyLocalEdit(target => TransformKeepingVisibleCenter(target,
                    delegate { target.SetRotation(degrees); }));
            if (SelectionEditsMainDocument())
                PerformEdit(delegate
                {
                    TransformDocumentPreservingLockedPosition(delegate
                    {
                        document.SetRotation(degrees);
                    });
                });
        }

        private void FitDocumentPreservingLocks()
        {
            if (lockedSplitDocuments.Count == 0)
            {
                document.FitForNaturalSplit();
                return;
            }
            List<PageTile> occupiedTiles = document.GetCoveredTiles();
            int targetColumn = occupiedTiles.Count == 0 ? 0 :
                occupiedTiles.Min(tile => tile.Column);
            int targetRow = occupiedTiles.Count == 0 ? 0 :
                occupiedTiles.Min(tile => tile.Row);
            float imageRatio = (float)document.Source.Width / document.Source.Height;
            const float fitInset = 2f;
            float targetScale = imageRatio >= document.TileWidth / document.TileHeight
                ? (document.TileHeight - fitInset * 2f) / document.Source.Height
                : (document.TileWidth - fitInset * 2f) / document.Source.Width;
            PointF[] editableSource = document.GetEditableSourceCorners();
            PointF anchor = new PointF(editableSource.Average(point => point.X),
                editableSource.Average(point => point.Y));
            document.ScaleKeepingSourcePoint(
                targetScale / Math.Max(document.Scale, 0.0001f), anchor);
            RectangleF fittedBounds = document.GetEditableBounds();
            // Keep the fitted edges just inside the logical tile boundary. GDI+
            // region scans can expand an exact floating-point edge by a fraction
            // of a pixel and otherwise create an unwanted extra row or column.
            document.Move(targetColumn * document.TileWidth + fitInset - fittedBounds.Left,
                targetRow * document.TileHeight + fitInset - fittedBounds.Top);
        }

        private void TransformDocumentPreservingLockedPosition(Action transform)
        {
            if (lockedSplitDocuments.Count == 0)
            {
                transform();
                return;
            }
            RectangleF before = document.GetEditableBounds();
            PointF fixedCenter = new PointF(before.Left + before.Width / 2f,
                before.Top + before.Height / 2f);
            transform();
            RectangleF after = document.GetEditableBounds();
            document.Move(fixedCenter.X - (after.Left + after.Width / 2f),
                fixedCenter.Y - (after.Top + after.Height / 2f));
        }

        private void ResetToOriginal(object sender, EventArgs e)
        {
            SplitPageView sourcePage = editMenu.SourceControl as SplitPageView;
            if (splitMode && sourcePage != null && lockedSplitDocuments.ContainsKey(sourcePage.Tile))
            {
                activeIndependentPage = sourcePage;
                sourcePage.ResetLocalTransform();
                SetStatus("선택한 A4 조각의 이미지를 원래 상태로 되돌렸습니다.");
                return;
            }
            PerformEdit(delegate
            {
                document.ResetTransform();
                gridPreview.SetGridZoom(1f);
            });
        }

        private void UndoEdit()
        {
            if (splitMode && activeIndependentPage != null &&
                lockedSplitDocuments.ContainsKey(activeIndependentPage.Tile))
            {
                activeIndependentPage.UndoLocal();
                SetStatus("선택한 A4 조각의 이전 작업으로 되돌렸습니다.");
                return;
            }
            if (!document.HasImage || undoHistory.Count == 0) return;
            redoHistory.Push(CaptureEditorState());
            ApplyEditorState(undoHistory.Pop());
            SetStatus("이전 작업으로 되돌렸습니다. Ctrl+X로 다시 실행할 수 있습니다.");
        }

        private void RedoEdit()
        {
            if (splitMode && activeIndependentPage != null &&
                lockedSplitDocuments.ContainsKey(activeIndependentPage.Tile))
            {
                activeIndependentPage.RedoLocal();
                SetStatus("선택한 A4 조각의 다음 작업을 다시 적용했습니다.");
                return;
            }
            if (!document.HasImage || redoHistory.Count == 0) return;
            undoHistory.Push(CaptureEditorState());
            ApplyEditorState(redoHistory.Pop());
            SetStatus("다음 작업을 다시 적용했습니다.");
        }

        private void ApplyEditorState(EditorState state)
        {
            pendingEdit = null;
            interactiveEdit = false;
            document.RestoreState(state.DocumentState);
            gridPreview.SetGridZoom(state.GridZoom);
            NotifyDocumentChanged();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                UndoEdit();
                return true;
            }
            if (keyData == (Keys.Control | Keys.X))
            {
                RedoEdit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowMarginSettings(object sender, EventArgs e)
        {
            using (MarginSettingsDialog dialog = new MarginSettingsDialog(pageMargins))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                dialog.ApplyTo(pageMargins);
            }
            document.SetTileSize(pageMargins.PrintableWidth, pageMargins.PrintableHeight);
            foreach (ImageDocument lockedDocument in lockedSplitDocuments.Values)
                lockedDocument.SetTileSize(pageMargins.PrintableWidth, pageMargins.PrintableHeight);
            gridPreview.SetGridZoom(gridPreview.GridZoom);
            if (splitMode)
            {
                List<PageTile> tiles = document.GetCoveredTiles()
                    .Union(lockedSplitDocuments.Keys)
                    .OrderBy(tile => tile.Row).ThenBy(tile => tile.Column).ToList();
                RebuildSplitPages(tiles);
            }
            else gridPreview.Invalidate();
            SetStatus(string.Format("여백: 위 {0:0.0}mm · 아래 {1:0.0}mm · 왼쪽 {2:0.0}mm · 오른쪽 {3:0.0}mm",
                pageMargins.Top, pageMargins.Bottom, pageMargins.Left, pageMargins.Right));
        }

        private void ShowHelp(object sender, EventArgs e)
        {
            using (HelpForm help = new HelpForm()) help.ShowDialog(this);
        }

        private void OpenImage(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "편집할 이미지 선택";
                dialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tif;*.tiff|모든 파일|*.*";
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) == DialogResult.OK) LoadImage(dialog.FileName);
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                ClearSplitPages();
                ClearLockedSplits();
                document.Load(path);
                gridPreview.SetGridZoom(1f);
                undoHistory.Clear();
                redoHistory.Clear();
                pendingEdit = null;
                interactiveEdit = false;
                activeIndependentPage = null;
                splitMode = false;
                previewHost.Visible = true;
                splitPanel.Visible = false;
                shownTiles.Clear();
                Text = string.Format("A4 이미지 분할 편집기 — {0}", Path.GetFileName(path));
                SetStatus("불러왔습니다. 격자에서 위치·크기·회전을 조절한 뒤 ‘임시 분할’을 누르세요.");
                NotifyDocumentChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "이미지를 열 수 없습니다.\n\n" + ex.Message, "열기 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImageDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                e.Effect = files != null && files.Length == 1 && IsSupportedImage(files[0])
                    ? DragDropEffects.Copy : DragDropEffects.None;
            }
        }

        private void ImageDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0) LoadImage(files[0]);
        }

        private static bool IsSupportedImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" ||
                   ext == ".bmp" || ext == ".tif" || ext == ".tiff";
        }

        private void ShowTemporarySplit(object sender, EventArgs e)
        {
            if (!document.HasImage)
            {
                MessageBox.Show(this, "먼저 편집할 이미지를 불러오세요.", "이미지가 필요합니다",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            document.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
            if (lockedSplitDocuments.Count > 0 && splitPanel.Controls.Count > 0)
            {
                splitMode = true;
                previewHost.Visible = false;
                splitPanel.Visible = true;
                SetStatus("일부 A4 조각이 고정되어 있습니다. 고정되지 않은 조각은 계속 연동 편집됩니다.");
                UpdateCommandState();
                return;
            }

            List<PageTile> tiles = document.GetCoveredTiles();
            if (tiles.Count > 300)
            {
                MessageBox.Show(this, "현재 설정은 300장보다 많은 A4 페이지를 만듭니다.\n크기를 줄인 후 다시 시도하세요.",
                    "분할 수가 너무 많습니다", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            splitMode = true;
            previewHost.Visible = false;
            splitPanel.Visible = true;
            RebuildSplitPages(tiles);
            SetStatus("임시 분할: 파란 이미지 끝단을 드래그해 크기를 조절합니다. Ctrl을 누른 채 이동하면 현재 격자를 넘지 않습니다.");
            UpdateCommandState();
        }

        private void ShowGrid(object sender, EventArgs e)
        {
            document.SetExcludedSourcePolygons(null);
            selectedTiles.Clear();
            activeIndependentPage = null;
            splitMode = false;
            splitPanel.Visible = false;
            previewHost.Visible = true;
            SetStatus("격자 편집 화면입니다. 이미지가 차지하는 A4 칸이 자동으로 표시됩니다.");
            UpdateCommandState();
            gridPreview.Invalidate();
        }

        private void LockSplitEditing(object sender, EventArgs e)
        {
            SplitPageView page = editMenu.SourceControl as SplitPageView;
            if (!splitMode || page == null || lockedSplitDocuments.ContainsKey(page.Tile)) return;
            PointF[] sourcePolygon = GetTileSourcePolygon(page.Tile);
            ImageDocument lockedDocument = document.CreateLinkedCopy();
            lockedDocument.SetIncludedSourcePolygon(sourcePolygon);
            // Preserve exactly what was visible at the moment of locking. Previously
            // locked source areas must not reappear inside this new fragment.
            lockedDocument.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
            lockedSplitDocuments[page.Tile] = lockedDocument;
            lockedSourcePolygons[page.Tile] = sourcePolygon;
            document.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
            activeIndependentPage = null;
            RebuildSplitPages(new List<PageTile>(shownTiles));
            SetStatus(string.Format("A4 {0} 조각을 고정했습니다. 나머지 조각은 고정 영역을 제외하고 계속 연동됩니다.", page.Tile));
            UpdateCommandState();
        }

        private void UnlockSplitEditing(object sender, EventArgs e)
        {
            SplitPageView page = editMenu.SourceControl as SplitPageView;
            if (!splitMode || page == null || !lockedSplitDocuments.ContainsKey(page.Tile)) return;
            ImageDocument lockedDocument = lockedSplitDocuments[page.Tile];
            lockedSplitDocuments.Remove(page.Tile);
            lockedSourcePolygons.Remove(page.Tile);
            lockedDocument.Dispose();
            document.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
            activeIndependentPage = null;
            List<PageTile> tiles = document.GetCoveredTiles()
                .Union(lockedSplitDocuments.Keys).ToList();
            RebuildSplitPages(tiles);
            SetStatus(string.Format("A4 {0} 조각의 고정을 해제했습니다.", page.Tile));
            UpdateCommandState();
        }

        private PointF[] GetTileSourcePolygon(PageTile tile)
        {
            PointF[] world = new PointF[]
            {
                new PointF(tile.Column * document.TileWidth, tile.Row * document.TileHeight),
                new PointF((tile.Column + 1) * document.TileWidth, tile.Row * document.TileHeight),
                new PointF((tile.Column + 1) * document.TileWidth, (tile.Row + 1) * document.TileHeight),
                new PointF(tile.Column * document.TileWidth, (tile.Row + 1) * document.TileHeight)
            };
            PointF[] source = new PointF[4];
            for (int index = 0; index < world.Length; index++)
                document.TryMapWorldToSource(world[index], out source[index]);
            return source;
        }

        private void ToggleSaveExclusion(object sender, EventArgs e)
        {
            SplitPageView page = editMenu.SourceControl as SplitPageView;
            if (page == null) return;
            if (!excludedSaveTiles.Add(page.Tile)) excludedSaveTiles.Remove(page.Tile);
            RebuildSplitPages(new List<PageTile>(shownTiles));
            SetStatus(excludedSaveTiles.Contains(page.Tile)
                ? string.Format("A4 {0} 조각을 저장 대상에서 제외했습니다.", page.Tile)
                : string.Format("A4 {0} 조각을 다시 저장 대상에 포함했습니다.", page.Tile));
            UpdateCommandState();
        }

        private void ClearSplitPages()
        {
            while (splitPanel.Controls.Count > 0)
            {
                Control control = splitPanel.Controls[0];
                splitPanel.Controls.RemoveAt(0);
                control.Dispose();
            }
        }

        private void ClearLockedSplits()
        {
            foreach (ImageDocument lockedDocument in lockedSplitDocuments.Values)
                lockedDocument.Dispose();
            lockedSplitDocuments.Clear();
            lockedSourcePolygons.Clear();
            excludedSaveTiles.Clear();
            selectedTiles.Clear();
            document.SetExcludedSourcePolygons(null);
        }

        private void ResizeSplitPages()
        {
            if (splitPanel.Controls.Count == 0 || shownTiles.Count == 0) return;
            int left = shownTiles.Min(tile => tile.Column);
            int right = shownTiles.Max(tile => tile.Column);
            int top = shownTiles.Min(tile => tile.Row);
            int bottom = shownTiles.Max(tile => tile.Row);
            int gridColumns = Math.Max(1, right - left + 1);
            int gridRows = Math.Max(1, bottom - top + 1);
            const int outerPadding = 24;
            const int itemSpacing = 16;
            float maxControlWidth = Math.Max(55f,
                (splitPanel.ClientSize.Width - outerPadding - (gridColumns - 1) * itemSpacing) /
                (float)gridColumns);
            float maxControlHeight = Math.Max(80f,
                (splitPanel.ClientSize.Height - outerPadding - (gridRows - 1) * itemSpacing) /
                (float)gridRows);
            float pageWidth = Math.Min(maxControlWidth - 10f,
                (maxControlHeight - 38f) * ImageDocument.PageWidth / ImageDocument.PageHeight);
            pageWidth = Math.Max(45f, pageWidth) * splitViewZoom;
            int controlWidth = (int)Math.Ceiling(pageWidth + 10f);
            int controlHeight = (int)Math.Ceiling(
                pageWidth * ImageDocument.PageHeight / ImageDocument.PageWidth + 38f);
            int totalWidth = gridColumns * controlWidth + (gridColumns - 1) * itemSpacing;
            int totalHeight = gridRows * controlHeight + (gridRows - 1) * itemSpacing;
            int startX = Math.Max(8, (splitPanel.ClientSize.Width - totalWidth) / 2);
            int startY = Math.Max(8, (splitPanel.ClientSize.Height - totalHeight) / 2);
            foreach (Control control in splitPanel.Controls)
            {
                control.Size = new Size(controlWidth, controlHeight);
                SplitPageView page = control as SplitPageView;
                if (page != null)
                {
                    control.Location = new Point(
                        startX + (page.Tile.Column - left) * (controlWidth + itemSpacing),
                        startY + (page.Tile.Row - top) * (controlHeight + itemSpacing));
                }
            }
        }

        private void RebuildSplitPages(List<PageTile> tiles)
        {
            splitPanel.SuspendLayout();
            try
            {
                ClearSplitPages();
                selectedTiles.RemoveWhere(tile => !tiles.Contains(tile));
                shownTiles = tiles;
                int left = tiles.Count == 0 ? 0 : tiles.Min(t => t.Column);
                int right = tiles.Count == 0 ? 0 : tiles.Max(t => t.Column);
                int top = tiles.Count == 0 ? 0 : tiles.Min(t => t.Row);
                int bottom = tiles.Count == 0 ? 0 : tiles.Max(t => t.Row);
                Dictionary<PageTile, RectangleF> editableBoundaries =
                    BuildEditableComponentBoundaries(tiles);
                foreach (PageTile tile in tiles)
                {
                    ImageDocument pageDocument;
                    bool independent = lockedSplitDocuments.TryGetValue(tile, out pageDocument);
                    if (!independent) pageDocument = document;
                    bool hasLeft = tiles.Contains(new PageTile(tile.Column - 1, tile.Row));
                    bool hasRight = tiles.Contains(new PageTile(tile.Column + 1, tile.Row));
                    bool hasTop = tiles.Contains(new PageTile(tile.Column, tile.Row - 1));
                    bool hasBottom = tiles.Contains(new PageTile(tile.Column, tile.Row + 1));
                    RectangleF pageBoundary = independent
                        ? new RectangleF(tile.Column * document.TileWidth,
                            tile.Row * document.TileHeight,
                            document.TileWidth, document.TileHeight)
                        : editableBoundaries[tile];
                    SplitPageView page = new SplitPageView(pageDocument, tile, pageBoundary,
                        independent, false, !hasLeft, !hasRight, !hasTop, !hasBottom,
                        pageMargins);
                    page.ViewZoomRequested += SplitViewZoomRequested;
                    page.SelectionRequested += SplitPageSelectionRequested;
                    page.SaveExcluded = excludedSaveTiles.Contains(tile);
                    page.IsSelected = selectedTiles.Contains(tile);
                    WireEditingControl(page, independent);
                    page.ContextMenuStrip = editMenu;
                    toolTip.SetToolTip(page, page.ToolTipText);
                    splitPanel.Controls.Add(page);
                }
            }
            finally
            {
                splitPanel.ResumeLayout();
                ResizeSplitPages();
            }
        }

        private void SplitViewZoomRequested(object sender, MouseEventArgs e)
        {
            SplitPageView page = sender as SplitPageView;
            if (page == null) return;
            Point cursor = splitPanel.PointToClient(page.PointToScreen(e.Location));
            ZoomSplitViewAt(cursor, e.Delta);
        }

        private void SplitPanelMouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != Keys.Control) return;
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null) handled.Handled = true;
            ZoomSplitViewAt(e.Location, e.Delta);
        }

        private void ZoomSplitViewAt(Point cursor, int delta)
        {
            Point oldScroll = new Point(-splitPanel.AutoScrollPosition.X,
                -splitPanel.AutoScrollPosition.Y);
            float next = Math.Max(0.35f, Math.Min(6f,
                splitViewZoom * (delta > 0 ? 1.16f : 1f / 1.16f)));
            float applied = next / splitViewZoom;
            if (Math.Abs(applied - 1f) < 0.0001f) return;
            splitViewZoom = next;
            ResizeSplitPages();
            splitPanel.AutoScrollPosition = new Point(
                Math.Max(0, (int)Math.Round((oldScroll.X + cursor.X) * applied - cursor.X)),
                Math.Max(0, (int)Math.Round((oldScroll.Y + cursor.Y) * applied - cursor.Y)));
            splitPanel.Invalidate(true);
            splitPanel.Update();
        }

        private void SplitPageSelectionRequested(object sender, EventArgs e)
        {
            SplitPageView page = sender as SplitPageView;
            if (page == null) return;
            bool additive = (ModifierKeys & Keys.Control) == Keys.Control;
            if (!additive) selectedTiles.Clear();
            if (additive && selectedTiles.Contains(page.Tile)) selectedTiles.Remove(page.Tile);
            else selectedTiles.Add(page.Tile);
            activeIndependentPage = selectedTiles.Count == 1 &&
                lockedSplitDocuments.ContainsKey(page.Tile) ? page : null;
            RefreshPageSelection();
            UpdateRotationForSelection();
        }

        private void SplitPanelSelectionMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            selectingPages = true;
            selectionAdditive = (ModifierKeys & Keys.Control) == Keys.Control;
            selectionStart = e.Location;
            selectionCurrent = e.Location;
            if (!selectionAdditive) selectedTiles.Clear();
            splitPanel.Capture = true;
            splitPanel.Invalidate();
        }

        private void SplitPanelSelectionMouseMove(object sender, MouseEventArgs e)
        {
            if (!selectingPages) return;
            selectionCurrent = e.Location;
            splitPanel.Invalidate();
        }

        private void SplitPanelSelectionMouseUp(object sender, MouseEventArgs e)
        {
            if (!selectingPages) return;
            selectingPages = false;
            splitPanel.Capture = false;
            Rectangle selection = NormalizeRectangle(selectionStart, e.Location);
            if (selection.Width >= 4 || selection.Height >= 4)
            {
                foreach (SplitPageView page in splitPanel.Controls.OfType<SplitPageView>())
                    if (selection.IntersectsWith(page.Bounds)) selectedTiles.Add(page.Tile);
            }
            activeIndependentPage = null;
            RefreshPageSelection();
            UpdateRotationForSelection();
            splitPanel.Invalidate();
        }

        private void DrawSplitSelectionRectangle(object sender, PaintEventArgs e)
        {
            if (!selectingPages) return;
            Rectangle rectangle = NormalizeRectangle(selectionStart, selectionCurrent);
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(48, 255, 235, 145)))
            using (Pen outline = new Pen(Color.FromArgb(240, 245, 205, 85)))
            {
                outline.DashStyle = DashStyle.Dash;
                e.Graphics.FillRectangle(fill, rectangle);
                e.Graphics.DrawRectangle(outline, rectangle);
            }
        }

        private static Rectangle NormalizeRectangle(Point first, Point second)
        {
            return Rectangle.FromLTRB(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y),
                Math.Max(first.X, second.X), Math.Max(first.Y, second.Y));
        }

        private void RefreshPageSelection()
        {
            foreach (SplitPageView page in splitPanel.Controls.OfType<SplitPageView>())
            {
                page.IsSelected = selectedTiles.Contains(page.Tile);
                page.Invalidate();
            }
        }

        private void UpdateRotationForSelection()
        {
            ImageDocument selectedDocument = null;
            if (selectedTiles.Count == 1)
                lockedSplitDocuments.TryGetValue(selectedTiles.First(), out selectedDocument);
            float rotation = selectedDocument == null ? document.Rotation : selectedDocument.Rotation;
            applyingRotation = true;
            rotationBox.Value = Math.Max(rotationBox.Minimum,
                Math.Min(rotationBox.Maximum, (decimal)rotation));
            applyingRotation = false;
        }

        private Dictionary<PageTile, RectangleF> BuildEditableComponentBoundaries(
            List<PageTile> tiles)
        {
            HashSet<PageTile> unlocked = new HashSet<PageTile>(
                tiles.Where(tile => !lockedSplitDocuments.ContainsKey(tile)));
            HashSet<PageTile> visited = new HashSet<PageTile>();
            Dictionary<PageTile, RectangleF> result = new Dictionary<PageTile, RectangleF>();
            foreach (PageTile start in unlocked)
            {
                if (!visited.Add(start)) continue;
                Queue<PageTile> queue = new Queue<PageTile>();
                List<PageTile> component = new List<PageTile>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    PageTile current = queue.Dequeue();
                    component.Add(current);
                    PageTile[] neighbors = new PageTile[]
                    {
                        new PageTile(current.Column - 1, current.Row),
                        new PageTile(current.Column + 1, current.Row),
                        new PageTile(current.Column, current.Row - 1),
                        new PageTile(current.Column, current.Row + 1)
                    };
                    foreach (PageTile neighbor in neighbors)
                        if (unlocked.Contains(neighbor) && visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                }
                int componentLeft = component.Min(tile => tile.Column);
                int componentRight = component.Max(tile => tile.Column);
                int componentTop = component.Min(tile => tile.Row);
                int componentBottom = component.Max(tile => tile.Row);
                RectangleF boundary = RectangleF.FromLTRB(
                    componentLeft * document.TileWidth,
                    componentTop * document.TileHeight,
                    (componentRight + 1) * document.TileWidth,
                    (componentBottom + 1) * document.TileHeight);
                foreach (PageTile tile in component) result[tile] = boundary;
            }
            return result;
        }

        private void NotifyDocumentChanged()
        {
            if (!document.HasImage) return;
            applyingRotation = true;
            decimal shownRotation = Math.Max(rotationBox.Minimum,
                Math.Min(rotationBox.Maximum, (decimal)document.Rotation));
            rotationBox.Value = shownRotation;
            applyingRotation = false;
            gridPreview.Invalidate();
            if (splitMode && interactiveEdit)
            {
                splitPanel.Invalidate(true);
                splitPanel.Update();
                return;
            }
            else if (splitMode)
            {
                List<PageTile> current = document.GetCoveredTiles()
                    .Union(lockedSplitDocuments.Keys)
                    .OrderBy(tile => tile.Row).ThenBy(tile => tile.Column).ToList();
                if (current.Count > 300)
                {
                    SetStatus("현재 설정은 300장이 넘습니다. 크기를 줄여 주세요.");
                }
                else if (!TilesMatch(shownTiles, current))
                {
                    RebuildSplitPages(current);
                }
                else
                {
                    foreach (Control control in splitPanel.Controls) control.Invalidate();
                }
            }
            SetStatus(string.Format("이미지 {0:0.0}% · 격자 {1:0}% · 회전 {2:0.0}° · 예상 A4 {3}장",
                document.Scale * 100f / GetBaseScale(), gridPreview.GridZoom * 100f,
                document.Rotation, document.GetCoveredTiles().Count));
            UpdateCommandState();
        }

        private float GetBaseScale()
        {
            if (!document.HasImage) return 1f;
            float imageRatio = (float)document.Source.Width / document.Source.Height;
            return imageRatio >= document.TileWidth / document.TileHeight
                ? document.TileHeight / document.Source.Height
                : document.TileWidth / document.Source.Width;
        }

        private static bool TilesMatch(List<PageTile> left, List<PageTile> right)
        {
            return left.Count == right.Count && !left.Where((tile, index) => !tile.Equals(right[index])).Any();
        }

        private void RotationBoxChanged(object sender, EventArgs e)
        {
            if (applyingRotation || !document.HasImage) return;
            SetSelectedRotation((float)rotationBox.Value);
        }

        private void UpdateCommandState()
        {
            bool enabled = document.HasImage;
            temporarySplitButton.Enabled = enabled && !splitMode;
            returnToGridButton.Visible = splitMode;
            int outputCount = GetOutputTiles().Count;
            finalizeButton.Enabled = enabled && outputCount > 0 && outputCount <= 300;
            printButton.Enabled = enabled && outputCount > 0 && outputCount <= 300;
        }

        private List<PageTile> GetOutputTiles()
        {
            List<PageTile> tiles = splitMode
                ? new List<PageTile>(shownTiles) : document.GetCoveredTiles();
            return tiles.Where(tile => !excludedSaveTiles.Contains(tile))
                .OrderBy(tile => tile.Row).ThenBy(tile => tile.Column).ToList();
        }

        private void PrintSplit(object sender, EventArgs e)
        {
            if (!document.HasImage) return;
            List<PageTile> tiles = GetOutputTiles();
            if (tiles.Count == 0 || tiles.Count > 300) return;

            int pageIndex = 0;
            using (PrintDocument printDocument = new PrintDocument())
            {
                printDocument.DocumentName = Path.GetFileNameWithoutExtension(document.SourcePath) +
                    " - A4 이미지 분할";
                PaperSize a4 = printDocument.PrinterSettings.PaperSizes.Cast<PaperSize>()
                    .FirstOrDefault(size => size.Kind == PaperKind.A4);
                printDocument.DefaultPageSettings.PaperSize = a4 ??
                    new PaperSize("A4", 827, 1169);
                printDocument.DefaultPageSettings.Landscape = false;
                printDocument.DefaultPageSettings.Color = true;
                printDocument.DefaultPageSettings.Margins = new Margins(
                    (int)Math.Round(pageMargins.Left / 25.4f * 100f),
                    (int)Math.Round(pageMargins.Right / 25.4f * 100f),
                    (int)Math.Round(pageMargins.Top / 25.4f * 100f),
                    (int)Math.Round(pageMargins.Bottom / 25.4f * 100f));
                printDocument.BeginPrint += delegate { pageIndex = 0; };
                printDocument.PrintPage += delegate(object printSender, PrintPageEventArgs args)
                {
                    using (Bitmap page = CreateRenderedPage(tiles[pageIndex]))
                    {
                        args.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        args.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        args.Graphics.DrawImage(page, new Rectangle(0, 0,
                            args.PageBounds.Width, args.PageBounds.Height));
                    }
                    pageIndex++;
                    args.HasMorePages = pageIndex < tiles.Count;
                };
                try
                {
                    if (lockedSourcePolygons.Count > 0)
                        document.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
                    using (EnhancedPrintPreviewForm preview =
                        new EnhancedPrintPreviewForm(printDocument, tiles.Count))
                        preview.ShowDialog(this);
                    SetStatus(string.Format("인쇄 미리보기: 저장 제외 항목을 뺀 A4 {0}장", tiles.Count));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "인쇄 중 오류가 발생했습니다.\n\n" + ex.Message,
                        "인쇄 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (!splitMode) document.SetExcludedSourcePolygons(null);
                }
            }
        }

        private Bitmap CreateRenderedPage(PageTile tile)
        {
            Bitmap page = new Bitmap((int)ImageDocument.PageWidth,
                (int)ImageDocument.PageHeight, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(page))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                ImageDocument renderDocument = document;
                ImageDocument lockedDocument;
                if (lockedSplitDocuments.TryGetValue(tile, out lockedDocument))
                    renderDocument = lockedDocument;
                renderDocument.DrawPage(graphics, tile, pageMargins);
            }
            return page;
        }

        private void FinalizeSplit(object sender, EventArgs e)
        {
            if (!document.HasImage) return;
            List<PageTile> tiles = GetOutputTiles();
            if (tiles.Count == 0 || tiles.Count > 300)
            {
                MessageBox.Show(this, "생성할 A4 조각 수를 확인하세요.", "생성할 수 없습니다",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string folder = Path.GetDirectoryName(document.SourcePath);
            string baseName = Path.GetFileNameWithoutExtension(document.SourcePath);
            string extension = Path.GetExtension(document.SourcePath);
            List<string> targets = new List<string>();
            for (int i = 0; i < tiles.Count; i++)
            {
                targets.Add(Path.Combine(folder, string.Format("{0}_part{1:000}{2}", baseName, i + 1, extension)));
            }
            if (targets.Any(File.Exists))
            {
                DialogResult overwrite = MessageBox.Show(this,
                    "같은 이름의 분할 이미지가 이미 있습니다. 덮어쓸까요?",
                    "기존 파일 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (overwrite != DialogResult.Yes) return;
            }

            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            Enabled = false;
            try
            {
                if (lockedSourcePolygons.Count > 0)
                    document.SetExcludedSourcePolygons(lockedSourcePolygons.Values);
                for (int i = 0; i < tiles.Count; i++)
                {
                    using (Bitmap page = CreateRenderedPage(tiles[i]))
                    {
                        SavePage(page, targets[i], extension);
                    }
                }
                SetStatus(string.Format("완료: 원본 폴더에 A4 이미지 {0}장을 생성했습니다.", tiles.Count));
                MessageBox.Show(this, string.Format("A4 분할 이미지 {0}장을 생성했습니다.\n\n{1}",
                    tiles.Count, folder), "분할 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "이미지 생성 중 오류가 발생했습니다.\n\n" + ex.Message,
                    "생성 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!splitMode) document.SetExcludedSourcePolygons(null);
                Enabled = true;
                Cursor = previous;
            }
        }

        private static void SavePage(Bitmap page, string target, string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    page.Save(target, ImageFormat.Jpeg);
                    break;
                case ".png": page.Save(target, ImageFormat.Png); break;
                case ".gif": page.Save(target, ImageFormat.Gif); break;
                case ".bmp": page.Save(target, ImageFormat.Bmp); break;
                case ".tif":
                case ".tiff": page.Save(target, ImageFormat.Tiff); break;
                default: throw new NotSupportedException("이 확장자의 저장 형식은 지원하지 않습니다: " + extension);
            }
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }
    }
}
