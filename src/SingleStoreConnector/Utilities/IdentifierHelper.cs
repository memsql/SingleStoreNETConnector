using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SingleStoreConnector.Utilities;

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

		// Backticks inside the identifier must be doubled.
		return "`" + identifier.Replace("`", "``") + "`";
	}

	/// <summary>
	/// Quotes a qualified identifier (e.g., "database.table" becomes "`database`.`table`").
	/// </summary>
	public static string QuoteQualifiedIdentifier(string qualifiedName)
	{
		if (string.IsNullOrWhiteSpace(qualifiedName))
			throw new ArgumentException("Qualified name cannot be null or empty.", nameof(qualifiedName));

		if (qualifiedName.Contains('\0'))
			throw new ArgumentException("Qualified name cannot contain null characters.", nameof(qualifiedName));

		// Split on dots that are outside backtick-quoted identifier parts.
		// This supports names such as:
		//   database.table
		//   `database`.`table`
		//   `database.with.dot`.`table`
		//   `database`.`table.with.dot`
		var parts = SplitQualifiedIdentifier(qualifiedName);

		return string.Join(".", parts.Select(QuoteIdentifier));
	}

	private static List<string> SplitQualifiedIdentifier(string qualifiedName)
	{
		var parts = new List<string>();
		var current = new StringBuilder();
		var inBackticks = false;

		for (var i = 0; i < qualifiedName.Length; i++)
		{
			var ch = qualifiedName[i];

			if (ch == '`')
			{
				if (inBackticks)
				{
					if (i + 1 < qualifiedName.Length && qualifiedName[i + 1] == '`')
					{
						// Escaped backtick inside a quoted identifier.
						current.Append("``");
						i++;
					}
					else
					{
						// Closing backtick.
						current.Append(ch);
						inBackticks = false;
					}
				}
				else if (IsOnlyWhitespace(current))
				{
					// Opening backtick at the start of an identifier part.
					current.Append(ch);
					inBackticks = true;
				}
				else
				{
					// Treat backticks in unquoted input as literal identifier characters.
					// They will be escaped later by QuoteIdentifier.
					current.Append(ch);
				}
			}
			else if (ch == '.' && !inBackticks)
			{
				AddPart(parts, current, qualifiedName);
			}
			else
			{
				current.Append(ch);
			}
		}

		if (inBackticks)
			throw new ArgumentException("Qualified name contains an unterminated quoted identifier.", nameof(qualifiedName));

		AddPart(parts, current, qualifiedName);
		return parts;
	}

	private static void AddPart(List<string> parts, StringBuilder current, string qualifiedName)
	{
		var part = current.ToString().Trim();

		if (part.Length == 0)
			throw new ArgumentException("Qualified name contains an empty identifier part.", nameof(qualifiedName));

		parts.Add(UnquoteIdentifierPart(part, qualifiedName));
		current.Clear();
	}

	private static string UnquoteIdentifierPart(string part, string qualifiedName)
	{
		if (part[0] != '`')
			return part;

		var identifier = new StringBuilder();

		for (var i = 1; i < part.Length; i++)
		{
			var ch = part[i];

			if (ch == '`')
			{
				if (i + 1 < part.Length && part[i + 1] == '`')
				{
					identifier.Append('`');
					i++;
				}
				else
				{
					if (i != part.Length - 1)
						throw new ArgumentException("Qualified name contains unexpected characters after a quoted identifier.", nameof(qualifiedName));

					return identifier.ToString();
				}
			}
			else
			{
				identifier.Append(ch);
			}
		}

		throw new ArgumentException("Qualified name contains an unterminated quoted identifier.", nameof(qualifiedName));
	}

	private static bool IsOnlyWhitespace(StringBuilder builder)
	{
		for (var i = 0; i < builder.Length; i++)
		{
			if (!char.IsWhiteSpace(builder[i]))
				return false;
		}

		return true;
	}
}
