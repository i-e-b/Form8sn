using System;
using System.Collections.Generic;
using System.Linq;
using Portable.Drawing.Text;
using Portable.Drawing.Toolkit.Fonts;
using Portable.Drawing.Toolkit.Portable;
using Portable.Drawing.Toolkit.Portable.Rasteriser;

namespace Portable.Drawing.Toolkit.TextLayout
{
    /// <summary>
    /// Performs text layout for drawing and measuring calculations.
    /// </summary>
    internal sealed class TextLayoutManager
    {
        // Internal state.
        private bool onlyWholeLines;
        private bool prevIsNewLine;
        private int hotkeyIndex;
        private int lineHeight;
        private int lineNumber;
        private int glyphNumber;
        private int lineSpaceUsedUp;
        private int maxLineSpaceUsed;
        private int nextIndex;
        private Rectangle layout;

        public int LinesFitted => lineNumber + (glyphNumber>0?1:0);
        public int CharactersFitted => glyphNumber;
        
        public SizeF SizeFilled => new SizeF(maxLineSpaceUsed, lineHeight+LinesFitted); // IEB: need to get max width and total height

        private Font? _underlineFont;
        private Font? _font;
        private Graphics? _graphics;
        private string? _text;
        private StringFormat? _format;

        private static readonly StringFormat _defaultStringFormat = StringFormat.GenericDefault;
        private static readonly Point[] _measureLayoutRect = new Point[0];
            
