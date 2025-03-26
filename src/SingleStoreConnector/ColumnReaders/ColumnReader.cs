using SingleStoreConnector.Protocol;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector.ColumnReaders;

internal abstract class ColumnReader
{
	public static ColumnReader Create(bool isBinary, ColumnDefinitionPayload columnDefinition, SingleStoreConnection connection)
	{
		var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
		switch (columnDefinition.ColumnType)
		{
			case ColumnType.Tiny:
				if (connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned)
					return isBinary ? BinaryBooleanColumnReader.Instance : TextBooleanColumnReader.Instance;
				return (isBinary, isUnsigned) switch
				{
					(false, false) => TextSignedInt8ColumnReader.Instance,
					(false, true) => TextUnsignedInt8ColumnReader.Instance,
					(true, false) => BinarySignedInt8ColumnReader.Instance,
					(true, true) => BinaryUnsignedInt8ColumnReader.Instance,
				};

			case ColumnType.Int24:
			case ColumnType.Long:
				return (isBinary, isUnsigned) switch
				{
					(false, false) => TextSignedInt32ColumnReader.Instance,
					(false, true) => TextUnsignedInt32ColumnReader.Instance,
					(true, false) => BinarySignedInt32ColumnReader.Instance,
					(true, true) => BinaryUnsignedInt32ColumnReader.Instance,
				};

			case ColumnType.Longlong:
				return (isBinary, isUnsigned) switch
				{
					(false, false) => TextSignedInt64ColumnReader.Instance,
					(false, true) => TextUnsignedInt64ColumnReader.Instance,
					(true, false) => BinarySignedInt64ColumnReader.Instance,
					(true, true) => BinaryUnsignedInt64ColumnReader.Instance,
				};

			case ColumnType.Bit:
				return BitColumnReader.Instance;

			case ColumnType.String:
				if (connection.GuidFormat == SingleStoreGuidFormat.Char36 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return GuidChar36ColumnReader.Instance;
				if (connection.GuidFormat == SingleStoreGuidFormat.Char32 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
					return GuidChar32ColumnReader.Instance;
				if (connection.TreatChar48AsGeographyPoint && columnDefinition.ColumnLength == 48)
					return StringColumnReader.Instance;
				if (columnDefinition.ColumnLength == 1073741823)
					return StringColumnReader.Instance;
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.VarChar:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
			case ColumnType.Enum:
			case ColumnType.Set:
				if (columnDefinition.CharacterSet != CharacterSet.Binary)
					return StringColumnReader.Instance;
				if (columnDefinition.ColumnLength == 16)
				{
					return connection.GuidFormat switch
					{
						SingleStoreGuidFormat.Binary16 => GuidBinary16ColumnReader.Instance,
						SingleStoreGuidFormat.TimeSwapBinary16 => GuidTimeSwapBinary16ColumnReader.Instance,
						SingleStoreGuidFormat.LittleEndianBinary16 => GuidLittleEndianBinary16ColumnReader.Instance,
						_ => BytesColumnReader.Instance,
					};
				}
				return BytesColumnReader.Instance;

			case ColumnType.Json:
				return StringColumnReader.Instance;

			case ColumnType.Short:
				return (isBinary, isUnsigned) switch
				{
					(false, false) => TextSignedInt16ColumnReader.Instance,
					(false, true) => TextUnsignedInt16ColumnReader.Instance,
					(true, false) => BinarySignedInt16ColumnReader.Instance,
					(true, true) => BinaryUnsignedInt16ColumnReader.Instance,
				};

			case ColumnType.Date:
			case ColumnType.DateTime:
			case ColumnType.NewDate:
			case ColumnType.Timestamp:
				return isBinary ? new BinaryDateTimeColumnReader(connection) : new TextDateTimeColumnReader(connection);

			case ColumnType.Time:
				return isBinary ? BinaryTimeColumnReader.Instance : TextTimeColumnReader.Instance;

			case ColumnType.Year:
				return isBinary ? BinaryYearColumnReader.Instance : TextSignedInt32ColumnReader.Instance;

			case ColumnType.Float:
				return isBinary ? BinaryFloatColumnReader.Instance : TextFloatColumnReader.Instance;

			case ColumnType.Double:
				return isBinary ? BinaryDoubleColumnReader.Instance : TextDoubleColumnReader.Instance;

			case ColumnType.Decimal:
			case ColumnType.NewDecimal:
				return DecimalColumnReader.Instance;

			case ColumnType.Geography:
				return StringColumnReader.Instance;

			case ColumnType.GeographyPoint:
				return StringColumnReader.Instance;

			case ColumnType.Null:
				return NullColumnReader.Instance;

			default:
				throw new NotImplementedException($"Reading {columnDefinition.ColumnType} not implemented");
		}
	}

	public abstract object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	public virtual int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		default;
}
