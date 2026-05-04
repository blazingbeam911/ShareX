#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

namespace ShareX.HelpersLib
{
    /// <summary>
    /// User-facing knobs for the WebP encoder. Maps onto a subset of libwebp's
    /// WebPConfig (see https://developers.google.com/speed/webp/docs/api). Fields
    /// not exposed here stay at libwebp's preset defaults.
    /// </summary>
    public class WebPEncodingOptions
    {
        /// <summary>Lossy or Lossless. Drives how Quality is interpreted.</summary>
        public WebPCompressionMode Mode { get; set; } = WebPCompressionMode.Lossy;

        /// <summary>
        /// 0..100. For Lossy: visual quality (higher = bigger, better).
        /// For Lossless: amount of effort spent compressing (higher = slower, smaller).
        /// </summary>
        public int Quality { get; set; } = 75;

        /// <summary>0..6. Quality/speed trade-off. 0 = fastest, 6 = slower-better. libwebp default 4.</summary>
        public int Method { get; set; } = 4;

        /// <summary>Source-image preset. Tunes multiple internal config fields.</summary>
        public WebPEncodingPreset Preset { get; set; } = WebPEncodingPreset.Default;

        /// <summary>0..100. Quality of the alpha plane. libwebp default 100.</summary>
        public int AlphaQuality { get; set; } = 100;

        /// <summary>
        /// If true, preserve the exact RGB values under fully transparent pixels (helpful
        /// when downstream tooling reads through alpha). libwebp default is false because
        /// discarding the values gives smaller files.
        /// </summary>
        public bool Exact { get; set; } = false;
    }
}
