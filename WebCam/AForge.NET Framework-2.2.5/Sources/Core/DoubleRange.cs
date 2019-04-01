// AForge Core Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
//

namespace AForge
{
    using System;

    /// <summary>
    /// Represents a double range with minimum and maximum values.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>The class represents a double range with inclusive limits -
    /// both minimum and maximum values of the range are included into it.
    /// Mathematical notation of such range is <b>[min, max]</b>.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create [0.25, 1.5] range
    /// DoubleRange range1 = new DoubleRange( 0.25, 1.5 );
    /// // create [1.00, 2.25] range
    /// DoubleRange range2 = new DoubleRange( 1.00, 2.25 );
    /// // check if values is inside of the first range
    /// if ( range1.IsInside( 0.75 ) )
    /// {
    ///     // ...
    /// }
    /// // check if the second range is inside of the first range
    /// if ( range1.IsInside( range2 ) )
    /// {
    ///     // ...
    /// }
    /// // check if two ranges overlap
    /// if ( range1.IsOverlapping( range2 ) )
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </remarks>
    /// 
    [Serializable]
    public struct DoubleRange : IEquatable<DoubleRange>
    {

        /// <summary>
        /// Minimum value of the range.
        /// </summary>
        /// 
        /// <remarks><para>The property represents minimum value (left side limit) or the range -
        /// [<b>min</b>, max].</para></remarks>
        /// 
        public double Min { get; set; }

        /// <summary>
        /// Maximum value of the range.
        /// </summary>
        /// 
        /// <remarks><para>The property represents maximum value (right side limit) or the range -
        /// [min, <b>max</b>].</para></remarks>
        /// 
        public double Max { get; set; }

        /// <summary>
        /// Length of the range (difference between maximum and minimum values).
        /// </summary>
        public double Length
        {
            get { return Max - Min; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleRange"/> class.
        /// </summary>
        /// 
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// 
        public DoubleRange( double min, double max )
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Check if the specified value is inside of the range.
        /// </summary>
        /// 
        /// <param name="x">Value to check.</param>
        /// 
        /// <returns><b>True</b> if the specified value is inside of the range or
        /// <b>false</b> otherwise.</returns>
        /// 
        public bool IsInside( double x )
        {
            return ( ( x >= Min ) && ( x <= Max ) );
        }

        /// <summary>
        /// Check if the specified range is inside of the range.
        /// </summary>
        /// 
        /// <param name="range">Range to check.</param>
        /// 
        /// <returns><b>True</b> if the specified range is inside of the range or
        /// <b>false</b> otherwise.</returns>
        /// 
        public bool IsInside( DoubleRange range )
        {
            return ( ( IsInside( range.Min ) ) && ( IsInside( range.Max ) ) );
        }

        /// <summary>
        /// Check if the specified range overlaps with the range.
        /// </summary>
        /// 
        /// <param name="range">Range to check for overlapping.</param>
        /// 
        /// <returns><b>True</b> if the specified range overlaps with the range or
        /// <b>false</b> otherwise.</returns>
        /// 
        public bool IsOverlapping( DoubleRange range )
        {
            return ( ( IsInside( range.Min ) ) || ( IsInside( range.Max ) ) ||
                     ( range.IsInside( Min ) ) || ( range.IsInside( Max ) ) );
        }

        /// <summary>
        /// Convert the signle precision range to integer range.
        /// </summary>
        /// 
        /// <param name="provideInnerRange">Specifies if inner integer range must be returned or outer range.</param>
        /// 
        /// <returns>Returns integer version of the range.</returns>
        /// 
        /// <remarks>If <paramref name="provideInnerRange"/> is set to <see langword="true"/>, then the
        /// returned integer range will always fit inside of the current single precision range.
        /// If it is set to <see langword="false"/>, then current single precision range will always
        /// fit into the returned integer range.</remarks>
        ///
        public IntRange ToIntRange( bool provideInnerRange )
        {
            int iMin, iMax;

            if ( provideInnerRange )
            {
                iMin = (int)System.Math.Ceiling( Min );
                iMax = (int)System.Math.Floor( Max );
            }
            else
            {
                iMin = (int)System.Math.Floor( Min );
                iMax = (int)System.Math.Ceiling( Max );
            }

            return new IntRange( iMin, iMax );
        }

        /// <summary>
        /// Equality operator - checks if two ranges have equal min/max values.
        /// </summary>
        /// 
        /// <param name="range1">First range to check.</param>
        /// <param name="range2">Second range to check.</param>
        /// 
        /// <returns>Returns <see langword="true"/> if min/max values of specified
        /// ranges are equal.</returns>
        ///
        public static bool operator ==( DoubleRange range1, DoubleRange range2 )
        {
            return ( ( range1.Min == range2.Min ) && ( range1.Max == range2.Max ) );
        }

        /// <summary>
        /// Inequality operator - checks if two ranges have different min/max values.
        /// </summary>
        /// 
        /// <param name="range1">First range to check.</param>
        /// <param name="range2">Second range to check.</param>
        /// 
        /// <returns>Returns <see langword="true"/> if min/max values of specified
        /// ranges are not equal.</returns>
        ///
        public static bool operator !=( DoubleRange range1, DoubleRange range2 )
        {
            return ( range1.Min != range2.Min ) || ( range1.Max != range2.Max );

        }

        /// <summary>
        /// Check if this instance of <see cref="Range"/> equal to the specified one.
        /// </summary>
        /// 
        /// <param name="obj">Another range to check equalty to.</param>
        /// 
        /// <returns>Return <see langword="true"/> if objects are equal.</returns>
        /// 
        public override bool Equals( object obj )
        {
            return ( obj is Range ) ? ( this == (DoubleRange) obj ) : false;
        }

        /// <summary>
        /// Get hash code for this instance.
        /// </summary>
        /// 
        /// <returns>Returns the hash code for this instance.</returns>
        /// 
        public override int GetHashCode( )
        {
            return Min.GetHashCode( ) + Max.GetHashCode( );
        }

        /// <summary>
        /// Get string representation of the class.
        /// </summary>
        /// 
        /// <returns>Returns string, which contains min/max values of the range in readable form.</returns>
        ///
        public override string ToString( )
        {
            return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}", Min, Max );
        }

        public bool Equals(DoubleRange other)
        {
            return (Min == other.Min) && (Max == other.Max);
        }
    }
}
