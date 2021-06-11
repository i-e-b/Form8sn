#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange
//
// Copyright (c) 2005-2019 empira Software GmbH, Cologne Area (Germany)
//
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

#pragma warning disable 1591

// ReSharper disable InconsistentNaming

namespace PdfSharp.Pdf.Content.Objects
{
    /// <summary>
    /// The names of the op-codes. 
    /// </summary>
    public enum OpCodeName
    {
        Dictionary,  // Name followed by dictionary.

        // I know that this is not usable in VB or other languages with no case sensitivity.

        // Reference: TABLE A.1  PDF content stream operators / Page 985
        
        /// <summary>
        /// Close, fill, and stroke path using nonzero winding number rule.
        /// </summary>
        b,

        /// <summary>
        /// Fill and stroke path using nonzero winding number rule.
        /// </summary>
        B,

        /// <summary>
        /// Close, fill, and stroke path using even-odd rule.
        /// </summary>
        bx,  // b*

        /// <summary>
        /// Fill and stroke path using even-odd rule.
        /// </summary>
        Bx,  // B*

        /// <summary>
        /// (PDF 1.2) Begin marked-content sequence with property list.
        /// </summary>
        BDC,

        /// <summary>
        /// Begin inline image object.
        /// </summary>
        BI,

        /// <summary>
        /// (PDF 1.2) Begin marked-content sequence.
        /// </summary>
        BMC,

        /// <summary>
        /// Begin text object.
        /// </summary>
        BT,

        /// <summary>
        /// (PDF 1.1) Begin compatibility section.
        /// </summary>
        BX,

        /// <summary>
        /// curveto - append curved segment to path (3 control points)
        /// </summary>
        c,
        
        /// <summary>
        /// concat - concatenate matrix to current transform matrix
        /// </summary>
        cm,
        
        /// <summary>
        /// set color space - 1.1 set color space for stroking operations
        /// </summary>
        CS,
        /// <summary>
        /// set color space - 1.1 set color space for non-stroking operations
        /// </summary>
        cs,
        /// <summary>
        /// set line dash pattern
        /// </summary>
        d,
        /// <summary>
        /// Set glyph width in Type 3 font
        /// </summary>
        d0,
        /// <summary>
        /// set glyph width and bounding box in Type 3 font
        /// </summary>
        d1,
        /// <summary>
        /// Invoke a named object
        /// </summary>
        Do,

        /// <summary>
        /// (PDF 1.2) Define marked-content point with property list.
        /// </summary>
        DP,

        /// <summary>
        /// End inline image object
        /// </summary>
        EI,

        /// <summary>
        /// (PDF 1.2) End marked-content sequence.
        /// </summary>
        EMC,

        /// <summary>
        /// End text object
        /// </summary>
        ET,

        /// <summary>
        /// (PDF 1.1) End compatibility section.
        /// </summary>
        EX,

        /// <summary>
        /// Fill path using non-zero winding rule
        /// </summary>
        f,
        /// <summary>
        /// (obsolete version of 'f')
        /// </summary>
        F,
        /// <summary>
        /// Fill path using even-odd rule
        /// </summary>
        fx,  // f*
        /// <summary>
        /// Set gray level for stroking operations
        /// </summary>
        G,
        /// <summary>
        /// set gray level for non-stroking operations
        /// </summary>
        g,
        /// <summary>
        /// (PDF 1.2) Set params from graphics state parameter dictionary
        /// </summary>
        gs,
        /// <summary>
        /// Close sub-path
        /// </summary>
        h,
        /// <summary>
        /// Set flatness tolerance
        /// </summary>
        i,
        /// <summary>
        /// Begin inline image data
        /// </summary>
        ID,
        /// <summary>
        /// Set line join style
        /// </summary>
        j,
        /// <summary>
        /// Set line cap style
        /// </summary>
        J,
        /// <summary>
        /// Set CMYK color for stroking operations
        /// </summary>
        K,
        /// <summary>
        /// set CMYK color for non-stroking operations
        /// </summary>
        k,
        /// <summary>
        /// line-to - append straight line segment to path
        /// </summary>
        l,
        /// <summary>
        /// move-to - begin new sub path
        /// </summary>
        m,
        /// <summary>
        /// Set mitre limit
        /// </summary>
        M,

        /// <summary>
        /// (PDF 1.2) Define marked-content point
        /// </summary>
        MP,

        /// <summary>
        /// End path without filling or stroking (used for masks)
        /// </summary>
        n,
        /// <summary>
        /// Push graphics state
        /// </summary>
        q,
        /// <summary>
        /// Pop graphics state
        /// </summary>
        Q,
        /// <summary>
        /// Add rectangle to path
        /// </summary>
        re,
        /// <summary>
        /// set RGB color for stroking operations
        /// </summary>
        RG,
        /// <summary>
        /// set RGB color for non-stroking operations
        /// </summary>
        rg,
        /// <summary>
        /// Set color rendering intent
        /// </summary>
        ri,
        /// <summary>
        /// Close path and stroke
        /// </summary>
        s,
        /// <summary>
        /// Stroke path
        /// </summary>
        S,
        /// <summary>
        /// (PDF 1.1) Set color for stroking operations
        /// </summary>
        SC,
        /// <summary>
        /// (PDF 1.1) Set color for non-stroking operations
        /// </summary>
        sc,
        /// <summary>
        /// (PDF 1.2) Set color for stroking operations - ICCBased and special color spaces
        /// </summary>
        SCN,
        /// <summary>
        /// (PDF 1.2) Set color for non-stroking operations - ICCBased and special color spaces
        /// </summary>
        scn,
        /// <summary>
        /// (PDF 1.3) Paint area defined by shading pattern
        /// </summary>
        sh,
        /// <summary>
        /// Move to start of next text line
        /// </summary>
        Tx,  // T*
        /// <summary>
        /// Set character spacing
        /// </summary>
        Tc,
        /// <summary>
        /// Move text position
        /// </summary>
        Td,
        /// <summary>
        /// Move text position and set leading
        /// </summary>
        TD,
        /// <summary>
        /// Set font and size
        /// </summary>
        Tf,
        /// <summary>
        /// Show text
        /// </summary>
        Tj,
        /// <summary>
        /// Show text with glyph positioning
        /// </summary>
        TJ,
        /// <summary>
        /// Set text leading
        /// </summary>
        TL,
        /// <summary>
        /// Set text matrix and text line matrix
        /// </summary>
        Tm,
        /// <summary>
        /// Set text render mode
        /// </summary>
        Tr,
        /// <summary>
        /// Set text rise
        /// </summary>
        Ts,
        /// <summary>
        /// Set word spacing
        /// </summary>
        Tw,
        /// <summary>
        /// Set horizontal text scaling
        /// </summary>
        Tz,
        /// <summary>
        /// Append curved segment to path (initial point replicated)
        /// </summary>
        v,
        /// <summary>
        /// Set line width
        /// </summary>
        w,
        /// <summary>
        /// Set clipping path using non-zero winding number rule
        /// </summary>
        W,
        /// <summary>
        /// Set clipping path using even-odd rule
        /// </summary>
        Wx,  // W*
        /// <summary>
        /// Append curved segment to path (final point replicated)
        /// </summary>
        y,

        /// <summary>
        /// Move to next line and show text.
        /// </summary>
        QuoteSingle, // '

        /// <summary>
        /// Set word and character spacing, move to next line, and show text.
        /// </summary>
        QuoteDbl,  // "
    }
}
