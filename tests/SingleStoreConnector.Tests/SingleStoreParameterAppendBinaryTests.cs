using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector.Tests;

public class SingleStoreParameterAppendBinaryTests
{
	[Theory]
	[InlineData(DummySByteEnum.SecondValue, SingleStoreDbType.Byte, new byte[] { 0x11 })]
	[InlineData(DummyByteEnum.SecondValue, SingleStoreDbType.UByte, new byte[] { 0x11 })]
	[InlineData(DummyShortEnum.SecondValue, SingleStoreDbType.Int16, new byte[] { 0x22, 0x11 })]
	[InlineData(DummyUShortEnum.SecondValue, SingleStoreDbType.UInt16, new byte[] { 0x22, 0x11 })]
	[InlineData(DummyIntEnum.SecondValue, SingleStoreDbType.Int32, new byte[] { 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyUIntEnum.SecondValue, SingleStoreDbType.UInt32, new byte[] { 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyEnum.SecondValue, SingleStoreDbType.Int32, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
	[InlineData(DummyLongEnum.SecondValue, SingleStoreDbType.Int64, new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 })]
	[InlineData(DummyULongEnum.SecondValue, SingleStoreDbType.UInt64, new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 })]
	public void WriteBinaryEnumType(object value, SingleStoreDbType expectedSingleStoreDbType, byte[] expectedBinary)
	{
		var parameter = new SingleStoreParameter { Value = value };
		var writer = new ByteBufferWriter();
		parameter.AppendBinary(writer, StatementPreparerOptions.None);

		Assert.Equal(parameter.SingleStoreDbType, expectedSingleStoreDbType);
		Assert.Equal(writer.Position, expectedBinary.Length);
		Assert.Equal(writer.ArraySegment.ToArray(), expectedBinary);
	}
}
