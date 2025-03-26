using System.Buffers.Text;
using System.Text;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class TextDateTimeColumnReader : ColumnReader
{
	public TextDateTimeColumnReader(SingleStoreConnection connection)
	{
		m_allowZeroDateTime = connection.AllowZeroDateTime;
		m_convertZeroDateTime = connection.ConvertZeroDateTime;
		m_dateTimeKind = connection.DateTimeKind;
	}

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, m_convertZeroDateTime, m_allowZeroDateTime, m_dateTimeKind);

	public static object ParseDateTime(ReadOnlySpan<byte> data, bool convertZeroDateTime, bool allowZeroDateTime, DateTimeKind dateTimeKind)
	{
		Exception? exception = null;
		if (!Utf8Parser.TryParse(data, out int year, out var bytesConsumed) || bytesConsumed != 4)
			goto InvalidDateTime;
		if (data.Length < 5 || data[4] != 45)
			goto InvalidDateTime;
		if (!Utf8Parser.TryParse(data[5..], out int month, out bytesConsumed) || bytesConsumed != 2)
			goto InvalidDateTime;
		if (data.Length < 8 || data[7] != 45)
			goto InvalidDateTime;
		if (!Utf8Parser.TryParse(data[8..], out int day, out bytesConsumed) || bytesConsumed != 2)
			goto InvalidDateTime;

		if (year == 0 && month == 0 && day == 0)
		{
			if (convertZeroDateTime)
				return DateTime.MinValue;
			if (allowZeroDateTime)
				return default(SingleStoreDateTime);
			throw new InvalidCastException("Unable to convert SingleStore date/time to System.DateTime, set AllowZeroDateTime=True or ConvertZeroDateTime=True in the connection string. See https://mysqlconnector.net/connection-options/");
		}

		int hour, minute, second, ticks;
		if (data.Length == 10)
		{
			hour = 0;
			minute = 0;
			second = 0;
			ticks = 0;
		}
		else
		{
			if (data[10] != 32)
				goto InvalidDateTime;
			if (!Utf8Parser.TryParse(data[11..], out hour, out bytesConsumed) || bytesConsumed != 2)
				goto InvalidDateTime;
			if (data.Length < 14 || data[13] != 58)
				goto InvalidDateTime;
			if (!Utf8Parser.TryParse(data[14..], out minute, out bytesConsumed) || bytesConsumed != 2)
				goto InvalidDateTime;
			if (data.Length < 17 || data[16] != 58)
				goto InvalidDateTime;
			if (!Utf8Parser.TryParse(data[17..], out second, out bytesConsumed) || bytesConsumed != 2)
				goto InvalidDateTime;

			if (data.Length == 19)
			{
				ticks = 0;
			}
			else
			{
				if (data[19] != 46)
					goto InvalidDateTime;

				if (!Utf8Parser.TryParse(data[20..], out ticks, out bytesConsumed) || bytesConsumed != data.Length - 20 || bytesConsumed > 7)
					goto InvalidDateTime;
				for (; bytesConsumed < 7; bytesConsumed++)
					ticks *= 10;
			}
		}

		try
		{
			return allowZeroDateTime ? (ticks % 10 == 0 ? (object) new SingleStoreDateTime(year, month, day, hour, minute, second, ticks / 10) : throw new NotSupportedException("MySqlDateTime does not support sub-microsecond precision")) :
				new DateTime(year, month, day, hour, minute, second, dateTimeKind).AddTicks(ticks);
		}
		catch (Exception ex)
		{
			exception = ex;
		}

InvalidDateTime:
		throw new FormatException($"Couldn't interpret value as a valid DateTime: {Encoding.UTF8.GetString(data)}", exception);
	}

	private readonly bool m_allowZeroDateTime;
	private readonly bool m_convertZeroDateTime;
	private readonly DateTimeKind m_dateTimeKind;
}
