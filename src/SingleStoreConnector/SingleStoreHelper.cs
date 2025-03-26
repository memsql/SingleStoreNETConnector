using System.Text;

namespace SingleStoreConnector;

public sealed class SingleStoreHelper
{
	[Obsolete("Use SingleStoreConnection.ClearAllPools or SingleStoreConnection.ClearAllPoolsAsync")]
	public static void ClearConnectionPools() => SingleStoreConnection.ClearAllPools();

	/// <summary>
	/// Escapes single and double quotes, and backslashes in <paramref name="value"/>.
	/// </summary>
	public static string EscapeString(string value)
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(value);
#else
		if (value is null)
			throw new ArgumentNullException(nameof(value));
#endif

		StringBuilder? sb = null;
		int last = -1;
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] is '\'' or '\"' or '\\')
			{
				sb ??= new();
				sb.Append(value, last + 1, i - (last + 1));
				sb.Append('\\');
				sb.Append(value[i]);
				last = i;
			}
		}
		sb?.Append(value, last + 1, value.Length - (last + 1));

		return sb?.ToString() ?? value;
	}
}
