#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Portable.Drawing.Imaging.ImageFormats.Jpeg
{
   /// <summary>
    /// A builder to build <see cref="JpegHuffmanEncodingTable"/>
    /// </summary>
    public class JpegHuffmanEncodingTableBuilder
    {
        private readonly long[] _frequencies;

        /// <summary>
        /// Initialize the Huffman table builder.
        /// </summary>
        public JpegHuffmanEncodingTableBuilder()
        {
            _frequencies = new long[256];
        }

        /// <summary>
        /// Increment frequency for the specified symbol.
        /// </summary>
        /// <param name="symbol">The symbol to record.</param>
        public void IncrementCodeCount(int symbol)
        {
            Debug.Assert(symbol <= 255);
            _frequencies[symbol]++;
        }

        /// <summary>
        /// Reset the frequencies of all symbols to zero.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void Reset()
        {
            _frequencies.AsSpan().Clear();
        }

        struct Symbol
        {
            public long Frequency;
            public short Value;
            public ushort CodeSize;
            public short Others;

            public override string ToString()
            {
                return $"Symbol[{Value}](Frequency={Frequency}, CodeSize={CodeSize})";
            }
        }

        /// <summary>
        /// Build the <see cref="JpegHuffmanEncodingTable"/>.
        /// </summary>
        /// <param name="optimal">True to use the optimal algorithm. False to use the modified version of the algorithm specified in ITU-T81.</param>
        /// <returns>The Huffman encoding table.</returns>
        public JpegHuffmanEncodingTable Build(bool optimal = false)
        {
            return optimal ? BuildUsingPackageMerge() : BuildUsingStandardMethod();
        }

        #region Standard Method

        private JpegHuffmanEncodingTable BuildUsingStandardMethod()
        {
            // Find code count
            var codeCount = 0;
            var frequencies = _frequencies;
            for (var i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] > 0)
                {
                    codeCount++;
                }
            }

            if (codeCount == 0)
            {
                throw new InvalidOperationException("No symbol is recorded.");
            }

            // Build symbol list
            var symbols = new Symbol[codeCount + 1];
            var index = 0;
            for (var i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] != 0)
                {
                    symbols[index++] = new Symbol
                    {
                        Value = (short)i,
                        Frequency = frequencies[i],
                        CodeSize = 0,
                        Others = -1
                    };
                }
            }
            symbols[index] = new Symbol
            {
                Value = -1,
                Frequency = 1,
                CodeSize = 0,
                Others = -1
            };

            // Figure K.1 – Procedure to find Huffman code sizes
            FindHuffmanCodeSize(symbols);

            // Figure K.2 – Procedure to find the number of codes of each size
            Span<byte> bits = stackalloc byte[60];
            bits.Clear();

            index = 32;
            for (var i = 0; i < symbols.Length; i++)
            {
                int codeSize = symbols[i].CodeSize;
                if (codeSize > 0)
                {
                    index = Math.Max(index, codeSize);
                    bits[codeSize - 1]++;
                }
            }

            // Figure K.3 – Procedure for limiting code lengths to 16 bits
            while (true)
            {
                while (bits[index] > 0)
                {
                    var j = index - 1;
                    do
                    {
                        j -= 1;
                    } while (bits[j] == 0);

                    bits[index] -= 2;
                    bits[index - 1] += 1;
                    bits[j + 1] += 2;
                    bits[j] = bits[j] -= 1;
                }

                index -= 1;

                if (index != 15)
                {
                    continue;
                }

                while (bits[index] == 0)
                {
                    index--;
                }

                bits[index]--;
                break;
            }

            // Sort symbols
            for (var i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].Value == -1)
                {
                    symbols[i].CodeSize = ushort.MaxValue;
                }
            }
            Array.Sort(symbols, (x, y) => x.CodeSize.CompareTo(y.CodeSize));

            // Figure K.4 – Sorting of input values according to code size
            var codes = BuildCanonicalCode(bits, symbols.AsSpan(0, codeCount));

            return new JpegHuffmanEncodingTable(codes);
        }

        private static void FindHuffmanCodeSize(Span<Symbol> symbols)
        {
            while (true)
            {
                int v1 = -1, v2 = -1;
                long v1Frequency = -1, v2Frequency = -1;

                // Find V1 for least value of FREQ(V1) > 0
                for (var i = 0; i < symbols.Length; i++)
                {
                    var frequency = symbols[i].Frequency;
                    if (frequency >= 0)
                    {
                        if (v1 == -1 || frequency < v1Frequency)
                        {
                            v1 = i;
                            v1Frequency = frequency;
                        }
                    }
                }

                // Find V2 for next least value of FREQ(V2) > 0
                for (var i = 0; i < symbols.Length; i++)
                {
                    var frequency = symbols[i].Frequency;
                    if (frequency >= 0 && i != v1)
                    {
                        if (v2 == -1 || frequency < v2Frequency)
                        {
                            v2 = i;
                            v2Frequency = frequency;
                        }
                    }
                }

                // V2 exists
                if (v2 == -1)
                {
                    break;
                }

                symbols[v1].Frequency += symbols[v2].Frequency;
                symbols[v2].Frequency = -1;

                symbols[v1].CodeSize++;
                while (symbols[v1].Others != -1)
                {
                    v1 = symbols[v1].Others;
                    symbols[v1].CodeSize++;
                }

                symbols[v1].Others = (short)v2;

                symbols[v2].CodeSize++;
                while (symbols[v2].Others != -1)
                {
                    v2 = symbols[v2].Others;
                    symbols[v2].CodeSize++;
                }
            }
        }

        private static JpegHuffmanCanonicalCode[] BuildCanonicalCode(Span<byte> bits, ReadOnlySpan<Symbol> symbols)
        {
            var codeCount = symbols.Length;
            var codes = new JpegHuffmanCanonicalCode[codeCount];

            var currentCodeLength = 1;
            ref var codeLengthsRef = ref MemoryMarshal.GetReference(bits);

            for (var i = 0; i < codes.Length; i++)
            {
                while (codeLengthsRef == 0)
                {
                    codeLengthsRef = ref Unsafe.Add(ref codeLengthsRef, 1);
                    currentCodeLength++;
                }
                codeLengthsRef--;

                codes[i].Symbol = (byte)symbols[i].Value;
                codes[i].CodeLength = (byte)currentCodeLength;
            }

            var bitCode = codes[0].Code = 0;
            int bitCount = codes[0].CodeLength;

            for (var i = 1; i < codes.Length; i++)
            {
                ref var code = ref codes[i];

                if (code.CodeLength > bitCount)
                {
                    bitCode++;
                    bitCode <<= (code.CodeLength - bitCount);
                    code.Code = bitCode;
                    bitCount = code.CodeLength;
                }
                else
                {
                    code.Code = ++bitCode;
                }
            }

            return codes;
        }

        #endregion

        #region Package Merge Method

        private JpegHuffmanEncodingTable BuildUsingPackageMerge()
        {
            // Find code count
            var codeCount = 0;
            var frequencies = _frequencies;
            for (var i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] > 0)
                {
                    codeCount++;
                }
            }

            // Build symbol list
            var symbols = new Symbol[codeCount + 1];
            var index = 0;
            for (var i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] != 0)
                {
                    symbols[index++] = new Symbol
                    {
                        Value = (short)i,
                        Frequency = frequencies[i],
                        CodeSize = 0
                    };
                }
            }
            symbols[index] = new Symbol
            {
                Value = -1,
                Frequency = 0,
                CodeSize = 0
            };

            RunPackageMerge(symbols);

            Array.Sort(symbols, SymbolComparer.Instance);

            index = 0;
            for (var i = symbols.Length - 1; i >= 0; i--)
            {
                if (symbols[i].Value == -1)
                {
                    index = i;
                    break;
                }
            }

            for (var i = index; i < symbols.Length - 1; i++)
            {
                symbols[i] = symbols[i + 1];
            }

            var codes = BuildCanonicalCode(symbols.AsSpan(0, codeCount));

            return new JpegHuffmanEncodingTable(codes);
        }

        private static void RunPackageMerge(Symbol[] symbols)
        {
            Array.Sort(symbols, (x, y) => y.Frequency.CompareTo(x.Frequency)); // descending
            var codeCount = symbols.Length;

            // Initialize
            var levels = new List<Node>[16];
            for (int l = levels.Length - 1, nodeCount = codeCount; l >= 0; l--, nodeCount += nodeCount / 2)
            {
                var nodes = new List<Node>(nodeCount);
                for (var i = 0; i < codeCount; i++)
                {
                    var node = new Node();
                    node.Set((short)i, symbols[i].Frequency);
                    nodes.Add(node);
                }
                levels[l] = nodes;
            }

            // Run package merge
            for (var l = levels.Length - 1; l > 0; l--)
            {
                var nodes = levels[l];
                var nextLevelNodes = levels[l - 1];
                nodes.Sort((x, y) => y.Frequency.CompareTo(x.Frequency)); // descending
                for (var nodeCount = nodes.Count; nodeCount >= 2; nodeCount = nodes.Count)
                {
                    // Take last two nodes
                    var node1 = nodes[nodeCount - 1];
                    var node2 = nodes[nodeCount - 2];
                    nodes.RemoveAt(nodeCount - 1);
                    nodes.RemoveAt(nodeCount - 2);

                    // Package
                    var node = new Node();
                    node.Set(node1, node2);

                    // Merge
                    nextLevelNodes.Add(node);
                }
            }

            var level0 = levels[0];
            level0.Sort((x, y) => x.Frequency.CompareTo(y.Frequency)); // ascending
            var selectCount = Math.Max(1, 2 * (codeCount - 1));
            for (var i = 0; i < selectCount; i++)
            {
                TraverseNode(level0[i], symbols);
            }

            static void TraverseNode(Node? node, Symbol[] symbols)
            {
                while (true)
                {
                    if (node is null) return;
                    if (node.Left is null)
                    {
                        symbols[node.Index].CodeSize++;
                    }
                    else
                    {
                        TraverseNode(node.Left, symbols);
                        node = node.Right;
                        continue;
                    }

                    break;
                }
            }
        }

        class SymbolComparer : Comparer<Symbol>
        {
            public static SymbolComparer Instance { get; } = new SymbolComparer();

            public override int Compare(Symbol x, Symbol y)
            {
                if (x.CodeSize > y.CodeSize)
                {
                    return 1;
                }
                if (x.CodeSize < y.CodeSize)
                {
                    return -1;
                }
                if (x.Frequency > y.Frequency)
                {
                    return -1;
                }
                if (x.Frequency < y.Frequency)
                {
                    return 1;
                }
                return 0;
            }
        }

        class Node
        {
            public long Frequency { get; private set; }
            public short Index { get; private set; }
            public Node? Left { get; private set; }
            public Node? Right { get; private set; }

            public void Set(short index, long frequency)
            {
                Index = index;
                Frequency = frequency;
            }

            public void Set(Node? left, Node? right)
            {
                Frequency = (left?.Frequency ?? 0) + (right?.Frequency ?? 0);
                Left = left;
                Right = right;
            }
        }

        private static JpegHuffmanCanonicalCode[] BuildCanonicalCode(ReadOnlySpan<Symbol> symbols)
        {
            var codeCount = symbols.Length;
            var codes = new JpegHuffmanCanonicalCode[codeCount];

            for (var i = 0; i < codes.Length; i++)
            {
                codes[i].Symbol = (byte)symbols[i].Value;
                codes[i].CodeLength = (byte)symbols[i].CodeSize;
            }

            var bitCode = codes[0].Code = 0;
            int bitCount = codes[0].CodeLength;

            for (var i = 1; i < codes.Length; i++)
            {
                ref var code = ref codes[i];

                if (code.CodeLength > bitCount)
                {
                    bitCode++;
                    bitCode <<= (code.CodeLength - bitCount);
                    code.Code = bitCode;
                    bitCount = code.CodeLength;
                }
                else
                {
                    code.Code = ++bitCode;
                }
            }

            return codes;
        }

        #endregion
    }}
