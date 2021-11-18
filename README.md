# Form8sn
Generate PDF based templates, and fill them in from data objects

The general plan:
-----------------

1. Someone with a form does a Print to PDF with all the spaces blank
2. They get a data structure (schema) from another app or API
3. Both the PDF and the data are loaded into the **Form8sn UI**, and they:
   - Place boxes on the PDF page surfaces
   - Add rules to partition the data and repeat PDF pages (plus running totals and the like)
4. This is exported to a command file embedded in a modified version of the PDF
5. A programmer can then pass the command file and data to the **Form8sn SDK** to get back a finished PDF

The PDF generation phase should be as fast as possible. If we can, it should be a streaming process so we
don't need to load the whole PDF template or output into memory.

What is working now:
--------------------

Inputs are JPEG images.
Schema is implicit with a sample data file.
Direct output of PDF is working, but the special file format is not present.

Components
==========

* **PDFSharp** - A modified version of the main project (so I can add or specialise bits)
* **PortableGdi** - A minimal version of `System.Drawing` to support PDFSharp. Based on GNU Portable.Net base libraries.
* **BasicImageFormFiller** - GUI app to take scanned paper forms (in JPEG) and
  fill them in from arbitrary data, based on examples.
* **TestApp** - Console app scratch space
* **PdfSharp.Charting** - Currently unused.
* **Form8snCore** - File formats and filtering to produce PDFs from project files
* **WebFormFiller** - .Net MVC website and JS app for editing Form8sn projects

To-do:
------

* [ ] Write some documentation
* [x] Ensure the core PDFsharp and Form8snCore don't use GDI+ **(important)**
  * [x] Portable system.drawing
  * [x] JPEG reading and writing ( `Portable.Drawing.Imaging.ImageFormats.JpegReader.Load` )
  * [x] Font reading & metrics ( `PdfSharp.Drawing.XFontSource.GetOrCreateFromFile` )
  * [x] Remove defunct platform specific calls in PDFsharp.
* [x] Handle PDFs as input
* [x] Decode PDF forms so we can use existing 'boxes'
* [ ] Embed Form8sn instructions in PDF files and handle those? *(not important)*
* [x] Web-based form editor?
* [ ] Full suite of tests
