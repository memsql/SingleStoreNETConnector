namespace SingleStoreConnector.Core;

internal interface IServerCapabilities
{
	bool SupportsDeprecateEof { get; }
	bool SupportsSessionTrack { get; }
}
