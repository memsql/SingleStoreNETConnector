using System;
using System.Data;
using System.Text;
using SingleStoreConnector.Core;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector;
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreParameterTests
{
	private string EncodeParameterToAscii(SingleStoreParameter parameter, StatementPreparerOptions options = StatementPreparerOptions.None)
	{
		var writer = new ByteBufferWriter();
		parameter.AppendSqlString(writer, options);
		return Encoding.ASCII.GetString(writer.ToPayloadData().Span.ToArray());
	}

	[Fact]
	public void ZeroByteInBinary()
	{
		var parameter = new SingleStoreParameter {Direction = ParameterDirection.Input, Value = new byte[]{0x54, 0x00, 0x45, 0x53, 0x54}};
		Assert.Equal(@"_binary'T\0EST'", EncodeParameterToAscii(parameter));
	}

	[Fact]
	public void ZeroByteInGuid()
	{
		var parameter = new SingleStoreParameter {Direction = ParameterDirection.Input, Value = new Guid(new byte[]{0x44, 0x49, 0x55, 0x47,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00})};
		Assert.Equal(@"_binary'GUID\0\0\0\0\0\0\0\0\0\0\0\0'", EncodeParameterToAscii(parameter, StatementPreparerOptions.GuidFormatBinary16));
	}
}
