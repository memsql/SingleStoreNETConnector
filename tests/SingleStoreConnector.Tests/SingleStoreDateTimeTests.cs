using System.Globalization;

namespace SingleStoreConnector.Tests;

public class SingleStoreDateTimeTests
{
	[Fact]
	public void NewSingleStoreDateTimeIsNotValidDateTime()
	{
		var msdt = new SingleStoreDateTime();
		Assert.False(msdt.IsValidDateTime);
	}

	[Fact]
	public void ZeroSingleStoreDateTimeIsNotValidDateTime()
	{
		var msdt = new SingleStoreDateTime(0, 0, 0, 0, 0, 0, 0);
		Assert.False(msdt.IsValidDateTime);
	}

	[Fact]
	public void NonZeroSingleStoreDateTimeIsValidDateTime()
	{
		var msdt = new SingleStoreDateTime(2018, 6, 9, 0, 0, 0, 0);
		Assert.True(msdt.IsValidDateTime);
	}

	[Fact]
	public void CreateFromDateTime()
	{
		var msdt = new SingleStoreDateTime(s_dateTime);
		Assert.True(msdt.IsValidDateTime);
		Assert.Equal(2018, msdt.Year);
		Assert.Equal(6, msdt.Month);
		Assert.Equal(9, msdt.Day);
		Assert.Equal(12, msdt.Hour);
		Assert.Equal(34, msdt.Minute);
		Assert.Equal(56, msdt.Second);
		Assert.Equal(123, msdt.Millisecond);
		Assert.Equal(123456, msdt.Microsecond);
	}

	[Fact]
	public void GetDateTime()
	{
		var msdt = s_mySqlDateTime;
		Assert.True(msdt.IsValidDateTime);
		var dt = msdt.GetDateTime();
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void GetDateTimeForInvalidDate()
	{
		var msdt = new SingleStoreDateTime();
		Assert.False(msdt.IsValidDateTime);
		Assert.Throws<SingleStoreConversionException>(() => msdt.GetDateTime());
	}

	[Fact]
	public void SetMicrosecond()
	{
		var msdt = new SingleStoreDateTime();
		Assert.Equal(0, msdt.Microsecond);
		msdt.Microsecond = 123456;
		Assert.Equal(123, msdt.Millisecond);
	}

	[Fact]
	public void ConvertibleToDateTime()
	{
		IConvertible convertible = s_mySqlDateTime;
		var dt = convertible.ToDateTime(CultureInfo.InvariantCulture);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void ConvertToDateTime()
	{
		object obj = s_mySqlDateTime;
		var dt = Convert.ToDateTime(obj);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void ChangeTypeToDateTime()
	{
		object obj = s_mySqlDateTime;
		var dt = Convert.ChangeType(obj, TypeCode.DateTime);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void NotConvertibleToDateTime()
	{
		IConvertible convertible = new SingleStoreDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => convertible.ToDateTime(CultureInfo.InvariantCulture));
#else
		Assert.Throws<SingleStoreConversionException>(() => convertible.ToDateTime(CultureInfo.InvariantCulture));
#endif
	}

	[Fact]
	public void NotConvertToDateTime()
	{
		object obj = new SingleStoreDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(obj));
#else
		Assert.Throws<SingleStoreConversionException>(() => Convert.ToDateTime(obj));
#endif
	}

	[Fact]
	public void NotChangeTypeToDateTime()
	{
		object obj = new SingleStoreDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => Convert.ChangeType(obj, TypeCode.DateTime));
#else
		Assert.Throws<SingleStoreConversionException>(() => Convert.ChangeType(obj, TypeCode.DateTime));
#endif
	}

#if !BASELINE
	[Fact]
	public void ValidDateTimeConvertibleToString()
	{
		IConvertible convertible = s_mySqlDateTime;
		Assert.Equal("06/09/2018 12:34:56", convertible.ToString(CultureInfo.InvariantCulture));
	}

	[Fact]
	public void InvalidDateTimeConvertibleToString()
	{
		IConvertible convertible = new SingleStoreDateTime();
		Assert.Equal("0000-00-00", convertible.ToString(CultureInfo.InvariantCulture));
	}
#endif

	[Fact]
	public void CompareInvalidObject()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
#if BASELINE
		Assert.Throws<InvalidCastException>(() => left.CompareTo(new object()));
#else
		Assert.Throws<ArgumentException>(() => left.CompareTo(new object()));
#endif
	}

	[Fact]
	public void CompareYear()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2001, 1, 1, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMonth()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 2, 1, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareDay()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 2, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareHour()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 1, 2, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMinute()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 1, 1, 2, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareSecond()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 1, 1, 1, 2, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMicrosecond()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareEqual()
	{
		IComparable left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		Assert.Equal(0, left.CompareTo(right));
	}

#if !BASELINE
	[Fact]
	public void Operators()
	{
		var left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		var same = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		var right = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left < right);
		Assert.True(left <= same);
		Assert.True(left <= right);
		Assert.False(right < left);
		Assert.False(right <= left);
		Assert.True(left == same);
		Assert.True(left != right);
		Assert.False(left > right);
		Assert.False(left >= right);
		Assert.True(right > left);
		Assert.True(right >= left);
		Assert.True(same >= left);
	}

	[Fact]
	public void Equal()
	{
		IEquatable<SingleStoreDateTime> left = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		var same = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 1);
		var right = new SingleStoreDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left.Equals(same));
		Assert.True(same.Equals(left));
		Assert.False(left.Equals(right));
		Assert.False(left.Equals(new object()));
	}
#endif

	static readonly SingleStoreDateTime s_mySqlDateTime = new(2018, 6, 9, 12, 34, 56, 123456);
	static readonly DateTime s_dateTime = new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560);
}
