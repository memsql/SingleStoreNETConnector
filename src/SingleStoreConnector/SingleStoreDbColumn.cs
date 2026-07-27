using System.Globalization;
using SingleStoreConnector.Core;
using SingleStoreConnector.Protocol;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector;

public sealed class SingleStoreDbColumn : DbColumn
{
	internal SingleStoreDbColumn(int ordinal, ColumnDefinitionPayload column, bool allowZeroDateTime, SingleStoreDbType mySqlDbType, Version serverVersion)
	{
		var columnTypeMetadata = TypeMapper.Instance.GetColumnTypeMetadata(mySqlDbType);

		var type = columnTypeMetadata.DbTypeMapping.ClrType;
		var dataTypeName = columnTypeMetadata.SimpleDataTypeName;

		VectorDimensions = null;
		VectorElementTypeName = null;

		if (mySqlDbType == SingleStoreDbType.Vector)
		{
			dataTypeName = "VECTOR";

			VectorDimensions = column.VectorDimensions is { } dims
				? checked((int) dims)
				: null;

			VectorElementTypeName = column.VectorElementType?.ToString();

			type = column.VectorElementType switch
			{
				SingleStoreVectorElementType.F32 => typeof(ReadOnlyMemory<float>),
				SingleStoreVectorElementType.F64 => typeof(ReadOnlyMemory<double>),
				SingleStoreVectorElementType.I8 => typeof(ReadOnlyMemory<sbyte>),
				SingleStoreVectorElementType.I16 => typeof(ReadOnlyMemory<short>),
				SingleStoreVectorElementType.I32 => typeof(ReadOnlyMemory<int>),
				SingleStoreVectorElementType.I64 => typeof(ReadOnlyMemory<long>),
				null => throw new FormatException("VECTOR column is missing VectorElementType metadata."),
				_ => throw new NotSupportedException(
					$"Unsupported VECTOR element type: {column.VectorElementType}."),
			};
		}

		if (mySqlDbType == SingleStoreDbType.Vector && VectorDimensions is { } vectorDimensions)
		{
			ColumnSize = vectorDimensions;
		}
		// starting from 7.8 SingleStore returns number of characters (not amount of bytes)
		// for text types (e.g. Text, TinyText, MediumText, LongText)
		// (see https://grizzly.internal.memcompute.com/D54237)
		else if (serverVersion >= new Version(7, 8, 0) &&
			mySqlDbType is SingleStoreDbType.LongText or SingleStoreDbType.MediumText or SingleStoreDbType.Text or SingleStoreDbType.TinyText)
		{
			// overflow may occur here for SingleStoreDbType.LongText
			ColumnSize = (int) column.ColumnLength;
		}
		else
		{
			if (mySqlDbType == SingleStoreDbType.JSON || mySqlDbType == SingleStoreDbType.LongBlob)
				ColumnSize = int.MaxValue;
			else

				// overflow may occur here
				ColumnSize = (int) (column.ColumnLength / ProtocolUtility.GetBytesPerCharacter(column.CharacterSet));
		}

		// if overflow occured, i.e. when column.ColumnLength > int.MaxValue and char size was 1,
		// we set ColumnSize to max
		if (ColumnSize < 0)
			ColumnSize = int.MaxValue;

		AllowDBNull = (column.ColumnFlags & ColumnFlags.NotNull) == 0;
		BaseCatalogName = null;
		BaseColumnName = column.PhysicalName;
		BaseSchemaName = column.SchemaName;
		BaseTableName = column.PhysicalTable;
		ColumnName = column.Name;
		ColumnOrdinal = ordinal;
		DataType = (allowZeroDateTime && type == typeof(DateTime)) ? typeof(SingleStoreDateTime) : type;
		DataTypeName = dataTypeName;
		if (mySqlDbType == SingleStoreDbType.String)
			DataTypeName += string.Format(CultureInfo.InvariantCulture, "({0})", ColumnSize);
		else if (mySqlDbType == SingleStoreDbType.Vector && column is { VectorDimensions: { } dimensions, VectorElementType: { } elementType })
		{
			DataTypeName += string.Format(CultureInfo.InvariantCulture, "({0}, {1})", dimensions, elementType);
		}
		IsAliased = column.PhysicalName != column.Name;
		IsAutoIncrement = (column.ColumnFlags & ColumnFlags.AutoIncrement) != 0;
		IsExpression = false;
		IsHidden = false;
		IsKey = (column.ColumnFlags & ColumnFlags.PrimaryKey) != 0;
		IsLong = mySqlDbType != SingleStoreDbType.Vector &&
				 column.ColumnLength > 255 &&
				 ((column.ColumnFlags & ColumnFlags.Blob) != 0 || column.ColumnType is ColumnType.TinyBlob or ColumnType.Blob or ColumnType.MediumBlob or ColumnType.LongBlob);
		IsReadOnly = false;
		IsUnique = (column.ColumnFlags & ColumnFlags.UniqueKey) != 0;
		if (column.ColumnType is ColumnType.Decimal or ColumnType.NewDecimal)
		{
			NumericPrecision = (int) column.ColumnLength;
			if ((column.ColumnFlags & ColumnFlags.Unsigned) == 0)
				NumericPrecision--;
			if (column.Decimals > 0)
				NumericPrecision--;
		}
		NumericScale = column.Decimals;
		ProviderType = mySqlDbType;
		TableName = column.Table;
	}

	public SingleStoreDbType ProviderType { get; }

	public int? VectorDimensions { get; }

	public string? VectorElementTypeName { get; }

	/// <summary>
	/// Gets the name of the table that the column belongs to. This will be the alias if the table is aliased in the query.
	/// </summary>
	public string TableName { get; }
}
