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
    using System.Drawing;
    using System.Drawing.Drawing2D;

    /// <summary>
    /// Modern theme configuration for the waiting dialog.
    /// </summary>
    internal class ModernTheme
    {
        /// <summary>
        /// Gets the color palette for different states.
        /// </summary>
        public static class Colors
        {
            // State colors
            public static readonly Color Success = Color.FromArgb(46, 204, 113); // Green
            public static readonly Color SuccessLight = Color.FromArgb(75, 223, 135);
            public static readonly Color Error = Color.FromArgb(231, 76, 60); // Red
            public static readonly Color ErrorLight = Color.FromArgb(246, 103, 88);
            public static readonly Color Unknown = Color.FromArgb(149, 165, 166); // Grey
            public static readonly Color UnknownLight = Color.FromArgb(178, 190, 195);
            public static readonly Color ManualAction = Color.FromArgb(243, 156, 18); // Amber
            public static readonly Color ManualActionLight = Color.FromArgb(248, 196, 113);

            // UI element colors
            public static readonly Color Background = Color.FromArgb(255, 255, 255);
            public static readonly Color TextPrimary = Color.FromArgb(44, 62, 80);
            public static readonly Color TextSecondary = Color.FromArgb(127, 140, 141);
            public static readonly Color Border = Color.FromArgb(236, 240, 241);
            public static readonly Color Shadow = Color.FromArgb(30, 0, 0, 0);

            // Button colors
            public static readonly Color ButtonPositive = Color.FromArgb(46, 204, 113);
            public static readonly Color ButtonPositiveHover = Color.FromArgb(39, 174, 96);
            public static readonly Color ButtonNegative = Color.FromArgb(231, 76, 60);
            public static readonly Color ButtonNegativeHover = Color.FromArgb(192, 57, 43);
            public static readonly Color ButtonDisabled = Color.FromArgb(189, 195, 199);
        }

        /// <summary>
        /// Gets the spacing and sizing constants.
        /// </summary>
        public static class Layout
        {
            public const int CornerRadius = 12;
            public const int Padding = 24;
            public const int PaddingSmall = 12;
            public const int PaddingLarge = 32;
            public const int Spacing = 16;
            public const int SpacingSmall = 8;
            public const int ButtonHeight = 48;
            public const int DialogWidth = 520;
            public const int MinDialogHeight = 200;
            public const int ShadowOffset = 4;
            public const int BorderWidth = 1;
        }

        /// <summary>
        /// Gets the typography settings.
        /// </summary>
        public static class Typography
        {
            public static Font GetHeaderFont()
            {
                return new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            }

            public static Font GetBodyFont()
            {
                return new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
            }

            public static Font GetButtonFont()
            {
                // Increased from 14f to 16f for better readability.
                return new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            }

            public static Font GetTimerFont()
            {
                return new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// Gets the animation settings.
        /// </summary>
        public static class Animation
        {
            public const int TransitionDuration = 200; // milliseconds
            public const int HoverFadeDuration = 150; // milliseconds
        }

        /// <summary>
        /// Creates a rounded rectangle path.
        /// </summary>
        public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Top-left corner
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            // Top-right corner
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            // Bottom-right corner
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            // Bottom-left corner
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Gets the background color for a given state.
        /// </summary>
        public static Color GetStateColor(DialogState state)
        {
            switch (state)
            {
                case DialogState.Success:
                    return Colors.Success;
                case DialogState.Error:
                    return Colors.Error;
                case DialogState.Unknown:
                    return Colors.Unknown;
                case DialogState.ManualAction:
                    return Colors.ManualAction;
                default:
                    return Colors.Unknown;
            }
        }

        /// <summary>
        /// Gets the light background color for a given state.
        /// </summary>
        public static Color GetStateLightColor(DialogState state)
        {
            switch (state)
            {
                case DialogState.Success:
                    return Colors.SuccessLight;
                case DialogState.Error:
                    return Colors.ErrorLight;
                case DialogState.Unknown:
                    return Colors.UnknownLight;
                case DialogState.ManualAction:
                    return Colors.ManualActionLight;
                default:
                    return Colors.UnknownLight;
            }
        }
    }

    /// <summary>
    /// Dialog state enumeration.
    /// </summary>
    internal enum DialogState
    {
        /// <summary>
        /// Unknown state (grey).
        /// </summary>
        Unknown,

        /// <summary>
        /// Success state (green).
        /// </summary>
        Success,

        /// <summary>
        /// Error state (red).
        /// </summary>
        Error,

        /// <summary>
        /// Manual action required (amber).
        /// </summary>
        ManualAction
    }
}
