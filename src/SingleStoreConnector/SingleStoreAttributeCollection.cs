using System.Collections;

namespace SingleStoreConnector
{
	/// <summary>
	/// <see cref="SingleStoreAttributeCollection"/> represents a collection of query attributes that can be added to a <see cref="SingleStoreCommand"/>.
	/// </summary>
	public sealed class SingleStoreAttributeCollection : IEnumerable<SingleStoreAttribute>
	{
		/// <summary>
		/// Returns the number of attributes in the collection.
		/// </summary>
		public int Count => m_attributes.Count;

		/// <summary>
		/// Adds a new <see cref="SingleStoreAttribute"/> to the collection.
		/// </summary>
		/// <param name="attribute">The attribute to add.</param>
		/// <remarks>The attribute name must not be empty, and must not already exist in the collection.</remarks>
		public void Add(SingleStoreAttribute attribute)
		{
#if NET6_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(attribute);
#else
			if (attribute is null)
				throw new ArgumentNullException(nameof(attribute));
#endif
#if NET8_0_OR_GREATER
			ArgumentException.ThrowIfNullOrEmpty(attribute.AttributeName);
#else
			if (string.IsNullOrEmpty(attribute.AttributeName))
				throw new ArgumentException("Attribute name must not be empty", nameof(attribute));
#endif
			foreach (var existingAttribute in m_attributes)
			{
				if (existingAttribute.AttributeName == attribute.AttributeName)
					throw new ArgumentException($"An attribute with the name {attribute.AttributeName} already exists in the collection", nameof(attribute));
			}
			m_attributes.Add(attribute);
		}

		/// <summary>
		/// Sets the attribute with the specified name to the given value, overwriting it if it already exists.
		/// </summary>
		/// <param name="attributeName">The attribute name.</param>
		/// <param name="value">The attribute value.</param>
		public void SetAttribute(string attributeName, object? value)
		{
#if NET8_0_OR_GREATER
			ArgumentException.ThrowIfNullOrEmpty(attributeName);
#else
			if (string.IsNullOrEmpty(attributeName))
				throw new ArgumentException("Attribute name must not be empty", nameof(attributeName));
#endif
			for (var i = 0; i < m_attributes.Count; i++)
			{
				if (m_attributes[i].AttributeName == attributeName)
				{
					m_attributes[i] = new SingleStoreAttribute(attributeName, value);
					return;
				}
			}
			m_attributes.Add(new SingleStoreAttribute(attributeName, value));
		}

		/// <summary>
		/// Gets the attribute at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="SingleStoreAttribute"/> at that index.</returns>
		public SingleStoreAttribute this[int index] => m_attributes[index];

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear() => m_attributes.Clear();

		/// <summary>
		/// Returns an enumerator for the collection.
		/// </summary>
		public IEnumerator<SingleStoreAttribute> GetEnumerator() => m_attributes.GetEnumerator();

		/// <summary>
		/// Removes the specified attribute from the collection.
		/// </summary>
		/// <param name="attribute">The attribute to remove.</param>
		/// <returns><c>true</c> if that attribute was removed; otherwise, <c>false</c>.</returns>
		public bool Remove(SingleStoreAttribute attribute) => m_attributes.Remove(attribute);

		/// <summary>
		/// Returns an enumerator for the collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal SingleStoreAttributeCollection() => m_attributes = [];

		private readonly List<SingleStoreAttribute> m_attributes;
	}
}