        public List<RenderableGlyph> LayoutText(Graphics graphics, string? text, Font font, Rectangle drawLayout, StringFormat? format)
        {
            var output = new List<RenderableGlyph>();
            if (text == null) return output;
            
            // set the current graphics
            _graphics = graphics;

            // set the current text
            _text = text;

            // set the current font
            _font = font;

            // set the current layout rectangle
            layout = drawLayout;

            // set the current string format
            _format = format ?? _defaultStringFormat;

            // set the default hotkey index
            hotkeyIndex = -1;

            // set the current line height
            lineHeight = font.Height;
            if (lineHeight < 1) throw new Exception("Invalid line height");

            // set the only whole lines flag
            onlyWholeLines = (_format.FormatFlags & StringFormatFlags.LineLimit) != 0
                             || _format.Trimming != StringTrimming.None;

            
            nextIndex = 0;
            lineSpaceUsedUp = 0;
            lineNumber = 0;
            glyphNumber = 0;

            // set the previous span ended in new line flag
            prevIsNewLine = false;

            // select the current font into the graphics context
            graphics.SelectFont(font);

            // set the current text start
            int textStart = 0;

            // set the current text length
            int textLength = 0;

            // set the current text width
            int textWidth = 0;

            // get the actual hotkey index, if needed
            if (_format.HotkeyPrefix != HotkeyPrefix.None)
            {
                // get the hotkey index
                hotkeyIndex = text.IndexOf('&');

                // handle the hotkey as needed
                if (hotkeyIndex != -1)
                {
                    if (hotkeyIndex >= (text.Length - 1) ||
                        char.IsControl(text[hotkeyIndex + 1]))
                    {
                        // no need for this anymore
                        hotkeyIndex = -1;
                    }
                    else
                    {
                        // remove the hotkey character
                        text = text.Substring(0, hotkeyIndex) +
                               text.Substring(hotkeyIndex + 1);

                        // set the current text
                        _text = text;

                        // prepare to show or hide the underline
                        if (_format.HotkeyPrefix == HotkeyPrefix.Show)
                        {
                            // get the underline font
                            _underlineFont = new Font
                                (font, font.Style | FontStyle.Underline);
                        }
                        else
                        {
                            // no need for this anymore
                            hotkeyIndex = -1;
                        }
                    }
                }
            }

            // layout the text
            try
            {
                // handle drawing based on line alignment
                if (_format.LineAlignment == StringAlignment.Near)
                {
                    // set the current y position
                    int y = layout.Top;

                    // get the maximum y position
                    int maxY = layout.Bottom;

                    // adjust for whole lines, if needed
                    if (onlyWholeLines)
                    {
                        maxY -= ((maxY - y) % lineHeight);
                    }

                    // get the last line y position
                    int lastLineY = maxY - lineHeight;

                    // create character spans
                    CharSpan span = new();
                    CharSpan prev = new();

                    // set the first span flag
                    bool firstSpan = true;

                    // process the text
                    while (nextIndex < text.Length)
                    {
                        // get the next span of characters
                        GetNextSpan(span);

                        // draw the pending line, as needed
                        if (span.newline && !firstSpan)
                        {
                            // draw the line, if needed
                            if (textWidth > 0)
                            {
                                // remove trailing spaces, if needed
                                if (!firstSpan && text[prev.start] == ' ')
                                {
                                    // update text width
                                    textWidth -= GetSpanWidth(prev);

                                    // update text length
                                    textLength -= prev.length;
                                }

                                // draw the line
                                DrawLine (output, textStart, textLength, textWidth, y, (y > lastLineY));
                            }

                            // update the y position
                            y += lineHeight;

                            // update the line number
                            ++lineNumber;

                            // update the text start
                            textStart = span.start;

                            // reset the text length
                            textLength = 0;

                            // reset the text width
                            textWidth = 0;
                        }

                        // update the text length
                        textLength += span.length;

                        // update the text width
                        textWidth += GetSpanWidth(span);

                        // copy span values to previous span
                        span.CopyTo(prev);

                        // set the first span flag
                        firstSpan = false;

                        // break if the y position is out of bounds
                        if (y > maxY)
                        {
                            break;
                        }
                    }

                    // draw the last line, if needed
                    if (textWidth > 0 && y <= maxY)
                    {
                        // draw the last line
                        DrawLine(output, textStart, textLength, textWidth, y, (y > lastLineY));
                    }
                }
                else
                {
                    // set default lines to draw
                    int linesToDraw;

                    // calculate lines to draw
                    if (onlyWholeLines)
                    {
                        linesToDraw = layout.Height / lineHeight;
                    }
                    else
                    {
                        linesToDraw = (int) Math.Ceiling((double) layout.Height / lineHeight);
                    }

                    // create line span list
                    LineSpan[] lines = new LineSpan[linesToDraw];

                    // create character spans
                    CharSpan span = new();
                    CharSpan prev = new();

                    // set the first span flag
                    bool firstSpan = true;

                    // set the current line position
                    int linePos = 0;

                    // populate line span list
                    while (linePos < lines.Length &&
                           nextIndex < text.Length)
                    {
                        // get the next span of characters
                        GetNextSpan(span);

                        // handle span on new line
                        if (span.newline && !firstSpan)
                        {
                            // remove trailing spaces, if needed
                            if (!firstSpan && text[prev.start] == ' ')
                            {
                                // update text width
                                textWidth -= GetSpanWidth(prev);

                                // update text length
                                textLength -= prev.length;
                            }

                            // create line span for current line
                            LineSpan lineSpan = new LineSpan
                                (textStart, textLength, textWidth);

                            // add current line span to line span list
                            lines[linePos++] = lineSpan;

                            // update text start
                            textStart = span.start;

                            // update text length
                            textLength = 0;

                            // update text width
                            textWidth = 0;
                        }

                        // update text length
                        textLength += span.length;

                        // update text width
                        textWidth += GetSpanWidth(span);

                        // copy span values to previous span
                        span.CopyTo(prev);

                        // set the first span flag
                        firstSpan = false;
                    }

                    // add the last line to the line span list
                    if (linePos < lines.Length)
                    {
                        // create line span for last line
                        LineSpan lineSpan = new LineSpan
                            (textStart, textLength, textWidth);

                        // add last line span to the line span list
                        lines[linePos++] = lineSpan;
                    }

                    // calculate the top line y
                    int y = (layout.Height - (linePos * lineHeight));

                    // adjust y for center alignment, if needed
                    if (_format.LineAlignment == StringAlignment.Center)
                    {
                        y /= 2;
                    }

                    // translate y to layout rectangle
                    y += layout.Top;

                    // adjust line position to last line
                    --linePos;

                    // draw the lines
                    for (int i = 0; i < linePos; ++i)
                    {
                        // get the current line
                        LineSpan line = lines[i];

                        // draw the current line
                        DrawLine (output, line.start, line.length, line.pixelWidth, y, false);

                        // update the y position
                        y += lineHeight;
                    }

                    // draw the last line
                    DrawLine (output, lines[linePos].start, lines[linePos].length, lines[linePos].pixelWidth, y, true);
                }
            }
            finally
            {
                // dispose the underline font, if we have one
                if (_underlineFont != null)
                {
                    // dispose the underline font
                    _underlineFont.Dispose();

                    // reset the underline font
                    _underlineFont = null;
                }
            }
            
            return output;
        }


