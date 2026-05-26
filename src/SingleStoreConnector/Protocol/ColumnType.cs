namespace SingleStoreConnector.Protocol;

/// Base column type values from the MySQL-compatible protocol.
/// SingleStore-specific types such as BSON and VECTOR are exposed through extended metadata,
/// not as additional ColumnType values.
internal enum ColumnType
{
	Decimal = 0,
	Tiny = 1,
	Short = 2,
	Long = 3,
	Float = 4,
	Double = 5,
	Null = 6,
	Timestamp = 7,
	Longlong = 8,
	Int24 = 9,
	Date = 10,
	Time = 11,
	DateTime = 12,
	Year = 13,
	NewDate = 14,
	VarChar = 15,
	Bit = 16,
	Timestamp2 = 17,
	DateTime2 = 18,
	GeographyPoint = 0xF2,
	Geography = 0xF4,
	Json = 0xF5,
	NewDecimal = 0xF6,
	Enum = 0xF7,
	Set = 0xF8,
	TinyBlob = 0xF9,
	MediumBlob = 0xFA,
	LongBlob = 0xFB,
	Blob = 0xFC,
	VarString = 0xFD,
	String = 0xFE,
}
