
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BasicImageFormFiller.Interfaces;
using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms
{
    public sealed partial class BoxPlacer : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly FileSystemProject? _project;
        private readonly int _pageIndex;
        private readonly Bitmap? _imageCache;
        
        private const int PreviewImageSize = 1024;
        private const float FarScale = 0.5f;
        private const float NearScale = 1.0f;
        
        private float _scale = FarScale; // visual scale. Forms tend to be huge, so we start scaled out
        private float _x, _y; // visual offset
        private bool _drag; // is the user panning the image?
        private bool _drawingBox, _validDraw; // is the user drawing a new box? Did they drag before moving the mouse?
        private float _my, _mx; // drag start
        private float _mye, _mxe; // drag end
        private string? _boxToAdjust;
        private bool _movingBox;
        private bool _resizingBox;

        public BoxPlacer() { InitializeComponent(); }
        
        public BoxPlacer(IScreenModule returnModule, FileSystemProject project, int pageIndex)
        {
            InitializeComponent();
            _returnModule = returnModule;
            _project = project;
            _pageIndex = pageIndex;
            
            _imageCache = LoadFromFile(_project.Pages[_pageIndex].GetBackgroundPath());
            
            DoubleBuffered = true;
            MouseWheel += ChangeZoom;
        }
        
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.Gray);
            
            if (_imageCache == null || _project == null) return;
            
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            
            DrawAtScaleAndOffset(g, _scale, _x, _y);
            
            if (_drawingBox && _validDraw)
            {
                var width = Math.Abs(_mxe - _mx);
                var height = Math.Abs(_mye - _my);
                var left = Math.Min(_mxe, _mx);
                var top = Math.Min(_mye, _my);
                g.DrawRectangle(Pens.Orchid!, _x + left, _y + top, width, height);
            }
        }

        private void DrawAtScaleAndOffset(Graphics g, float scale, float dx, float dy)
        {
            if (_imageCache == null || _project == null) return;
            
            g.Transform.Reset();
            g.ScaleTransform(scale, scale);
            g.DrawImage(_imageCache, new PointF(dx, dy));
            
            var explicitlyOrdered = new List<TemplateBox>();
            var dependentBoxes = new Dictionary<string, string>(); // key =(depends on)=> value

            // Draw the boxes
            foreach (var entry in _project.Pages[_pageIndex].Boxes)
            {
                var name = entry.Key;
                var box = entry.Value;
                var hasMap = box.MappingPath != null && box.MappingPath.Length > 0;

                var boxColor = hasMap ? Brushes.Turquoise! : Brushes.Pink!;
                if (box.BoxOrder != null) explicitlyOrdered.Add(box);
                if (box.DependsOn != null) dependentBoxes.Add(name, box.DependsOn);

                var rect = new Rectangle((int) box.Left, (int) box.Top, (int) box.Width, (int) box.Height);
                rect.Offset((int) dx, (int) dy);
                g.FillRectangle(boxColor, rect);
                g.DrawRectangle(Pens.Black!, rect);

                AlignTextToRect(g, name, rect, box, out var top, out var left);
                g.DrawString(name, Font, Brushes.Black!, left, top);
            }

            // Draw arrows for any explicitly ordered boxes
            if (explicitlyOrdered.Count > 1)
            {
                // Create a new pen to draw the arrow
                using var b = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
                using Pen p = new Pen(b, 2.1f) {CustomEndCap = new AdjustableArrowCap(5, 5, true)};
                int prevX = -1;
                int prevY = -1;
                foreach (var box in explicitlyOrdered.OrderBy(o => o.BoxOrder))
                {
                    int nextX = (int) (box.Left + (box.Width / 2));
                    int nextY = (int) (box.Top + (box.Height / 2));

                    if (prevX >= 0 || prevY >= 0)
                    {
                        // Draw an arrow
                        g.DrawLine(p, dx + prevX, dy + prevY, dx + nextX, dy + nextY);
                    }

                    prevX = nextX;
                    prevY = nextY;
                }
            }

            // Draw arrows for any dependent box chains
            if (dependentBoxes.Count > 0)
            {
                using var b = new SolidBrush(Color.FromArgb(100, 0, 0, 255));
                using Pen p = new Pen(b, 2.1f) {DashStyle = DashStyle.Dot, CustomEndCap = new AdjustableArrowCap(5, 5, true)};
                foreach (var (key, value) in dependentBoxes)
                {
                    var fromBox = _project.Pages[_pageIndex].Boxes[key];
                    var toBox = _project.Pages[_pageIndex].Boxes[value];
                    
                    var prevX = (int) (fromBox.Left + (fromBox.Width / 2));
                    var prevY = (int) (fromBox.Top + (fromBox.Height / 2));
                    var nextX = (int) (toBox.Left + (toBox.Width / 2));
                    var nextY = (int) (toBox.Top + (toBox.Height / 2));

                    if (prevX >= 0 || prevY >= 0) { // Draw an arrow
                        g.DrawLine(p, dx + prevX, dy + prevY, dx + nextX, dy + nextY);
                    }
                }
            }
        }

        private void AlignTextToRect(Graphics g, string name, Rectangle rect, TemplateBox box, out float top, out float left)
        {
            var size = g.MeasureString(name, Font);
            left = rect.Left;
            top = rect.Top;
            var halfWidth = rect.Width / 2.0f;
            var halfHeight = rect.Height / 2.0f;
            var textWidth = size.Width;
            var textHeight = size.Height;
            switch (box.Alignment)
            {
                case TextAlignment.TopLeft: break;
                case TextAlignment.TopCentre:
                    left += halfWidth - textWidth / 2.0f;
                    break;
                case TextAlignment.TopRight:
                    left += rect.Width - textWidth;
                    break;
                case TextAlignment.MidlineLeft:
                    top += halfHeight - textHeight / 2.0f;
                    break;
                case TextAlignment.MidlineCentre:
                    left += halfWidth - textWidth / 2.0f;
                    top += halfHeight - textHeight / 2.0f;
                    break;
                case TextAlignment.MidlineRight:
                    top += halfHeight - textHeight / 2.0f;
                    left += rect.Width - textWidth;
                    break;
                case TextAlignment.BottomLeft:
                    top += rect.Height - textHeight;
                    break;
                case TextAlignment.BottomCentre:
                    top += rect.Height - textHeight;
                    left += halfWidth - textWidth / 2.0f;
                    break;
                case TextAlignment.BottomRight:
                    top += rect.Height - textHeight;
                    left += rect.Width - textWidth;
                    break;
            }
        }

        private static Bitmap? LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            if (!File.Exists(filePath)) return null;

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var bmp = Image.FromStream(fs);
            
            return new Bitmap(bmp);
        }

        private void BoxPlacer_MouseDown(object sender, MouseEventArgs e)
        {
            if (_project == null) return;
            var pressingControlKey = (ModifierKeys & Keys.Control) != 0;
            var pressingAltKey = (ModifierKeys & Keys.Alt) != 0;
            var pressingShiftKey = (ModifierKeys & Keys.Shift) != 0;
            
            if (pressingAltKey && pressingControlKey) // move an existing box
            {
                GetMouseInDocSpace(e.X, e.Y, out var x, out var y);
                _boxToAdjust = FindBoxKey(x,y);
                _mx = e.X;
                _my = e.Y;
                _movingBox = true;
            }
            else if (pressingShiftKey && pressingControlKey) // re-size an existing box
            {
                GetMouseInDocSpace(e.X, e.Y, out var x, out var y);
                _boxToAdjust = FindBoxKey(x,y);
                _mx = e.X;
                _my = e.Y;
                _resizingBox = true;
            }
            else if (pressingAltKey) // pop-up the edit screen for a box
            {
                GetMouseInDocSpace(e.X, e.Y, out var x, out var y);
                var key = FindBoxKey(x,y);
                if (string.IsNullOrWhiteSpace(key)) return;
                
                var editDialog = new BoxEdit(_project!, _pageIndex, key);
                editDialog.ShowDialog();
                _project.Reload();
                Invalidate();
            }
            else if (pressingControlKey) // start drawing a new box
            {
                GetMouseInDocSpace(e.X, e.Y, out _mx, out _my);
                _drawingBox = true;
                _validDraw = false;
            }
            else if (e.Clicks > 1) // flip scale with a double-click
            {
                var newScale = _scale >= NearScale ? FarScale : NearScale;
                UpdateScaleAdjusted(e.X, e.Y, newScale);
                Invalidate();
            }
            else // regular drag-to-pan
            {
                _mx = e.X;
                _my = e.Y;
                _drag = true;
            }
        }

        private void BoxPlacer_MouseUp(object sender, MouseEventArgs e) {
            if (_drawingBox && _validDraw)
            {
                AddBox();
            }
            if (_resizingBox || _movingBox) _project?.Save();

            _drag = false;
            _drawingBox = false;
            _movingBox = false;
            _resizingBox = false;
        }

        private void BoxPlacer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drawingBox)
            {
                GetMouseInDocSpace(e.X, e.Y, out _mxe, out _mye);
                _validDraw = true;
                Invalidate();
            }

            if (_resizingBox && _boxToAdjust != null)
            {
                _project!.Pages[_pageIndex].Boxes[_boxToAdjust].Width += (e.X - _mx) / _scale;
                _project!.Pages[_pageIndex].Boxes[_boxToAdjust].Height  += (e.Y - _my) / _scale;
                
                _mx = e.X;
                _my = e.Y;
                
                Invalidate();
            }

            if (_movingBox && _boxToAdjust != null)
            {
                _project!.Pages[_pageIndex].Boxes[_boxToAdjust].Left += (e.X - _mx) / _scale;
                _project!.Pages[_pageIndex].Boxes[_boxToAdjust].Top  += (e.Y - _my) / _scale;
                
                _mx = e.X;
                _my = e.Y;
                
                Invalidate();
            }

            if (_drag)
            {
                _x += (e.X - _mx) / _scale;
                _y += (e.Y - _my) / _scale;
                _mx = e.X;
                _my = e.Y;

                PinScrollToScreen();
                Invalidate();
            }
        }
        
        private string? FindBoxKey(float docX, float docY)
        {
            if (_project == null) return null;
            foreach (var (key, box) in _project.Pages[_pageIndex].Boxes.Reverse()) // reverse so you can pick 'on top' boxes in an intuitive way.
            {
                if (box.Top > docY) continue;
                if (box.Left > docX) continue;
                if (box.Top + box.Height < docY) continue;
                if (box.Left + box.Width < docX) continue;
                
                return key;
            }
            return null;
        }

        private void AddBox()
        {
            // Check the box isn't silly small, then
            // add a default box (the user can edit it when ready)

            if (_project == null) return;
            var width = Math.Abs(_mxe - _mx);
            var height = Math.Abs(_mye - _my);
            var left = Math.Min(_mxe, _mx);
            var top = Math.Min(_mye, _my);

            if (width < 5 || height < 2) return;

            var container = _project.Pages[_pageIndex].Boxes;
            string key = "";
            for (int i = 1; i < 256; i++)
            {
                key = $"Box_{i}";
                if (!container.ContainsKey(key)) break;
            }

            if (container.ContainsKey(key))
            {
                MessageBox.Show("Please rename some of your boxes before adding more");
                return; // ran out of default keys. User will need to rename some.
            }

            container.Add(key, new TemplateBox
            {
                Alignment = TextAlignment.MidlineLeft,
                Height = height,
                Width = width,
                Left = left,
                Top = top,
                DisplayFormat = null,
                WrapText = false,
                ShrinkToFit = true
            });
            _project.Save();
            Invalidate();
        }

        private void PinScrollToScreen()
        {
            if (_imageCache == null) return;
            
            
            var imgWidth = _imageCache.Width;
            var halfScreenWidth = (Width / 2.0f) / _scale;
            var imgHeight = _imageCache.Height;
            var halfScreenHeight = (Height / 2.0f) / _scale;
            
            _x = Math.Min(halfScreenWidth,Math.Max(-imgWidth + halfScreenWidth,_x));
            _y = Math.Min(halfScreenHeight,Math.Max(-imgHeight + halfScreenHeight,_y));
        }

        private void BoxPlacer_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Write a 'preview' of boxes-over-background
            WritePreview();
            
            // Unlock parent
            _returnModule?.Activate();
        }

        private void WritePreview()
        {
            if (_imageCache == null || _project == null) return;
            var path = _project.Pages[_pageIndex].GetPreviewPath(_project);
            if (string.IsNullOrWhiteSpace(path)) return;
            
            var maxDim = Math.Max(_imageCache.Width, _imageCache.Height);
            var scale = (maxDim > PreviewImageSize) ? (float)PreviewImageSize / maxDim : 1.0f;
            
            var pWidth = (int)(_imageCache.Width * scale);
            var pHeight = (int)(_imageCache.Height * scale);
            
            using var bmp = new Bitmap(pWidth, pHeight);
            using var g = Graphics.FromImage(bmp);
            
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            DrawAtScaleAndOffset(g, scale, 0.0f, 0.0f);
            SaveJpeg(bmp, path);
        }

        private static void SaveJpeg(Bitmap src, string filePath, int quality = 95)
        {
            using var fs = new FileStream(filePath, FileMode.Create);
            JpegStream(src, fs, quality);
            fs.Flush(false);
            fs.Close();
        }

        private static void JpegStream(Bitmap src, Stream outputStream, int quality = 95)
        {
            var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg!.Guid);
            if (encoder == null) return;
            // ReSharper disable once UseObjectOrCollectionInitializer
            var parameters = new EncoderParameters(1);
            parameters.Param![0] = new EncoderParameter(Encoder.Quality!, quality);

            src.Save(outputStream, encoder, parameters);
            outputStream.Flush();
        }

        private void GetMouseInDocSpace(float mouseX, float mouseY, out float docX, out float docY)
        {
            docX = -_x + (mouseX / _scale);
            docY = -_y + (mouseY / _scale);
        }

        private void UpdateScaleAdjusted(float mouseX, float mouseY, float newScale)
        {
            var oldScale = _scale;
            
            // compensate for centre point
            // Should zoom over relative mouse point if possible
            
            // the _x,_y offset is scaled, so we want to find out
            // where the mouse is in relation to it before the scale
            // and then update _x,_y so that point is still under the
            // mouse after the scale
            
            // page xy at old scale
            var px = -_x + (mouseX / oldScale);
            var py = -_y + (mouseY / oldScale);
            
            // where would we be on the page at the new scale?
            var ax = -_x + (mouseX / newScale);
            var ay = -_y + (mouseY / newScale);

            _x += ax - px;
            _y += ay - py;
            _scale = newScale;
            PinScrollToScreen();
        }
        
        private void ChangeZoom(object sender, MouseEventArgs e)
        {
            if (_imageCache == null) return;
            if (e.Delta == 0) return;
            
            var speed = (ModifierKeys & Keys.Control) != 0 ? 0.1f : 0.01f;
            var newScale = _scale + Math.Sign(e.Delta) * speed;
                
            if (newScale < 0.01f) newScale = 0.01f;
            if (newScale > 10.0f) newScale = 10.0f;
            
            UpdateScaleAdjusted(e.X, e.Y, newScale);
            
            PinScrollToScreen();
            Invalidate();
        }

    }
}