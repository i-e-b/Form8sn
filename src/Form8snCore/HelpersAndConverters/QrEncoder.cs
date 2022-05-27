using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PdfSharp.Extensions;

namespace Form8snCore.HelpersAndConverters;

/// <summary>
/// A basic QR code generator.
/// This implementation assumes a single error correction level, and UTF-8
/// strings only for a simpler encoder.
/// </summary>
public class QrEncoder
{
    /// <summary>
    /// Encode a string as a general 'data' QR code.
    /// The smallest QR code is generated to give a 25% redundancy.
    /// </summary>
    public static bool[,] EncodeString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        
        var dataLength = bytes.Length;
        var qrVersion = GetVersionForSize(dataLength);
        
        // Input data for the QR code (with ECC not yet applied)
        var bits = new List<byte>();
        
        // "Mode" -- we always use 'byte'
        Add(bits, 0,1,0,0);
        
        // Data "count"
        AddIntBits(bits, dataLength, CountBits(qrVersion));
        
        // Transform data with error correction
        var blocks = EccEncode(bits, qrVersion);
        
        // interleave resulting data
        var interleaved = InterleaveBlocks(qrVersion, blocks);


        // Make a bit matrix for the QR code, and place data into it
        var size = GetSizeForVersion(qrVersion);
        
