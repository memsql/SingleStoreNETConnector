using System.Buffers.Binary;

namespace SingleStoreConnector;

/// <summary>
/// Represents SingleStore's internal GEOGRAPHYPOINT format: https://docs.singlestore.com/managed-service/en/reference/sql-reference/data-types/geospatial-types.html
/// </summary>
public sealed class SingleStoreGeographyPoint
{
	internal string Value => m_wkt;

	/// <summary>
	/// Constructs a <see cref="SingleStoreGeographyPoint"/> from Well-known Text (WKT) bytes.
	/// </summary>
	/// <param name="wkt">The Well-known Text serialization of the geography.</param>
	/// <returns>A new <see cref="SingleStoreGeographyPoint"/> containing the specified geography.</returns>
	internal SingleStoreGeographyPoint(string wkt) => m_wkt = wkt;

	private readonly string m_wkt;
}
