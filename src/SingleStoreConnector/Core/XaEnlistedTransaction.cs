using System.Globalization;
using System.Transactions;

namespace SingleStoreConnector.Core;

internal sealed class XaEnlistedTransaction(Transaction transaction, SingleStoreConnection connection) : EnlistedTransactionBase(transaction, connection)
{
	protected override void OnStart()
	{
		// generate an "xid" with "gtrid" (Global TRansaction ID) from the .NET Transaction and "bqual" (Branch QUALifier)
		// unique to this object
		var id = Interlocked.Increment(ref s_currentId);
		m_xid = "'" + Transaction.TransactionInformation.LocalIdentifier + "', '" + id.ToString(CultureInfo.InvariantCulture) + "'";

		ExecuteXaCommand("START");

		// TODO: Support EnlistDurable and enable recovery via "XA RECOVER"
	}

	protected override void OnPrepare(PreparingEnlistment enlistment)
	{
		ExecuteXaCommand("END");
		ExecuteXaCommand("PREPARE");
	}

	protected override void OnCommit(Enlistment enlistment)
	{
		ExecuteXaCommand("COMMIT");
	}

	protected override void OnRollback(Enlistment enlistment)
	{
		try
		{
			if (!IsPrepared)
				ExecuteXaCommand("END");

			ExecuteXaCommand("ROLLBACK");
		}
		catch (SingleStoreException ex) when (ex.ErrorCode is SingleStoreErrorCode.XARBDeadlock)
		{
			// ignore deadlock when rolling back
		}
	}

	private void ExecuteXaCommand(string statement)
	{
		using var cmd = Connection.CreateCommand();
		cmd.CommandText = "XA " + statement + " " + m_xid;
		cmd.ExecuteNonQuery();
	}

	private static int s_currentId;

	private string? m_xid;
}
