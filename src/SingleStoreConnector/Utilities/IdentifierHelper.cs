using System;
using System.Linq;

namespace SingleStoreConnector.Utilities
{
	internal static class IdentifierHelper
	{
		/// <summary>
		/// Quotes a SQL identifier with backticks, escaping any backticks within the identifier.
		/// </summary>
		/// <param name="identifier">The identifier to quote.</param>
		/// <returns>The quoted identifier.</returns>
		/// <exception cref="ArgumentException">If identifier is null, empty, or contains null characters.</exception>
		public static string QuoteIdentifier(string identifier)
		{
			if (string.IsNullOrWhiteSpace(identifier))
				throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

			if (identifier.Contains('\0'))
				throw new ArgumentException("Identifier cannot contain null characters.", nameof(identifier));

			// Backticks inside the identifier must be doubled
			return "`" + identifier.Replace("`", "``") + "`";
		}

		/// <summary>
		/// Quotes a qualified identifier (e.g., "database.table" becomes "`database`.`table`").
		/// </summary>
		public static string QuoteQualifiedIdentifier(string qualifiedName)
		{
			if (string.IsNullOrWhiteSpace(qualifiedName))
				throw new ArgumentException("Qualified name cannot be null or empty.", nameof(qualifiedName));

			return string.Join(".", qualifiedName.Split('.').Select(QuoteIdentifier));
		}
	}
}
