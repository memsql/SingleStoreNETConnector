namespace SingleStoreConnector;

#pragma warning disable CA2225 // Operator overloads have named alternates

/// <summary>
/// Represents a SingleStore date/time value. This type can be used to store <c>DATETIME</c> values such
/// as <c>0000-00-00</c> that can be stored in SingleStore (when <see cref="SingleStoreConnectionStringBuilder.AllowZeroDateTime"/>
/// is true) but can't be stored in a <see cref="DateTime"/> value.
/// </summary>
/// <param name="year">The year.</param>
/// <param name="month">The (one-based) month.</param>
/// <param name="day">The (one-based) day of the month.</param>
/// <param name="hour">The hour.</param>
/// <param name="minute">The minute.</param>
/// <param name="second">The second.</param>
/// <param name="microsecond">The microsecond.</param>
public struct SingleStoreDateTime(int year, int month, int day, int hour, int minute, int second, int microsecond) : IComparable, IComparable<SingleStoreDateTime>, IConvertible, IEquatable<SingleStoreDateTime>
{
	/// <summary>
	/// Initializes a new instance of <see cref="SingleStoreDateTime"/> from a <see cref="DateTime"/>.
	/// </summary>
	/// <param name="dt">The <see cref="DateTime"/> whose values will be copied.</param>
	public SingleStoreDateTime(DateTime dt)
		: this(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, (int) (dt.Ticks % 10_000_000) / 10)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SingleStoreDateTime"/> from another <see cref="SingleStoreDateTime"/>.
	/// </summary>
	/// <param name="other">The <see cref="SingleStoreDateTime"/> whose values will be copied.</param>
	public SingleStoreDateTime(SingleStoreDateTime other)
		: this(other.Year, other.Month, other.Day, other.Hour, other.Minute, other.Second, other.Microsecond)
	{
	}

	/// <summary>
	/// Returns <c>true</c> if this value is a valid <see cref="DateTime"/>.
	/// </summary>
	public readonly bool IsValidDateTime => Year != 0 && Month != 0 && Day != 0;

	/// <summary>
	/// Gets or sets the year.
	/// </summary>
	public int Year { get; set; } = year;

	/// <summary>
	/// Gets or sets the month.
	/// </summary>
	public int Month { get; set; } = month;

	/// <summary>
	/// Gets or sets the day of the month.
	/// </summary>
	public int Day { get; set; } = day;

	/// <summary>
	/// Gets or sets the hour.
	/// </summary>
	public int Hour { get; set; } = hour;

	/// <summary>
	/// Gets or sets the minute.
	/// </summary>
	public int Minute { get; set; } = minute;

	/// <summary>
	/// Gets or sets the second.
	/// </summary>
	public int Second { get; set; } = second;

	/// <summary>
	/// Gets or sets the microseconds.
	/// </summary>
	public int Microsecond { get; set; } = microsecond;

	/// <summary>
	/// Gets or sets the milliseconds.
	/// </summary>
	public int Millisecond
	{
		readonly get => Microsecond / 1000;
		set => Microsecond = value * 1000;
	}

	/// <summary>
	/// Returns a <see cref="DateTime"/> value (if <see cref="IsValidDateTime"/> is <c>true</c>), or throws a
	/// <see cref="SingleStoreConversionException"/>.
	/// </summary>
	public readonly DateTime GetDateTime() =>
		!IsValidDateTime ? throw new SingleStoreConversionException("Cannot convert SingleStoreDateTime to DateTime when IsValidDateTime is false.") :
			new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Unspecified).AddTicks(Microsecond * 10);

	/// <summary>
	/// Converts this object to a <see cref="string"/>.
	/// </summary>
#pragma warning disable CA1305 // Specify IFormatProvider
	public readonly override string ToString() => IsValidDateTime ? GetDateTime().ToString() : "0000-00-00";
