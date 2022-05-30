// Derived from code found on https://www.codeproject.com/Articles/1250071/QR-Code-Encoder-and-Decoder-Csharp-Class-Library-f

using System;
using System.Linq;
using System.Text;

/////////////////////////////////////////////////////////////////////
//
//	QR Code Encoder Library
//
//	QR Code encoder.
//
//	Author: Uzi Granot
//
//	Current Version: 3.1.0
//	Date: March 17, 2022
//
//	Original Version: 1.0
//	Date: June 30, 2018

//	Copyright (C) 2018-2022 Uzi Granot. All Rights Reserved
//
//	QR Code Library C# class library and the attached test/demo
//  applications are free software.
//	Software developed by this author is licensed under CPOL 1.02.
//	Some portions of the QRCodeVideoDecoder are licensed under GNU Lesser
//	General Public License v3.0.
//
//	The main points of CPOL 1.02 subject to the terms of the License are:
//
//	Source Code and Executable Files can be used in commercial applications;
//	Source Code and Executable Files can be redistributed; and
//	Source Code can be modified to create derivative works.
//	No claim of suitability, guarantee, or any warranty whatsoever is
//	provided. The software is provided "as-is".
//	The Article accompanying the Work may not be distributed or republished
//	without the Author's consent
//
/////////////////////////////////////////////////////////////////////

namespace Form8snCore.HelpersAndConverters;

/// <summary>
/// QR Code error correction code enumeration
/// </summary>
public enum ErrorCorrection
{
    /// <summary>
    /// Low (01)
    /// </summary>
    L,

    /// <summary>
    /// Medium (00)
    /// </summary>
    M,

    /// <summary>
    /// Medium-high (11)
    /// </summary>
    Q,

    /// <summary>
    /// High (10)
    /// </summary>
    H,
}

/// <summary>
/// QR Code encoding modes (non-exhaustive)
/// </summary>
enum EncodingMode
{
    /// <summary>
    /// Numeric
    /// </summary>
    Numeric = 1,

    /// <summary>
    /// Alpha numeric
    /// </summary>
    AlphaNumeric = 2,

    /// <summary>
    /// byte encoding
    /// </summary>
    Byte = 4
}

/// <summary>
/// QR Code Encoder class
/// </summary>
public class QrEncoder
{
    /// <summary>
    /// QR code matrix (no quiet zone)
    /// Black module = true, White module = false
    /// </summary>
    public bool[,]? QrCodeMatrix { get; private set; }

    /// <summary>
    /// Gets QR Code matrix version
    /// </summary>
    public int QrCodeVersion { get; private set; }

    /// <summary>
    /// Gets QR Code matrix dimension in bits
    /// </summary>
    public int QrCodeDimension { get; private set; }

    public ErrorCorrection ErrorCorrectionLevel = ErrorCorrection.M;

    // private variables
    private byte[][] _dataSegArray;
    private int _encodedDataBits;
    private int _maxCodewords;
    private int _maxDataCodewords;
    private int _maxDataBits;
    private int _errCorrCodewords;
    private int _blocksGroup1;
    private int _dataCodewordsGroup1;
    private int _blocksGroup2;
    private int _dataCodewordsGroup2;
    private int _maskCode;
    private EncodingMode[] _encodingSegMode;
    private byte[] _codewordsArray;
    private int _codewordsPtr;
    private uint _bitBuffer;
    private int _bitBufferLen;
    private byte[,] _baseMatrix;
    private byte[,] _maskMatrix;
    private byte[,] _resultMatrix;

    /// <summary>
    /// Encode one string into QRCode boolean matrix
    /// </summary>
    /// <param name="stringDataSegment">string data segment</param>
    public bool[,] Encode(string stringDataSegment)
    {
        // empty
        if (string.IsNullOrEmpty(stringDataSegment))
            throw new ArgumentException("String data segment is null or missing");

        // convert string to byte array
        var binaryData = Encoding.UTF8.GetBytes(stringDataSegment);

        // encode data
        return Encode(new[] { binaryData });
    }

    /// <summary>
    /// Encode data segments array into QRCode boolean matrix
    /// </summary>
    /// <param name="dataSegArray">Data array of byte arrays</param>
    /// <returns>QR Code boolean matrix</returns>
    public bool[,] Encode
    (
        byte[][] dataSegArray
    )
    {
        // test data segments array
        if (dataSegArray == null || dataSegArray.Length == 0)
            throw new ArgumentException("Data segments argument is null or empty");

        // reset result variables
        QrCodeMatrix = null;
        QrCodeVersion = 0;
        QrCodeDimension = 0;

        // loop for all segments
        var bytes = dataSegArray.Sum(t => t.Length);

        if (bytes == 0) throw new ArgumentException("There is no data to encode.");

        // save data segments array
        _dataSegArray = dataSegArray;

        // initialization
        Initialization();

        // encode data
        EncodeData();

        // calculate error correction
        CalculateErrorCorrection();

        // interleave data and error correction codewords
        InterleaveBlocks();

        // build base matrix
        BuildBaseMatrix();

        // load base matrix with data and error correction codewords
        LoadMatrixWithData();

        // data masking
        SelectBestMask();

        // add format information (error code level and mask code)
        AddFormatInformation();

        // output matrix each element is one module
        QrCodeMatrix = new bool[QrCodeDimension, QrCodeDimension];

        // convert result matrix to output matrix
        // Black=true, White=false
        for (var row = 0; row < QrCodeDimension; row++)
        {
            for (var col = 0; col < QrCodeDimension; col++)
            {
                if ((_resultMatrix[row, col] & 1) != 0) QrCodeMatrix[row, col] = true;
            }
        }

        // exit
        return QrCodeMatrix;
    }

    public QrEncoder()
    {
        // We don't use any of these, but they keep the null access static analysis happy.
        _dataSegArray = new byte[][]{};
        _encodingSegMode = new EncodingMode[0];
        _codewordsArray = new byte[0];
        _baseMatrix = new byte[0,0];
        _maskMatrix = new byte[0,0];
        _resultMatrix = new byte[0,0];
    }

    // Initialization
    private void Initialization()
    {
        // create encoding mode array
        _encodingSegMode = new EncodingMode[_dataSegArray.Length];

        // reset total encoded data bits
        _encodedDataBits = 0;

        // loop for all segments
        for (var segIndex = 0; segIndex < _dataSegArray.Length; segIndex++)
        {
            // input string length
            var dataSeg = _dataSegArray[segIndex];
            var dataLength = dataSeg.Length;

            // find encoding mode
            var encodingMode = EncodingMode.Numeric;
            for (var index = 0; index < dataLength; index++)
            {
                int code = _lookupEncodingTable[dataSeg[index]];
                if (code < 10)
                    continue;
                if (code < 45)
                {
                    encodingMode = EncodingMode.AlphaNumeric;
                    continue;
                }

                encodingMode = EncodingMode.Byte;
                break;
            }

            // calculate required bit length
            var dataBits = 4;
            switch (encodingMode)
            {
                case EncodingMode.Numeric:
                    dataBits += 10 * (dataLength / 3);
                    if ((dataLength % 3) == 1) dataBits += 4;
                    else if ((dataLength % 3) == 2) dataBits += 7;
                    break;

                case EncodingMode.AlphaNumeric:
                    dataBits += 11 * (dataLength / 2);
                    if ((dataLength & 1) != 0) dataBits += 6;
                    break;

                case EncodingMode.Byte:
                    dataBits += 8 * dataLength;
                    break;
            }

            _encodingSegMode[segIndex] = encodingMode;
            _encodedDataBits += dataBits;
        }

        // find best version
        var totalDataLenBits = 0;
        for (QrCodeVersion = 1; QrCodeVersion <= 40; QrCodeVersion++)
        {
            // number of bits on each side of the QR code square
            QrCodeDimension = 17 + 4 * QrCodeVersion;

            SetDataCodewordsLength();
            totalDataLenBits = 0;
            for (var seg = 0; seg < _encodingSegMode.Length; seg++)
                totalDataLenBits += DataLengthBits(_encodingSegMode[seg]);
            if (_encodedDataBits + totalDataLenBits <= _maxDataBits) break;
        }

        if (QrCodeVersion > 40)
            throw new ApplicationException("Input data string is too long");
        _encodedDataBits += totalDataLenBits;
    }

