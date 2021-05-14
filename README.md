# Formation
Generate PDF based templates, and fill them in from data objects

Here's a big pile of links for when I get around to this:
https://www.codeproject.com/Articles/10157/Gios-PDF-NET-library
https://github.com/libharu/libharu
http://podofo.sourceforge.net/
https://github.com/galkahana/PDF-Writer
http://www.jagpdf.org/downloads.htm
https://github.com/sumatrapdfreader/sumatrapdf
https://github.com/diegomura/react-pdf   &    https://github.com/wojtekmaj/react-pdf
https://github.com/prawnpdf/prawn


The general plan:

1. Someone with a form does a Print to PDF with all the spaces blank
2. They get a data structure (schema) from another app or API
3. Both the PDF and the data are loaded into the **Formation UI**, and they:
   - Place boxes on the PDF page surfaces
   - Add rules to partition the data and repeat PDF pages (plus running totals and the like)
4. This is exported to a command file embedded in a modified version of the PDF
5. A programmer can then pass the command file and data to the **Formation SDK** to get back a finished PDF

The PDF generation phase should be as fast as possible. If we can, it should be a streaming process so we
don't need to load the whole PDF template or output into memory.