        // Calculate whether a word wraps to a new line.
        private void CheckForWrap(CharSpan span)
        {
            // bail out now if there's nothing to do
            if (span.length == 0 || _text == null || _format == null) return;

            // reset the line space usage, if needed
            if (span.newline)
            {
                maxLineSpaceUsed = Math.Max(maxLineSpaceUsed, lineSpaceUsedUp);
                lineSpaceUsedUp = 0;
            }

            // get the first character of the span
            char c = _text[span.start];

            // handle no-wrap span, if needed
            if (c != ' ' && (_format.FormatFlags & StringFormatFlags.NoWrap) == 0)
            {
                // get the width of the span
                int width = GetSpanWidth(span);

                // handle wrapping of span, as needed
                if ((lineSpaceUsedUp + width) > layout.Width)
                {
                    // handle no-wrap span trimming, if needed
                    if (width > layout.Width)
                    {
                        // trim the span
                        span.length = TrimTextToChar
                        (span.start, span.length, layout.Width,
                            out span.pixelWidth);

                        // update the text position
                        nextIndex = span.start + span.length;

                        // set the new line flag, if needed
                        if (lineNumber > 0)
                        {
                            span.newline = true;
                        }

                        // set the previous span ended in new line flag
                        prevIsNewLine = true;
                    }

                    // reset line space usage
                    lineSpaceUsedUp = 0;

                    // set the new line flag
                    span.newline = true;
                }
            }

            // update line space usage
            lineSpaceUsedUp += GetSpanWidth(span);
            maxLineSpaceUsed = Math.Max(maxLineSpaceUsed, lineSpaceUsedUp);
        }
        
