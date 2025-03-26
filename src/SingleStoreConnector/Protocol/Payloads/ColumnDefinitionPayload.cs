using System.Text;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Protocol.Payloads;

internal sealed class ColumnDefinitionPayload
{
	public string Name
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_name!;
		}
	}

	public CharacterSet CharacterSet { get; private set; }

	public uint ColumnLength { get; private set; }

	public ColumnType ColumnType { get; private set; }

	public ColumnFlags ColumnFlags { get; private set; }

	public string SchemaName
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_schemaName!;
		}
	}

	public string CatalogName
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_catalogName!;
		}
	}

	public string Table
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_table!;
		}
	}

	public string PhysicalTable
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_physicalTable!;
		}
	}

	public string PhysicalName
	{
		get
		{
			if (!m_readNames)
				ReadNames();
			return m_physicalName!;
		}
	}

	public byte Decimals { get; private set; }

	public static void Initialize(ref ColumnDefinitionPayload payload, ResizableArraySegment<byte> arraySegment)
	{
		payload ??= new ColumnDefinitionPayload();
		payload.Initialize(arraySegment);
	}
	private void Initialize(ResizableArraySegment<byte> originalData)
	{
		m_originalData = originalData;
		var reader = new ByteArrayReader(originalData);
		SkipLengthEncodedByteString(ref reader); // catalog
		SkipLengthEncodedByteString(ref reader); // schema
		SkipLengthEncodedByteString(ref reader); // table
		SkipLengthEncodedByteString(ref reader); // physical table
		SkipLengthEncodedByteString(ref reader); // name
		SkipLengthEncodedByteString(ref reader); // physical name
		reader.ReadByte(0x0C); // length of fixed-length fields, always 0x0C
		CharacterSet = (CharacterSet) reader.ReadUInt16();
		ColumnLength = reader.ReadUInt32();
		ColumnType = (ColumnType) reader.ReadByte();
		ColumnFlags = (ColumnFlags) reader.ReadUInt16();
		Decimals = reader.ReadByte(); // 0x00 for integers and static strings, 0x1f for dynamic strings, double, float, 0x00 to 0x51 for decimals
		reader.ReadByte(0); // reserved byte 1
		reader.ReadByte(0); // reserved byte 2

		if (m_readNames)
		{
			m_catalogName = null;
			m_schemaName = null;
			m_table = null;
			m_physicalTable = null;
			m_name = null;
			m_physicalName = null;
			m_readNames = false;
		}
	}

	private static void SkipLengthEncodedByteString(ref ByteArrayReader reader)
	{
		var length = checked((int) reader.ReadLengthEncodedInteger());
		reader.Offset += length;
	}

	private ColumnDefinitionPayload()
	{
	}

	private void ReadNames()
	{
		var reader = new ByteArrayReader(m_originalData);
		m_catalogName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_schemaName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_table = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_physicalTable = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_name = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_physicalName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
		m_readNames = true;
	}

	private ResizableArraySegment<byte> m_originalData;
	private bool m_readNames;
	private string? m_name;
	private string? m_schemaName;
	private string? m_catalogName;
	private string? m_table;
	private string? m_physicalTable;
	private string? m_physicalName;
}
