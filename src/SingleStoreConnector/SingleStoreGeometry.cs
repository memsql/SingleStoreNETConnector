using System.Buffers.Binary;

namespace SingleStoreConnector;

/// <summary>
/// Represents SingleStore's internal GEOMETRY format: https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format
/// </summary>
public sealed class SingleStoreGeometry
{
	/// <summary>
	/// Constructs a <see cref="SingleStoreGeometry"/> from a SRID and Well-known Binary bytes.
	/// </summary>
	/// <param name="srid">The SRID (Spatial Reference System ID).</param>
	/// <param name="wkb">The Well-known Binary serialization of the geometry.</param>
	/// <returns>A new <see cref="SingleStoreGeometry"/> containing the specified geometry.</returns>
	public static SingleStoreGeometry FromWkb(int srid, ReadOnlySpan<byte> wkb)
	{
		var bytes = new byte[wkb.Length + 4];
		BinaryPrimitives.WriteInt32LittleEndian(bytes, srid);
		wkb.CopyTo(bytes.AsSpan().Slice(4));
		return new SingleStoreGeometry(bytes);
	}

	/// <summary>
	/// Constructs a <see cref="SingleStoreGeometry"/> from SingleStore's internal format.
	/// </summary>
	/// <param name="value">The raw bytes of SingleStore's internal GEOMETRY format.</param>
	/// <returns>A new <see cref="SingleStoreGeometry"/> containing the specified geometry.</returns>
	/// <remarks>See <a href="https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format">Internal Geometry Storage Format</a>.</remarks>
	public static SingleStoreGeometry FromMySql(ReadOnlySpan<byte> value) => new SingleStoreGeometry(value.ToArray());

	/// <summary>
	/// The Spatial Reference System ID of this geometry.
	/// </summary>
	public int SRID => BinaryPrimitives.ReadInt32LittleEndian(m_bytes);

	/// <summary>
	/// The Well-known Binary serialization of this geometry.
	/// </summary>
	public ReadOnlySpan<byte> WKB => ValueSpan.Slice(4);

	/// <summary>
	/// The internal SingleStore form of this geometry.
	/// </summary>
	public byte[] Value => ValueSpan.ToArray();

	internal ReadOnlySpan<byte> ValueSpan => m_bytes;

	internal SingleStoreGeometry(byte[] bytes) => m_bytes = bytes;

	readonly byte[] m_bytes;
}
