#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Portable.Drawing.Imaging.ImageFormats.Jpeg
{
    /// <summary>
    /// The encoder to encode image into baseline JPEG stream.
    /// </summary>
    public class JpegEncoder
    {
        private int _minimumBufferSegmentSize;

        private JpegBlockInputReader? _input;
        private IBufferWriter<byte>? _output;

        private List<JpegQuantizationTable>? _quantizationTables;
        private JpegHuffmanEncodingTableCollection _huffmanTables;
        private List<JpegHuffmanEncodingComponent>? _encodeComponents;

        /// <summary>
        /// Initialize the encoder.
        /// </summary>
        public JpegEncoder() : this(4096) { }

        /// <summary>
        /// Initialize the encoder.
        /// </summary>
        /// <param name="minimumBufferSegmentSize">The minimum size of buffer to rent from the output writer.</param>
        public JpegEncoder(int minimumBufferSegmentSize)
        {
            _minimumBufferSegmentSize = minimumBufferSegmentSize;
        }

        /// <summary>
        /// True to generate the most optimal Huffman codes. This may takes more time than the standard method but yields better codes.
        /// </summary>
        public bool MostOptimalCoding { get; set; }

        /// <summary>
        /// Get the minimum size of buffer to rent from the output writer.
        /// </summary>
        protected int MinimumBufferSegmentSize => _minimumBufferSegmentSize;

        /// <summary>
        /// Clone the current parameters of the encoder.
        /// </summary>
        /// <typeparam name="T">The cloned encoder type.</typeparam>
        /// <returns>The cloned encoder.</returns>
        protected T CloneParameters<T>() where T : JpegEncoder, new()
        {
            var optimizeCoding = _huffmanTables.ContainsTableBuilder();
            var cloned = new T()
            {
                _minimumBufferSegmentSize = _minimumBufferSegmentSize,
                _quantizationTables = _quantizationTables,
                _huffmanTables = optimizeCoding ? _huffmanTables.DeepClone() : _huffmanTables
            };
            var components = _encodeComponents;
            if (!(components is null))
            {
                foreach (var item in components)
                {
                    cloned.AddComponent((byte)item.ComponentIndex, item.QuantizationTable.Identifier, item.DcTableIdentifier, item.AcTableIdentifier, item.HorizontalSamplingFactor, item.VerticalSamplingFactor);
                }
            }
            return cloned;
        }

        /// <summary>
        /// Get or set the memory pool to use when allocating large chunks of temporary buffer.
        /// </summary>
        public MemoryPool<byte>? MemoryPool { get; set; }

        /// <summary>
        /// Set the input buffer writer.
        /// </summary>
        /// <param name="inputReader">The output buffer reader.</param>
        public void SetInputReader(JpegBlockInputReader inputReader)
        {
            _input = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        /// <summary>
        /// Set the output writer that JPEG stream will be written to.
        /// </summary>
        /// <param name="output">The output writer.</param>
        public void SetOutput(IBufferWriter<byte> output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        /// <summary>
        /// Set the quantization table.
        /// </summary>
        /// <param name="table">The quantization table.</param>
        public void SetQuantizationTable(JpegQuantizationTable table)
        {
            if (table.IsEmpty)
            {
                throw new ArgumentException("Quantization table is not initialized.", nameof(table));
            }
            if (table.ElementPrecision != 0)
            {
                throw new InvalidOperationException("Only baseline JPEG is supported.");
            }

            var tables = _quantizationTables;
            if (tables is null)
            {
                _quantizationTables = tables = new List<JpegQuantizationTable>(2);
            }

            for (var i = 0; i < tables.Count; i++)
            {
                if (tables[i].Identifier == table.Identifier)
                {
                    tables[i] = table;
                    return;
                }
            }

            tables.Add(table);
        }

        /// <summary>
        /// Set the Huffman table.
        /// </summary>
        /// <param name="isDcTable">Whether the table is DC table.</param>
        /// <param name="identifier">The identifier of the Huffman table.</param>
        /// <param name="table">The Huffman table.</param>
        public void SetHuffmanTable(bool isDcTable, byte identifier, JpegHuffmanEncodingTable? table)
        {
            _huffmanTables.AddTable(isDcTable ? (byte)0 : (byte)1, identifier, table);
        }

        /// <summary>
        /// Set the Huffman table that should be automatically generated.
        /// </summary>
        /// <param name="isDcTable">Whether the table is DC table.</param>
        /// <param name="identifier">The identifier of the Huffman table.</param>
        public void SetHuffmanTable(bool isDcTable, byte identifier)
            => SetHuffmanTable(isDcTable, identifier, null);

        private JpegQuantizationTable GetQuantizationTable(byte identifier)
        {
            if (_quantizationTables is null)
            {
                return default;
            }
            foreach (var item in _quantizationTables)
            {
                if (item.Identifier == identifier)
                {
                    return item;
                }
            }
            return default;
        }

        /// <summary>
        /// Add a component to encode.
        /// </summary>
        /// <param name="componentIndex">The index of the component.</param>
        /// <param name="quantizationTableIdentifier">The identifier of the quantization table.</param>
        /// <param name="huffmanDcTableIdentifier">The identifier of the DC Huffman table.</param>
        /// <param name="huffmanAcTableIdentifier">The identifier of the AC Huffman table.</param>
        /// <param name="horizontalSubsampling">The horizontal subsampling factor.</param>
        /// <param name="verticalSubsampling">The horizontal subsampling factor.</param>
        public void AddComponent(byte componentIndex, byte quantizationTableIdentifier, byte huffmanDcTableIdentifier, byte huffmanAcTableIdentifier, byte horizontalSubsampling, byte verticalSubsampling)
        {
            if (horizontalSubsampling != 1 && horizontalSubsampling != 2 && horizontalSubsampling != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(horizontalSubsampling), "Subsampling factor can only be 1, 2 or 4.");
            }
            if (verticalSubsampling != 1 && verticalSubsampling != 2 && verticalSubsampling != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(verticalSubsampling), "Subsampling factor can only be 1, 2 or 4.");
            }

            var components = _encodeComponents;
            if (components is null)
            {
                _encodeComponents = components = new List<JpegHuffmanEncodingComponent>(4);
            }
            foreach (var item in components)
            {
                if (item.ComponentIndex == componentIndex)
                {
                    throw new ArgumentException("The component index is already used by another component.", nameof(componentIndex));
                }
            }

            var quantizationTable = GetQuantizationTable(quantizationTableIdentifier);
            if (quantizationTable.IsEmpty)
            {
                throw new ArgumentException("Quantization table is not defined.", nameof(quantizationTableIdentifier));
            }
            var dcTable = _huffmanTables.GetTable(true, huffmanDcTableIdentifier);
            JpegHuffmanEncodingTableBuilder? dcTableBuilder = null;
            if (dcTable is null)
            {
                dcTableBuilder = _huffmanTables.GetTableBuilder(true, huffmanDcTableIdentifier);
                if (dcTableBuilder is null)
                {
                    throw new ArgumentException("Huffman table is not defined.", nameof(huffmanDcTableIdentifier));
                }
            }
            var acTable = _huffmanTables.GetTable(false, huffmanAcTableIdentifier);
            JpegHuffmanEncodingTableBuilder? acTableBuilder = null;
            if (acTable is null)
            {
                acTableBuilder = _huffmanTables.GetTableBuilder(false, huffmanAcTableIdentifier);
                if (acTableBuilder is null)
                {
                    throw new ArgumentException("Huffman table is not defined.", nameof(huffmanAcTableIdentifier));
                }
            }

            var component = new JpegHuffmanEncodingComponent
            {
                Index = components.Count,
                ComponentIndex = componentIndex,
                HorizontalSamplingFactor = horizontalSubsampling,
                VerticalSamplingFactor = verticalSubsampling,
                DcTableIdentifier = huffmanDcTableIdentifier,
                AcTableIdentifier = huffmanAcTableIdentifier,
                DcTable = dcTable,
                AcTable = acTable,
                DcTableBuilder = dcTableBuilder,
                AcTableBuilder = acTableBuilder,
                QuantizationTable = quantizationTable
            };
            components.Add(component);
        }

        /// <summary>
        /// Create a JPEG writer.
        /// </summary>
        /// <returns>The JPEG writer.</returns>
        protected JpegWriter CreateJpegWriter()
        {
            var output = _output ?? throw new InvalidOperationException("Output is not specified.");
            return new JpegWriter(output, _minimumBufferSegmentSize);
        }

        /// <summary>
        /// Encode the image.
        /// </summary>
        public virtual void Encode()
        {
            var optimizeCoding = _huffmanTables.ContainsTableBuilder();

            var writer = CreateJpegWriter();

            WriteStartOfImage(ref writer);
            WriteQuantizationTables(ref writer);
            var frameHeader = WriteStartOfFrame(ref writer);
            var allocator = optimizeCoding ? new JpegBlockAllocator(MemoryPool) : null;
            try
            {
                if (allocator is not null)
                {
                    allocator.Allocate(frameHeader);
                    TransformBlocks(allocator);
                    BuildHuffmanTables(frameHeader, allocator, optimal: MostOptimalCoding);
                    WriteHuffmanTables(ref writer);
                    WriteStartOfScan(ref writer);
                    WritePreparedScanData(frameHeader, allocator, ref writer);
                }
                else
                {
                    WriteHuffmanTables(ref writer);
                    WriteStartOfScan(ref writer);
                    WriteScanData(ref writer);
                }
            }
            finally
            {
                allocator?.Dispose();
            }
            WriteEndOfImage(ref writer);

            writer.Flush();
        }

        /// <summary>
        /// Write the StartOfImage marker.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected static void WriteStartOfImage(ref JpegWriter writer)
        {
            writer.WriteMarker(JpegMarker.StartOfImage);
        }

        /// <summary>
        /// Write quantization tables.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected void WriteQuantizationTables(ref JpegWriter writer)
        {
            var quantizationTables = _quantizationTables;
            if (quantizationTables is null)
            {
                throw new InvalidOperationException();
            }


            writer.WriteMarker(JpegMarker.DefineQuantizationTable);

            ushort totalByteCount = 0;
            foreach (var table in quantizationTables)
            {
                totalByteCount += table.BytesRequired;
            }

            writer.WriteLength(totalByteCount);

            foreach (var table in quantizationTables)
            {
                var buffer = writer.GetSpan(table.BytesRequired);
                table.TryWrite(buffer, out var bytesWritten);
                writer.Advance(bytesWritten);
            }
        }

        /// <summary>
        /// Write Huffman tables.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected void WriteHuffmanTables(ref JpegWriter writer)
        {
            if (_huffmanTables.IsEmpty)
            {
                throw new InvalidOperationException();
            }

            writer.WriteMarker(JpegMarker.DefineHuffmanTable);
            var totalByteCoubt = _huffmanTables.GetTotalBytesRequired();
            writer.WriteLength(totalByteCoubt);
            _huffmanTables.Write(ref writer);
        }

        /// <summary>
        /// Write the StartOfFrame header.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected JpegFrameHeader WriteStartOfFrame(ref JpegWriter writer)
        {
            var input = _input;
            if (input is null)
            {
                throw new InvalidOperationException("Input is not specified.");
            }
            var encodeComponents = _encodeComponents;
            if (encodeComponents is null || encodeComponents.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }
            var components = new JpegFrameComponentSpecificationParameters[encodeComponents.Count];
            for (var i = 0; i < encodeComponents.Count; i++)
            {
                var thisComponent = encodeComponents[i];
                components[i] = new JpegFrameComponentSpecificationParameters((byte)thisComponent.ComponentIndex, thisComponent.HorizontalSamplingFactor, thisComponent.VerticalSamplingFactor, thisComponent.QuantizationTable.Identifier);
            }
            var frameHeader = new JpegFrameHeader(8, (ushort)input.Height, (ushort)input.Width, (byte)components.Length, components);

            writer.WriteMarker(JpegMarker.StartOfFrame0);
            var bytesCount = frameHeader.BytesRequired;
            writer.WriteLength(bytesCount);
            var buffer = writer.GetSpan(bytesCount);
            frameHeader.TryWrite(buffer, out _);
            writer.Advance(bytesCount);

            return frameHeader;
        }

        /// <summary>
        /// Write the StartOfScan header.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected void WriteStartOfScan(ref JpegWriter writer)
        {
            var encodeComponents = _encodeComponents;
            if (encodeComponents is null || encodeComponents.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }
            var components = new JpegScanComponentSpecificationParameters[encodeComponents.Count];
            for (var i = 0; i < encodeComponents.Count; i++)
            {
                var thisComponent = encodeComponents[i];
                components[i] = new JpegScanComponentSpecificationParameters((byte)thisComponent.ComponentIndex, thisComponent.DcTableIdentifier, thisComponent.AcTableIdentifier);
            }
            var scanHeader = new JpegScanHeader((byte)components.Length, components, 0, 63, 0, 0);

            writer.WriteMarker(JpegMarker.StartOfScan);
            var bytesCount = scanHeader.BytesRequired;
            writer.WriteLength(bytesCount);
            var buffer = writer.GetSpan(bytesCount);
            scanHeader.TryWrite(buffer, out _);
            writer.Advance(bytesCount);
        }

        /// <summary>
        /// Encode each block and save the coefficients.
        /// </summary>
        /// <param name="allocator">The coefficient allocator.</param>
        protected void TransformBlocks(JpegBlockAllocator allocator)
        {
            var inputReader = _input ?? throw new InvalidOperationException("Input is not specified.");
            var components = _encodeComponents;
            if (components is null || components.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }

            // Compute maximum sampling factor and reset DC predictor
            var maxHorizontalSampling = 1;
            var maxVerticalSampling = 1;
            foreach (var currentComponent in components)
            {
                currentComponent.DcPredictor = 0;
                maxHorizontalSampling = Math.Max(maxHorizontalSampling, currentComponent.HorizontalSamplingFactor);
                maxVerticalSampling = Math.Max(maxVerticalSampling, currentComponent.VerticalSamplingFactor);
            }
            foreach (var currentComponent in components)
            {
                currentComponent.HorizontalSubsamplingFactor = maxHorizontalSampling / currentComponent.HorizontalSamplingFactor;
                currentComponent.VerticalSubsamplingFactor = maxVerticalSampling / currentComponent.VerticalSamplingFactor;
            }

            var mcusPerLine = (inputReader.Width + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
            var mcusPerColumn = (inputReader.Height + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);
            const int levelShift = 1 << (8 - 1);

            var inputFBuffer = new JpegBlock8x8F();
            var outputFBuffer = new JpegBlock8x8F();
            var tempFBuffer = new JpegBlock8x8F();

            for (var rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
            {
                for (var colMcu = 0; colMcu < mcusPerLine; colMcu++)
                {
                    foreach (var component in components)
                    {
                        //int index = component.ComponentIndex;
                        var index = component.Index;
                        int h = component.HorizontalSamplingFactor;
                        int v = component.VerticalSamplingFactor;
                        var hs = component.HorizontalSubsamplingFactor;
                        var vs = component.VerticalSubsamplingFactor;
                        var offsetX = colMcu * h;
                        var offsetY = rowMcu * v;

                        for (var y = 0; y < v; y++)
                        {
                            var blockOffsetY = offsetY + y;
                            for (var x = 0; x < h; x++)
                            {
                                ref var blockRef = ref allocator.GetBlockReference(index, offsetX + x, blockOffsetY);

                                // Read Block
                                ReadBlock(inputReader, out blockRef, component.Index, (offsetX + x) * 8 * hs, blockOffsetY * 8 * vs, hs, vs);

                                // Level shift
                                ShiftDataLevel(ref blockRef, ref inputFBuffer, levelShift);

                                // FDCT
                                FastFloatingPointDCT.TransformFDCT(ref inputFBuffer, ref outputFBuffer, ref tempFBuffer);

                                // ZigZagAndQuantize
                                ZigZagAndQuantizeBlock(component.QuantizationTable, ref outputFBuffer, ref blockRef);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Build Huffman table from the coefficients.
        /// </summary>
        /// <param name="frameHeader">The JPEG frame header.</param>
        /// <param name="allocator">The coefficient allocator.</param>
        /// <param name="optimal">Whether to use the optimal algorithm.</param>
        protected void BuildHuffmanTables(JpegFrameHeader frameHeader, JpegBlockAllocator allocator, bool optimal = false)
        {
            var components = _encodeComponents;
            if (components is null || components.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }

            // Compute maximum sampling factor and reset DC predictor
            var maxHorizontalSampling = 1;
            var maxVerticalSampling = 1;
            foreach (var currentComponent in components)
            {
                currentComponent.DcPredictor = 0;
                maxHorizontalSampling = Math.Max(maxHorizontalSampling, currentComponent.HorizontalSamplingFactor);
                maxVerticalSampling = Math.Max(maxVerticalSampling, currentComponent.VerticalSamplingFactor);
            }

            var mcusPerLine = (frameHeader.SamplesPerLine + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
            var mcusPerColumn = (frameHeader.NumberOfLines + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);

            for (var rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
            {
                for (var colMcu = 0; colMcu < mcusPerLine; colMcu++)
                {
                    foreach (var component in components)
                    {
                        var index = component.Index;
                        int h = component.HorizontalSamplingFactor;
                        int v = component.VerticalSamplingFactor;
                        var offsetX = colMcu * h;
                        var offsetY = rowMcu * v;

                        for (var y = 0; y < v; y++)
                        {
                            var blockOffsetY = offsetY + y;
                            for (var x = 0; x < h; x++)
                            {
                                ref var blockRef = ref allocator.GetBlockReference(index, offsetX + x, blockOffsetY);

                                GatherBlockStatistics(component, ref blockRef);
                            }
                        }
                    }
                }
            }

            // Build huffman table
            _huffmanTables.BuildTables(optimal);

            // Reset huffman table
            foreach (var component in components)
            {
                component.DcTable = _huffmanTables.GetTable(true, component.DcTableIdentifier);
                component.AcTable = _huffmanTables.GetTable(false, component.AcTableIdentifier);
                component.DcTableBuilder = null;
                component.DcTableBuilder = null;
            }
        }

        private static void GatherBlockStatistics(JpegHuffmanEncodingComponent component, ref JpegBlock8x8 block)
        {
            ref var blockRef = ref Unsafe.As<JpegBlock8x8, short>(ref block);

            // DC
            int blockValue = blockRef;
            var t = blockValue - component.DcPredictor;
            component.DcPredictor = blockValue;
            if (!(component.DcTableBuilder is null))
            {
                GatherRunLengthCodeStatistics(component.DcTableBuilder, 0, t);
            }

            // AC
            var acTableBuilder = component.AcTableBuilder;
            if (acTableBuilder is null)
            {
                return;
            }
            var runLength = 0;
            for (var i = 1; i < 64; i++)
            {
                t = Unsafe.Add(ref blockRef, i);

                if (t == 0)
                {
                    runLength++;
                }
                else
                {
                    while (runLength > 15)
                    {
                        acTableBuilder.IncrementCodeCount(0xf0);
                        runLength -= 16;
                    }

                    GatherRunLengthCodeStatistics(acTableBuilder, runLength, t);
                    runLength = 0;
                }
            }

            if (runLength > 0)
            {
                // EOB
                acTableBuilder.IncrementCodeCount(0);
            }
        }

        /// <summary>
        /// Write the prepared scan data.
        /// </summary>
        /// <param name="frameHeader">The JPEG frame header.</param>
        /// <param name="allocator">The coefficient allocator.</param>
        /// <param name="writer">The JPEG writer.</param>
        protected void WritePreparedScanData(JpegFrameHeader frameHeader, JpegBlockAllocator allocator, ref JpegWriter writer)
        {
            var components = _encodeComponents;
            if (components is null || components.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }

            // Compute maximum sampling factor and reset DC predictor
            var maxHorizontalSampling = 1;
            var maxVerticalSampling = 1;
            foreach (var currentComponent in components)
            {
                currentComponent.DcPredictor = 0;
                maxHorizontalSampling = Math.Max(maxHorizontalSampling, currentComponent.HorizontalSamplingFactor);
                maxVerticalSampling = Math.Max(maxVerticalSampling, currentComponent.VerticalSamplingFactor);
            }

            var mcusPerLine = (frameHeader.SamplesPerLine + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
            var mcusPerColumn = (frameHeader.NumberOfLines + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);

            writer.EnterBitMode();

            for (var rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
            {
                for (var colMcu = 0; colMcu < mcusPerLine; colMcu++)
                {
                    foreach (var component in components)
                    {
                        var index = component.Index;
                        int h = component.HorizontalSamplingFactor;
                        int v = component.VerticalSamplingFactor;
                        var offsetX = colMcu * h;
                        var offsetY = rowMcu * v;

                        for (var y = 0; y < v; y++)
                        {
                            var blockOffsetY = offsetY + y;
                            for (var x = 0; x < h; x++)
                            {
                                ref var blockRef = ref allocator.GetBlockReference(index, offsetX + x, blockOffsetY);

                                EncodeBlock(ref writer, component, ref blockRef);
                            }
                        }
                    }
                }
            }

            // Padding
            writer.ExitBitMode();
        }

        /// <summary>
        /// Encode the image and write scan data.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected void WriteScanData(ref JpegWriter writer)
        {
            var inputReader = _input ?? throw new InvalidOperationException("Input is not specified.");
            var components = _encodeComponents;
            if (components is null || components.Count == 0)
            {
                throw new InvalidOperationException("No component is specified.");
            }

            // Compute maximum sampling factor and reset DC predictor
            var maxHorizontalSampling = 1;
            var maxVerticalSampling = 1;
            foreach (var currentComponent in components)
            {
                currentComponent.DcPredictor = 0;
                maxHorizontalSampling = Math.Max(maxHorizontalSampling, currentComponent.HorizontalSamplingFactor);
                maxVerticalSampling = Math.Max(maxVerticalSampling, currentComponent.VerticalSamplingFactor);
            }
            foreach (var currentComponent in components)
            {
                currentComponent.HorizontalSubsamplingFactor = maxHorizontalSampling / currentComponent.HorizontalSamplingFactor;
                currentComponent.VerticalSubsamplingFactor = maxVerticalSampling / currentComponent.VerticalSamplingFactor;
            }

            // Prepare
            var mcusPerLine = (inputReader.Width + 8 * maxHorizontalSampling - 1) / (8 * maxHorizontalSampling);
            var mcusPerColumn = (inputReader.Height + 8 * maxVerticalSampling - 1) / (8 * maxVerticalSampling);

            writer.EnterBitMode();

            const int levelShift = 1 << (8 - 1);

            var inputFBuffer = new JpegBlock8x8F();
            var outputFBuffer = new JpegBlock8x8F();
            var tempFBuffer = new JpegBlock8x8F();


            for (var rowMcu = 0; rowMcu < mcusPerColumn; rowMcu++)
            {
                var offsetY = rowMcu * maxVerticalSampling;
                for (var colMcu = 0; colMcu < mcusPerLine; colMcu++)
                {
                    var offsetX = colMcu * maxHorizontalSampling;

                    // Scan an interleaved mcu... process components in order
                    foreach (var component in components)
                    {
                        int h = component.HorizontalSamplingFactor;
                        int v = component.VerticalSamplingFactor;
                        var hs = component.HorizontalSubsamplingFactor;
                        var vs = component.VerticalSubsamplingFactor;

                        for (var y = 0; y < v; y++)
                        {
                            var blockOffsetY = (offsetY + y) * 8;
                            for (var x = 0; x < h; x++)
                            {
                                // Read Block
                                ReadBlock(inputReader, out var inputBuffer, component.Index, (offsetX + x) * 8, blockOffsetY, hs, vs);

                                // Level shift
                                ShiftDataLevel(ref inputBuffer, ref inputFBuffer, levelShift);

                                // FDCT
                                FastFloatingPointDCT.TransformFDCT(ref inputFBuffer, ref outputFBuffer, ref tempFBuffer);

                                // ZigZagAndQuantize
                                ZigZagAndQuantizeBlock(component.QuantizationTable, ref outputFBuffer, ref inputBuffer);

                                // Write to bit stream
                                EncodeBlock(ref writer, component, ref inputBuffer);
                            }
                        }
                    }
                }
            }

            // Padding
            writer.ExitBitMode();
        }

        private static void ReadBlock(JpegBlockInputReader inputReader, out JpegBlock8x8 block, int componentIndex, int x, int y, int h, int v)
        {
            ref var blockRef = ref Unsafe.As<JpegBlock8x8, short>(ref block);

            if (h == 1 && v == 1)
            {
                inputReader.ReadBlock(ref blockRef, componentIndex, x, y);
                return;
            }

            ReadBlockWithSubsample(inputReader, ref blockRef, componentIndex, x, y, h, v);
        }

        private static void ReadBlockWithSubsample(JpegBlockInputReader inputReader, ref short blockRef, int componentIndex, int x, int y, int horizontalSubsampling, int verticalSubsampling)
        {
            var temp = new JpegBlock8x8();

            ref var tempRef = ref Unsafe.As<JpegBlock8x8, short>(ref temp);

            var hShift = JpegMathHelper.Log2((uint)horizontalSubsampling);
            var vShift = JpegMathHelper.Log2((uint)verticalSubsampling);
            var hBlockShift = 3 - hShift;
            var vBlockShift = 3 - vShift;

            for (var v = 0; v < verticalSubsampling; v++)
            {
                for (var h = 0; h < horizontalSubsampling; h++)
                {
                    inputReader.ReadBlock(ref tempRef, componentIndex, x + 8 * h, y + 8 * v);

                    CopySubsampleBlock(ref tempRef, ref blockRef, h << hBlockShift, v << vBlockShift, hShift, vShift);
                }
            }

            var totalShift = hShift + vShift;
            if (totalShift > 0)
            {
                var delta = 1 << (totalShift - 1);
                for (var i = 0; i < 64; i++)
                {
                    Unsafe.Add(ref blockRef, i) = (short)((Unsafe.Add(ref blockRef, i) + delta) >> totalShift);
                }
            }
        }

        private static void CopySubsampleBlock(ref short sourceRef, ref short destinationRef, int blockOffsetX, int blockOffsetY, int hShift, int vShift)
        {
            for (var y = 0; y < 8; y++)
            {
                ref var sourceRowRef = ref Unsafe.Add(ref sourceRef, y * 8);
                ref var destinationRowRef = ref Unsafe.Add(ref destinationRef, (blockOffsetY + (y >> vShift)) * 8 + blockOffsetX);
                for (var x = 0; x < 8; x++)
                {
                    Unsafe.Add(ref destinationRowRef, x >> hShift) += Unsafe.Add(ref sourceRowRef, x);
                }
            }
        }

        private static void ShiftDataLevel(ref JpegBlock8x8 source, ref JpegBlock8x8F destination, int levelShift)
        {
            ref var sourceRef = ref Unsafe.As<JpegBlock8x8, short>(ref source);
            ref var destinationRef = ref Unsafe.As<JpegBlock8x8F, float>(ref destination);

            for (var i = 0; i < 64; i++)
            {
                Unsafe.Add(ref destinationRef, i) = Unsafe.Add(ref sourceRef, i) - levelShift;
            }
        }

        private static void ZigZagAndQuantizeBlock(JpegQuantizationTable quantizationTable, ref JpegBlock8x8F input, ref JpegBlock8x8 output)
        {
            Debug.Assert(!quantizationTable.IsEmpty);

            ref var elementRef = ref MemoryMarshal.GetReference(quantizationTable.Elements);
            ref var sourceRef = ref Unsafe.As<JpegBlock8x8F, float>(ref input);
            ref var destinationRef = ref Unsafe.As<JpegBlock8x8, short>(ref output);

            for (var i = 0; i < 64; i++)
            {
                var coefficient = Unsafe.Add(ref sourceRef, JpegZigZag.InternalBufferIndexToBlock(i));
                var element = Unsafe.Add(ref elementRef, i);
                Unsafe.Add(ref destinationRef, i) = JpegMathHelper.RoundToInt16(coefficient / element);
            }
        }

        private static void EncodeBlock(ref JpegWriter writer, JpegHuffmanEncodingComponent component, ref JpegBlock8x8 block)
        {
            ref var blockRef = ref Unsafe.As<JpegBlock8x8, short>(ref block);

            Debug.Assert(!(component.DcTable is null));
            Debug.Assert(!(component.AcTable is null));

            // DC
            int blockValue = blockRef;
            var t = blockValue - component.DcPredictor;
            component.DcPredictor = blockValue;
            EncodeRunLength(ref writer, component.DcTable!, 0, t);

            // AC
            var acTable = component.AcTable!;
            var runLength = 0;
            for (var i = 1; i < 64; i++)
            {
                t = Unsafe.Add(ref blockRef, i);

                if (t == 0)
                {
                    runLength++;
                }
                else
                {
                    while (runLength > 15)
                    {
                        EncodeHuffmanSymbol(ref writer, acTable, 0xf0);
                        runLength -= 16;
                    }

                    EncodeRunLength(ref writer, acTable, runLength, t);
                    runLength = 0;
                }
            }

            if (runLength > 0)
            {
                // EOB
                EncodeHuffmanSymbol(ref writer, acTable, 0);
            }
        }

        private static void GatherRunLengthCodeStatistics(JpegHuffmanEncodingTableBuilder tableBuilder, int zeroRunLength, int value)
        {
            var a = value;
            if (a < 0)
            {
                a = -value;
            }

            int bitCount;
            if (a < 0x100)
            {
                bitCount = BitCountTable[a];
            }
            else
            {
                bitCount = 8 + BitCountTable[a >> 8];
            }

            tableBuilder.IncrementCodeCount(zeroRunLength << 4 | bitCount);
        }

        private static void EncodeRunLength(ref JpegWriter writer, JpegHuffmanEncodingTable encodingTable, int zeroRunLength, int value)
        {
            var a = value;
            var b = value;
            if (a < 0)
            {
                a = -value;
                b = value - 1;
            }

            int bitCount;
            if (a < 0x100)
            {
                bitCount = BitCountTable[a];
            }
            else
            {
                bitCount = 8 + BitCountTable[a >> 8];
            }

            EncodeHuffmanSymbol(ref writer, encodingTable, zeroRunLength << 4 | bitCount);
            if (bitCount > 0)
            {
                writer.WriteBits((uint)b & (uint)((1 << bitCount) - 1), bitCount);
            }
        }

        private static void EncodeHuffmanSymbol(ref JpegWriter writer, JpegHuffmanEncodingTable encodingTable, int symbol)
        {
            encodingTable.GetCode(symbol, out var code, out var codeLength);
            writer.WriteBits(code, codeLength);
        }

        /// <summary>
        /// Write the EndOfImage marker.
        /// </summary>
        /// <param name="writer">The JPEG writer.</param>
        protected static void WriteEndOfImage(ref JpegWriter writer)
        {
            writer.WriteMarker(JpegMarker.EndOfImage);
        }

        /// <summary>
        /// Gets the counts the number of bits needed to hold an integer.
        /// </summary>
        private static ReadOnlySpan<byte> BitCountTable => new byte[]
        {
            0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8,
        };

        /// <summary>
        /// Reset the input reader.
        /// </summary>
        public void ResetInputReader()
        {
            _input = null;
        }

        /// <summary>
        /// Reset JPEG tables.
        /// </summary>
        public void ResetTables()
        {
            _quantizationTables = default;
            _huffmanTables = default;
        }

        /// <summary>
        /// Reset the components.
        /// </summary>
        public void ResetComponents()
        {
            _encodeComponents = default;
        }

        /// <summary>
        /// Reset the output.
        /// </summary>
        public void ResetOutput()
        {
            _output = null;
        }

        /// <summary>
        /// Reset the encoder to the initial state.
        /// </summary>
        public void Reset()
        {
            ResetInputReader();
            ResetTables();
            ResetComponents();
            ResetOutput();
        }
    }
}