        // Draw a line.
        private void DrawLine (List<RenderableGlyph> output, int start, int length, int width, int y, bool lastLine)
        {
            if (_text == null || _format == null) throw new InvalidOperationException("Text layout manager has invalid arguments");
            
            // set default x position
            int x;

            // set truncate line flag
            bool truncateLine = (lastLine && ((start + length) < _text.Length)) ||
                                ((width > layout.Width) &&
                                 ((_format.FormatFlags & StringFormatFlags.NoWrap) != 0));

            // update the truncate line flag, as needed

            // handle no truncation case
            if (!truncateLine)
            {
                // update x position
                x = GetXPosition(width);

                // draw the line
                if (hotkeyIndex < start ||
                    hotkeyIndex >= (start + length))
                {
                    string s = _text.Substring(start, length);
                    AddGlyphs(output, s, x, y, _format);
                }
                else
                {
                    DrawLineWithHotKey(output, _text, start, length, x, y);
                }

                // we're done here
                return;
            }

            // set the default ellipsis
            string? ellipsis = null;

            // set the maximum width
            int maxWidth = layout.Width;

            // 
            if (_format.Trimming == StringTrimming.EllipsisCharacter ||
                _format.Trimming == StringTrimming.EllipsisWord ||
                _format.Trimming == StringTrimming.EllipsisPath)
            {
                // set the ellipsis
                ellipsis = "...";

                // update the maximum width, if needed
                if (_format.Trimming != StringTrimming.EllipsisPath)
                {
                    // update the maximum width
                    maxWidth -= MeasureStringWidth(ellipsis, _measureLayoutRect, _format);
                }
            }

            // set the default draw string
            string? drawS;

            // trim and draw the string
            switch (_format.Trimming)
            {
                case StringTrimming.None:
                case StringTrimming.EllipsisCharacter:
                case StringTrimming.Character:
                {
                    // update length, if needed
                    if (width > maxWidth)
                    {
                        // update length
                        length = TrimTextToChar
                            (start, length, maxWidth, out width);
                    }

                    // set the draw string
                    drawS = _text.Substring(start, length);

                    // update the draw string, if needed
                    if (ellipsis != null)
                    {
                        drawS += ellipsis;
                    }

                    // update the x position
                    x = GetXPosition(width);

                    // draw the line
                    if (hotkeyIndex < start ||
                        hotkeyIndex >= (start + length))
                    {
                        // draw the line
                        AddGlyphs(output, drawS, x, y, _format);
                    }
                    else
                    {
                        // draw the line with hotkey underlining
                        DrawLineWithHotKey(output, drawS, 0, drawS.Length, x, y);
                    }
                }
                    break;

                case StringTrimming.EllipsisWord:
                case StringTrimming.Word:
                {
                    // set the draw string
                    drawS = _text.Substring
                    (start,
                        TrimTextToWord
                            (start, length, maxWidth, out width));

                    // update the draw string, if needed
                    if (ellipsis != null)
                    {
                        drawS += ellipsis;
                    }

                    // update the x position
                    x = GetXPosition(width);

                    // draw the line
                    if (hotkeyIndex < start ||
                        hotkeyIndex >= (start + length))
                    {
                        // draw the line
                        AddGlyphs(output, drawS, x, y, _format);
                    }
                    else
                    {
                        // draw the line with hotkey underlining
                        DrawLineWithHotKey(output, drawS, 0, drawS.Length, x, y);
                    }
                }
                    break;

                case StringTrimming.EllipsisPath:
                {
                    ellipsis ??= "...";
                    // set the draw string
                    drawS = TrimToPath
                    (start, (_text.Length - start), maxWidth,
                        out width, ellipsis);

                    // update the x position
                    x = GetXPosition(width);

                    // draw the line
                    if (hotkeyIndex < start ||
                        hotkeyIndex >= (start + length))
                    {
                        // draw the line
                        AddGlyphs(output, drawS, x, y, _format);
                    }
                    else
                    {
                        // draw the line with hotkey underlining
                        DrawLineWithHotKey(output, drawS, 0, drawS.Length, x, y);
                    }
                }
                    break;
            }
        }

        /// <summary>
        /// Measure the string width
        /// TODO: we currently ignore layout-rect and format
        /// </summary>
        private int MeasureStringWidth(string str, Point[] measureLayoutRect, StringFormat format)
        {
            var tkf = _font?.GetToolkitFont();
            if (!(tkf is PortableFont pf)) throw new Exception($"Tried to measure using font def: {tkf?.GetType().FullName ?? "<null>"}");
            
            var font = pf.BaseTrueTypeFont;
            
            var scale = pf.GetScale();

            // Really dumb way to start -- not using layout rect, just count up the character sizes.
            // when we do implement it, make sure DrawString uses the same logic
            var width = 0.0;
            foreach (char c in str)
            {
                var gl = font.ReadGlyph(c);
                if (gl == null) continue;
                var w = (gl.xMax - gl.xMin) * scale;
                width += w;
            }
            return (int)Math.Round(width);
        }

