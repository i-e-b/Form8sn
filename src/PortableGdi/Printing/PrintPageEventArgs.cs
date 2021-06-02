/*
 * PrintPageEventArgs.cs - Implementation of the
 *			"System.Drawing.Printing.PrintPageEventArgs" class.
 *
 * Copyright (C) 2003  Southern Storm Software, Pty Ltd.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

namespace System.Drawing.Printing
{

public class PrintPageEventArgs : EventArgs
{
	// Internal state.
	internal Graphics graphics;
	private Rectangle pageBounds;

	// Constructor.
	public PrintPageEventArgs(Graphics graphics,
							  Rectangle marginBounds,
							  Rectangle pageBounds,
							  PageSettings pageSettings)
			{
				Cancel = false;
				HasMorePages = false;
				this.graphics = graphics;
				MarginBounds = marginBounds;
				this.pageBounds = pageBounds;
				PageSettings = pageSettings;
			}
	internal PrintPageEventArgs(PageSettings pageSettings)
			{
				Cancel = false;
				HasMorePages = false;
				graphics = null;
				pageBounds = pageSettings.Bounds;
				Margins margins = pageSettings.Margins;
				MarginBounds = new Rectangle
					(margins.Left, margins.Top,
					 pageBounds.Width - margins.Left - margins.Right,
					 pageBounds.Height - margins.Top - margins.Bottom);
				PageSettings = pageSettings;
			}

	// Event properties.
	public bool Cancel { get; set; }

	public Graphics Graphics
			{
				get
				{
					return graphics;
				}
			}
	public bool HasMorePages { get; set; }

	public Rectangle MarginBounds { get; }

	public Rectangle PageBounds
			{
				get
				{
					return pageBounds;
				}
			}
	public PageSettings PageSettings { get; }
}; // class PrintPageEventArgs

}; // namespace System.Drawing.Printing
