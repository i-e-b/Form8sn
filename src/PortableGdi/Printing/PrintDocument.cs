/*
 * PrintDocument.cs - Implementation of the
 *			"System.Drawing.Printing.PrintDocument" class.
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

using Portable.Drawing.Toolkit;

namespace Portable.Drawing.Printing
{
	public delegate void QueryPageSettingsEventHandler (object sender, QueryPageSettingsEventArgs e);
public delegate void PrintEventHandler(object sender, PrintEventArgs e);
public delegate void PrintPageEventHandler(object sender, PrintPageEventArgs e);



#if CONFIG_COMPONENT_MODEL
[DefaultEvent("PrintPage")]
[DefaultProperty("DocumentName")]
[ToolboxItemFilter("System.Drawing.Printing")]
#endif
public class PrintDocument
#if CONFIG_COMPONENT_MODEL
	: Component
#endif
{
	// Internal state.
	internal IToolkitPrintSession session;

	// Constructor.
	public PrintDocument()
			{
				DocumentName = "document";
				OriginAtMargins = false;
				PrintController = null;
				PrinterSettings = new PrinterSettings();
				DefaultPageSettings = new PageSettings(PrinterSettings);
			}

	// Get or set the document's properties.
#if CONFIG_COMPONENT_MODEL
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
#endif
	public PageSettings DefaultPageSettings { get; set; }
#if CONFIG_COMPONENT_MODEL
	[DefaultValue("document")]
#endif
	public string DocumentName { get; set; }
#if CONFIG_COMPONENT_MODEL
	[DefaultValue(false)]
#endif
	public bool OriginAtMargins { get; set; }
#if CONFIG_COMPONENT_MODEL
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
#endif
	public PrintController PrintController { get; set; }
#if CONFIG_COMPONENT_MODEL
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
#endif
	public PrinterSettings PrinterSettings { get; set; }

	// Print the document.
	public void Print()
			{
				PrintController controller = PrintController;
				PrintEventArgs printArgs;
				QueryPageSettingsEventArgs queryArgs;
				PrintPageEventArgs pageArgs;
				Graphics graphics;

				// Begin the printing process.
				printArgs = new PrintEventArgs();
				OnBeginPrint(printArgs);
			#if CONFIG_COMPONENT_MODEL
				if(printArgs.Cancel)
				{
					return;
				}
			#endif
				controller.OnStartPrint(this, printArgs);

				// Wrap the rest in a "try" block so that the controller
				// will be properly shut down if an exception occurs.
				try
				{
					queryArgs = new QueryPageSettingsEventArgs
						((PageSettings)(DefaultPageSettings.Clone()));
					do
					{
						// Query the page settings for the next page.
						OnQueryPageSettings(queryArgs);
					#if CONFIG_COMPONENT_MODEL
						if(queryArgs.Cancel)
						{
							break;
						}
					#endif

						// Create the page argument structure.
						pageArgs = new PrintPageEventArgs
							(queryArgs.PageSettings);

						// Get the graphics object to use to draw the page.
						graphics = controller.OnStartPage(this, pageArgs);
						pageArgs.graphics = graphics;

						// Print the page.
						try
						{
							OnPrintPage(pageArgs);
							controller.OnEndPage(this, pageArgs);
						}
						finally
						{
							graphics.Dispose();
						}
					}
					while(!(pageArgs.Cancel) && pageArgs.HasMorePages);
				}
				finally
				{
					try
					{
						OnEndPrint(printArgs);
					}
					finally
					{
						controller.OnEndPrint(this, printArgs);
					}
				}
			}

	// Convert this object into a string.
	public override string ToString()
			{
				return "[PrintDocument " + DocumentName + "]";
			}

	// Event that is emitted at the beginning of the print process.
	public event PrintEventHandler BeginPrint;

	// Event that is emitted at the end of the print process.
	public event PrintEventHandler EndPrint;

	// Event that is emitted to print the current page.
	public event PrintPageEventHandler PrintPage;

	// Event that is emitted to query page settings prior to printing a page.
	public event QueryPageSettingsEventHandler QueryPageSettings;

	// Raise the "BeginPrint" event.
	protected virtual void OnBeginPrint(PrintEventArgs e)
			{
				if(BeginPrint != null)
				{
					BeginPrint(this, e);
				}
			}

	// Raise the "EndPrint" event.
	protected virtual void OnEndPrint(PrintEventArgs e)
			{
				if(EndPrint != null)
				{
					EndPrint(this, e);
				}
			}

	// Raise the "PrintPage" event.
	protected virtual void OnPrintPage(PrintPageEventArgs e)
			{
				if(PrintPage != null)
				{
					PrintPage(this, e);
				}
			}

	// Raise the "QueryPageSettings" event.
	protected virtual void OnQueryPageSettings(QueryPageSettingsEventArgs e)
			{
				if(QueryPageSettings != null)
				{
					QueryPageSettings(this, e);
				}
			}

}; // class PrintDocument

}; // namespace System.Drawing.Printing