        /// <summary>
        /// Position font glyphs and add them to the output. This does no rendering
        /// </summary>
        private void AddGlyphs(List<RenderableGlyph> output, string str, int x, int y, StringFormat format)
        {
            var tkf = _font?.GetToolkitFont();
            if (!(tkf is PortableFont pf)) throw new Exception($"Tried to measure using font def: {tkf?.GetType().FullName ?? "<null>"}");
            
            var font = pf.BaseTrueTypeFont;
            
            var scale = pf.GetScale();
            
            // adjustment to have (x,y) be the top-left, rather than baseline-left
            //var ry = y + font.Height() * scale;
            var ry = y + pf.GetLineHeight();
            
            double xOff = x; // advance for each character (not correct yet)
            foreach (char c in str)
            {
                glyphNumber++; // number of glyphs attempted, regardless of whitespace
                
                var gl = font.ReadGlyph(c);
                if (gl == null) continue;
                
                var bearing = gl.LeftBearing * scale;
                var yAdj = gl.yMin * scale;
                var outline = gl.NormalisedContours(); // break into simple lines

                var glyph = PositionGlyph(ry, outline, xOff + bearing, scale, yAdj);
                if (glyph != null) output.Add(glyph);
                
                xOff += gl.Advance * scale;
            }
        }
        
        private RenderableGlyph? PositionGlyph(double y, List<GlyphPoint[]> outline, double xOff, double scale, double yAdj)
        {
            if (outline.Count < 1) return null;
            
            var points = new RenderableGlyph();
            foreach (var curve in outline)
            {
                var contour = new RasterContour(
                    curve.Select(p =>
                        new Vector2(
                            xOff + p.X * scale,
                            y + (0-p.Y) * scale - yAdj
                        )
                    )
                );
                points.Add(contour);
            }
            return points;
        }


        // Draw a line containing hotkey text.
        private void DrawLineWithHotKey(List<RenderableGlyph> output, string text, int start, int length, int x, int y)
        {
            if (_graphics == null || _format == null) return;
            
            // set the default text
            string? s;

            // draw the pre-hotkey text
            if (hotkeyIndex > start)
            {
                // get the pre-hotkey text
                s = text.Substring(start, (hotkeyIndex - start));

                // draw the pre-hotkey text
                AddGlyphs(output, s, x, y, _format);

                // update the x position
                x += MeasureStringWidth(s, _measureLayoutRect, _format);
            }

            // get the hotkey text
            s = text.Substring(hotkeyIndex, 1);

            // select the underline font
            if (_underlineFont != null) _graphics.SelectFont(_underlineFont);

            // draw the hotkey text
            AddGlyphs(output, s, x, y, _format);

            // revert to the regular font
            if (_font != null) _graphics.SelectFont(_font);

            // update the x position
            x += MeasureStringWidth(s, _measureLayoutRect, _format);

            // draw the post-hotkey text
            if (hotkeyIndex < ((start + length) - 1))
            {
                // get the start index of the post-hotkey text
                int index = (hotkeyIndex + 1);

                // get the length of the post-hotkey text
                length -= (index - start);

                // get the post-hotkey text
                s = text.Substring(index, length);

                // draw the post-hotkey text
                AddGlyphs(output, s, x, y, _format);
            }
        }

