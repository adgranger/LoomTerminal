namespace LoomTerminal.Pty.Tests;

/// <summary>The command line splitter of the Unix pty: platform neutral, so it runs everywhere.</summary>
public sealed class PosixCommandLineTests
{
    [Theory]
    [InlineData("/bin/sh -c 'echo hi'", new[] { "/bin/sh", "-c", "echo hi" })]
    [InlineData("  a   b  ", new[] { "a", "b" })]
    [InlineData("a\\ b c", new[] { "a b", "c" })]
    [InlineData("\"a b\" \"c\\\"d\" \"e\\xf\"", new[] { "a b", "c\"d", "e\\xf" })]
    [InlineData("it\"'\"s 'a\"b'", new[] { "it's", "a\"b" })]
    [InlineData("'' x", new[] { "", "x" })]
    [InlineData("pre'mid'post", new[] { "premidpost" })]
    [InlineData("", new string[0])]
    public void Words_are_split_with_posix_rules(string commandLine, string[] expected)
    {
        Assert.Equal(expected, PosixCommandLine.Split(commandLine));
    }

    [Theory]
    [InlineData("echo 'unterminated")]
    [InlineData("echo \"unterminated")]
    public void An_unterminated_quote_is_rejected(string commandLine)
    {
        Assert.Throws<FormatException>(() => PosixCommandLine.Split(commandLine));
    }
}
