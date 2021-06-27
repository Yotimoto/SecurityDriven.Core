﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SecurityDriven.Core
{
	public partial class CryptoRandom
	{

		#region System.Random overrides

		/// <summary>Returns a non-negative random integer.</summary>
		/// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="int.MaxValue"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next()
		{
			int result;
			Span<byte> span4 = stackalloc byte[sizeof(int)];
			do
			{
				_impl.NextBytes(span4);
				result = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(span4)) & 0x7FFF_FFFF; // Mask away the sign bit
			} while (result == int.MaxValue); // the range must be [0, int.MaxValue)
			return result;
		}//Next()

		/// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
		/// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
		/// <returns>
		/// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
		/// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int maxValue)
		{
			if (maxValue < 0) ThrowNewArgumentOutOfRangeException(nameof(maxValue));
			return Next(0, maxValue);
		}//Next(maxValue)

		/// <summary>Returns a random integer that is within a specified range.</summary>
		/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
		/// <returns>
		/// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
		/// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int Next(int minValue, int maxValue)
		{
			if (minValue == maxValue) return minValue;
			if (minValue > maxValue) ThrowNewArgumentOutOfRangeException(nameof(minValue));

			// The total possible range is [0, 4,294,967,295). Subtract 1 to account for zero being an actual possibility.
			uint range = (uint)(maxValue - minValue) - 1;

			// If there is only one possible choice, nothing random will actually happen, so return the only possibility.
			if (range == 0) return minValue;

			// Create a mask for the bits that we care about for the range. The other bits will be masked away.
			uint mask = range;
			mask |= mask >> 01;
			mask |= mask >> 02;
			mask |= mask >> 04;
			mask |= mask >> 08;
			mask |= mask >> 16;

			Span<byte> span4 = stackalloc byte[sizeof(uint)];
			ref uint result = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span4));

			do
			{
				_impl.NextBytes(span4);
				result &= mask;
			} while (result > range);
			return minValue + (int)result;
		}//Next(minValue, maxValue)

		/// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
		/// <param name="buffer">The array to be filled with random numbers.</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(byte[] buffer)
		{
			if (buffer is null) ThrowNewArgumentNullException(nameof(buffer));
			_impl.NextBytes(new Span<byte>(buffer));
		}//NextBytes(byte[])

		/// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
		/// <param name="buffer">The array to be filled with random numbers.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void NextBytes(Span<byte> buffer)
		{
			_impl.NextBytes(buffer);
		}//NextBytes(Span<byte>)

		/// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
		/// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override double NextDouble()
		{
			const double ONE_OVER_MAX = 1.0D / (1UL << (64 - 11)); // https://en.wikipedia.org/wiki/Double-precision_floating-point_format

			Utils.LongStruct longStruct = default;
			_impl.NextBytes(MemoryMarshal.CreateSpan(ref longStruct.B1, 8));

			return (longStruct.UlongValue >> 11) * ONE_OVER_MAX;
		}//NextDouble()

		/// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
		/// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual float NextSingle()
		{
			const float ONE_OVER_MAX = 1.0F / (1U << (32 - 8)); // https://en.wikipedia.org/wiki/Single-precision_floating-point_format

			Utils.IntStruct intStruct = default;
			_impl.NextBytes(MemoryMarshal.CreateSpan(ref intStruct.B1, 4));

			return (intStruct.UintValue >> 8) * ONE_OVER_MAX;
		}//NextSingle()

		/// <summary>Returns a random floating-point number between 0.0 and 1.0.</summary>
		/// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override double Sample()
		{
			return NextDouble();
		}//Sample()
		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void ThrowNewArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName: paramName);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void ThrowNewArgumentNullException(string paramName) => throw new ArgumentNullException(paramName: paramName);
	}//class CryptoRandom
}//ns
