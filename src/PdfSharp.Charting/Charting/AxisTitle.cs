#region PDFsharp Charting - A .NET charting library based on PDFsharp
//
// Authors:
//   Niklas Schneider
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

using System;

namespace PdfSharp.Charting
{
    /// <summary>
    /// Represents the title of an axis.
    /// </summary>
    public class AxisTitle : ChartObject
    {
        /// <summary>
        /// Initializes a new instance of the AxisTitle class.
        /// </summary>
        public AxisTitle()
        { }

        /// <summary>
        /// Initializes a new instance of the AxisTitle class with the specified parent.
        /// </summary>
        internal AxisTitle(DocumentObject parent) : base(parent) { }

        #region Methods
        /// <summary>
        /// Creates a deep copy of this object.
        /// </summary>
        public new AxisTitle Clone()
        {
            return (AxisTitle)DeepCopy();
        }

        /// <summary>
        /// Implements the deep copy of the object.
        /// </summary>
        protected override object DeepCopy()
        {
            AxisTitle axisTitle = (AxisTitle)base.DeepCopy();
            if (axisTitle._font != null)
            {
                axisTitle._font = axisTitle._font.Clone();
                axisTitle._font._parent = axisTitle;
            }
            return axisTitle;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the caption of the title.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }
        internal string _caption = String.Empty;

        /// <summary>
        /// Gets the font of the title.
        /// </summary>
        public Font Font
        {
            get { return _font ?? (_font = new Font(this)); }
        }
        internal Font? _font;

        /// <summary>
        /// Gets or sets the orientation of the caption.
        /// </summary>
        public double Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
        }
        internal double _orientation;

        /// <summary>
        /// Gets or sets the horizontal alignment of the caption.
        /// </summary>
        public HorizontalAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; }
        }
        internal HorizontalAlignment _alignment;

        /// <summary>
        /// Gets or sets the vertical alignment of the caption.
        /// </summary>
        public VerticalAlignment VerticalAlignment
        {
            get { return _verticalAlignment; }
            set { _verticalAlignment = value; }
        }
        internal VerticalAlignment _verticalAlignment;
        #endregion
    }
}
