// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>A minimal logging service. Messages are already formatted (use interpolated strings).</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface ILogService
{
    /// <summary>The minimum level of the messages that are logged.</summary>
    LogLevel Level { get; }

    /// <summary>Logs a trace message.</summary>
    void Trace(string message);

    /// <summary>Logs a debug message.</summary>
    void Debug(string message);

    /// <summary>Logs an info message.</summary>
    void Info(string message);

    /// <summary>Logs a warning.</summary>
    void Warn(string message);

    /// <summary>Logs an error.</summary>
    void Error(string message);
}

/// <summary>A log service that does nothing.</summary>
/// <remarks>Threading: stateless; safe to share between threads.</remarks>
internal sealed class NullLogService : ILogService
{
    /// <summary>The shared instance.</summary>
    public static NullLogService Instance { get; } = new();

    private NullLogService()
    {
    }

    /// <inheritdoc />
    public LogLevel Level => LogLevel.Off;

    /// <inheritdoc />
    public void Trace(string message)
    {
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
    }

    /// <inheritdoc />
    public void Info(string message)
    {
    }

    /// <inheritdoc />
    public void Warn(string message)
    {
    }

    /// <inheritdoc />
    public void Error(string message)
    {
    }
}