        // Calculate text metrics information.
        //
        // Note that this is currently broken. Turn this on at your own risk.
        public Size GetBounds
        (Graphics graphics, string text, Font font,
            SizeF layoutSize, StringFormat? format,
            out int charactersFitted, out int linesFilled)
        {
            // set the current graphics
            _graphics = graphics;

            // set the current text
            _text = text;

            // set the current font
            _font = font;

            // ensure we have a string format
            format ??= _defaultStringFormat;

            // set the current string format
            _format = format;

            // set the current layout rectangle
            layout = new Rectangle
                (0, 0, (int) layoutSize.Width, (int) layoutSize.Height);

            // set the current line height
            lineHeight = font.Height;

            // set the only whole lines flag
            onlyWholeLines = (format.FormatFlags & StringFormatFlags.LineLimit) != 0
                             || format.Trimming == StringTrimming.None;

            nextIndex = 0;
            lineSpaceUsedUp = 0;
            maxLineSpaceUsed = 0;

            // set the previous span ended in new line flag
            prevIsNewLine = false;

            // select the current font into the graphics context
            graphics.SelectFont(font);

            // set the text width
            int textWidth = 0;

            // set the maximum width
            int maxWidth = 0;

            // set the default characters fitted
            charactersFitted = 0;

            // set the default lines filled
            linesFilled = 0;

            // remove the hotkey prefix, if needed
            if (format.HotkeyPrefix != HotkeyPrefix.None)
            {
                // get the hotkey index
                hotkeyIndex = text.IndexOf('&');

                // handle the hotkey as needed
                if (hotkeyIndex != -1)
                {
                    if (hotkeyIndex < (text.Length - 1) &&
                        !char.IsControl(text[hotkeyIndex + 1]))
                    {
                        // remove the hotkey character
                        text = text.Substring(0, hotkeyIndex) +
                               text.Substring(hotkeyIndex + 1);

                        // set the current text
                        _text = text;

                        // update characters fitted
                        ++charactersFitted;
                    }

                    // no need for this anymore
                    hotkeyIndex = -1;
                }
            }

            // create character spans
            CharSpan span = new();
            CharSpan prev = new();

            // set the first span flag
            bool firstSpan = true;

            // set the measure trailing spaces flag
            bool mts = ((format.FormatFlags &
                         StringFormatFlags.MeasureTrailingSpaces) != 0);

            // process the text
            while (nextIndex < text.Length)
            {
                // get the next span of characters
                GetNextSpan(span);

                // handle span on new line
                if (span.newline)
                {
                    // remove trailing spaces, if needed
                    if (!firstSpan && !mts && text[prev.start] == ' ')
                    {
                        // update the text width
                        textWidth -= GetSpanWidth(prev);
                    }

                    // update the maximum width, if needed
                    if (textWidth > maxWidth)
                    {
                        maxWidth = textWidth;
                    }

                    // update the text width
                    textWidth = 0;

                    // update the lines filled
                    ++linesFilled;
                }

                // update the text width
                textWidth += GetSpanWidth(span);

                // update the characters fitted
                charactersFitted += span.length;

                // copy span values to previous span
                span.CopyTo(prev);
            }

            // update the maximum width, if needed
            if (textWidth > maxWidth)
            {
                maxWidth = textWidth;
            }

            // update the lines filled to account for the first line
            ++linesFilled;

            // update the maximum width, if needed
            if (maxWidth > layout.Width)
            {
                maxWidth = layout.Width;
            }

            // calculate the height
            int height = (lineHeight * linesFilled);

            // update the height, if needed
            if (height > layout.Height)
            {
                height = layout.Height;
            }

            // return the size of the text
            return new Size(maxWidth, height);
        }

        // Get the next span of characters.
        private void GetNextSpan(CharSpan span)
        {
            if (_text == null) throw new InvalidOperationException();
            
            // set new line flag
            var newline = false;

            // get the start index
            var start = nextIndex;

            // handle whitespace span
            while (nextIndex < _text.Length && _text[nextIndex] == ' ')
            {
                ++nextIndex;
            }

            // handle word span
            if (nextIndex == start)
            {
                // find the end of the word
                while (nextIndex < _text.Length)
                {
                    // get the current character
                    char c = _text[nextIndex];

                    // find the end of the word
                    if (c == ' ' || c == '\n' || c == '\r')
                    {
                        break;
                    }

                    // we also split on minus to mimic MS behavior
                    if (c == '-')
                    {
                        nextIndex++;
                        break;
                    }

                    // move to the next character
                    ++nextIndex;
                }
            }

            // get the length of the span
            int length = nextIndex - start;

            // handle new line characters
            if (nextIndex < _text.Length)
            {
                // get the current character
                char c = _text[nextIndex];

                // check for new line characters
                if (c == '\r')
                {
                    // move past the carriage return
                    ++nextIndex;

                    // move past the line feed, if needed
                    if (nextIndex < _text.Length &&
                        _text[nextIndex] == '\n')
                    {
                        ++nextIndex;
                    }

                    // set the new line flag
                    newline = true;
                }
                else if (c == '\n')
                {
                    // move past the line feed
                    ++nextIndex;

                    // set the new line flag
                    newline = true;
                }
            }

            // set the span values
            span.Set(start, length, prevIsNewLine);

            // update the previous span ended in new line flag
            prevIsNewLine = newline;

            // handle wrapping
            CheckForWrap(span);
        }

