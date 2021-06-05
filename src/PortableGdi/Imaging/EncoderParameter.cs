/*
 * EncoderParameter.cs - Implementation of the
 *			"System.Drawing.Imaging.EncoderParameter" class.
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

using System;

namespace Portable.Drawing.Imaging
{
	public enum EncoderParameterValueType
	{
		None				= 0,
		ValueTypeByte			= 1,
		ValueTypeAscii			= 2,
		ValueTypeShort			= 3,
		ValueTypeLong			= 4,
		ValueTypeRational		= 5,
		ValueTypeLongRange		= 6,
		ValueTypeUndefined		= 7,
		ValueTypeRationalRange	= 8

	};
#if !ECMA_COMPAT

public sealed class EncoderParameter : IDisposable
{
	// Storage for a tuple of values.
	private sealed class Tuple
	{
		// Accessible state.
		public object value1;
		public object value2;
		public object value3;
		public object value4;

		// Constructor.
		public Tuple(object value1, object value2)
				{
					this.value1 = value1;
					this.value2 = value2;
				}
		public Tuple(object value1, object value2,
					 object value3, object value4)
				{
					this.value1 = value1;
					this.value2 = value2;
					this.value3 = value3;
					this.value4 = value4;
				}

	}; // class Tuple

	// Internal state.
	private Guid encoder;
	private object value;

	// Constructors.
	public EncoderParameter(Encoder encoder, byte value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeByte;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, byte value, bool undefined)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = (undefined ?
							  EncoderParameterValueType.ValueTypeUndefined :
							  EncoderParameterValueType.ValueTypeByte);
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, byte[] value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = value.Length;
				Type = EncoderParameterValueType.ValueTypeByte;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, byte[] value, bool undefined)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = value.Length;
				Type = (undefined ?
							  EncoderParameterValueType.ValueTypeUndefined :
							  EncoderParameterValueType.ValueTypeByte);
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, short value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeShort;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, short[] value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = value.Length;
				Type = EncoderParameterValueType.ValueTypeShort;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, long value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeLong;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, long[] value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = value.Length;
				Type = EncoderParameterValueType.ValueTypeLong;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, string value)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = value.Length + 1;
				Type = EncoderParameterValueType.ValueTypeAscii;
				this.value = value;
			}
	public EncoderParameter(Encoder encoder, int numerator, int denominator)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeRational;
				value = new Tuple(numerator, denominator);
			}
	public EncoderParameter(Encoder encoder, int[] numerator,
							int[] denominator)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = numerator.Length;
				Type = EncoderParameterValueType.ValueTypeRational;
				value = new Tuple(numerator, denominator);
			}
	public EncoderParameter(Encoder encoder, long rangebegin, long rangeend)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeLongRange;
				value = new Tuple(rangebegin, rangeend);
			}
	public EncoderParameter(Encoder encoder, long[] rangebegin,
							long[] rangeend)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = rangebegin.Length;
				Type = EncoderParameterValueType.ValueTypeLongRange;
				value = new Tuple(rangebegin, rangeend);
			}
	public EncoderParameter(Encoder encoder, int NumberOfValues,
							int Type, int Value)
			{
				this.encoder = encoder.Guid;
				this.NumberOfValues = NumberOfValues;
				this.Type = (EncoderParameterValueType)Type;
				value = Value;
			}
	public EncoderParameter(Encoder encoder, int numerator1, int denominator1,
							int numerator2, int denominator2)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = 1;
				Type = EncoderParameterValueType.ValueTypeRationalRange;
				value = new Tuple(numerator1, denominator1,
									   numerator2, denominator2);
			}
	public EncoderParameter(Encoder encoder, int[] numerator1,
							int[] denominator1, int[] numerator2,
							int[] denominator2)
			{
				this.encoder = encoder.Guid;
				NumberOfValues = numerator1.Length;
				Type = EncoderParameterValueType.ValueTypeRationalRange;
				value = new Tuple(numerator1, denominator1,
									   numerator2, denominator2);
			}

	// Get or set this object's properties.
	public Encoder Encoder
			{
				get
				{
					return new Encoder(encoder);
				}
				set
				{
					encoder = value.Guid;
				}
			}
	public int NumberOfValues { get; }

	public EncoderParameterValueType Type { get; }

	public EncoderParameterValueType ValueType
			{
				get
				{
					// For some reason, the API defines two ways
					// of obtaining the same value.
					return Type;
				}
			}

	// Dispose this object.
	public void Dispose()
			{
				// Nothing to do here in this implementation.
				GC.SuppressFinalize(this);
			}

}; // class EncoderParameter

#endif // !ECMA_COMPAT

}; // namespace System.Drawing.Imaging
