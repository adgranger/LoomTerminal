using System.Text;

namespace LoomTerminal.Pty;

/// <summary>Splits a command line with POSIX shell rules (no expansion, no <c>sh -c</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class PosixCommandLine
{
    /// <summary>
    /// Whitespace separates words; single quotes keep everything literal; inside double quotes a backslash escapes
    /// only <c>"</c>, <c>\</c>, <c>$</c> and <c>`</c>; outside quotes a backslash escapes the next character.
    /// </summary>
    /// <param name="commandLine">The command line.</param>
    /// <returns>The words (an empty quoted word is kept).</returns>
    /// <exception cref="FormatException">A quote is not closed.</exception>
    public static List<string> Split(string commandLine)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        bool inWord = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\'')
            {
                int end = commandLine.IndexOf('\'', i + 1);
                if (end < 0)
                {
                    throw new FormatException("Unterminated single quote in the command line.");
                }

                word.Append(commandLine, i + 1, end - i - 1);
                i = end;
                inWord = true;
            }
            else if (c == '"')
            {
                i = AppendDoubleQuoted(commandLine, i + 1, word);
                inWord = true;
            }
            else if (c == '\\' && i + 1 < commandLine.Length)
            {
                word.Append(commandLine[++i]);
                inWord = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (inWord)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    inWord = false;
                }
            }
            else
            {
                word.Append(c);
                inWord = true;
            }
        }

        if (inWord)
        {
            words.Add(word.ToString());
        }

        return words;
    }

    // Returns the index of the closing quote.
    private static int AppendDoubleQuoted(string text, int start, StringBuilder word)
    {
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                return i;
            }

            if (c == '\\' && i + 1 < text.Length && text[i + 1] is '"' or '\\' or '$' or '`')
            {
                c = text[++i];
            }

            word.Append(c);
        }

        throw new FormatException("Unterminated double quote in the command line.");
    }
}