        return ArrangeQrData(size, interleaved, qrVersion);
    }

    private static List<byte> InterleaveBlocks(int qrVersion, List<EccDataBlock> blocks)
    {
        var interleaved = new List<byte>();
        GetEccData(qrVersion, out _, out var eccPerBlock, out _, out var group1Words, out _, out var group2Words);
        var upperLimit = Math.Max(group1Words, group2Words);
        for (var i = 0; i < upperLimit; i++)
        {
            foreach (var codeBlock in blocks.Where(codeBlock => codeBlock.CodeWords.Count > i))
                interleaved.AddRange(codeBlock.CodeWords[i]!);
        }

        for (var i = 0; i < eccPerBlock; i++)
        {
            foreach (var codeBlock in blocks.Where(codeBlock => codeBlock.EccWords.Count > i))
                interleaved.AddRange(codeBlock.EccWords[i]!);
        }

        for (int i = 0; i < _remainderBits[qrVersion - 1]; i++)
        {
            interleaved.Add(0);
        }

        return interleaved;
    }


    private static readonly int[] _remainderBits = { 0, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0 };
    private static readonly FieldPoint[] _galoisField = CreateGaloisField();
    
    
    private static bool[,] ArrangeQrData(int size, List<byte> bits, int version)
    {
        var matrix = new bool[size,size];
        
        // record blocks that contain fixed patterns or mandatory quiet space
        // we will fit data into the remaining space afterwards
        var reserved = new bool[size,size];
        
        PlaceFinders(size, matrix, reserved);
        PlaceSeparators(size, reserved);
        PlaceAligners(version, size, matrix, reserved);
        PlaceTimingStrips(size, matrix, reserved);
        PlaceDarkSpot(version, matrix, reserved);
        
        // Reserve some locations for later data TODO: clean this up to 1 phase?
        ReserveVersion(size, version, reserved);
        
        // zig-zag bits into what's left unreserved in the matrix
        FitDataIntoFreeModules(size, matrix, reserved, bits);
        
        // TODO: write mask code data, format marker, version marker
        
        // Apply a bitmask to the encoded data. This is quite complex.
        // We need to run all the predefined masks against the data,
        // and then get a penalty score for a predefined set of tests.
        // This is intended to reduce the chance that data looks like
        // the timing and locating patterns
        var maskVersion = SearchForBestMaskPattern(size, matrix, version, reserved, out var maskedMatrix);
        
        return maskedMatrix;
    }

    private static int CalculateMatrixScore(int size, bool[,] matrix)
    {
        int score1 = 0,
            score2 = 0,
            score3 = 0,
            score4 = 0;

        // Penalty 1: looking for patches and gaps
        for (var y = 0; y < size; y++)
        {
            var modInRow = 0;
            var modInColumn = 0;
            var lastValRow = matrix[0,y];
            var lastValColumn = matrix[y,0]; // yes, backwards.
            for (var x = 0; x < size; x++)
            {
                if (matrix[x,y] == lastValRow) modInRow++;
                else modInRow = 1;
                
                if (modInRow == 5) score1 += 3;
                else if (modInRow > 5) score1++;
                
                lastValRow = matrix[x,y];


                if (matrix[x,y] == lastValColumn) modInColumn++;
                else modInColumn = 1;
                
                if (modInColumn == 5) score1 += 3;
                else if (modInColumn > 5) score1++;
                
                lastValColumn = matrix[y,x]; // yes, backwards
            }
        }


        // Penalty 2: top-left corner of a square
        for (var y = 0; y < size - 1; y++)
        {
            for (var x = 0; x < size - 1; x++)
            {
                if (matrix[x, y] == matrix[x + 1, y] &&
                    matrix[x, y] == matrix[x, y + 1] &&
                    matrix[x, y] == matrix[x + 1, y + 1])
                    score2 += 3;
            }
        }

        // Penalty 3: horz/vert stripes that look like bits of timing/location boxes
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size - 10; x++)
            {
                // these make a lot more sense as bit patterns
                if (( matrix[x,y] && !matrix[x + 1,y] && matrix[x + 2,y] && matrix[x + 3,y] && matrix[x + 4,y] && !matrix[x + 5,y] && matrix[x + 6,y] && !matrix[x + 7,y] && !matrix[x + 8,y] && !matrix[x + 9,y] && !matrix[x + 10,y])
                    ||
                    (!matrix[x,y] && !matrix[x + 1,y] && !matrix[x + 2,y] && !matrix[x + 3,y] && matrix[x + 4,y] && !matrix[x + 5,y] && matrix[x + 6,y] && matrix[x + 7,y] && matrix[x + 8,y] && !matrix[x + 9,y] && matrix[x + 10,y]))
                {
                    score3 += 40;
                }

                if ((matrix[y, x] && !matrix[y, x + 1] && matrix[y, x + 2] && matrix[y, x + 3] && matrix[y, x + 4] && !matrix[y, x + 5] && matrix[y, x + 6] && !matrix[y, x + 7] && !matrix[y, x + 8] && !matrix[y, x + 9] && !matrix[y, x + 10])
                    ||
                    (!matrix[y, x] && !matrix[y, x + 1] && !matrix[y, x + 2] && !matrix[y, x + 3] && matrix[y, x + 4] && !matrix[y, x + 5] && matrix[y, x + 6] && matrix[y, x + 7] && matrix[y, x + 8] && !matrix[y, x + 9] && matrix[y, x + 10]))
                {
                    score3 += 40;
                }
            }
        }

        // Penalty 4: unbalanced between dark and light
        double blackModules = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (matrix[i, j]) blackModules++;
            }
        }

        var percent = (blackModules / (size * size)) * 100;
        var prevMultipleOf5 = Math.Abs((int)Math.Floor(percent / 5) * 5 - 50) / 5;
        var nextMultipleOf5 = Math.Abs((int)Math.Floor(percent / 5) * 5 - 45) / 5;
        score4 = Math.Min(prevMultipleOf5, nextMultipleOf5) * 10;

        return score1 + score2 + score3 + score4;
    }

    private delegate bool MaskFunction(int x, int y);
    private static int SearchForBestMaskPattern(int size, bool[,] matrix, int version, bool[,] reserved, out bool[,] transformed)
    {
        // The 8 predefined patterns. We could bake these into bitmasks rather than calculating.
        // Note, the spec has these as 1-based, so we need to account for that later
        var masks = new MaskFunction[]{
            (x, y) => (x + y) % 2 == 0,
            (_,y)=>y % 2 == 0,
            (x,_)=>x%3 ==0,
            (x,y)=>(x+y)%3==0,
            (x,y)=>((y/2) + (x/3))%2 == 0,
            (x,y)=>((x*y)%2)+((x*y)%3) == 0,
            (x,y)=>(((x * y) % 2) + ((x * y) % 3)) % 2 == 0,
            (x,y)=>(((x + y) % 2) + ((x * y) % 3)) % 2 == 0
        };
        
        // Now, apply each pattern to our raw matrix, and score it. Pick the result with lowest score.

        var bestIndex = -1;
        var bestScore = int.MaxValue;
        transformed = matrix;
        for (int i = 0; i < masks.Length; i++)
        {
            var masked = ApplyMask(size, matrix, reserved, masks[i]);
            var score = CalculateMatrixScore(size, masked);

            if (score < bestScore)
            {
                bestIndex = i;
                transformed = masked;
            }
        }
        
        return bestIndex + 1;
    }

    private static bool[,] ApplyMask(int size, bool[,] matrix, bool[,] reserved, MaskFunction mask)
    {
        var result = new bool[size,size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!reserved[x,y]) result[x,y] = matrix[x,y] ^ mask(x,y);
                else result[x,y] = matrix[x,y];
            }
        }
        return result;
    }

    private static void FitDataIntoFreeModules(int size, bool[,] matrix, bool[,] reserved, List<byte> bits)
    {
        var up = true;
        var data = new Queue<bool>();
        foreach (var t in bits) { data.Enqueue(t != 0); }
        
        for (var x = size - 1; x >= 0; x -= 2)
        {
            if (x == 6) x = 5; // skip timing column
            
            for (var yMod = 1; yMod <= size; yMod++)
            {
                int y;
                if (up)
                {
                    // zig-zag within a two-module range
                    y = size - yMod;
                    if (data.Count > 0 && !reserved[x,y]) matrix[x,y] = data.Dequeue();
                    if (data.Count > 0 && x > 0 && !reserved[x - 1, y]) matrix[x-1,y] = data.Dequeue();
                }
                else
                {
                    // zig-zag within a two-module range
                    y = yMod - 1;
                    if (data.Count > 0 && !reserved[x, y]) matrix[x,y] = data.Dequeue();
                    if (data.Count > 0 && x > 0 && !reserved[x - 1, y]) matrix[x - 1, y] = data.Dequeue();
                }
            }
            up = !up;
        }
    }

    private static void ReserveVersion(int size, int version, bool[,] reserved)
    {
        BlockOut(reserved, x: 8, y:0, w:1, h:6);
        BlockOut(reserved, x: 8, y:7, w:1, h:1);
        BlockOut(reserved, x: 0, y:8, w:6, h:1);
        BlockOut(reserved, x: 7, y:8, w:2, h:1);
        BlockOut(reserved, x: size-8, y:8, w:8, h:1);
        BlockOut(reserved, x: 8, y:size-7, w:1, h:7);
        
        if (version < 7) return;
        
        BlockOut(reserved, x: size-11, y:0, w:3, h:6);
        BlockOut(reserved, x: 0, y:size-11, w:6, h:3);
    }

    private static void BlockOut(bool[,] reserved, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                reserved[x + dx, y + dy] = true;
            }
        }
    }

    private static void PlaceDarkSpot(int version, bool[,] matrix, bool[,] reserved)
    {
        var x = 8;
        var y = 4 * version + 9;
        matrix[x,y] = true;
        reserved[x,y] = true;
    }

    private static void PlaceTimingStrips(int size, bool[,] matrix, bool[,] reserved)
    {
        for (var i = 8; i < size - 8; i++)
        {
            if (i % 2 == 0)
            {
                matrix[6, i] = true;
                matrix[i, 6] = true;
            }
            reserved[6, i] = true;
            reserved[i, 6] = true;
        }
    }

    private static void PlaceAligners(int version, int size, bool[,] matrix, bool[,] reserved)
    {
        var locations = AlignmentPoints(version);

        foreach (var loc in locations)
        {
            // check if any point in the proposed module has been allocated
            // to another pattern. Skip this aligner if so.
            if (OverlapsReserved(size, reserved, loc, 5, 5)) continue;

            // Draw the aligner pattern and mark reserved
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    if (y == 0 || y == 4 || x == 0 || x == 4 || (x == 2 && y == 2))
                    {
                        matrix[loc.X + x, loc.Y + y] = true;
                    }
                    reserved[loc.X + x, loc.Y + y] = true;
                }
            }
        }
    }

    private static bool OverlapsReserved(int size, bool[,] reserved, MatrixPoint loc, int width, int height)
    {
        for (var px = 0; px < width; px++)
        {
            for (var py = 0; py < height; py++)
            {
                var x = px + loc.X;
                var y = py + loc.Y;
                if (x < 0 || x > size) return true;
                if (y < 0 || y > size) return true;
                if (reserved[x, y]) return true;
            }
        }

        return false;
    }


    private static readonly int[] _alignmentPositions = { 0, 0, 0, 0, 0, 0, 0, 6, 18, 0, 0, 0, 0, 0, 6, 22, 0, 0, 0, 0, 0, 6, 26, 0, 0, 0, 0, 0, 6, 30, 0, 0, 0, 0, 0, 6, 34, 0, 0, 0, 0, 0, 6, 22, 38, 0, 0, 0, 0, 6, 24, 42, 0, 0, 0, 0, 6, 26, 46, 0, 0, 0, 0, 6, 28, 50, 0, 0, 0, 0, 6, 30, 54, 0, 0, 0, 0, 6, 32, 58, 0, 0, 0, 0, 6, 34, 62, 0, 0, 0, 0, 6, 26, 46, 66, 0, 0, 0, 6, 26, 48, 70, 0, 0, 0, 6, 26, 50, 74, 0, 0, 0, 6, 30, 54, 78, 0, 0, 0, 6, 30, 56, 82, 0, 0, 0, 6, 30, 58, 86, 0, 0, 0, 6, 34, 62, 90, 0, 0, 0, 6, 28, 50, 72, 94, 0, 0, 6, 26, 50, 74, 98, 0, 0, 6, 30, 54, 78, 102, 0, 0, 6, 28, 54, 80, 106, 0, 0, 6, 32, 58, 84, 110, 0, 0, 6, 30, 58, 86, 114, 0, 0, 6, 34, 62, 90, 118, 0, 0, 6, 26, 50, 74, 98, 122, 0, 6, 30, 54, 78, 102, 126, 0, 6, 26, 52, 78, 104, 130, 0, 6, 30, 56, 82, 108, 134, 0, 6, 34, 60, 86, 112, 138, 0, 6, 30, 58, 86, 114, 142, 0, 6, 34, 62, 90, 118, 146, 0, 6, 30, 54, 78, 102, 126, 150, 6, 24, 50, 76, 102, 128, 154, 6, 28, 54, 80, 106, 132, 158, 6, 32, 58, 84, 110, 136, 162, 6, 26, 54, 82, 110, 138, 166, 6, 30, 58, 86, 114, 142, 170 };

    private static List<MatrixPoint> AlignmentPoints(int version)
    {
        var locations = new List<MatrixPoint>();

        var offset = 7 * (version - 1);
        for (var x = 0; x < 7; x++)
        {
            if (_alignmentPositions[offset + x] == 0) continue;
            for (var y = 0; y < 7; y++)
            {
                if (_alignmentPositions[offset + y] == 0) continue;
                locations.Add(new MatrixPoint
                {
                    X = _alignmentPositions[offset + x] - 2,
                    Y = _alignmentPositions[offset + y] - 2
                });
            }
        }
        
        return locations;
    }

    private struct MatrixPoint
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// reserve the various internal quiet strips
    /// </summary>
    private static void PlaceSeparators(int size, bool[,] reserved)
    {
        for (int i = 0; i < 8; i++)
        {
            reserved[7, i] = true;
            reserved[i, size - 8] = true;
            reserved[size - 8, i] = true;
        }

        for (int i = 0; i < 7; i++)
        {
            reserved[0, i + 7] = true;
            reserved[i + 7, size - 7] = true;
            reserved[size - 7, i + 7] = true;
        }
    }

    /// <summary>
    /// Mark out the three 'finder' patterns
    /// </summary>
    private static void PlaceFinders(int size, bool[,] matrix, bool[,] reserved)
    {
        int[] positions = { 0, 0, size - 7, 0, 0, size - 7 };

        for (var i = 0; i < 6; i += 2) // 3 'finder' patterns (2 coords each)
        {
            for (var x = 0; x < 7; x++)
            {
                for (var y = 0; y < 7; y++)
                {
                    // flip to dark in the stipulated pattern
                    if (!(((x == 1 || x == 5) && y > 0 && y < 6) || (x > 0 && x < 6 && (y == 1 || y == 5))))
                    {
                        matrix[ x + positions[i], y + positions[i + 1]] = true;
                    }

                    // mark this location occupied (both dark and light)
                    reserved[x + positions[i], y + positions[i + 1]] = true;
                }
            }
        }
    }

    private static int GetSizeForVersion(int qrVersion) => 21 + (qrVersion - 1) * 4;

    /// <summary>
    /// Transform data to encode with 25% redundancy ('Q' level by the QR code specs)
    /// </summary>
    private static List<EccDataBlock> EccEncode(List<byte> bits, int version)
    {
        // get code parameters
        GetEccData(version, out var totalDataCodewords, out var eccPerBlock, out var group1Blocks, out var group1Words, out var group2Blocks, out var group2Words);
        
        var sanity = (group1Blocks * group1Words) + (group2Blocks * group2Words);
        if (sanity != totalDataCodewords) throw new Exception($"ECC data table contains an error for version {version}");
        
        // pad data to the exact code length
        PadCodeBits(bits, totalDataCodewords);
        
        // now calculate the ECC blocks 1 & 2
        var generatorPolynomial = GeneratorPolynomial(eccPerBlock);
        var encodedData = new List<EccDataBlock>();
        
        // Block 1
        var offset = 0;
        for (int i = 0; i < group1Blocks; i++)
        {
            var block = new EccDataBlock();
            var blockBits = bits.GetRange(offset, group1Words * 8);
            offset += group1Words * 8;
            
            block.CodeWords = ChopIntoEights(blockBits);
            block.EccWords = CalculateReedSolomonCodes(blockBits, generatorPolynomial, eccPerBlock);
            encodedData.Add(block);
        }
        
        // Block 2
        for (int i = 0; i < group2Blocks; i++)
        {
            var block = new EccDataBlock();
            var blockBits = bits.GetRange(offset, group2Words * 8);
            offset += group2Words * 8;
            
            block.CodeWords = ChopIntoEights(blockBits);
            block.EccWords = CalculateReedSolomonCodes(blockBits, generatorPolynomial, eccPerBlock);
            encodedData.Add(block);
        }
        
        return encodedData;
    }

    /// <summary>
    /// Core of the error correcting mechanism.
    /// </summary>
    private static List<byte[]> CalculateReedSolomonCodes(List<byte> blockBits, List<PolynomialTerm> generatorPolynomial, int eccPerBlock)
    {
        var messagePolynomial = MessagePolynomial(blockBits);

        for (var i = 0; i < messagePolynomial.Count; i++)
        {
            messagePolynomial[i] = new PolynomialTerm
            {
                Coefficient = messagePolynomial[i].Coefficient,
                Exponent = messagePolynomial[i].Exponent + eccPerBlock
            };
        }

        for (var i = 0; i < generatorPolynomial.Count; i++)
        {
            generatorPolynomial[i] = new PolynomialTerm
            {
                Coefficient = generatorPolynomial[i].Coefficient,
                Exponent = generatorPolynomial[i].Exponent + (messagePolynomial.Count - 1)
            };
        }
        
        
        var leadTermSource = messagePolynomial;
        for (var i = 0; leadTermSource.Count > 0 && leadTermSource[^1].Exponent > 0; i++)
        {
            if (leadTermSource[0].Coefficient == 0)
            {
                leadTermSource.RemoveAt(0);
                leadTermSource.Add(new PolynomialTerm{
                    Coefficient = 0,
                    Exponent = leadTermSource[^1].Exponent - 1
                });
            }
            else
            {
                var leadGenerator = MultiplyGeneratorByLeadTerm(
                    generatorPolynomial,
                    ConvertToAlphaNotation(leadTermSource[0]), i);
                leadGenerator = ConvertToDecNotation(leadGenerator);
                leadGenerator = XorPolynomialTerms(leadTermSource, leadGenerator);
                leadTermSource = leadGenerator;
            }
        }
        
        return leadTermSource.Select(x => IntBits(x.Coefficient, 8)).ToList();
    }

    private static List<PolynomialTerm> MultiplyGeneratorByLeadTerm(List<PolynomialTerm> generator, PolynomialTerm leadTerm, int lowerExponentBy)
    {
        var result = new List<PolynomialTerm>();
        foreach (var polItemBase in generator)
        {
            result.Add(new PolynomialTerm
            {
                Coefficient = (polItemBase.Coefficient + leadTerm.Coefficient) % 255,
                Exponent = polItemBase.Exponent - lowerExponentBy
            });
        }

        return result;
    }

    private static List<PolynomialTerm> XorPolynomialTerms(List<PolynomialTerm> message, List<PolynomialTerm> leadGenerator)
    {
        var result = new List<PolynomialTerm>();
        List<PolynomialTerm> longPoly, shortPoly;
        if (message.Count >= leadGenerator.Count)
        {
            longPoly = message;
            shortPoly = leadGenerator;
        }
        else
        {
            longPoly = leadGenerator;
            shortPoly = message;
        }

        for (var i = 0; i < longPoly.Count; i++)
        {
            result.Add(new PolynomialTerm
            {
                Coefficient = longPoly[i].Coefficient ^ (shortPoly.Count > i ? shortPoly[i].Coefficient : 0),
                Exponent = message[0].Exponent - i
            });
        }

        result.RemoveAt(0);
        return result;
    }
    
    private static List<PolynomialTerm> ConvertToDecNotation(List<PolynomialTerm> poly)
    {
        var result = new List<PolynomialTerm>();
        for (var i = 0; i < poly.Count; i++)
        {
            result.Add(new PolynomialTerm{
                Coefficient = GetIntValFromAlphaExp(poly[i].Coefficient),
                Exponent = poly[i].Exponent
                }
            );
        }

        return result;
    }
    
    private static PolynomialTerm ConvertToAlphaNotation(PolynomialTerm src)
    {
        return src with { Coefficient = src.Coefficient == 0 ? 0 : GetAlphaExpFromIntVal(src.Coefficient) };
    }

    // TODO: push proper use of bytes up to save recombining
    private static List<PolynomialTerm> MessagePolynomial(List<byte> blockBits)
    {
        var result = new List<PolynomialTerm>();
        var offset = 0;
        for (var i = blockBits.Count / 8 - 1; i >= 0; i--)
        {
            result.Add(new PolynomialTerm{
                Coefficient = BitsToInt(blockBits.GetRange(offset, 8)),
                Exponent = i
            });
            offset += 8;
        }

        return result;
    }

    private static List<PolynomialTerm> GeneratorPolynomial(int codesPerBlock)
    {
        var denormal = new List<PolynomialTerm>
        {
            new() { Coefficient = 0, Exponent = 1 },
            new() { Coefficient = 0, Exponent = 0 }
        };

        for (int i = 1; i < codesPerBlock; i++)
        {
            var mul = new List<PolynomialTerm>
            {
                new() { Coefficient = 0, Exponent = 1 },
                new() { Coefficient = i, Exponent = 0 }
            };
            
            denormal = MultiplyAlpha(denormal, mul);
        }
        
        return denormal;
    }

    private static List<PolynomialTerm> MultiplyAlpha(List<PolynomialTerm> generator, List<PolynomialTerm> mul)
    {
        var result = new List<PolynomialTerm>();

        foreach (var  multiItem in mul)
        {
            foreach (var baseItem in generator)
            {
                result.Add(
                    new PolynomialTerm
                    {
                        Coefficient = ReduceAlpha(baseItem.Coefficient + multiItem.Coefficient),
                        Exponent = baseItem.Exponent + multiItem.Exponent
                    }
                );
            }
        }
        
        var glue = result.GroupBy(x => x.Exponent)
            .Where(x => x.Count() > 1)
            .Select(x => x.First().Exponent)
            .ToListOrEmpty();
        
        var final = new List<PolynomialTerm>();
        foreach (var exp in glue)
        {
            var coeff = result
                .Where(x => x.Exponent == exp)
                .Aggregate(0, (current, old) => current ^ GetIntValFromAlphaExp(old.Coefficient));
            final.Add(new PolynomialTerm{
                Coefficient = GetAlphaExpFromIntVal(coeff),
                Exponent = exp
            });
        }

        final.Sort((x, y) => -x.Exponent.CompareTo(y.Exponent));
        return final;
    }

    private static int GetIntValFromAlphaExp(int exp)
    {
        for (int i = 0; i < _galoisField.Length; i++)
        {
            if (_galoisField[i].ExponentAlpha == exp) return _galoisField[i].IntegerValue;
        }
        throw new Exception($"Exponent {exp} not found in field");
    }
    
    private static int GetAlphaExpFromIntVal(int intVal)
    {
        for (int i = 0; i < _galoisField.Length; i++)
        {
            if (_galoisField[i].IntegerValue == intVal) return _galoisField[i].ExponentAlpha;
        }
        throw new Exception($"Value {intVal} not found in field");
    }


    private static int ReduceAlpha(int exp) => (int)(exp % 256 + Math.Floor(exp / 256.0));

    private struct PolynomialTerm
    {
        public int Coefficient;
        public int Exponent;
    }

    private static List<byte[]> ChopIntoEights(List<byte> blockBits)
    {
        var result = new List<byte[]>();
        for (var i = 0; i < blockBits.Count; i += 8)
        {
            var array = blockBits.GetRange(i, 8).ToArray();
            result.Add(array);
        }
        return result;
    }

    /// <summary>
    /// Pad data out to required length with "cooking"
    /// </summary>
    private static void PadCodeBits(List<byte> bits, int totalDataCodewords)
    {
        var codeBits = totalDataCodewords * 8;
        var emptyBits = codeBits - bits.Count;
        
        // pad up to 4 zeros
        var tailBits = Math.Min(emptyBits, 4);
        for (var i = 0; i < tailBits; i++) { bits.Add(0); }
        
        // align end of real data to a byte boundary
        var alignErr = bits.Count % 8;
        if (alignErr != 0) {
            alignErr = 8 - alignErr;
            for (var i = 0; i < alignErr; i++) bits.Add(0);
        }
        
        // if there is any code space left, pad it with the 'cooking' string "1110110000010001"
        var cooking = new byte[]{1,1,1,0,1,1,0,0,0,0,0,1,0,0,0,1};
        var remains = codeBits - bits.Count;
        if (remains < 0) throw new Exception("Logical error in code bit padding");
        for (var i = 0; i < remains; i++) bits.Add(cooking[i % 16]);
    }

    /// <summary>
    /// Encode an integer into a bit string, with a fixed number of bits that will be added
    /// </summary>
    private static void AddIntBits(ICollection<byte> bits, int value, int bitCount)
    {
        for (var i = bitCount - 1; i >= 0; i--)
        {
            bits.Add((byte)((value >> i) & 1));
        }
    }
    
    /// <summary>
    /// Encode an integer into a bit string, with a fixed number of bits that will be added
    /// </summary>
    private static byte[] IntBits(int value, int bitCount)
    {
        var result = new byte[bitCount];
        var j = 0;
        for (var i = bitCount - 1; i >= 0; i--)
        {
            result[j++] = (byte)((value >> i) & 1);
        }
        return result;
    }

    private static int BitsToInt(List<byte> bits)
    {
        int result = 0;
        for (int i = 0; i < bits.Count; i++)
        {
            result <<= 1;
            result |= bits[i];
        }
        
        return result;
    }

    /// <summary>
    /// Add a fixed number of bits to a bit string
    /// </summary>
    private static void Add(ICollection<byte> bits, params byte[] b)
    {
        foreach (var bit in b) bits.Add(bit);
    }

    
    /// <summary>
    /// Lookup table for ECC settings for a single QR 'version'. Always assumes 'Q' level ECC
    /// </summary>
    private static void GetEccData(int version, out int totalDataCodewords, out int eccPerBlock, out int group1Blocks, out int group1Words, out int group2Blocks, out int group2Words)
    {
        switch (version)
        {
            case 1:
                totalDataCodewords = 13; eccPerBlock = 13; group1Blocks=1; group1Words = 13; group2Blocks = 0; group2Words = 0;
                break;
            case 2:
                totalDataCodewords = 22; eccPerBlock = 22; group1Blocks=1; group1Words = 22; group2Blocks = 0; group2Words = 0;
                break;
            case 3:
                totalDataCodewords = 34; eccPerBlock = 18; group1Blocks=2; group1Words = 17; group2Blocks = 0; group2Words = 0;
                break;
            case 4:
                totalDataCodewords = 48; eccPerBlock = 26; group1Blocks=2; group1Words = 24; group2Blocks = 0; group2Words = 0;
                break;
            case 5:
                totalDataCodewords = 62; eccPerBlock = 18; group1Blocks = 2; group1Words = 15; group2Blocks = 2; group2Words = 16;
                break;
            case 6:
                totalDataCodewords = 76; eccPerBlock = 24; group1Blocks = 4; group1Words = 19; group2Blocks = 0; group2Words = 0;
                break;
            case 7:
                totalDataCodewords = 88; eccPerBlock = 18; group1Blocks = 2; group1Words = 14; group2Blocks = 4; group2Words = 15;
                break;
            case 8:
                totalDataCodewords = 110; eccPerBlock = 22; group1Blocks = 4; group1Words = 18; group2Blocks = 2; group2Words = 19;
                break;
            case 9:
                totalDataCodewords = 132; eccPerBlock = 20; group1Blocks = 4; group1Words = 16; group2Blocks = 4; group2Words = 17;
                break;
            case 10:
                totalDataCodewords = 154; eccPerBlock = 24; group1Blocks = 6; group1Words = 19; group2Blocks = 2; group2Words = 20;
                break;
            case 11:
                totalDataCodewords = 180; eccPerBlock = 28; group1Blocks = 4; group1Words = 22; group2Blocks = 4; group2Words = 23;
                break;
            case 12:
                totalDataCodewords = 206; eccPerBlock = 26; group1Blocks = 4; group1Words = 20; group2Blocks = 6; group2Words = 21;
                break;
            case 13:
                totalDataCodewords = 244; eccPerBlock = 24; group1Blocks = 8; group1Words = 20; group2Blocks = 4; group2Words = 21;
                break;
            case 14:
                totalDataCodewords = 261; eccPerBlock = 20; group1Blocks = 11; group1Words = 16; group2Blocks = 5; group2Words = 17;
                break;
            case 15:
                totalDataCodewords = 295; eccPerBlock = 30; group1Blocks = 5; group1Words = 24; group2Blocks = 7; group2Words = 25;
                break;
            case 16:
                totalDataCodewords = 325; eccPerBlock = 24; group1Blocks = 15; group1Words = 19; group2Blocks = 2; group2Words = 20;
                break;
            case 17:
                totalDataCodewords = 367; eccPerBlock = 28; group1Blocks = 1; group1Words = 22; group2Blocks = 15; group2Words = 23;
                break;
            case 18:
                totalDataCodewords = 397; eccPerBlock = 28; group1Blocks = 17; group1Words = 22; group2Blocks = 1; group2Words = 23;
                break;
            case 19:
                totalDataCodewords = 445; eccPerBlock = 26; group1Blocks = 17; group1Words = 21; group2Blocks = 4; group2Words = 22;
                break;
            case 20:
                totalDataCodewords = 485; eccPerBlock = 30; group1Blocks = 15; group1Words = 24; group2Blocks = 5; group2Words = 25;
                break;
            case 21:
                totalDataCodewords = 512; eccPerBlock = 28; group1Blocks = 17; group1Words = 22; group2Blocks = 6; group2Words = 23;
                break;
            case 22:
                totalDataCodewords = 568; eccPerBlock = 30; group1Blocks = 7; group1Words = 24; group2Blocks = 16; group2Words = 25;
                break;
            case 23:
                totalDataCodewords = 614; eccPerBlock = 30; group1Blocks = 11; group1Words = 24; group2Blocks = 14; group2Words = 25;
                break;
            case 24:
                totalDataCodewords = 664; eccPerBlock = 30; group1Blocks = 11; group1Words = 24; group2Blocks = 16; group2Words = 25;
                break;
            case 25:
                totalDataCodewords = 718; eccPerBlock = 30; group1Blocks = 7; group1Words = 24; group2Blocks = 22; group2Words = 25;
                break;
            case 26:
                totalDataCodewords = 754; eccPerBlock = 28; group1Blocks = 28; group1Words = 22; group2Blocks = 6; group2Words = 23;
                break;
            case 27:
                totalDataCodewords = 808; eccPerBlock = 30; group1Blocks = 8; group1Words = 23; group2Blocks = 26; group2Words = 24;
                break;
            case 28:
                totalDataCodewords = 871; eccPerBlock = 30; group1Blocks = 4; group1Words = 24; group2Blocks = 31; group2Words = 25;
                break;
            case 29:
                totalDataCodewords = 911; eccPerBlock = 30; group1Blocks = 1; group1Words = 23; group2Blocks = 37; group2Words = 24;
                break;
            case 30:
                totalDataCodewords = 985; eccPerBlock = 30; group1Blocks = 15; group1Words = 24; group2Blocks = 25; group2Words = 25;
                break;
            case 31:
                totalDataCodewords = 1033; eccPerBlock = 30; group1Blocks = 42; group1Words = 24; group2Blocks = 1; group2Words = 25;
                break;
            case 32:
                totalDataCodewords = 1115; eccPerBlock = 30; group1Blocks = 10; group1Words = 24; group2Blocks = 35; group2Words = 25;
                break;
            case 33:
                totalDataCodewords = 1171; eccPerBlock = 30; group1Blocks = 29; group1Words = 24; group2Blocks = 19; group2Words = 25;
                break;
            case 34:
                totalDataCodewords = 1231; eccPerBlock = 30; group1Blocks = 44; group1Words = 24; group2Blocks = 7; group2Words = 25;
                break;
            case 35:
                totalDataCodewords = 1286; eccPerBlock = 30; group1Blocks = 39; group1Words = 24; group2Blocks = 14; group2Words = 25;
                break;
            case 36:
                totalDataCodewords = 1354; eccPerBlock = 30; group1Blocks = 46; group1Words = 24; group2Blocks = 10; group2Words = 25;
                break;
            case 37:
                totalDataCodewords = 1426; eccPerBlock = 30; group1Blocks = 49; group1Words = 24; group2Blocks = 10; group2Words = 25;
                break;
            case 38:
                totalDataCodewords = 1502; eccPerBlock = 30; group1Blocks = 48; group1Words = 24; group2Blocks = 14; group2Words = 25;
                break;
            case 39:
                totalDataCodewords = 1582; eccPerBlock = 30; group1Blocks = 43; group1Words = 24; group2Blocks = 22; group2Words = 25;
                break;
            case 40:
                totalDataCodewords = 1666; eccPerBlock = 30; group1Blocks = 34; group1Words = 24; group2Blocks = 34; group2Words = 25;
                break;

            default: throw new Exception($"Invalid QR code version {version}");
        }
    }
    
    /// <summary>
    /// QR codes have a bunch of 'versions' based on the ECC level
    /// and the length of data to be encoded.
    ///
    /// This assumes an ECC level of 'Q' -- or 25% redundant
    /// which gives an overall maximum size of 1'663 bytes
    /// </summary>
    /// <remarks>
    /// The symbol versions of QR Code range from Version 1 to Version 40. Each version has a different number of modules.
    /// (The module refers to the black or white dots that make up QR Code.) "Module configuration" refers to the number
    /// of modules contained in a symbol, commencing with Version 1 (21 × 21 modules) up to Version 40 (177 × 177 modules).
    /// Each higher version number comprises 4 additional modules per side.</remarks>
    private static int GetVersionForSize(int dataLength)
    {
        if (dataLength > 1663) throw new Exception($"Data for QR code exceeds 1663 byte limit ({dataLength} bytes supplied).");
        
        if (dataLength <= 11) return 1;
        if (dataLength <= 20) return 2;
        if (dataLength <= 32) return 3;
        if (dataLength <= 46) return 4;
        if (dataLength <= 60) return 5;
        if (dataLength <= 74) return 6;
        if (dataLength <= 86) return 7;
        if (dataLength <= 108) return 8;
        if (dataLength <= 130) return 9;
        if (dataLength <= 151) return 10;
        
        if (dataLength <= 177) return 11;
        if (dataLength <= 203) return 12;
        if (dataLength <= 241) return 13;
        if (dataLength <= 258) return 14;
        if (dataLength <= 292) return 15;
        if (dataLength <= 322) return 16;
        if (dataLength <= 364) return 17;
        if (dataLength <= 394) return 18;
        if (dataLength <= 442) return 19;
        if (dataLength <= 482) return 20;
        
        if (dataLength <= 509) return 21;
        if (dataLength <= 565) return 22;
        if (dataLength <= 611) return 23;
        if (dataLength <= 661) return 24;
        if (dataLength <= 715) return 25;
        if (dataLength <= 751) return 26;
        if (dataLength <= 805) return 27;
        if (dataLength <= 868) return 28;
        if (dataLength <= 908) return 29;
        if (dataLength <= 982) return 30;
        
        if (dataLength <= 1030) return 31;
        if (dataLength <= 1112) return 32;
        if (dataLength <= 1168) return 33;
        if (dataLength <= 1228) return 34;
        if (dataLength <= 1283) return 35;
        if (dataLength <= 1351) return 36;
        if (dataLength <= 1423) return 37;
        if (dataLength <= 1499) return 38;
        if (dataLength <= 1579) return 39;
        
        return 40;
    }

    /// <summary>
    /// QR code has a data length count. Each version and encoding can have a different bit length for this count.
    /// We only use byte mode to simplify this.
    /// </summary>
    private static int CountBits(int version) => version < 10 ? 8 : 16;
    
    private static FieldPoint[] CreateGaloisField()
    {
        var localGaloisField = new FieldPoint[256];

        int gfItem = 1;
        for (var i = 0; i < 256; i++)
        {
            localGaloisField[i] = new FieldPoint{ExponentAlpha = i, IntegerValue = gfItem};
            gfItem *= 2;
            if (gfItem > 255) gfItem ^= 285;
        }
        return localGaloisField;
    }
    
    private struct EccDataBlock
    {
        public List<byte[]> CodeWords = new();
        public List<byte[]> EccWords = new();

        public EccDataBlock() { }
    }

    internal struct FieldPoint
    {
        public int ExponentAlpha;
        public int IntegerValue;
    }
}

