using System.Globalization;
using System.Text;
using SingleStoreConnector.Protocol;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Core;

internal sealed class TypeMapper
{
	public static TypeMapper Instance { get; } = new();

	private TypeMapper()
	{
		m_columnTypeMetadata = new();
		m_dbTypeMappingsByClrType = new();
		m_dbTypeMappingsByDbType = new();
		m_columnTypeMetadataLookup = new(StringComparer.OrdinalIgnoreCase);
		m_mySqlDbTypeToColumnTypeMetadata = new();

		// boolean
		var typeBoolean = AddDbTypeMapping(new(typeof(bool), new[] { DbType.Boolean }, convert: static o => Convert.ToBoolean(o, CultureInfo.InvariantCulture)));
		AddColumnTypeMetadata(new("TINYINT", typeBoolean, SingleStoreDbType.Bool, isUnsigned: false, length: 1, columnSize: 1, simpleDataTypeName: "BOOL", createFormat: "BOOL"));

		// integers
		var typeSbyte = AddDbTypeMapping(new(typeof(sbyte), new[] { DbType.SByte }, convert: static o => Convert.ToSByte(o, CultureInfo.InvariantCulture)));
		var typeByte = AddDbTypeMapping(new(typeof(byte), new[] { DbType.Byte }, convert: static o => Convert.ToByte(o, CultureInfo.InvariantCulture)));
		var typeShort = AddDbTypeMapping(new(typeof(short), new[] { DbType.Int16 }, convert: static o => Convert.ToInt16(o, CultureInfo.InvariantCulture)));
		var typeUshort = AddDbTypeMapping(new(typeof(ushort), new[] { DbType.UInt16 }, convert: static o => Convert.ToUInt16(o, CultureInfo.InvariantCulture)));
		var typeInt = AddDbTypeMapping(new(typeof(int), new[] { DbType.Int32 }, convert: static o => Convert.ToInt32(o, CultureInfo.InvariantCulture)));
		var typeUint = AddDbTypeMapping(new(typeof(uint), new[] { DbType.UInt32 }, convert: static o => Convert.ToUInt32(o, CultureInfo.InvariantCulture)));
		var typeLong = AddDbTypeMapping(new(typeof(long), new[] { DbType.Int64 }, convert: static o => Convert.ToInt64(o, CultureInfo.InvariantCulture)));
		var typeUlong = AddDbTypeMapping(new(typeof(ulong), new[] { DbType.UInt64 }, convert: static o => Convert.ToUInt64(o, CultureInfo.InvariantCulture)));
		AddColumnTypeMetadata(new("TINYINT", typeSbyte, SingleStoreDbType.Byte, isUnsigned: false));
		AddColumnTypeMetadata(new("TINYINT", typeByte, SingleStoreDbType.UByte, isUnsigned: true, length: 1));
		AddColumnTypeMetadata(new("TINYINT", typeByte, SingleStoreDbType.UByte, isUnsigned: true));
		AddColumnTypeMetadata(new("SMALLINT", typeShort, SingleStoreDbType.Int16, isUnsigned: false));
		AddColumnTypeMetadata(new("SMALLINT", typeUshort, SingleStoreDbType.UInt16, isUnsigned: true));
		AddColumnTypeMetadata(new("INT", typeInt, SingleStoreDbType.Int32, isUnsigned: false));
		AddColumnTypeMetadata(new("INT", typeUint, SingleStoreDbType.UInt32, isUnsigned: true));
		AddColumnTypeMetadata(new("MEDIUMINT", typeInt, SingleStoreDbType.Int24, isUnsigned: false));
		AddColumnTypeMetadata(new("MEDIUMINT", typeUint, SingleStoreDbType.UInt24, isUnsigned: true));
		AddColumnTypeMetadata(new("BIGINT", typeLong, SingleStoreDbType.Int64, isUnsigned: false));
		AddColumnTypeMetadata(new("BIGINT", typeUlong, SingleStoreDbType.UInt64, isUnsigned: true));
		AddColumnTypeMetadata(new("BIT", typeUlong, SingleStoreDbType.Bit));

		// decimals
		var typeDecimal = AddDbTypeMapping(new(typeof(decimal), new[] { DbType.Decimal, DbType.Currency, DbType.VarNumeric }, convert: static o => Convert.ToDecimal(o, CultureInfo.InvariantCulture)));
		var typeDouble = AddDbTypeMapping(new(typeof(double), new[] { DbType.Double }, convert: static o => Convert.ToDouble(o, CultureInfo.InvariantCulture)));
		var typeFloat = AddDbTypeMapping(new(typeof(float), new[] { DbType.Single }, convert: static o => Convert.ToSingle(o, CultureInfo.InvariantCulture)));
		AddColumnTypeMetadata(new("DECIMAL", typeDecimal, SingleStoreDbType.NewDecimal, createFormat: "DECIMAL({0},{1});precision,scale"));
		AddColumnTypeMetadata(new("DECIMAL", typeDecimal, SingleStoreDbType.NewDecimal, isUnsigned: true, createFormat: "DECIMAL({0},{1}) UNSIGNED;precision,scale"));
		AddColumnTypeMetadata(new("DECIMAL", typeDecimal, SingleStoreDbType.Decimal));
		AddColumnTypeMetadata(new("DOUBLE", typeDouble, SingleStoreDbType.Double));
		AddColumnTypeMetadata(new("FLOAT", typeFloat, SingleStoreDbType.Float));

		// string
		var typeFixedString = AddDbTypeMapping(new(typeof(string), new[] { DbType.StringFixedLength, DbType.AnsiStringFixedLength }, convert: Convert.ToString!));
		var typeString = AddDbTypeMapping(new(typeof(string), new[] { DbType.String, DbType.AnsiString, DbType.Xml }, convert: Convert.ToString!));
		AddColumnTypeMetadata(new("VARCHAR", typeString, SingleStoreDbType.VarChar, createFormat: "VARCHAR({0});size"));
		AddColumnTypeMetadata(new("VARCHAR", typeString, SingleStoreDbType.VarString));
		AddColumnTypeMetadata(new("CHAR", typeFixedString, SingleStoreDbType.String, createFormat: "CHAR({0});size"));
		AddColumnTypeMetadata(new("TINYTEXT", typeString, SingleStoreDbType.TinyText, columnSize: byte.MaxValue, simpleDataTypeName: "VARCHAR"));
		AddColumnTypeMetadata(new("TEXT", typeString, SingleStoreDbType.Text, columnSize: ushort.MaxValue, simpleDataTypeName: "VARCHAR"));
		AddColumnTypeMetadata(new("MEDIUMTEXT", typeString, SingleStoreDbType.MediumText, columnSize: 16777215, simpleDataTypeName: "VARCHAR"));
		AddColumnTypeMetadata(new("LONGTEXT", typeString, SingleStoreDbType.LongText, columnSize: uint.MaxValue, simpleDataTypeName: "VARCHAR"));
		AddColumnTypeMetadata(new("ENUM", typeString, SingleStoreDbType.Enum));
		AddColumnTypeMetadata(new("SET", typeString, SingleStoreDbType.Set));
		AddColumnTypeMetadata(new("JSON", typeString, SingleStoreDbType.JSON));

		// binary
		var typeBinary = AddDbTypeMapping(new(typeof(byte[]), new[] { DbType.Binary }));
		AddColumnTypeMetadata(new("BLOB", typeBinary, SingleStoreDbType.Blob, binary: true, columnSize: ushort.MaxValue, simpleDataTypeName: "BLOB"));
		AddColumnTypeMetadata(new("BINARY", typeBinary, SingleStoreDbType.Binary, binary: true, simpleDataTypeName: "BLOB", createFormat: "BINARY({0});length"));
		AddColumnTypeMetadata(new("VARBINARY", typeBinary, SingleStoreDbType.VarBinary, binary: true, simpleDataTypeName: "BLOB", createFormat: "VARBINARY({0});length"));
		AddColumnTypeMetadata(new("TINYBLOB", typeBinary, SingleStoreDbType.TinyBlob, binary: true, columnSize: byte.MaxValue, simpleDataTypeName: "BLOB"));
		AddColumnTypeMetadata(new("MEDIUMBLOB", typeBinary, SingleStoreDbType.MediumBlob, binary: true, columnSize: 16777215, simpleDataTypeName: "BLOB"));
		AddColumnTypeMetadata(new("LONGBLOB", typeBinary, SingleStoreDbType.LongBlob, binary: true, columnSize: uint.MaxValue, simpleDataTypeName: "BLOB"));

		// spatial
		AddColumnTypeMetadata(new("GEOGRAPHY", typeString, SingleStoreDbType.Geography, columnSize: 1431655765));
		AddColumnTypeMetadata(new("POINT", typeString, SingleStoreDbType.GeographyPoint, columnSize: 48));
		AddColumnTypeMetadata(new("LINESTRING", typeString, SingleStoreDbType.Geography, columnSize: 1431655765));
		AddColumnTypeMetadata(new("POLYGON", typeString, SingleStoreDbType.Geography, columnSize: 1431655765));

		// date/time
#if NET6_0_OR_GREATER
		AddDbTypeMapping(new(typeof(DateOnly), new[] { DbType.Date }));
#endif
		var typeDate = AddDbTypeMapping(new(typeof(DateTime), new[] { DbType.Date }));
		var typeDateTime = AddDbTypeMapping(new(typeof(DateTime), new[] { DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset }));
		AddDbTypeMapping(new(typeof(DateTimeOffset), new[] { DbType.DateTimeOffset }));
#if NET6_0_OR_GREATER
		AddDbTypeMapping(new(typeof(TimeOnly), new[] { DbType.Time }));
#endif
		var typeTime = AddDbTypeMapping(new(typeof(TimeSpan), new[] { DbType.Time }, convert: static o => o is string s ? Utility.ParseTimeSpan(Encoding.UTF8.GetBytes(s)) : Convert.ChangeType(o, typeof(TimeSpan), CultureInfo.InvariantCulture)));
		AddColumnTypeMetadata(new("DATETIME", typeDateTime, SingleStoreDbType.DateTime));
		AddColumnTypeMetadata(new("DATE", typeDate, SingleStoreDbType.Date));
		AddColumnTypeMetadata(new("DATE", typeDate, SingleStoreDbType.Newdate));
		AddColumnTypeMetadata(new("TIME", typeTime, SingleStoreDbType.Time));
		AddColumnTypeMetadata(new("TIMESTAMP", typeDateTime, SingleStoreDbType.Timestamp));
		AddColumnTypeMetadata(new("YEAR", typeInt, SingleStoreDbType.Year));

		// guid
#if NET7_0_OR_GREATER
		Func<object, object> convertGuid = static o => Guid.Parse(Convert.ToString(o, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
#else
		Func<object, object> convertGuid = static o => Guid.Parse(Convert.ToString(o, CultureInfo.InvariantCulture)!);
#endif
		var typeGuid = AddDbTypeMapping(new(typeof(Guid), new[] { DbType.Guid }, convert: convertGuid));
		AddColumnTypeMetadata(new("CHAR", typeGuid, SingleStoreDbType.Guid, length: 36, simpleDataTypeName: "CHAR(36)", createFormat: "CHAR(36)"));

		// null
		var typeNull = AddDbTypeMapping(new(typeof(object), new[] { DbType.Object }));
		AddColumnTypeMetadata(new("NULL", typeNull, SingleStoreDbType.Null));
	}

	public IReadOnlyList<ColumnTypeMetadata> GetColumnTypeMetadata() => m_columnTypeMetadata.AsReadOnly();

	public ColumnTypeMetadata GetColumnTypeMetadata(SingleStoreDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType];

	public DbType GetDbTypeForSingleStoreDbType(SingleStoreDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType].DbTypeMapping.DbTypes[0];

	public SingleStoreDbType GetSingleStoreDbTypeForDbType(DbType dbType)
	{
		foreach (var pair in m_mySqlDbTypeToColumnTypeMetadata)
		{
			if (pair.Value.DbTypeMapping.DbTypes.Contains(dbType))
				return pair.Key;
		}
		return SingleStoreDbType.VarChar;
	}

	private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
	{
		m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;

		if (dbTypeMapping.DbTypes is not null)
		{
			foreach (var dbType in dbTypeMapping.DbTypes)
				m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;
		}

		return dbTypeMapping;
	}

	private void AddColumnTypeMetadata(ColumnTypeMetadata columnTypeMetadata)
	{
		m_columnTypeMetadata.Add(columnTypeMetadata);
		var lookupKey = columnTypeMetadata.CreateLookupKey();
		if (!m_columnTypeMetadataLookup.ContainsKey(lookupKey))
			m_columnTypeMetadataLookup.Add(lookupKey, columnTypeMetadata);
		if (!m_mySqlDbTypeToColumnTypeMetadata.ContainsKey(columnTypeMetadata.SingleStoreDbType))
			m_mySqlDbTypeToColumnTypeMetadata.Add(columnTypeMetadata.SingleStoreDbType, columnTypeMetadata);
	}

	internal DbTypeMapping? GetDbTypeMapping(Type clrType)
	{
		if (clrType.IsEnum)
			clrType = Enum.GetUnderlyingType(clrType);
		m_dbTypeMappingsByClrType.TryGetValue(clrType, out var dbTypeMapping);
		return dbTypeMapping;
	}

	internal DbTypeMapping? GetDbTypeMapping(DbType dbType)
	{
		m_dbTypeMappingsByDbType.TryGetValue(dbType, out var dbTypeMapping);
		return dbTypeMapping;
	}

	public DbTypeMapping? GetDbTypeMapping(string columnTypeName, bool unsigned = false, int length = 0)
	{
		return GetColumnTypeMetadata(columnTypeName, unsigned, length)?.DbTypeMapping;
	}

	public SingleStoreDbType GetSingleStoreDbType(string typeName, bool unsigned, int length) => GetColumnTypeMetadata(typeName, unsigned, length)!.SingleStoreDbType;

	private ColumnTypeMetadata? GetColumnTypeMetadata(string columnTypeName, bool unsigned, int length)
	{
		if (!m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, length), out var columnTypeMetadata) && length != 0)
			m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, 0), out columnTypeMetadata);
		return columnTypeMetadata;
	}

	public static SingleStoreDbType ConvertToSingleStoreDbType(ColumnDefinitionPayload columnDefinition, bool treatTinyAsBoolean, bool treatChar48AsGeographyPoint, SingleStoreGuidFormat guidFormat)
	{
		var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
		if ((columnDefinition.ColumnFlags & ColumnFlags.Enum) != 0)
			return SingleStoreDbType.Enum;
		if ((columnDefinition.ColumnFlags & ColumnFlags.Set) != 0)
			return SingleStoreDbType.Set;

		switch (columnDefinition.ColumnType)
		{
		case ColumnType.Tiny:
			return treatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned ? SingleStoreDbType.Bool :
				isUnsigned ? SingleStoreDbType.UByte : SingleStoreDbType.Byte;

		case ColumnType.Int24:
			return isUnsigned ? SingleStoreDbType.UInt24 : SingleStoreDbType.Int24;

		case ColumnType.Long:
			return isUnsigned ? SingleStoreDbType.UInt32 : SingleStoreDbType.Int32;

		case ColumnType.Longlong:
			return isUnsigned ? SingleStoreDbType.UInt64 : SingleStoreDbType.Int64;

		case ColumnType.Bit:
			return SingleStoreDbType.Bit;

		case ColumnType.String:
			var columnLen = columnDefinition.ColumnLength /
			                ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet);
			if (guidFormat == SingleStoreGuidFormat.Char36 && columnLen == 36)
				return SingleStoreDbType.Guid;
			if (guidFormat == SingleStoreGuidFormat.Char32 && columnLen == 32)
				return SingleStoreDbType.Guid;
			if (treatChar48AsGeographyPoint && columnLen == 48)
				return SingleStoreDbType.GeographyPoint;
			if (columnLen == 1431655765)
				return SingleStoreDbType.Geography;
			goto case ColumnType.VarString;

		case ColumnType.VarChar:
		case ColumnType.VarString:
		case ColumnType.TinyBlob:
		case ColumnType.Blob:
		case ColumnType.MediumBlob:
		case ColumnType.LongBlob:
			var type = columnDefinition.ColumnType;
			if (columnDefinition.CharacterSet == CharacterSet.Binary)
			{
				if ((guidFormat is SingleStoreGuidFormat.Binary16 or SingleStoreGuidFormat.TimeSwapBinary16 or SingleStoreGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
					return SingleStoreDbType.Guid;

				return type switch
				{
					ColumnType.String => SingleStoreDbType.Binary,
					ColumnType.VarString => SingleStoreDbType.VarBinary,
					ColumnType.TinyBlob => SingleStoreDbType.TinyBlob,
					ColumnType.Blob => SingleStoreDbType.Blob,
					ColumnType.MediumBlob => SingleStoreDbType.MediumBlob,
					_ => SingleStoreDbType.LongBlob,
				};
			}
			return type switch
			{
				ColumnType.String => SingleStoreDbType.String,
				ColumnType.VarString => SingleStoreDbType.VarChar,
				ColumnType.TinyBlob => SingleStoreDbType.TinyText,
				ColumnType.Blob => SingleStoreDbType.Text,
				ColumnType.MediumBlob => SingleStoreDbType.MediumText,
				_ => SingleStoreDbType.LongText,
			};

		case ColumnType.Json:
			return SingleStoreDbType.JSON;

		case ColumnType.Short:
			return isUnsigned ? SingleStoreDbType.UInt16 : SingleStoreDbType.Int16;

		case ColumnType.Date:
		case ColumnType.NewDate:
			return SingleStoreDbType.Date;

		case ColumnType.DateTime:
			return SingleStoreDbType.DateTime;

		case ColumnType.Timestamp:
			return SingleStoreDbType.Timestamp;

		case ColumnType.Time:
			return SingleStoreDbType.Time;

		case ColumnType.Year:
			return SingleStoreDbType.Year;

		case ColumnType.Float:
			return SingleStoreDbType.Float;

		case ColumnType.Double:
			return SingleStoreDbType.Double;

		case ColumnType.Decimal:
			return SingleStoreDbType.Decimal;

		case ColumnType.NewDecimal:
			return SingleStoreDbType.NewDecimal;

		case ColumnType.Geography:
			return SingleStoreDbType.Geography;

		case ColumnType.GeographyPoint:
			return SingleStoreDbType.GeographyPoint;

		case ColumnType.Null:
			return SingleStoreDbType.Null;

		case ColumnType.Enum:
			return SingleStoreDbType.Enum;

		case ColumnType.Set:
			return SingleStoreDbType.Set;

		default:
			throw new NotImplementedException($"ConvertToMySqlDbType for {columnDefinition.ColumnType} is not implemented");
		}
	}

	public static ushort ConvertToColumnTypeAndFlags(SingleStoreDbType dbType, SingleStoreGuidFormat guidFormat)
	{
		var isUnsigned = dbType is SingleStoreDbType.UByte or SingleStoreDbType.UInt16 or SingleStoreDbType.UInt24 or SingleStoreDbType.UInt32 or SingleStoreDbType.UInt64;
		var columnType = dbType switch
		{
			SingleStoreDbType.Bool or SingleStoreDbType.Byte or SingleStoreDbType.UByte => ColumnType.Tiny,
			SingleStoreDbType.Int16 or SingleStoreDbType.UInt16 => ColumnType.Short,
			SingleStoreDbType.Int24 or SingleStoreDbType.UInt24 => ColumnType.Int24,
			SingleStoreDbType.Int32 or SingleStoreDbType.UInt32 => ColumnType.Long,
			SingleStoreDbType.Int64 or SingleStoreDbType.UInt64 => ColumnType.Longlong,
			SingleStoreDbType.Bit => ColumnType.Bit,
			SingleStoreDbType.Guid => (guidFormat is SingleStoreGuidFormat.Char36 or SingleStoreGuidFormat.Char32) ? ColumnType.String : ColumnType.Blob,
			SingleStoreDbType.Enum or SingleStoreDbType.Set => ColumnType.String,
			SingleStoreDbType.Binary or SingleStoreDbType.String => ColumnType.String,
			SingleStoreDbType.VarBinary or SingleStoreDbType.VarChar or SingleStoreDbType.VarString => ColumnType.VarString,
			SingleStoreDbType.TinyBlob or SingleStoreDbType.TinyText => ColumnType.TinyBlob,
			SingleStoreDbType.Blob or SingleStoreDbType.Text => ColumnType.Blob,
			SingleStoreDbType.MediumBlob or SingleStoreDbType.MediumText => ColumnType.MediumBlob,
			SingleStoreDbType.LongBlob or SingleStoreDbType.LongText => ColumnType.LongBlob,
			SingleStoreDbType.JSON => ColumnType.Json, // TODO: test
			SingleStoreDbType.Date or SingleStoreDbType.Newdate => ColumnType.Date,
			SingleStoreDbType.DateTime => ColumnType.DateTime,
			SingleStoreDbType.Timestamp => ColumnType.Timestamp,
			SingleStoreDbType.Time => ColumnType.Time,
			SingleStoreDbType.Year => ColumnType.Year,
			SingleStoreDbType.Float => ColumnType.Float,
			SingleStoreDbType.Double => ColumnType.Double,
			SingleStoreDbType.Decimal => ColumnType.Decimal,
			SingleStoreDbType.NewDecimal => ColumnType.NewDecimal,
			SingleStoreDbType.Geography => ColumnType.Geography,
			SingleStoreDbType.GeographyPoint => ColumnType.GeographyPoint,
			SingleStoreDbType.Null => ColumnType.Null,
			_ => throw new NotImplementedException($"ConvertToColumnTypeAndFlags for {dbType} is not implemented"),
		};
		return (ushort) ((byte) columnType | (isUnsigned ? 0x8000 : 0));
	}

	internal IEnumerable<ColumnTypeMetadata> GetColumnMappings()
	{
		return m_columnTypeMetadataLookup.Values.AsEnumerable();
	}

	private readonly List<ColumnTypeMetadata> m_columnTypeMetadata;
	private readonly Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType;
	private readonly Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType;
	private readonly Dictionary<string, ColumnTypeMetadata> m_columnTypeMetadataLookup;
	private readonly Dictionary<SingleStoreDbType, ColumnTypeMetadata> m_mySqlDbTypeToColumnTypeMetadata;
}
