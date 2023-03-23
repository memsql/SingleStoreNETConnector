using System.Collections;

namespace SingleStoreConnector;

public sealed class SingleStoreBatchCommandCollection
#if NET6_0_OR_GREATER
	: DbBatchCommandCollection
#else
	: IList<SingleStoreBatchCommand>, ICollection<SingleStoreBatchCommand>, IEnumerable<SingleStoreBatchCommand>, IEnumerable
#endif
{
	internal SingleStoreBatchCommandCollection() => m_commands = new();

#if NET6_0_OR_GREATER
	public new SingleStoreBatchCommand this[int index] { get => (SingleStoreBatchCommand) base[index]; set => base[index] = value; }
	public override int Count => m_commands.Count;
	public override bool IsReadOnly => false;
	public override void Add(DbBatchCommand item) => m_commands.Add((SingleStoreBatchCommand) item);
	public override void Clear() => m_commands.Clear();
	public override bool Contains(DbBatchCommand item) => m_commands.Contains((SingleStoreBatchCommand) item);
	public override void CopyTo(DbBatchCommand[] array, int arrayIndex) => throw new NotImplementedException();
	public override IEnumerator<DbBatchCommand> GetEnumerator()
	{
		foreach (var command in m_commands)
			yield return command;
	}
	public override int IndexOf(DbBatchCommand item) => m_commands.IndexOf((SingleStoreBatchCommand) item);
	public override void Insert(int index, DbBatchCommand item) => m_commands.Insert(index, (SingleStoreBatchCommand) item);
	public override bool Remove(DbBatchCommand item) => m_commands.Remove((SingleStoreBatchCommand) item);
	public override void RemoveAt(int index) => m_commands.RemoveAt(index);
	protected override DbBatchCommand GetBatchCommand(int index) => m_commands[index];
	protected override void SetBatchCommand(int index, DbBatchCommand batchCommand) => m_commands[index] = (SingleStoreBatchCommand) batchCommand;
#else
	public SingleStoreBatchCommand this[int index] { get => m_commands[index]; set => m_commands[index] = value; }
	public int Count => m_commands.Count;
	public bool IsReadOnly => false;
	public void Add(SingleStoreBatchCommand item) => m_commands.Add((SingleStoreBatchCommand) item);
	public void Clear() => m_commands.Clear();
	public bool Contains(SingleStoreBatchCommand item) => m_commands.Contains((SingleStoreBatchCommand) item);
	public void CopyTo(SingleStoreBatchCommand[] array, int arrayIndex) => throw new NotImplementedException();
	public IEnumerator<SingleStoreBatchCommand> GetEnumerator()
	{
		foreach (var command in m_commands)
			yield return command;
	}
	public int IndexOf(SingleStoreBatchCommand item) => m_commands.IndexOf((SingleStoreBatchCommand) item);
	public void Insert(int index, SingleStoreBatchCommand item) => m_commands.Insert(index, (SingleStoreBatchCommand) item);
	public bool Remove(SingleStoreBatchCommand item) => m_commands.Remove((SingleStoreBatchCommand) item);
	public void RemoveAt(int index) => m_commands.RemoveAt(index);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif

	internal IReadOnlyList<SingleStoreBatchCommand> Commands => m_commands;

	private readonly List<SingleStoreBatchCommand> m_commands;
}
