using System.Globalization;
using SingleStoreConnector.Core;
using SingleStoreConnector.Protocol;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Protocol.Serialization;

#if NET461
#pragma warning disable CA1716 // Don't use reserved language keywords
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name
namespace System.Data.Common
{
	public abstract class DbColumn
	{
		public bool? AllowDBNull { get; protected set; }
		public string? BaseCatalogName { get; protected set; }
		public string? BaseColumnName { get; protected set; }
		public string? BaseSchemaName { get; protected set; }
		public string? BaseServerName { get; protected set; }
		public string? BaseTableName { get; protected set; }
		public string ColumnName { get; protected set; } = "";
		public int? ColumnOrdinal { get; protected set; }
		public int? ColumnSize { get; protected set; }
		public bool? IsAliased { get; protected set; }
		public bool? IsAutoIncrement { get; protected set; }
		public bool? IsExpression { get; protected set; }
		public bool? IsHidden { get; protected set; }
		public bool? IsIdentity { get; protected set; }
		public bool? IsKey { get; protected set; }
		public bool? IsLong { get; protected set; }
		public bool? IsReadOnly { get; protected set; }
		public bool? IsUnique { get; protected set; }
		public int? NumericPrecision { get; protected set; }
		public int? NumericScale { get; protected set; }
		public string? UdtAssemblyQualifiedName { get; protected set; }
		public Type? DataType { get; protected set; }
		public string? DataTypeName { get; protected set; }
		public virtual object? this[string property] => null;
	}
}
#endif

namespace SingleStoreConnector
{
	public sealed class SingleStoreDbColumn : System.Data.Common.DbColumn
	{
		internal SingleStoreDbColumn(int ordinal, ColumnDefinitionPayload column, bool allowZeroDateTime, SingleStoreDbType mySqlDbType, Version serverVersion)
		{
			var columnTypeMetadata = TypeMapper.Instance.GetColumnTypeMetadata(mySqlDbType);

			var type = columnTypeMetadata.DbTypeMapping.ClrType;

			// starting from 7.8 SingleStore returns number of characters (not amount of bytes)
			// for text types (e.g. Text, TinyText, MediumText, LongText)
			// (see https://grizzly.internal.memcompute.com/D54237)
			if (serverVersion >= new Version(7, 8, 0) &&
			    mySqlDbType is SingleStoreDbType.LongText or SingleStoreDbType.MediumText or SingleStoreDbType.Text or SingleStoreDbType.TinyText)
			{
				// overflow may occur here for SingleStoreDbType.LongText
				ColumnSize = (int)column.ColumnLength;
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
			DataTypeName = columnTypeMetadata.SimpleDataTypeName;
			if (mySqlDbType == SingleStoreDbType.String)
				DataTypeName += string.Format(CultureInfo.InvariantCulture, "({0})", ColumnSize);
			IsAliased = column.PhysicalName != column.Name;
			IsAutoIncrement = (column.ColumnFlags & ColumnFlags.AutoIncrement) != 0;
			IsExpression = false;
			IsHidden = false;
			IsKey = (column.ColumnFlags & ColumnFlags.PrimaryKey) != 0;
			IsLong = column.ColumnLength > 255 &&
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
		}

		public SingleStoreDbType ProviderType { get; }
	}
}
