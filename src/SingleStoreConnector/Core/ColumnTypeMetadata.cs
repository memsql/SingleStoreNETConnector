using System.Runtime.CompilerServices;

namespace SingleStoreConnector.Core;

internal sealed class ColumnTypeMetadata(string dataTypeName, DbTypeMapping dbTypeMapping, SingleStoreDbType mySqlDbType, bool isUnsigned = false, bool binary = false, int length = 0, string? simpleDataTypeName = null, string? createFormat = null, long columnSize = 0, SingleStoreGuidFormat guidFormat = SingleStoreGuidFormat.Default)
{
	public static string CreateLookupKey(string columnTypeName, bool isUnsigned, int length, SingleStoreGuidFormat guidFormat) =>
		$"{columnTypeName}|{(isUnsigned ? "u" : "s")}|{length}|{GetGuidFormatLookupKey(guidFormat)}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetGuidFormatLookupKey(SingleStoreGuidFormat guidFormat) =>
		guidFormat switch
		{
			SingleStoreGuidFormat.Char36 => "c36",
			SingleStoreGuidFormat.Char32 => "c32",
			SingleStoreGuidFormat.Binary16 or SingleStoreGuidFormat.TimeSwapBinary16 or SingleStoreGuidFormat.LittleEndianBinary16 => "b16",
			_ => "def",
		};

	public string DataTypeName { get; } = dataTypeName;
	public string SimpleDataTypeName { get; } = simpleDataTypeName ?? dataTypeName;
	public string CreateFormat { get; } = createFormat ?? (dataTypeName + (isUnsigned ? " UNSIGNED" : ""));
	public DbTypeMapping DbTypeMapping { get; } = dbTypeMapping;
	public SingleStoreDbType SingleStoreDbType { get; } = mySqlDbType;
	public bool Binary { get; } = binary;
	public long ColumnSize { get; } = columnSize;
	public bool IsUnsigned { get; } = isUnsigned;
	public int Length { get; } = length;
	public SingleStoreGuidFormat GuidFormat { get; } = guidFormat;

	public string CreateLookupKey() => CreateLookupKey(DataTypeName, IsUnsigned, Length, GuidFormat);
}
