using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector.Plugins;

// TODO: probably should get rid of this one to avoid misguiding customers as SingleStore doesn't support compression
internal abstract class ZstandardPlugin
{
	public abstract IPayloadHandler CreatePayloadHandler(IByteHandler byteHandler);
	public abstract int CompressionLevel { get; }
}