        // Get the width of the span in pixels.
        private int GetSpanWidth(CharSpan span)
        {
            if (_text == null || _format == null) throw new InvalidOperationException();
            
            // set the width of the span, if needed
            if (span.pixelWidth != -1) return span.pixelWidth;
            
            // get the text of the span
            string s = _text.Substring(span.start, span.length);

            // set the width of the span
            span.pixelWidth = MeasureStringWidth(s, _measureLayoutRect, _format);

            // return the width of the span
            return span.pixelWidth;
        }

        // Calculate the position of the line based on the formatting and width.
        private int GetXPosition(int width)
        {
            if (_format == null) throw new InvalidOperationException();
            
            // set the default x position
            var x = layout.X;

            switch (_format.Alignment)
            {
                // update the x position based on alignment
                case StringAlignment.Near:
                    return x;
                case StringAlignment.Far:
                    x += (layout.Width - width);
                    break;
                case StringAlignment.Center:
                    //???
                    break;
                default:
                    x += ((layout.Width - width) / 2);
                    break;
            }

            // return the x position
            return x;
        }

        // Trim to the nearest character.
        //
        // Returns the length of characters from the string once it is trimmed.
        // The "width" variable returns the pixel width of the trimmed string.
        private int TrimTextToChar (int start, int length, int maxWidth, out int width)
        {
            if (_text == null || _format == null) throw new InvalidOperationException();
            
            // set default width
            width = 0;

            // get the current width
            int currWidth = MeasureStringWidth(_text.Substring(start, length), _measureLayoutRect, _format);
            
            // handle trivial case first
            if (currWidth <= maxWidth)
            {
                // set the width
                width = currWidth;

                // return the characters fitted
                return length;
            }

            // set the left boundary
            int left = 0;

            // set the right boundary
            int right = (length - 1);

            // set the best fit
            int best = 0;

            // find the maximum number of characters which fit
            while (left <= right)
            {
                // calculate the middle position
                int middle = ((left + right) / 2);

                // get the current width
                currWidth = MeasureStringWidth(_text.Substring(start, middle), _measureLayoutRect, _format);

                // continue search or return depending on comparison
                if (currWidth > maxWidth)
                {
                    // reposition right boundary
                    right = (middle - 1);
                }
                else if (currWidth < maxWidth)
                {
                    // update the best fit
                    best = middle;

                    // update the best fit width
                    width = currWidth;

                    // reposition left boundary
                    left = (middle + 1);
                }
                else
                {
                    // update the best fit width
                    width = currWidth;

                    // return the best fit
                    return middle;
                }
            }

            // return the best fit
            return best;
        }

