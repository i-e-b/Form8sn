using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using BasicImageFormFiller.FileFormats;
using BasicImageFormFiller.Interfaces;

namespace BasicImageFormFiller.EditForms
{
    public sealed partial class BoxPlacer : Form
    {
        private readonly IScreenModule? _returnModule;
        private readonly Project? _project;
        private readonly int _pageIndex;
        private readonly Bitmap? _imageCache;
        
        private const float FarScale = 0.5f;
        private const float NearScale = 1.0f;
        
        private float _scale = FarScale; // visual scale. Forms tend to be huge, so we start scaled out
        private float _x, _y; // visual offset
        private bool _drag; // is the user panning the image?
        private bool _drawingBox, _validDraw; // is the user drawing a new box? Did they drag before moving the mouse?
        private float _my, _mx; // drag start
        private float _mye, _mxe; // drag end

        public BoxPlacer() { InitializeComponent(); }
        
        public BoxPlacer(IScreenModule returnModule, Project project, int pageIndex)
        {
            InitializeComponent();
            _returnModule = returnModule;
            _project = project;
            _pageIndex = pageIndex;
            
            _imageCache = LoadFromFile(_project.Pages[_pageIndex].GetBackgroundPath(_project));
            
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
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            
            g.ScaleTransform(_scale,_scale);
            g.DrawImage(_imageCache, new PointF(_x,_y));

            if (_drawingBox && _validDraw)
            {
                g.DrawRectangle(Pens.Orchid!, _mx+_x, _my+_y, _mxe - _mx, _mye - _my);
            }

            foreach (var entry in _project.Pages[_pageIndex].Boxes)
            {
                var name = entry.Key;
                var box = entry.Value;
                
                var rect = new Rectangle((int)box.Left, (int)box.Top, (int)box.Width, (int)box.Height);
                rect.Offset((int)_x,(int)_y);
                g.FillRectangle(Brushes.Aqua!, rect);
                g.DrawRectangle(Pens.Black!, rect);
                g.DrawString(name, Font, Brushes.Black!, rect.Left, rect.Top); // TODO: align based on setting
            }

            g.Transform?.Reset();
        }

        private static Bitmap? LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            if (!File.Exists(filePath)) return null;

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var bmp = Image.FromStream(fs);
            if (bmp == null) return null;
            
            return new Bitmap(bmp);
        }

        private void BoxPlacer_MouseDown(object sender, MouseEventArgs e)
        {
            if (_project == null) return;
            var pressingControlKey = (ModifierKeys & Keys.Control) != 0;
            var pressingAltKey = (ModifierKeys & Keys.Alt) != 0;
            
            if (pressingAltKey && pressingControlKey) // TODO: move / re-size an existing box
            {
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

        private string? FindBoxKey(float docX, float docY)
        {
            if (_project == null) return null;
            foreach (var (key, box) in _project.Pages[_pageIndex].Boxes)
            {
                if (box.Top > docY) continue;
                if (box.Left > docX) continue;
                if (box.Top + box.Height < docY) continue;
                if (box.Left + box.Width < docX) continue;
                
                return key;
            }
            return null;
        }

        private void BoxPlacer_MouseUp(object sender, MouseEventArgs e) {
            if (_drawingBox && _validDraw)
            {
                AddBox();
            }

            _drag = false;
            _drawingBox = false;
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
            string key = "Box_1";
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
                FontScale = 1.0,
                WrapText = false,
                ShrinkToFit = false
            });
            _project.Save();
            Invalidate();
        }

        private void BoxPlacer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drawingBox)
            {
                GetMouseInDocSpace(e.X, e.Y, out _mxe, out _mye);
                _validDraw = true;
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
            _returnModule?.Activate();
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