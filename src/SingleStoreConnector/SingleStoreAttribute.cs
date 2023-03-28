namespace SingleStoreConnector
{
	/// <summary>
	/// <see cref="SingleStoreAttribute"/> represents an attribute that can be sent with a SingleStore query.
	/// </summary>
	/// <remarks>See <a href="https://dev.mysql.com/doc/refman/8.0/en/query-attributes.html">Query Attributes</a> for information on using query attributes.</remarks>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
	public sealed class SingleStoreAttribute : ICloneable
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
	{
		/// <summary>
		/// Initializes a new <see cref="SingleStoreAttribute"/>.
		/// </summary>
		public SingleStoreAttribute()
		{
			AttributeName = "";
		}

		/// <summary>
		/// Initializes a new <see cref="SingleStoreAttribute"/> with the specified attribute name and value.
		/// </summary>
		public SingleStoreAttribute(string attributeName, object? value)
		{
			AttributeName = attributeName ?? "";
			Value = value;
		}

		/// <summary>
		/// Gets or sets the attribute name.
		/// </summary>
		public string AttributeName { get; set; }

		/// <summary>
		/// Gets or sets the attribute value.
		/// </summary>
		public object? Value { get; set; }

		/// <summary>
		/// Returns a new <see cref="SingleStoreAttribute"/> with the same property values as this instance.
		/// </summary>
		public SingleStoreAttribute Clone() => new SingleStoreAttribute(AttributeName, Value);

		object ICloneable.Clone() => Clone();

		internal SingleStoreParameter ToParameter()
		{
			if (string.IsNullOrEmpty(AttributeName))
				throw new InvalidOperationException("AttributeName must not be null or empty");
			return new(AttributeName, Value);
		}
	}
}