    // QRCode: Convert data to bit array
    private void EncodeData()
    {
        // codewords array
        _codewordsArray = new byte[_maxCodewords];

        // reset encoding members
        _codewordsPtr = 0;
        _bitBuffer = 0;
        _bitBufferLen = 0;

        // loop for all segments
        for (var segIndex = 0; segIndex < _dataSegArray.Length; segIndex++)
        {
            // input string length
            var dataSeg = _dataSegArray[segIndex];
            var dataLength = dataSeg.Length;

            // first 4 bits is mode indicator
            // numeric code indicator is 0001, alpha numeric 0010, byte 0100
            SaveBitsToCodewordsArray((int)_encodingSegMode[segIndex], 4);

            // character count
            SaveBitsToCodewordsArray(dataLength, DataLengthBits(_encodingSegMode[segIndex]));

            // switch based on encode mode
            switch (_encodingSegMode[segIndex])
            {
                // numeric mode
                case EncodingMode.Numeric:
                    // encode digits in groups of 3
                    var numEnd = (dataLength / 3) * 3;
                    for (var index = 0; index < numEnd; index += 3)
                        SaveBitsToCodewordsArray(100 * _lookupEncodingTable[dataSeg[index]] +
                                                 10 * _lookupEncodingTable[dataSeg[index + 1]] +
                                                 _lookupEncodingTable[dataSeg[index + 2]], 10);

                    // we have one digit remaining
                    if (dataLength - numEnd == 1)
                        SaveBitsToCodewordsArray(_lookupEncodingTable[dataSeg[numEnd]], 4);

                    // we have two digits remaining
                    else if (dataLength - numEnd == 2)
                        SaveBitsToCodewordsArray(10 * _lookupEncodingTable[dataSeg[numEnd]] +
                                                 _lookupEncodingTable[dataSeg[numEnd + 1]], 7);
                    break;

                // alphanumeric mode
                case EncodingMode.AlphaNumeric:
                    // encode digits in groups of 2
                    var alphaNumEnd = (dataLength / 2) * 2;
                    for (var index = 0; index < alphaNumEnd; index += 2)
                        SaveBitsToCodewordsArray(45 * _lookupEncodingTable[dataSeg[index]] + _lookupEncodingTable[dataSeg[index + 1]], 11);

                    // we have one character remaining
                    if (dataLength - alphaNumEnd == 1)
                        SaveBitsToCodewordsArray(_lookupEncodingTable[dataSeg[alphaNumEnd]], 6);
                    break;


                // byte mode					
                case EncodingMode.Byte:
                    // append the data after mode and character count
                    for (var index = 0; index < dataLength; index++)
                        SaveBitsToCodewordsArray(dataSeg[index], 8);
                    break;
            }
        }

        // set terminator
        if (_encodedDataBits < _maxDataBits)
            SaveBitsToCodewordsArray(0, _maxDataBits - _encodedDataBits < 4 ? _maxDataBits - _encodedDataBits : 4);

        // flush bit buffer
        if (_bitBufferLen > 0)
            _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);

