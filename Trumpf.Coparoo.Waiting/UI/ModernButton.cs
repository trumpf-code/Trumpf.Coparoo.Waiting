// Copyright 2016 - 2025 TRUMPF Werkzeugmaschinen GmbH + Co. KG.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Trumpf.Coparoo.Waiting.UI
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    /// <summary>
    /// Modern styled button with rounded corners and hover effects.
    /// </summary>
    internal class ModernButton : Button
    {
        private Color normalColor;
        private Color hoverColor;
        private Color disabledColor;
        private bool isHovering;
        private Timer animationTimer;
        private float animationProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernButton"/> class.
        /// </summary>
        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Font = ModernTheme.Typography.GetButtonFont();
            Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(1, 0, 0, 0); // near-transparent, avoids WinForms artifact when fully transparent

            // Enable custom painting, double buffering, and all-paint-in-wm to prevent intermediate artifacts
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            animationTimer = new Timer { Interval = 16 }; // ~60fps
            animationTimer.Tick += AnimationTimer_Tick;
            UpdateRoundedRegion();
        }

        /// <summary>
        /// Keep region in sync on resize.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRoundedRegion();
            Invalidate();
        }

        private void UpdateRoundedRegion()
        {
            if (Width <= 1 || Height <= 1)
            {
                return;
            }

            // Use Width-1 / Height-1 to match drawing rectangle and avoid bottom-right stray pixel.
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ModernTheme.CreateRoundedRectangle(rect, ModernTheme.Layout.CornerRadius))
            {
                Region?.Dispose();
                Region = new Region(path);
            }
        }

        /// <summary>
        /// Gets or sets the normal background color.
        /// </summary>
        public Color NormalColor
        {
            get => normalColor;
            set
            {
                normalColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the hover background color.
        /// </summary>
        public Color HoverColor
        {
            get => hoverColor;
            set
            {
                hoverColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the disabled background color.
        /// </summary>
        public Color DisabledColor
        {
            get => disabledColor;
            set
            {
                disabledColor = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovering = true;
            animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovering = false;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            float targetProgress = isHovering ? 1f : 0f;
            float step = 0.1f;

            if (Math.Abs(animationProgress - targetProgress) < step)
            {
                animationProgress = targetProgress;
                animationTimer.Stop();
            }
            else
            {
                animationProgress += (targetProgress > animationProgress ? step : -step);
            }

            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Paint a clean background matching parent without relying on transparency (reduces artifacts).
            Color bg = Parent?.BackColor ?? ModernTheme.Colors.Background;
            pevent.Graphics.Clear(bg);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Supersample render: draw at 2x resolution into offscreen bitmap to reduce edge stair-stepping
            int scale = 2;
            int w = Width * scale;
            int h = Height * scale;
            using (var buffer = new Bitmap(w, h))
            using (var g = Graphics.FromImage(buffer))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Parent?.BackColor ?? ModernTheme.Colors.Background);

                // Interpolated color (hover animation)
                Color currentColor = !Enabled
                    ? disabledColor
                    : Color.FromArgb(
                        (int)(normalColor.R + (hoverColor.R - normalColor.R) * animationProgress),
                        (int)(normalColor.G + (hoverColor.G - normalColor.G) * animationProgress),
                        (int)(normalColor.B + (hoverColor.B - normalColor.B) * animationProgress));

            // Full-size path (Region already clipped) - no inset needed; draw border separately to avoid dark halo.
                var drawRectHiRes = new Rectangle(0, 0, w - scale, h - scale);
                using (var path = ModernTheme.CreateRoundedRectangle(drawRectHiRes, ModernTheme.Layout.CornerRadius * scale))
                using (var fill = new SolidBrush(currentColor))
                {
                    g.FillPath(fill, path);
                }
                // Render text at high resolution with scaled font
                using (var scaledFont = new Font(Font.FontFamily, Font.Size * scale, Font.Style, Font.Unit))
                using (var textBrush = new SolidBrush(ForeColor))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    var textSize = g.MeasureString(Text, scaledFont);
                    var textRect = new RectangleF(
                        (w - textSize.Width) / 2,
                        (h - textSize.Height) / 2,
                        textSize.Width,
                        textSize.Height);
                    g.DrawString(Text, scaledFont, textBrush, textRect);
                }

                // Downscale to control surface
                var targetGraphics = e.Graphics;
                targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                targetGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
                targetGraphics.DrawImage(buffer, new Rectangle(0, 0, Width, Height));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
