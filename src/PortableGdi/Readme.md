# Portable GDI

This project is an attempt to produce a library that fulfills the `System.Drawing`
namespace in DotNetStandard without any external dependencies (including having no
system library dependencies)

## Status

Various parts of the system work to some degree, but there is still a lot missing

### Imaging
Most image formats can be loaded. Most formats can be written (not JPEG yet).
Some color spaces are supported, but many are not.
Many less common image format modes are not supported

### Drawing
Drawing is supported against bitmap targets. 24bit RGB is the only fully supported
bit depth at present. Many drawing primitives are yet to be supported.
Smoothing and composite modes are ignored, output is always anti-aliased.

### Fonts
Basic loading and rendering of fonts is available. Many measurements are not yet
correct. Fall-back rendering of glyphs is not supported. Fonts are read from 
system defined directories.

## Goals and non-goals

The primary goal is to support software that requires `System.Drawing` functionality
and may be deployed across multiple platforms (including servers and containers)

This is part of the wider 'Form8sn' project, so should eventually have enough to
support the reading, display and writing of PDFs.

The project does **not** attempt to produce pixel-perfect output exactly as GDI
would, nor are we attempting to perform as well as a hand-tuned native library.
This project attempts to be 'good enough'