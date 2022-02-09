using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests;

public sealed class TransactionTests : TransactionTestBase<DbFactoryFixture>
{
	public TransactionTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void BeginTransaction_works() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Commit_transaction_clears_Connection() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Commit_transaction_then_Rollback_throws() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Commit_transaction_twice_throws() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Rollback_transaction_clears_Connection() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Rollback_transaction_then_Commit_throws() {}

	[Fact(Skip = "Deliberately throws System.NotSupportedException : IsolationLevel.Serializable is not supported.")]
	public override void Rollback_transaction_twice_throws() {}
}
