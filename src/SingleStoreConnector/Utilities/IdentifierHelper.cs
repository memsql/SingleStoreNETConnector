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
		ValidateIdentifierInput(identifier, nameof(identifier), "Identifier");

		return "`" + identifier.Replace("`", "``") + "`";
	}

	/// <summary>
	/// Quotes a qualified identifier (e.g., "database.table" becomes "`database`.`table`").
	/// </summary>
	public static string QuoteQualifiedIdentifier(string qualifiedName)
	{
		ValidateIdentifierInput(qualifiedName, nameof(qualifiedName), "Qualified name");

		var parts = SplitQualifiedIdentifier(qualifiedName);
		return string.Join(".", parts.Select(QuoteIdentifier));
	}

	private static void ValidateIdentifierInput(string value, string paramName, string displayName)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException($"{displayName} cannot be null or empty.", paramName);

		if (value.Contains('\0'))
			throw new ArgumentException($"{displayName} cannot contain null characters.", paramName);
	}

	private static List<string> SplitQualifiedIdentifier(string qualifiedName)
	{
		var parts = new List<string>();
		var current = new StringBuilder();
		var inBackticks = false;

		for (var i = 0; i < qualifiedName.Length; i++)
		{
			var ch = qualifiedName[i];

			if (ch == '.' && !inBackticks)
			{
				AddPart(parts, current, qualifiedName);
				continue;
			}

			if (ch != '`')
			{
				current.Append(ch);
				continue;
			}

			if (!inBackticks)
			{
				if (IsOnlyWhitespace(current))
				{
					// Opening backtick at the start of an identifier part.
					current.Append(ch);
					inBackticks = true;
				}
				else
				{
					// Literal backtick in an unquoted identifier.
					current.Append(ch);
				}

				continue;
			}

			if (i + 1 < qualifiedName.Length && qualifiedName[i + 1] == '`')
			{
				// Escaped backtick inside a quoted identifier.
				current.Append("``");
				i++;
				continue;
			}

			// Closing backtick.
			current.Append(ch);
			inBackticks = false;
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

			if (ch != '`')
			{
				identifier.Append(ch);
				continue;
			}

			// Escaped backtick: `` means one literal ` inside the identifier.
			if (i + 1 < part.Length && part[i + 1] == '`')
			{
				identifier.Append('`');
				i++;
				continue;
			}

			// Single backtick closes the quoted identifier.
			if (i == part.Length - 1)
				return identifier.ToString();

			throw new ArgumentException(
				"Qualified name contains unexpected characters after a quoted identifier.",
				nameof(qualifiedName));
		}

		throw new ArgumentException(
			"Qualified name contains an unterminated quoted identifier.",
			nameof(qualifiedName));
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