        // Trim to the path.
        //
        // Returns the trimmed string. The "width" variable returns the pixel
        // width of the trimmed string. The trimming algorithm tries to place
        // the characters removed in the center of the string but also tries
        // to guarantee that the last path separator character is shown.
        private string TrimToPath
        (int start, int length, int maxWidth, out int width,
            string ellipsis)
        {
            if (_text == null || _format == null) throw new InvalidOperationException();
            
            // set the default return value
            System.Text.StringBuilder outcome = new(_text.Substring(start, length));

            // measure the width of the return value
            width = MeasureStringWidth (outcome.ToString(), _measureLayoutRect, _format);

            // return the text if it fits
            if (width < maxWidth) return outcome.ToString();

            // set the middle position
            int middle = ((start + (length / 2)) + 2);

            // set the separator found flag
            bool separatorFound = false;

            // set the remove position
            int removePos = ((start + length) - 1);

            // find the optimal remove position
            while (removePos >= start)
            {
                // get the current character
                char c = _text[removePos];

                // check for separator
                if (c == '\\' || c == '/')
                {
                    separatorFound = true;
                }

                // break if we've found a separator before the middle
                if (separatorFound && removePos <= middle)
                {
                    break;
                }

                // update the remove position
                --removePos;
            }

            // remove from the middle if no separator was found
            if (!separatorFound)
            {
                removePos = middle;
            }

            // set the removal start position
            int removeStart = (removePos - 1);

            // find and return the optimal trim pattern
            while (true)
            {
                // attempt to fit the ellipsis
                if (width < maxWidth)
                {
                    // set the return value to the pre-removal text
                    outcome = new System.Text.StringBuilder(_text.Substring
                        (start, (removeStart - start)));

                    // append the ellipsis to the return value
                    outcome.Append(ellipsis);

                    // append the post-removal text to the return value
                    outcome.Append(_text.Substring
                        (removePos, ((start + length) - removePos)));

                    // measure the width of the return value
                    width = MeasureStringWidth(outcome.ToString(), _measureLayoutRect, _format);
                    
                    // return the text if it fits
                    if (width < maxWidth)
                    {
                        return outcome.ToString();
                    }
                }

                // set the reduced flag
                bool reduced = false;

                // attempt to reduce
                if (removeStart > start)
                {
                    // measure the width of the text
                    width -= MeasureStringWidth(_text.Substring(removeStart--, 1), _measureLayoutRect, _format);

                    // continue if no reduction is needed
                    if (width < maxWidth)
                    {
                        continue;
                    }

                    // set the reduced flag
                    reduced = true;
                }

                // attempt to reduce
                if (removePos < (start + length))
                {
                    // measure the width of the text
                    width -= MeasureStringWidth(_text.Substring(removePos++, 1), _measureLayoutRect, _format);

                    // continue if no reduction is needed
                    if (width < maxWidth)
                    {
                        continue;
                    }

                    // set the reduced flag
                    reduced = true;
                }

                // return the ellipsis, if needed
                if (!reduced)
                {
                    // measure the width of the ellipsis
                    width = MeasureStringWidth(ellipsis, _measureLayoutRect, _format);

                    // return the ellipsis
                    return ellipsis;
                }
            }
        }

        // Trim to the nearest word or character, as appropriate.
        //
        // Returns the length of characters from the string once it is trimmed.
        // The "width" variable returns the pixel width of the trimmed string.
        // If the string has no words then it is trimmed to the nearest
        // character.
        private int TrimTextToWord
            (int start, int length, int maxWidth, out int width)
        {
            if (_text == null || _format == null) throw new InvalidOperationException();
            
            // set the default width
            width = 0;

            // set the end position
            int end = start + length;

            // set the start position
            int pos = start;

            // set the previous position
            int prevPos = pos;

            // process the text
            while (pos < end)
            {
                // get the current character
                char c = _text[pos];

                // skip over leading spaces
                if (c == ' ')
                {
                    // move past space
                    ++pos;

                    // skip over remaining spaces
                    while (pos < end && _text[pos] == ' ')
                    {
                        ++pos;
                    }
                }

                // skip over word
                while (pos < end && _text[pos] != ' ')
                {
                    ++pos;
                }

                // get the width of the text
                int stringWidth = MeasureStringWidth(_text.Substring(prevPos, (pos - prevPos)), _measureLayoutRect, _format);

                // return the characters fitted, if max width exceeded
                if ((width + stringWidth) > maxWidth)
                {
                    // trim within the word, if needed
                    if (width == 0)
                    {
                        // trim within the word
                        return TrimTextToChar
                            (start, length, maxWidth, out width);
                    }

                    // return the characters fitted
                    return (prevPos - start);
                }

                // update the current width
                width += stringWidth;

                // update the previous position
                prevPos = pos;
            }

            // return the characters fitted
            return length;
        }
    };
}