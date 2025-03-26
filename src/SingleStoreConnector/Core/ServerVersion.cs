using System.Buffers.Text;
using System.Text;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Core;

internal sealed class ServerVersion
{
	public ServerVersion(ReadOnlySpan<byte> versionString)
	{
		OriginalString = Encoding.ASCII.GetString(versionString);

		var minor = 0;
		var build = 0;
		if (Utf8Parser.TryParse(versionString, out int major, out var bytesConsumed))
		{
			versionString = versionString[bytesConsumed..];
			if (versionString is [0x2E, ..])
			{
				versionString = versionString[1..];
				if (Utf8Parser.TryParse(versionString, out minor, out bytesConsumed))
				{
					versionString = versionString[bytesConsumed..];
					if (versionString is [0x2E, .. ])
					{
						versionString = versionString[1..];
						if (Utf8Parser.TryParse(versionString, out build, out bytesConsumed))
						{
							versionString = versionString[bytesConsumed..];
						}
					}
				}
			}
		}

		Version = new Version(major, minor, build);
	}

	public string OriginalString { get; }
	public Version Version { get; }

	public static ServerVersion Empty { get; } = new();

	private ServerVersion()
	{
		OriginalString = "";
		Version = new();
	}
}