        // add extra padding if there is still space
        var padEnd = _maxDataCodewords - _codewordsPtr;
        for (var padPtr = 0; padPtr < padEnd; padPtr++)
            _codewordsArray[_codewordsPtr + padPtr] = (byte)((padPtr & 1) == 0 ? 0xEC : 0x11);
    }

    // Save data to codeword array
    private void SaveBitsToCodewordsArray
    (
        int data,
        int bits
    )
    {
        _bitBuffer |= (uint)data << (32 - _bitBufferLen - bits);
        _bitBufferLen += bits;
        while (_bitBufferLen >= 8)
        {
            _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);
            _bitBuffer <<= 8;
            _bitBufferLen -= 8;
        }
    }

    // Calculate Error Correction
    private void CalculateErrorCorrection()
    {
        // set generator polynomial array
        var generator = GenArray[_errCorrCodewords - 7];
        if (generator is null) throw new Exception($"ECC generator [{_errCorrCodewords - 7}] is invalid (code error)");

        // error correction calculation buffer
        var bufSize = Math.Max(_dataCodewordsGroup1, _dataCodewordsGroup2) + _errCorrCodewords;
        var errCorrBuff = new byte[bufSize];

        // initial number of data codewords
        var dataCodewords = _dataCodewordsGroup1;
        var buffLen = dataCodewords + _errCorrCodewords;

        // codewords pointer
        var dataCodewordsPtr = 0;

        // codewords buffer error correction pointer
        var codewordsArrayErrCorrPtr = _maxDataCodewords;

        // loop one block at a time
        var totalBlocks = _blocksGroup1 + _blocksGroup2;
        for (var blockNumber = 0; blockNumber < totalBlocks; blockNumber++)
        {
            // switch to group2 data codewords
            if (blockNumber == _blocksGroup1)
            {
                dataCodewords = _dataCodewordsGroup2;
                buffLen = dataCodewords + _errCorrCodewords;
            }

            // copy next block of codewords to the buffer and clear the remaining part
            Array.Copy(_codewordsArray, dataCodewordsPtr, errCorrBuff, 0, dataCodewords);
            Array.Clear(errCorrBuff, dataCodewords, _errCorrCodewords);

            // update codewords array to next buffer
            dataCodewordsPtr += dataCodewords;

            // error correction polynomial division
            PolynomialDivision(errCorrBuff, buffLen, generator, _errCorrCodewords);

            // save error correction block			
            Array.Copy(errCorrBuff, dataCodewords, _codewordsArray, codewordsArrayErrCorrPtr, _errCorrCodewords);
            codewordsArrayErrCorrPtr += _errCorrCodewords;
        }
    }

    // Polynomial division for error correction
    private static void PolynomialDivision
    (
        byte[] polynomial,
        int polyLength,
        byte[] generator,
        int errCorrCodewords
    )
    {
        var dataCodewords = polyLength - errCorrCodewords;

        // error correction polynomial division
        for (var index = 0; index < dataCodewords; index++)
        {
            // current first codeword is zero
            if (polynomial[index] == 0)
                continue;

            // current first codeword is not zero
            int multiplier = IntToExp[polynomial[index]];

            // loop for error correction coefficients
            for (var generatorIndex = 0; generatorIndex < errCorrCodewords; generatorIndex++)
            {
                polynomial[index + 1 + generatorIndex] = (byte)(polynomial[index + 1 + generatorIndex] ^ ExpToInt[generator[generatorIndex] + multiplier]);
            }
        }
    }

    // Interleave data and error correction blocks
    private void InterleaveBlocks()
    {
        // allocate temp codewords array
        var tempArray = new byte[_maxCodewords];

        // total blocks
        var totalBlocks = _blocksGroup1 + _blocksGroup2;

        // create array of data blocks starting point
        var start = new int[totalBlocks];
        for (var index = 1; index < totalBlocks; index++)
            start[index] = start[index - 1] + (index <= _blocksGroup1 ? _dataCodewordsGroup1 : _dataCodewordsGroup2);

        // step one. interleave base on group one length
        var ptrEnd = _dataCodewordsGroup1 * totalBlocks;

        // interleave group one and two
        int ptr;
        var block = 0;
        for (ptr = 0; ptr < ptrEnd; ptr++)
        {
            tempArray[ptr] = _codewordsArray[start[block]];
            start[block]++;
            block++;
            if (block == totalBlocks)
                block = 0;
        }

        // interleave group two
        if (_dataCodewordsGroup2 > _dataCodewordsGroup1)
        {
            // step one. interleave base on group one length
            ptrEnd = _maxDataCodewords;

            block = _blocksGroup1;
            for (; ptr < ptrEnd; ptr++)
            {
                tempArray[ptr] = _codewordsArray[start[block]];
                start[block]++;
                block++;
                if (block == totalBlocks)
                    block = _blocksGroup1;
            }
        }

        // create array of error correction blocks starting point
        start[0] = _maxDataCodewords;
        for (var index = 1; index < totalBlocks; index++)
            start[index] = start[index - 1] + _errCorrCodewords;

        // step one. interleave base on group one length

        // interleave all groups
        ptrEnd = _maxCodewords;
        block = 0;
        for (; ptr < ptrEnd; ptr++)
        {
            tempArray[ptr] = _codewordsArray[start[block]];
            start[block]++;
            block++;
            if (block == totalBlocks)
                block = 0;
        }

        // save result
        _codewordsArray = tempArray;
    }

    // Load base matrix with data and error correction codewords
    private void LoadMatrixWithData()
    {
        // input array pointer initialization
        var ptr = 0;
        var ptrEnd = 8 * _maxCodewords;

        // bottom right corner of output matrix
        var row = QrCodeDimension - 1;
        var col = QrCodeDimension - 1;

        // step state
        var state = 0;
        for (;;)
        {
            // current module is data
            if ((_baseMatrix[row, col] & NonData) == 0)
            {
                // load current module with
                if ((_codewordsArray[ptr >> 3] & (1 << (7 - (ptr & 7)))) != 0)
                    _baseMatrix[row, col] = DataBlack;
                if (++ptr == ptrEnd)
                    break;
            }

            // current module is non data and vertical timing line condition is on
            else if (col == 6)
                col--;

            // update matrix position to next module
            switch (state)
            {
                // going up: step one to the left
                case 0:
                    col--;
                    state = 1;
                    continue;

                // going up: step one row up and one column to the right
                case 1:
                    col++;
                    row--;
                    // we are not at the top, go to state 0
                    if (row >= 0)
                    {
                        state = 0;
                        continue;
                    }

                    // we are at the top, step two columns to the left and start going down
                    col -= 2;
                    row = 0;
                    state = 2;
                    continue;

                // going down: step one to the left
                case 2:
                    col--;
                    state = 3;
                    continue;

                // going down: step one row down and one column to the right
                case 3:
                    col++;
                    row++;
                    // we are not at the bottom, go to state 2
                    if (row < QrCodeDimension)
                    {
                        state = 2;
                        continue;
                    }

                    // we are at the bottom, step two columns to the left and start going up
                    col -= 2;
                    row = QrCodeDimension - 1;
                    state = 0;
                    continue;
            }
        }
    }

    // Select Mask
    private void SelectBestMask()
    {
        var bestScore = int.MaxValue;
        _maskCode = 0;

        for (var testMask = 0; testMask < 8; testMask++)
        {
            // apply mask
            ApplyMask(testMask);

            // evaluate 4 test conditions
            var score = EvaluationCondition1();
            if (score >= bestScore)
                continue;
            score += EvaluationCondition2();
            if (score >= bestScore)
                continue;
            score += EvaluationCondition3();
            if (score >= bestScore)
                continue;
            score += EvaluationCondition4();
            if (score >= bestScore)
                continue;

            // save as best mask so far
            _resultMatrix = _maskMatrix;
            bestScore = score;
            _maskCode = testMask;
        }
    }

    // Evaluation condition #1
    // 5 consecutive or more modules of the same color
    private int EvaluationCondition1()
    {
        var score = 0;

        // test rows
        for (var row = 0; row < QrCodeDimension; row++)
        {
            var count = 1;
            for (var col = 1; col < QrCodeDimension; col++)
            {
                // current cell is not the same color as the one before
                if (((_maskMatrix[row, col - 1] ^ _maskMatrix[row, col]) & 1) != 0)
                {
                    if (count >= 5)
                        score += count - 2;
                    count = 0;
                }

                count++;
            }

            // last run
            if (count >= 5)
                score += count - 2;
        }

        // test columns
        for (var col = 0; col < QrCodeDimension; col++)
        {
            var count = 1;
            for (var row = 1; row < QrCodeDimension; row++)
            {
                // current cell is not the same color as the one before
                if (((_maskMatrix[row - 1, col] ^ _maskMatrix[row, col]) & 1) != 0)
                {
                    if (count >= 5)
                        score += count - 2;
                    count = 0;
                }

                count++;
            }

            // last run
            if (count >= 5)
                score += count - 2;
        }

        return score;
    }

    // Evaluation condition #2
    // same color in 2 by 2 area
    private int EvaluationCondition2()
    {
        var score = 0;
        // test rows
        for (var row = 1; row < QrCodeDimension; row++)
        for (var col = 1; col < QrCodeDimension; col++)
        {
            // all are black
            if (((_maskMatrix[row - 1, col - 1] & _maskMatrix[row - 1, col] & _maskMatrix[row, col - 1] & _maskMatrix[row, col]) & 1) != 0)
                score += 3;

            // all are white
            else if (((_maskMatrix[row - 1, col - 1] | _maskMatrix[row - 1, col] | _maskMatrix[row, col - 1] | _maskMatrix[row, col]) & 1) == 0)
                score += 3;
        }

        return score;
    }

    // Evaluation condition #3
    // pattern dark, light, dark, dark, dark, light, dark
    // before or after 4 light modules
    private int EvaluationCondition3()
    {
        var score = 0;

        // test rows
        for (var row = 0; row < QrCodeDimension; row++)
        {
            var start = 0;

            // look for a light run of at least 4 modules
            for (var col = 0; col < QrCodeDimension; col++)
            {
                // current cell is white
                if ((_maskMatrix[row, col] & 1) == 0)
                    continue;

                // more or equal to 4
                if (col - start >= 4)
                {
                    // we have 4 or more white
                    // test for pattern before the white space
                    if (start >= 7 && TestHorizontalDarkLight(row, start - 7))
                        score += 40;

                    // test for pattern after the white space
                    if (QrCodeDimension - col >= 7 && TestHorizontalDarkLight(row, col))
                    {
                        score += 40;
                        col += 6;
                    }
                }

                // assume next one is white
                start = col + 1;
            }

            // last run
            if (QrCodeDimension - start >= 4 && start >= 7 && TestHorizontalDarkLight(row, start - 7))
                score += 40;
        }

        // test columns
        for (var col = 0; col < QrCodeDimension; col++)
        {
            var start = 0;

            // look for a light run of at least 4 modules
            for (var row = 0; row < QrCodeDimension; row++)
            {
                // current cell is white
                if ((_maskMatrix[row, col] & 1) == 0)
                    continue;

                // more or equal to 4
                if (row - start >= 4)
                {
                    // we have 4 or more white
                    // test for pattern before the white space
                    if (start >= 7 && TestVerticalDarkLight(start - 7, col))
                        score += 40;

                    // test for pattern after the white space
                    if (QrCodeDimension - row >= 7 && TestVerticalDarkLight(row, col))
                    {
                        score += 40;
                        row += 6;
                    }
                }

                // assume next one is white
                start = row + 1;
            }

            // last run
            if (QrCodeDimension - start >= 4 && start >= 7 && TestVerticalDarkLight(start - 7, col))
                score += 40;
        }

        // exit
        return score;
    }

    // Evaluation condition #4
    // black to white ratio
    private int EvaluationCondition4()
    {
        // count black cells
        var black = 0;
        for (var row = 0; row < QrCodeDimension; row++)
        for (var col = 0; col < QrCodeDimension; col++)
            if ((_maskMatrix[row, col] & 1) != 0)
                black++;

        // ratio
        var ratio = black / (double)(QrCodeDimension * QrCodeDimension);

        // there are more black than white
        if (ratio > 0.55)
            return (int)(20.0 * (ratio - 0.5)) * 10;
        if (ratio < 0.45)
            return (int)(20.0 * (0.5 - ratio)) * 10;
        return 0;
    }

    // Test horizontal dark light pattern
    private bool TestHorizontalDarkLight
    (
        int row,
        int col
    )
    {
        return (_maskMatrix[row, col] & ~_maskMatrix[row, col + 1] & _maskMatrix[row, col + 2] & _maskMatrix[row, col + 3] &
                _maskMatrix[row, col + 4] & ~_maskMatrix[row, col + 5] & _maskMatrix[row, col + 6] & 1) != 0;
    }

    ////////////////////////////////////////////////////////////////////
    // Test vertical dark light pattern
    ////////////////////////////////////////////////////////////////////
    private bool TestVerticalDarkLight
    (
        int row,
        int col
    )
    {
        return (_maskMatrix[row, col] & ~_maskMatrix[row + 1, col] & _maskMatrix[row + 2, col] & _maskMatrix[row + 3, col] &
                _maskMatrix[row + 4, col] & ~_maskMatrix[row + 5, col] & _maskMatrix[row + 6, col] & 1) != 0;
    }

    // Add format information
    // version, error correction code plus mask code
    private void AddFormatInformation()
    {
        int mask;

        // version information
        if (QrCodeVersion >= 7)
        {
            var pos = QrCodeDimension - 11;
            var verInfo = VersionCodeArray[QrCodeVersion - 7];

            // top right
            mask = 1;
            for (var row = 0; row < 6; row++)
            for (var col = 0; col < 3; col++)
            {
                _resultMatrix[row, pos + col] = (verInfo & mask) != 0 ? FixedBlack : FixedWhite;
                mask <<= 1;
            }

            // bottom left
            mask = 1;
            for (var col = 0; col < 6; col++)
            for (var row = 0; row < 3; row++)
            {
                _resultMatrix[pos + row, col] = (verInfo & mask) != 0 ? FixedBlack : FixedWhite;
                mask <<= 1;
            }
        }

        // error correction code and mask number
        var formatInfoPtr = 0; // M is the default
        switch (ErrorCorrectionLevel)
        {
            case ErrorCorrection.L:
                formatInfoPtr = 8;
                break;

            case ErrorCorrection.Q:
                formatInfoPtr = 24;
                break;

            case ErrorCorrection.H:
                formatInfoPtr = 16;
                break;
        }

        var formatInfo = FormatInfoArray[formatInfoPtr + _maskCode];

        // load format bits into result matrix
        mask = 1;
        for (var index = 0; index < 15; index++)
        {
            int formatBit = (formatInfo & mask) != 0 ? FixedBlack : FixedWhite;
            mask <<= 1;

            // top left corner
            _resultMatrix[FormatInfoOne[index, 0], FormatInfoOne[index, 1]] = (byte)formatBit;

            // bottom left and top right corners
            var row = FormatInfoTwo[index, 0];
            if (row < 0)
                row += QrCodeDimension;
            var col = FormatInfoTwo[index, 1];
            if (col < 0)
                col += QrCodeDimension;
            _resultMatrix[row, col] = (byte)formatBit;
        }
    }

    // Set encoded data bits length
    private int DataLengthBits
    (
        EncodingMode encodingMode
    )
    {
        // numeric mode
        if (encodingMode == EncodingMode.Numeric)
            return QrCodeVersion < 10 ? 10 : (QrCodeVersion < 27 ? 12 : 14);

        // alpha numeric mode
        if (encodingMode == EncodingMode.AlphaNumeric)
            return QrCodeVersion < 10 ? 9 : (QrCodeVersion < 27 ? 11 : 13);

        // byte mode
        if (encodingMode == EncodingMode.Byte)
            return QrCodeVersion < 10 ? 8 : 16;

        // error
        throw new Exception("Encoding mode error");
    }

    // Set data and error correction codewords length
    private void SetDataCodewordsLength()
    {
        // index shortcut
        var blockInfoIndex = (QrCodeVersion - 1) * 4 + (int)ErrorCorrectionLevel;

        // Number of blocks in group 1
        _blocksGroup1 = _lookupEcBlockInfo[blockInfoIndex, BlocksGroup1Index];

        // Number of data codewords in blocks of group 1
        _dataCodewordsGroup1 = _lookupEcBlockInfo[blockInfoIndex, DataCodewordsGroup1Index];

        // Number of blocks in group 2
        _blocksGroup2 = _lookupEcBlockInfo[blockInfoIndex, BlocksGroup2Index];

        // Number of data codewords in blocks of group 2
        _dataCodewordsGroup2 = _lookupEcBlockInfo[blockInfoIndex, DataCodewordsGroup2Index];

        // Total number of data codewords for this version and EC level
        _maxDataCodewords = _blocksGroup1 * _dataCodewordsGroup1 + _blocksGroup2 * _dataCodewordsGroup2;
        _maxDataBits = 8 * _maxDataCodewords;

        // total data plus error correction bits
        _maxCodewords = _lookupMaxCodewordsArray[QrCodeVersion];

        // Error correction codewords per block
        _errCorrCodewords = (_maxCodewords - _maxDataCodewords) / (_blocksGroup1 + _blocksGroup2);
    }

    // Build Base Matrix
    private void BuildBaseMatrix()
    {
        // allocate base matrix
        _baseMatrix = new byte[QrCodeDimension + 5, QrCodeDimension + 5];

        // top left finder patterns
        for (var row = 0; row < 9; row++)
        for (var col = 0; col < 9; col++)
            _baseMatrix[row, col] = FinderPatternTopLeft[row, col];

        // top right finder patterns
        var pos = QrCodeDimension - 8;
        for (var row = 0; row < 9; row++)
        for (var col = 0; col < 8; col++)
            _baseMatrix[row, pos + col] = FinderPatternTopRight[row, col];

        // bottom left finder patterns
        for (var row = 0; row < 8; row++)
        for (var col = 0; col < 9; col++)
            _baseMatrix[pos + row, col] = FinderPatternBottomLeft[row, col];

        // Timing pattern
        for (var z = 8; z < QrCodeDimension - 8; z++)
            _baseMatrix[z, 6] = _baseMatrix[6, z] = (z & 1) == 0 ? FixedBlack : FixedWhite;

        // alignment pattern
        if (QrCodeVersion > 1)
        {
            var alignPos = _lookupAlignmentPositionArray[QrCodeVersion];
            var alignmentDimension = alignPos.Length;
            for (var row = 0; row < alignmentDimension; row++)
            for (var col = 0; col < alignmentDimension; col++)
            {
                if (col == 0 && row == 0 || col == alignmentDimension - 1 && row == 0 || col == 0 && row == alignmentDimension - 1)
                    continue;

                int posRow = alignPos[row];
                int posCol = alignPos[col];
                for (var aRow = -2; aRow < 3; aRow++)
                for (var aCol = -2; aCol < 3; aCol++)
                {
                    _baseMatrix[posRow + aRow, posCol + aCol] = AlignmentPattern[aRow + 2, aCol + 2];
                }
            }
        }

        // reserve version information
        if (QrCodeVersion >= 7)
        {
            // position of 3 by 6 rectangles
            pos = QrCodeDimension - 11;

            // top right
            for (var row = 0; row < 6; row++)
            for (var col = 0; col < 3; col++)
                _baseMatrix[row, pos + col] = FormatWhite;

            // bottom right
            for (var col = 0; col < 6; col++)
            for (var row = 0; row < 3; row++)
                _baseMatrix[pos + row, col] = FormatWhite;
        }
    }

    // Apply Mask
    private void ApplyMask
    (
        int mask
    )
    {
        _maskMatrix = (byte[,])_baseMatrix.Clone();
        switch (mask)
        {
            case 0:
                ApplyMask0();
                break;

            case 1:
                ApplyMask1();
                break;

            case 2:
                ApplyMask2();
                break;

            case 3:
                ApplyMask3();
                break;

            case 4:
                ApplyMask4();
                break;

            case 5:
                ApplyMask5();
                break;

            case 6:
                ApplyMask6();
                break;

            case 7:
                ApplyMask7();
                break;
        }
    }

    // Apply Mask 0
    // (row + column) % 2 == 0
    private void ApplyMask0()
    {
        for (var row = 0; row < QrCodeDimension; row += 2)
        for (var col = 0; col < QrCodeDimension; col += 2)
        {
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
            if ((_maskMatrix[row + 1, col + 1] & NonData) == 0)
                _maskMatrix[row + 1, col + 1] ^= 1;
        }
    }

    // Apply Mask 1
    // row % 2 == 0
    private void ApplyMask1()
    {
        for (var row = 0; row < QrCodeDimension; row += 2)
        for (var col = 0; col < QrCodeDimension; col++)
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
    }

    // Apply Mask 2
    // column % 3 == 0
    private void ApplyMask2()
    {
        for (var row = 0; row < QrCodeDimension; row++)
        for (var col = 0; col < QrCodeDimension; col += 3)
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
    }

    // Apply Mask 3
    // (row + column) % 3 == 0
    private void ApplyMask3()
    {
        for (var row = 0; row < QrCodeDimension; row += 3)
        for (var col = 0; col < QrCodeDimension; col += 3)
        {
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
            if ((_maskMatrix[row + 1, col + 2] & NonData) == 0)
                _maskMatrix[row + 1, col + 2] ^= 1;
            if ((_maskMatrix[row + 2, col + 1] & NonData) == 0)
                _maskMatrix[row + 2, col + 1] ^= 1;
        }
    }

    // Apply Mask 4
    // ((row / 2) + (column / 3)) % 2 == 0
    private void ApplyMask4()
    {
        for (var row = 0; row < QrCodeDimension; row += 4)
        for (var col = 0; col < QrCodeDimension; col += 6)
        {
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
            if ((_maskMatrix[row, col + 1] & NonData) == 0)
                _maskMatrix[row, col + 1] ^= 1;
            if ((_maskMatrix[row, col + 2] & NonData) == 0)
                _maskMatrix[row, col + 2] ^= 1;

            if ((_maskMatrix[row + 1, col] & NonData) == 0)
                _maskMatrix[row + 1, col] ^= 1;
            if ((_maskMatrix[row + 1, col + 1] & NonData) == 0)
                _maskMatrix[row + 1, col + 1] ^= 1;
            if ((_maskMatrix[row + 1, col + 2] & NonData) == 0)
                _maskMatrix[row + 1, col + 2] ^= 1;

            if ((_maskMatrix[row + 2, col + 3] & NonData) == 0)
                _maskMatrix[row + 2, col + 3] ^= 1;
            if ((_maskMatrix[row + 2, col + 4] & NonData) == 0)
                _maskMatrix[row + 2, col + 4] ^= 1;
            if ((_maskMatrix[row + 2, col + 5] & NonData) == 0)
                _maskMatrix[row + 2, col + 5] ^= 1;

            if ((_maskMatrix[row + 3, col + 3] & NonData) == 0)
                _maskMatrix[row + 3, col + 3] ^= 1;
            if ((_maskMatrix[row + 3, col + 4] & NonData) == 0)
                _maskMatrix[row + 3, col + 4] ^= 1;
            if ((_maskMatrix[row + 3, col + 5] & NonData) == 0)
                _maskMatrix[row + 3, col + 5] ^= 1;
        }
    }

    // Apply Mask 5
    // ((row * column) % 2) + ((row * column) % 3) == 0
    private void ApplyMask5()
    {
        for (var row = 0; row < QrCodeDimension; row += 6)
        for (var col = 0; col < QrCodeDimension; col += 6)
        {
            for (var delta = 0; delta < 6; delta++)
                if ((_maskMatrix[row, col + delta] & NonData) == 0)
                    _maskMatrix[row, col + delta] ^= 1;
            for (var delta = 1; delta < 6; delta++)
                if ((_maskMatrix[row + delta, col] & NonData) == 0)
                    _maskMatrix[row + delta, col] ^= 1;
            if ((_maskMatrix[row + 2, col + 3] & NonData) == 0)
                _maskMatrix[row + 2, col + 3] ^= 1;
            if ((_maskMatrix[row + 3, col + 2] & NonData) == 0)
                _maskMatrix[row + 3, col + 2] ^= 1;
            if ((_maskMatrix[row + 3, col + 4] & NonData) == 0)
                _maskMatrix[row + 3, col + 4] ^= 1;
            if ((_maskMatrix[row + 4, col + 3] & NonData) == 0)
                _maskMatrix[row + 4, col + 3] ^= 1;
        }
    }

    // Apply Mask 6
    // (((row * column) % 2) + ((row * column) mod 3)) mod 2 == 0
    private void ApplyMask6()
    {
        for (var row = 0; row < QrCodeDimension; row += 6)
        for (var col = 0; col < QrCodeDimension; col += 6)
        {
            for (var delta = 0; delta < 6; delta++)
                if ((_maskMatrix[row, col + delta] & NonData) == 0)
                    _maskMatrix[row, col + delta] ^= 1;
            for (var delta = 1; delta < 6; delta++)
                if ((_maskMatrix[row + delta, col] & NonData) == 0)
                    _maskMatrix[row + delta, col] ^= 1;
            if ((_maskMatrix[row + 1, col + 1] & NonData) == 0)
                _maskMatrix[row + 1, col + 1] ^= 1;
            if ((_maskMatrix[row + 1, col + 2] & NonData) == 0)
                _maskMatrix[row + 1, col + 2] ^= 1;
            if ((_maskMatrix[row + 2, col + 1] & NonData) == 0)
                _maskMatrix[row + 2, col + 1] ^= 1;
            if ((_maskMatrix[row + 2, col + 3] & NonData) == 0)
                _maskMatrix[row + 2, col + 3] ^= 1;
            if ((_maskMatrix[row + 2, col + 4] & NonData) == 0)
                _maskMatrix[row + 2, col + 4] ^= 1;
            if ((_maskMatrix[row + 3, col + 2] & NonData) == 0)
                _maskMatrix[row + 3, col + 2] ^= 1;
            if ((_maskMatrix[row + 3, col + 4] & NonData) == 0)
                _maskMatrix[row + 3, col + 4] ^= 1;
            if ((_maskMatrix[row + 4, col + 2] & NonData) == 0)
                _maskMatrix[row + 4, col + 2] ^= 1;
            if ((_maskMatrix[row + 4, col + 3] & NonData) == 0)
                _maskMatrix[row + 4, col + 3] ^= 1;
            if ((_maskMatrix[row + 4, col + 5] & NonData) == 0)
                _maskMatrix[row + 4, col + 5] ^= 1;
            if ((_maskMatrix[row + 5, col + 4] & NonData) == 0)
                _maskMatrix[row + 5, col + 4] ^= 1;
            if ((_maskMatrix[row + 5, col + 5] & NonData) == 0)
                _maskMatrix[row + 5, col + 5] ^= 1;
        }
    }

    // Apply Mask 7
    // (((row + column) % 2) + ((row * column) mod 3)) mod 2 == 0
    private void ApplyMask7()
    {
        for (var row = 0; row < QrCodeDimension; row += 6)
        for (var col = 0; col < QrCodeDimension; col += 6)
        {
            if ((_maskMatrix[row, col] & NonData) == 0)
                _maskMatrix[row, col] ^= 1;
            if ((_maskMatrix[row, col + 2] & NonData) == 0)
                _maskMatrix[row, col + 2] ^= 1;
            if ((_maskMatrix[row, col + 4] & NonData) == 0)
                _maskMatrix[row, col + 4] ^= 1;

            if ((_maskMatrix[row + 1, col + 3] & NonData) == 0)
                _maskMatrix[row + 1, col + 3] ^= 1;
            if ((_maskMatrix[row + 1, col + 4] & NonData) == 0)
                _maskMatrix[row + 1, col + 4] ^= 1;
            if ((_maskMatrix[row + 1, col + 5] & NonData) == 0)
                _maskMatrix[row + 1, col + 5] ^= 1;

            if ((_maskMatrix[row + 2, col] & NonData) == 0)
                _maskMatrix[row + 2, col] ^= 1;
            if ((_maskMatrix[row + 2, col + 4] & NonData) == 0)
                _maskMatrix[row + 2, col + 4] ^= 1;
            if ((_maskMatrix[row + 2, col + 5] & NonData) == 0)
                _maskMatrix[row + 2, col + 5] ^= 1;

            if ((_maskMatrix[row + 3, col + 1] & NonData) == 0)
                _maskMatrix[row + 3, col + 1] ^= 1;
            if ((_maskMatrix[row + 3, col + 3] & NonData) == 0)
                _maskMatrix[row + 3, col + 3] ^= 1;
            if ((_maskMatrix[row + 3, col + 5] & NonData) == 0)
                _maskMatrix[row + 3, col + 5] ^= 1;

            if ((_maskMatrix[row + 4, col] & NonData) == 0)
                _maskMatrix[row + 4, col] ^= 1;
            if ((_maskMatrix[row + 4, col + 1] & NonData) == 0)
                _maskMatrix[row + 4, col + 1] ^= 1;
            if ((_maskMatrix[row + 4, col + 2] & NonData) == 0)
                _maskMatrix[row + 4, col + 2] ^= 1;

            if ((_maskMatrix[row + 5, col + 1] & NonData) == 0)
                _maskMatrix[row + 5, col + 1] ^= 1;
            if ((_maskMatrix[row + 5, col + 2] & NonData) == 0)
                _maskMatrix[row + 5, col + 2] ^= 1;
            if ((_maskMatrix[row + 5, col + 3] & NonData) == 0)
                _maskMatrix[row + 5, col + 3] ^= 1;
        }
    }

    // alignment symbols position as function of dimension
    private static readonly byte[][] _lookupAlignmentPositionArray =
    {
        new byte[0],
        new byte[0],
        new byte[] { 6, 18 },
        new byte[] { 6, 22 },
        new byte[] { 6, 26 },
        new byte[] { 6, 30 },
        new byte[] { 6, 34 },
        new byte[] { 6, 22, 38 },
        new byte[] { 6, 24, 42 },
        new byte[] { 6, 26, 46 },
        new byte[] { 6, 28, 50 },
        new byte[] { 6, 30, 54 },
        new byte[] { 6, 32, 58 },
        new byte[] { 6, 34, 62 },
        new byte[] { 6, 26, 46, 66 },
        new byte[] { 6, 26, 48, 70 },
        new byte[] { 6, 26, 50, 74 },
        new byte[] { 6, 30, 54, 78 },
        new byte[] { 6, 30, 56, 82 },
        new byte[] { 6, 30, 58, 86 },
        new byte[] { 6, 34, 62, 90 },
        new byte[] { 6, 28, 50, 72, 94 },
        new byte[] { 6, 26, 50, 74, 98 },
        new byte[] { 6, 30, 54, 78, 102 },
        new byte[] { 6, 28, 54, 80, 106 },
        new byte[] { 6, 32, 58, 84, 110 },
        new byte[] { 6, 30, 58, 86, 114 },
        new byte[] { 6, 34, 62, 90, 118 },
        new byte[] { 6, 26, 50, 74, 98, 122 },
        new byte[] { 6, 30, 54, 78, 102, 126 },
        new byte[] { 6, 26, 52, 78, 104, 130 },
        new byte[] { 6, 30, 56, 82, 108, 134 },
        new byte[] { 6, 34, 60, 86, 112, 138 },
        new byte[] { 6, 30, 58, 86, 114, 142 },
        new byte[] { 6, 34, 62, 90, 118, 146 },
        new byte[] { 6, 30, 54, 78, 102, 126, 150 },
        new byte[] { 6, 24, 50, 76, 102, 128, 154 },
        new byte[] { 6, 28, 54, 80, 106, 132, 158 },
        new byte[] { 6, 32, 58, 84, 110, 136, 162 },
        new byte[] { 6, 26, 54, 82, 110, 138, 166 },
        new byte[] { 6, 30, 58, 86, 114, 142, 170 },
    };

    // maximum code words as function of dimension
    private static readonly int[] _lookupMaxCodewordsArray =
    {
        0,
        26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
        404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
        1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
        2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
    };

    // Character sets:
    // 1) numeric data (digits 0 - 9);
    // 2) alphanumeric data (digits 0 - 9; upper case letters A -Z; nine other characters: space, $ % * + - . / : );
    // 3) 8-bit byte data (JIS 8-bit character set (Latin and Kana) in accordance with JIS X 0201);
    // 4) Kanji characters (Shift JIS character set in accordance with JIS X 0208 Annex 1 Shift Coded
    //    Representation. Note that Kanji characters in QR Code can have values which can be compacted into 13 bits.)

    private static readonly byte[] _lookupEncodingTable =
    {
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        36, 45, 45, 45, 37, 38, 45, 45, 45, 45, 39, 40, 45, 41, 42, 43,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 44, 45, 45, 45, 45, 45,
        45, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
        45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
    };

    // Error correction block information
    // A-Number of blocks in group 1
    private const int BlocksGroup1Index = 0;

    // B-Number of data codewords in blocks of group 1
    private const int DataCodewordsGroup1Index = 1;

    // C-Number of blocks in group 2
    private const int BlocksGroup2Index = 2;

    // D-Number of data codewords in blocks of group 2
    private const int DataCodewordsGroup2Index = 3;

    private static readonly byte[,] _lookupEcBlockInfo =
    {
        // A,   B,   C,   D 
        { 1, 19, 0, 0 }, // 1-L
        { 1, 16, 0, 0 }, // 1-M
        { 1, 13, 0, 0 }, // 1-Q
        { 1, 9, 0, 0 }, // 1-H
        { 1, 34, 0, 0 }, // 2-L
        { 1, 28, 0, 0 }, // 2-M
        { 1, 22, 0, 0 }, // 2-Q
        { 1, 16, 0, 0 }, // 2-H
        { 1, 55, 0, 0 }, // 3-L
        { 1, 44, 0, 0 }, // 3-M
        { 2, 17, 0, 0 }, // 3-Q
        { 2, 13, 0, 0 }, // 3-H
        { 1, 80, 0, 0 }, // 4-L
        { 2, 32, 0, 0 }, // 4-M
        { 2, 24, 0, 0 }, // 4-Q
        { 4, 9, 0, 0 }, // 4-H
        { 1, 108, 0, 0 }, // 5-L
        { 2, 43, 0, 0 }, // 5-M
        { 2, 15, 2, 16 }, // 5-Q
        { 2, 11, 2, 12 }, // 5-H
        { 2, 68, 0, 0 }, // 6-L
        { 4, 27, 0, 0 }, // 6-M
        { 4, 19, 0, 0 }, // 6-Q
        { 4, 15, 0, 0 }, // 6-H
        { 2, 78, 0, 0 }, // 7-L
        { 4, 31, 0, 0 }, // 7-M
        { 2, 14, 4, 15 }, // 7-Q
        { 4, 13, 1, 14 }, // 7-H
        { 2, 97, 0, 0 }, // 8-L
        { 2, 38, 2, 39 }, // 8-M
        { 4, 18, 2, 19 }, // 8-Q
        { 4, 14, 2, 15 }, // 8-H
        { 2, 116, 0, 0 }, // 9-L
        { 3, 36, 2, 37 }, // 9-M
        { 4, 16, 4, 17 }, // 9-Q
        { 4, 12, 4, 13 }, // 9-H
        { 2, 68, 2, 69 }, // 10-L
        { 4, 43, 1, 44 }, // 10-M
        { 6, 19, 2, 20 }, // 10-Q
        { 6, 15, 2, 16 }, // 10-H
        { 4, 81, 0, 0 }, // 11-L
        { 1, 50, 4, 51 }, // 11-M
        { 4, 22, 4, 23 }, // 11-Q
        { 3, 12, 8, 13 }, // 11-H
        { 2, 92, 2, 93 }, // 12-L
        { 6, 36, 2, 37 }, // 12-M
        { 4, 20, 6, 21 }, // 12-Q
        { 7, 14, 4, 15 }, // 12-H
        { 4, 107, 0, 0 }, // 13-L
        { 8, 37, 1, 38 }, // 13-M
        { 8, 20, 4, 21 }, // 13-Q
        { 12, 11, 4, 12 }, // 13-H
        { 3, 115, 1, 116 }, // 14-L
        { 4, 40, 5, 41 }, // 14-M
        { 11, 16, 5, 17 }, // 14-Q
        { 11, 12, 5, 13 }, // 14-H
        { 5, 87, 1, 88 }, // 15-L
        { 5, 41, 5, 42 }, // 15-M
        { 5, 24, 7, 25 }, // 15-Q
        { 11, 12, 7, 13 }, // 15-H
        { 5, 98, 1, 99 }, // 16-L
        { 7, 45, 3, 46 }, // 16-M
        { 15, 19, 2, 20 }, // 16-Q
        { 3, 15, 13, 16 }, // 16-H
        { 1, 107, 5, 108 }, // 17-L
        { 10, 46, 1, 47 }, // 17-M
        { 1, 22, 15, 23 }, // 17-Q
        { 2, 14, 17, 15 }, // 17-H
        { 5, 120, 1, 121 }, // 18-L
        { 9, 43, 4, 44 }, // 18-M
        { 17, 22, 1, 23 }, // 18-Q
        { 2, 14, 19, 15 }, // 18-H
        { 3, 113, 4, 114 }, // 19-L
        { 3, 44, 11, 45 }, // 19-M
        { 17, 21, 4, 22 }, // 19-Q
        { 9, 13, 16, 14 }, // 19-H
        { 3, 107, 5, 108 }, // 20-L
        { 3, 41, 13, 42 }, // 20-M
        { 15, 24, 5, 25 }, // 20-Q
        { 15, 15, 10, 16 }, // 20-H
        { 4, 116, 4, 117 }, // 21-L
        { 17, 42, 0, 0 }, // 21-M
        { 17, 22, 6, 23 }, // 21-Q
        { 19, 16, 6, 17 }, // 21-H
        { 2, 111, 7, 112 }, // 22-L
        { 17, 46, 0, 0 }, // 22-M
        { 7, 24, 16, 25 }, // 22-Q
        { 34, 13, 0, 0 }, // 22-H
        { 4, 121, 5, 122 }, // 23-L
        { 4, 47, 14, 48 }, // 23-M
        { 11, 24, 14, 25 }, // 23-Q
        { 16, 15, 14, 16 }, // 23-H
        { 6, 117, 4, 118 }, // 24-L
        { 6, 45, 14, 46 }, // 24-M
        { 11, 24, 16, 25 }, // 24-Q
        { 30, 16, 2, 17 }, // 24-H
        { 8, 106, 4, 107 }, // 25-L
        { 8, 47, 13, 48 }, // 25-M
        { 7, 24, 22, 25 }, // 25-Q
        { 22, 15, 13, 16 }, // 25-H
        { 10, 114, 2, 115 }, // 26-L
        { 19, 46, 4, 47 }, // 26-M
        { 28, 22, 6, 23 }, // 26-Q
        { 33, 16, 4, 17 }, // 26-H
        { 8, 122, 4, 123 }, // 27-L
        { 22, 45, 3, 46 }, // 27-M
        { 8, 23, 26, 24 }, // 27-Q
        { 12, 15, 28, 16 }, // 27-H
        { 3, 117, 10, 118 }, // 28-L
        { 3, 45, 23, 46 }, // 28-M
        { 4, 24, 31, 25 }, // 28-Q
        { 11, 15, 31, 16 }, // 28-H
        { 7, 116, 7, 117 }, // 29-L
        { 21, 45, 7, 46 }, // 29-M
        { 1, 23, 37, 24 }, // 29-Q
        { 19, 15, 26, 16 }, // 29-H
        { 5, 115, 10, 116 }, // 30-L
        { 19, 47, 10, 48 }, // 30-M
        { 15, 24, 25, 25 }, // 30-Q
        { 23, 15, 25, 16 }, // 30-H
        { 13, 115, 3, 116 }, // 31-L
        { 2, 46, 29, 47 }, // 31-M
        { 42, 24, 1, 25 }, // 31-Q
        { 23, 15, 28, 16 }, // 31-H
        { 17, 115, 0, 0 }, // 32-L
        { 10, 46, 23, 47 }, // 32-M
        { 10, 24, 35, 25 }, // 32-Q
        { 19, 15, 35, 16 }, // 32-H
        { 17, 115, 1, 116 }, // 33-L
        { 14, 46, 21, 47 }, // 33-M
        { 29, 24, 19, 25 }, // 33-Q
        { 11, 15, 46, 16 }, // 33-H
        { 13, 115, 6, 116 }, // 34-L
        { 14, 46, 23, 47 }, // 34-M
        { 44, 24, 7, 25 }, // 34-Q
        { 59, 16, 1, 17 }, // 34-H
        { 12, 121, 7, 122 }, // 35-L
        { 12, 47, 26, 48 }, // 35-M
        { 39, 24, 14, 25 }, // 35-Q
        { 22, 15, 41, 16 }, // 35-H
        { 6, 121, 14, 122 }, // 36-L
        { 6, 47, 34, 48 }, // 36-M
        { 46, 24, 10, 25 }, // 36-Q
        { 2, 15, 64, 16 }, // 36-H
        { 17, 122, 4, 123 }, // 37-L
        { 29, 46, 14, 47 }, // 37-M
        { 49, 24, 10, 25 }, // 37-Q
        { 24, 15, 46, 16 }, // 37-H
        { 4, 122, 18, 123 }, // 38-L
        { 13, 46, 32, 47 }, // 38-M
        { 48, 24, 14, 25 }, // 38-Q
        { 42, 15, 32, 16 }, // 38-H
        { 20, 117, 4, 118 }, // 39-L
        { 40, 47, 7, 48 }, // 39-M
        { 43, 24, 22, 25 }, // 39-Q
        { 10, 15, 67, 16 }, // 39-H
        { 19, 118, 6, 119 }, // 40-L
        { 18, 47, 31, 48 }, // 40-M
        { 34, 24, 34, 25 }, // 40-Q
        { 20, 15, 61, 16 }, // 40-H
    };

    private static readonly byte[] _generator7 =
        { 87, 229, 146, 149, 238, 102, 21 };

    private static readonly byte[] _generator10 =
        { 251, 67, 46, 61, 118, 70, 64, 94, 32, 45 };

    private static readonly byte[] _generator13 =
        { 74, 152, 176, 100, 86, 100, 106, 104, 130, 218, 206, 140, 78 };

    private static readonly byte[] _generator15 =
        { 8, 183, 61, 91, 202, 37, 51, 58, 58, 237, 140, 124, 5, 99, 105 };

    private static readonly byte[] _generator16 =
        { 120, 104, 107, 109, 102, 161, 76, 3, 91, 191, 147, 169, 182, 194, 225, 120 };

    private static readonly byte[] _generator17 =
    {
        43, 139, 206, 78, 43, 239, 123, 206, 214, 147, 24, 99, 150, 39, 243, 163,
        136
    };

    private static readonly byte[] _generator18 =
    {
        215, 234, 158, 94, 184, 97, 118, 170, 79, 187, 152, 148, 252, 179, 5, 98,
        96, 153
    };

    private static readonly byte[] _generator20 =
    {
        17, 60, 79, 50, 61, 163, 26, 187, 202, 180, 221, 225, 83, 239, 156, 164,
        212, 212, 188, 190
    };

    private static readonly byte[] _generator22 =
    {
        210, 171, 247, 242, 93, 230, 14, 109, 221, 53, 200, 74, 8, 172, 98, 80,
        219, 134, 160, 105, 165, 231
    };

    private static readonly byte[] _generator24 =
    {
        229, 121, 135, 48, 211, 117, 251, 126, 159, 180, 169, 152, 192, 226, 228, 218,
        111, 0, 117, 232, 87, 96, 227, 21
    };

    private static readonly byte[] _generator26 =
    {
        173, 125, 158, 2, 103, 182, 118, 17, 145, 201, 111, 28, 165, 53, 161, 21,
        245, 142, 13, 102, 48, 227, 153, 145, 218, 70
    };

    private static readonly byte[] _generator28 =
    {
        168, 223, 200, 104, 224, 234, 108, 180, 110, 190, 195, 147, 205, 27, 232, 201,
        21, 43, 245, 87, 42, 195, 212, 119, 242, 37, 9, 123
    };

    private static readonly byte[] _generator30 =
    {
        41, 173, 145, 152, 216, 31, 179, 182, 50, 48, 110, 86, 239, 96, 222, 125,
        42, 173, 226, 193, 224, 130, 156, 37, 251, 216, 238, 40, 192, 180
    };

    private static readonly byte[] _generator32 =
    {
        10, 6, 106, 190, 249, 167, 4, 67, 209, 138, 138, 32, 242, 123, 89, 27,
        120, 185, 80, 156, 38, 60, 171, 60, 28, 222, 80, 52, 254, 185, 220, 241
    };

    private static readonly byte[] _generator34 =
    {
        111, 77, 146, 94, 26, 21, 108, 19, 105, 94, 113, 193, 86, 140, 163, 125,
        58, 158, 229, 239, 218, 103, 56, 70, 114, 61, 183, 129, 167, 13, 98, 62,
        129, 51
    };

    private static readonly byte[] _generator36 =
    {
        200, 183, 98, 16, 172, 31, 246, 234, 60, 152, 115, 0, 167, 152, 113, 248,
        238, 107, 18, 63, 218, 37, 87, 210, 105, 177, 120, 74, 121, 196, 117, 251,
        113, 233, 30, 120
    };

    private static readonly byte[] _generator40 =
    {
        59, 116, 79, 161, 252, 98, 128, 205, 128, 161, 247, 57, 163, 56, 235, 106,
        53, 26, 187, 174, 226, 104, 170, 7, 175, 35, 181, 114, 88, 41, 47, 163,
        125, 134, 72, 20, 232, 53, 35, 15
    };

    private static readonly byte[] _generator42 =
    {
        250, 103, 221, 230, 25, 18, 137, 231, 0, 3, 58, 242, 221, 191, 110, 84,
        230, 8, 188, 106, 96, 147, 15, 131, 139, 34, 101, 223, 39, 101, 213, 199,
        237, 254, 201, 123, 171, 162, 194, 117, 50, 96
    };

    private static readonly byte[] _generator44 =
    {
        190, 7, 61, 121, 71, 246, 69, 55, 168, 188, 89, 243, 191, 25, 72, 123,
        9, 145, 14, 247, 1, 238, 44, 78, 143, 62, 224, 126, 118, 114, 68, 163,
        52, 194, 217, 147, 204, 169, 37, 130, 113, 102, 73, 181
    };

    private static readonly byte[] _generator46 =
    {
        112, 94, 88, 112, 253, 224, 202, 115, 187, 99, 89, 5, 54, 113, 129, 44,
        58, 16, 135, 216, 169, 211, 36, 1, 4, 96, 60, 241, 73, 104, 234, 8,
        249, 245, 119, 174, 52, 25, 157, 224, 43, 202, 223, 19, 82, 15
    };

    private static readonly byte[] _generator48 =
    {
        228, 25, 196, 130, 211, 146, 60, 24, 251, 90, 39, 102, 240, 61, 178, 63,
        46, 123, 115, 18, 221, 111, 135, 160, 182, 205, 107, 206, 95, 150, 120, 184,
        91, 21, 247, 156, 140, 238, 191, 11, 94, 227, 84, 50, 163, 39, 34, 108
    };

    private static readonly byte[] _generator50 =
    {
        232, 125, 157, 161, 164, 9, 118, 46, 209, 99, 203, 193, 35, 3, 209, 111,
        195, 242, 203, 225, 46, 13, 32, 160, 126, 209, 130, 160, 242, 215, 242, 75,
        77, 42, 189, 32, 113, 65, 124, 69, 228, 114, 235, 175, 124, 170, 215, 232,
        133, 205
    };

    private static readonly byte[] _generator52 =
    {
        116, 50, 86, 186, 50, 220, 251, 89, 192, 46, 86, 127, 124, 19, 184, 233,
        151, 215, 22, 14, 59, 145, 37, 242, 203, 134, 254, 89, 190, 94, 59, 65,
        124, 113, 100, 233, 235, 121, 22, 76, 86, 97, 39, 242, 200, 220, 101, 33,
        239, 254, 116, 51
    };

    private static readonly byte[] _generator54 =
    {
        183, 26, 201, 84, 210, 221, 113, 21, 46, 65, 45, 50, 238, 184, 249, 225,
        102, 58, 209, 218, 109, 165, 26, 95, 184, 192, 52, 245, 35, 254, 238, 175,
        172, 79, 123, 25, 122, 43, 120, 108, 215, 80, 128, 201, 235, 8, 153, 59,
        101, 31, 198, 76, 31, 156
    };

    private static readonly byte[] _generator56 =
    {
        106, 120, 107, 157, 164, 216, 112, 116, 2, 91, 248, 163, 36, 201, 202, 229,
        6, 144, 254, 155, 135, 208, 170, 209, 12, 139, 127, 142, 182, 249, 177, 174,
        190, 28, 10, 85, 239, 184, 101, 124, 152, 206, 96, 23, 163, 61, 27, 196,
        247, 151, 154, 202, 207, 20, 61, 10
    };

    private static readonly byte[] _generator58 =
    {
        82, 116, 26, 247, 66, 27, 62, 107, 252, 182, 200, 185, 235, 55, 251, 242,
        210, 144, 154, 237, 176, 141, 192, 248, 152, 249, 206, 85, 253, 142, 65, 165,
        125, 23, 24, 30, 122, 240, 214, 6, 129, 218, 29, 145, 127, 134, 206, 245,
        117, 29, 41, 63, 159, 142, 233, 125, 148, 123
    };

    private static readonly byte[] _generator60 =
    {
        107, 140, 26, 12, 9, 141, 243, 197, 226, 197, 219, 45, 211, 101, 219, 120,
        28, 181, 127, 6, 100, 247, 2, 205, 198, 57, 115, 219, 101, 109, 160, 82,
        37, 38, 238, 49, 160, 209, 121, 86, 11, 124, 30, 181, 84, 25, 194, 87,
        65, 102, 190, 220, 70, 27, 209, 16, 89, 7, 33, 240
    };

    private static readonly byte[] _generator62 =
    {
        65, 202, 113, 98, 71, 223, 248, 118, 214, 94, 0, 122, 37, 23, 2, 228,
        58, 121, 7, 105, 135, 78, 243, 118, 70, 76, 223, 89, 72, 50, 70, 111,
        194, 17, 212, 126, 181, 35, 221, 117, 235, 11, 229, 149, 147, 123, 213, 40,
        115, 6, 200, 100, 26, 246, 182, 218, 127, 215, 36, 186, 110, 106
    };

    private static readonly byte[] _generator64 =
    {
        45, 51, 175, 9, 7, 158, 159, 49, 68, 119, 92, 123, 177, 204, 187, 254,
        200, 78, 141, 149, 119, 26, 127, 53, 160, 93, 199, 212, 29, 24, 145, 156,
        208, 150, 218, 209, 4, 216, 91, 47, 184, 146, 47, 140, 195, 195, 125, 242,
        238, 63, 99, 108, 140, 230, 242, 31, 204, 11, 178, 243, 217, 156, 213, 231
    };

    private static readonly byte[] _generator66 =
    {
        5, 118, 222, 180, 136, 136, 162, 51, 46, 117, 13, 215, 81, 17, 139, 247,
        197, 171, 95, 173, 65, 137, 178, 68, 111, 95, 101, 41, 72, 214, 169, 197,
        95, 7, 44, 154, 77, 111, 236, 40, 121, 143, 63, 87, 80, 253, 240, 126,
        217, 77, 34, 232, 106, 50, 168, 82, 76, 146, 67, 106, 171, 25, 132, 93,
        45, 105
    };

    private static readonly byte[] _generator68 =
    {
        247, 159, 223, 33, 224, 93, 77, 70, 90, 160, 32, 254, 43, 150, 84, 101,
        190, 205, 133, 52, 60, 202, 165, 220, 203, 151, 93, 84, 15, 84, 253, 173,
        160, 89, 227, 52, 199, 97, 95, 231, 52, 177, 41, 125, 137, 241, 166, 225,
        118, 2, 54, 32, 82, 215, 175, 198, 43, 238, 235, 27, 101, 184, 127, 3,
        5, 8, 163, 238
    };

    internal static readonly byte[]?[] GenArray =
    {
        _generator7, null, null, _generator10, null, null, _generator13, null, _generator15, _generator16,
        _generator17, _generator18, null, _generator20, null, _generator22, null, _generator24, null, _generator26,
        null, _generator28, null, _generator30, null, _generator32, null, _generator34, null, _generator36,
        null, null, null, _generator40, null, _generator42, null, _generator44, null, _generator46,
        null, _generator48, null, _generator50, null, _generator52, null, _generator54, null, _generator56,
        null, _generator58, null, _generator60, null, _generator62, null, _generator64, null, _generator66,
        null, _generator68
    };

    internal static readonly byte[] ExpToInt = //	ExpToInt =
    {
        1, 2, 4, 8, 16, 32, 64, 128, 29, 58, 116, 232, 205, 135, 19, 38,
        76, 152, 45, 90, 180, 117, 234, 201, 143, 3, 6, 12, 24, 48, 96, 192,
        157, 39, 78, 156, 37, 74, 148, 53, 106, 212, 181, 119, 238, 193, 159, 35,
        70, 140, 5, 10, 20, 40, 80, 160, 93, 186, 105, 210, 185, 111, 222, 161,
        95, 190, 97, 194, 153, 47, 94, 188, 101, 202, 137, 15, 30, 60, 120, 240,
        253, 231, 211, 187, 107, 214, 177, 127, 254, 225, 223, 163, 91, 182, 113, 226,
        217, 175, 67, 134, 17, 34, 68, 136, 13, 26, 52, 104, 208, 189, 103, 206,
        129, 31, 62, 124, 248, 237, 199, 147, 59, 118, 236, 197, 151, 51, 102, 204,
        133, 23, 46, 92, 184, 109, 218, 169, 79, 158, 33, 66, 132, 21, 42, 84,
        168, 77, 154, 41, 82, 164, 85, 170, 73, 146, 57, 114, 228, 213, 183, 115,
        230, 209, 191, 99, 198, 145, 63, 126, 252, 229, 215, 179, 123, 246, 241, 255,
        227, 219, 171, 75, 150, 49, 98, 196, 149, 55, 110, 220, 165, 87, 174, 65,
        130, 25, 50, 100, 200, 141, 7, 14, 28, 56, 112, 224, 221, 167, 83, 166,
        81, 162, 89, 178, 121, 242, 249, 239, 195, 155, 43, 86, 172, 69, 138, 9,
        18, 36, 72, 144, 61, 122, 244, 245, 247, 243, 251, 235, 203, 139, 11, 22,
        44, 88, 176, 125, 250, 233, 207, 131, 27, 54, 108, 216, 173, 71, 142, 1,

        2, 4, 8, 16, 32, 64, 128, 29, 58, 116, 232, 205, 135, 19, 38,
        76, 152, 45, 90, 180, 117, 234, 201, 143, 3, 6, 12, 24, 48, 96, 192,
        157, 39, 78, 156, 37, 74, 148, 53, 106, 212, 181, 119, 238, 193, 159, 35,
        70, 140, 5, 10, 20, 40, 80, 160, 93, 186, 105, 210, 185, 111, 222, 161,
        95, 190, 97, 194, 153, 47, 94, 188, 101, 202, 137, 15, 30, 60, 120, 240,
        253, 231, 211, 187, 107, 214, 177, 127, 254, 225, 223, 163, 91, 182, 113, 226,
        217, 175, 67, 134, 17, 34, 68, 136, 13, 26, 52, 104, 208, 189, 103, 206,
        129, 31, 62, 124, 248, 237, 199, 147, 59, 118, 236, 197, 151, 51, 102, 204,
        133, 23, 46, 92, 184, 109, 218, 169, 79, 158, 33, 66, 132, 21, 42, 84,
        168, 77, 154, 41, 82, 164, 85, 170, 73, 146, 57, 114, 228, 213, 183, 115,
        230, 209, 191, 99, 198, 145, 63, 126, 252, 229, 215, 179, 123, 246, 241, 255,
        227, 219, 171, 75, 150, 49, 98, 196, 149, 55, 110, 220, 165, 87, 174, 65,
        130, 25, 50, 100, 200, 141, 7, 14, 28, 56, 112, 224, 221, 167, 83, 166,
        81, 162, 89, 178, 121, 242, 249, 239, 195, 155, 43, 86, 172, 69, 138, 9,
        18, 36, 72, 144, 61, 122, 244, 245, 247, 243, 251, 235, 203, 139, 11, 22,
        44, 88, 176, 125, 250, 233, 207, 131, 27, 54, 108, 216, 173, 71, 142, 1
    };

    internal static readonly byte[] IntToExp = //	IntToExp =
    {
        0, 0, 1, 25, 2, 50, 26, 198, 3, 223, 51, 238, 27, 104, 199, 75,
        4, 100, 224, 14, 52, 141, 239, 129, 28, 193, 105, 248, 200, 8, 76, 113,
        5, 138, 101, 47, 225, 36, 15, 33, 53, 147, 142, 218, 240, 18, 130, 69,
        29, 181, 194, 125, 106, 39, 249, 185, 201, 154, 9, 120, 77, 228, 114, 166,
        6, 191, 139, 98, 102, 221, 48, 253, 226, 152, 37, 179, 16, 145, 34, 136,
        54, 208, 148, 206, 143, 150, 219, 189, 241, 210, 19, 92, 131, 56, 70, 64,
        30, 66, 182, 163, 195, 72, 126, 110, 107, 58, 40, 84, 250, 133, 186, 61,
        202, 94, 155, 159, 10, 21, 121, 43, 78, 212, 229, 172, 115, 243, 167, 87,
        7, 112, 192, 247, 140, 128, 99, 13, 103, 74, 222, 237, 49, 197, 254, 24,
        227, 165, 153, 119, 38, 184, 180, 124, 17, 68, 146, 217, 35, 32, 137, 46,
        55, 63, 209, 91, 149, 188, 207, 205, 144, 135, 151, 178, 220, 252, 190, 97,
        242, 86, 211, 171, 20, 42, 93, 158, 132, 60, 57, 83, 71, 109, 65, 162,
        31, 45, 67, 216, 183, 123, 164, 118, 196, 23, 73, 236, 127, 12, 111, 246,
        108, 161, 59, 82, 41, 157, 85, 170, 251, 96, 134, 177, 187, 204, 62, 90,
        203, 89, 95, 176, 156, 169, 160, 81, 11, 245, 22, 235, 122, 117, 44, 215,
        79, 174, 213, 233, 230, 231, 173, 232, 116, 214, 244, 234, 168, 80, 88, 175
    };

    internal static readonly int[] FormatInfoArray =
    {
        0x5412, 0x5125, 0x5E7C, 0x5B4B, 0x45F9, 0x40CE, 0x4F97, 0x4AA0, // M = 00
        0x77C4, 0x72F3, 0x7DAA, 0x789D, 0x662F, 0x6318, 0x6C41, 0x6976, // L = 01
        0x1689, 0x13BE, 0x1CE7, 0x19D0, 0x762, 0x255, 0xD0C, 0x83B, // H - 10
        0x355F, 0x3068, 0x3F31, 0x3A06, 0x24B4, 0x2183, 0x2EDA, 0x2BED, // Q = 11
    };

    internal static readonly int[,] FormatInfoOne = {
        { 0, 8 }, { 1, 8 }, { 2, 8 }, { 3, 8 }, { 4, 8 }, { 5, 8 }, { 7, 8 }, { 8, 8 },
        { 8, 7 }, { 8, 5 }, { 8, 4 }, { 8, 3 }, { 8, 2 }, { 8, 1 }, { 8, 0 }
    };

    internal static readonly int[,] FormatInfoTwo = {
        { 8, -1 }, { 8, -2 }, { 8, -3 }, { 8, -4 }, { 8, -5 }, { 8, -6 }, { 8, -7 }, { 8, -8 },
        { -7, 8 }, { -6, 8 }, { -5, 8 }, { -4, 8 }, { -3, 8 }, { -2, 8 }, { -1, 8 }
    };

    internal static readonly int[] VersionCodeArray =
    {
        0x7c94, 0x85bc, 0x9a99, 0xa4d3, 0xbbf6, 0xc762, 0xd847, 0xe60d, 0xf928, 0x10b78,
        0x1145d, 0x12a17, 0x13532, 0x149a6, 0x15683, 0x168c9, 0x177ec, 0x18ec4, 0x191e1, 0x1afab,
        0x1b08e, 0x1cc1a, 0x1d33f, 0x1ed75, 0x1f250, 0x209d5, 0x216f0, 0x228ba, 0x2379f, 0x24b0b,
        0x2542e, 0x26a64, 0x27541, 0x28c69
    };

    internal const byte White = 0;
    internal const byte Black = 1;
    internal const byte NonData = 2;
    internal const byte Fixed = 4;
    internal const byte DataBlack = Black;
    internal const byte FormatWhite = NonData | White;
    internal const byte FixedWhite = Fixed | NonData | White;
    internal const byte FixedBlack = Fixed | NonData | Black;

    internal static readonly byte[,] FinderPatternTopLeft =
    {
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FormatWhite },
        { FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FormatWhite },
        { FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite },
    };

    internal static readonly byte[,] FinderPatternTopRight =
    {
        { FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack },
        { FixedWhite, FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack },
        { FixedWhite, FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack },
        { FixedWhite, FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack },
        { FixedWhite, FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack },
        { FixedWhite, FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack },
        { FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack },
        { FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite },
        { FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite, FormatWhite },
    };

    internal static readonly byte[,] FinderPatternBottomLeft =
    {
        { FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack },
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedWhite, FixedBlack, FixedWhite, FormatWhite },
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedWhite, FormatWhite },
    };

    internal static readonly byte[,] AlignmentPattern =
    {
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedBlack },
        { FixedBlack, FixedWhite, FixedBlack, FixedWhite, FixedBlack },
        { FixedBlack, FixedWhite, FixedWhite, FixedWhite, FixedBlack },
        { FixedBlack, FixedBlack, FixedBlack, FixedBlack, FixedBlack },
    };
}