#pragma warning restore CA1305 // Specify IFormatProvider

	/// <summary>
	/// Converts this object to a <see cref="DateTime"/>.
	/// </summary>
	public static explicit operator DateTime(SingleStoreDateTime val) => !val.IsValidDateTime ? DateTime.MinValue : val.GetDateTime();

	/// <summary>
	/// Returns <c>true</c> if this <see cref="SingleStoreDateTime"/> is equal to <paramref name="obj"/>.
	/// </summary>
	/// <param name="obj">The object to compare against for equality.</param>
	/// <returns><c>true</c> if the objects are equal, otherwise <c>false</c>.</returns>
	public override bool Equals(object? obj) =>
		obj is SingleStoreDateTime other && ((IEquatable<SingleStoreDateTime>) this).Equals(other);

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	public override int GetHashCode() =>
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		HashCode.Combine(Year, Month, Day, Hour, Minute, Second, Microsecond);
#else
		(((((Year * 33 ^ Month) * 33 ^ Day) * 33 ^ Hour) * 33 ^ Minute) * 33 ^ Second) * 33 ^ Microsecond;
#endif

	public static bool operator ==(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) == 0;
	public static bool operator !=(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) != 0;
	public static bool operator <(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) < 0;
	public static bool operator <=(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) <= 0;
	public static bool operator >(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) > 0;
	public static bool operator >=(SingleStoreDateTime left, SingleStoreDateTime right) => ((IComparable<SingleStoreDateTime>) left).CompareTo(right) >= 0;

	/// <summary>
	/// Compares this object to another <see cref="SingleStoreDateTime"/>.
	/// </summary>
	/// <param name="obj">The object to compare to.</param>
	/// <returns>An <see cref="int"/> giving the results of the comparison: a negative value if this
	/// object is less than <paramref name="obj"/>, zero if this object is equal, or a positive value if this
	/// object is greater.</returns>
	readonly int IComparable.CompareTo(object? obj) =>
		obj is SingleStoreDateTime other ?
			((IComparable<SingleStoreDateTime>) this).CompareTo(other) :
			throw new ArgumentException("CompareTo can only be called with another SingleStoreDateTime", nameof(obj));

	/// <summary>
	/// Compares this object to another <see cref="SingleStoreDateTime"/>.
	/// </summary>
	/// <param name="other">The <see cref="SingleStoreDateTime"/> to compare to.</param>
	/// <returns>An <see cref="int"/> giving the results of the comparison: a negative value if this
	/// object is less than <paramref name="other"/>, zero if this object is equal, or a positive value if this
	/// object is greater.</returns>
	readonly int IComparable<SingleStoreDateTime>.CompareTo(SingleStoreDateTime other)
	{
		if (Year < other.Year)
			return -1;
		if (Year > other.Year)
			return 1;
		if (Month < other.Month)
			return -1;
		if (Month > other.Month)
			return 1;
		if (Day < other.Day)
			return -1;
		if (Day > other.Day)
			return 1;
		if (Hour < other.Hour)
			return -1;
		if (Hour > other.Hour)
			return 1;
		if (Minute < other.Minute)
			return -1;
		if (Minute > other.Minute)
			return 1;
		if (Second < other.Second)
			return -1;
		if (Second > other.Second)
			return 1;
		return Microsecond.CompareTo(other.Microsecond);
	}

	readonly bool IEquatable<SingleStoreDateTime>.Equals(SingleStoreDateTime other) => ((IComparable<SingleStoreDateTime>) this).CompareTo(other) == 0;

	DateTime IConvertible.ToDateTime(IFormatProvider? provider) => IsValidDateTime ? GetDateTime() : throw new InvalidCastException();
	string IConvertible.ToString(IFormatProvider? provider) => IsValidDateTime ? GetDateTime().ToString(provider) : "0000-00-00";

	object IConvertible.ToType(Type conversionType, IFormatProvider? provider) =>
		conversionType == typeof(DateTime) ? (object) GetDateTime() :
		conversionType == typeof(string) ? ((IConvertible) this).ToString(provider) :
		throw new InvalidCastException();

	TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
	bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();
	char IConvertible.ToChar(IFormatProvider? provider) => throw new InvalidCastException();
	sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new InvalidCastException();
	byte IConvertible.ToByte(IFormatProvider? provider) => throw new InvalidCastException();
	short IConvertible.ToInt16(IFormatProvider? provider) => throw new InvalidCastException();
	ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();
	int IConvertible.ToInt32(IFormatProvider? provider) => throw new InvalidCastException();
	uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();
	long IConvertible.ToInt64(IFormatProvider? provider) => throw new InvalidCastException();
	ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
	float IConvertible.ToSingle(IFormatProvider? provider) => throw new InvalidCastException();
	double IConvertible.ToDouble(IFormatProvider? provider) => throw new InvalidCastException();
	decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();
}
