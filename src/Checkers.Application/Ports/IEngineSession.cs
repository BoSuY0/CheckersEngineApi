namespace Checkers.Application.Ports;

/// <summary>
/// Exclusive use of one worker; disposing it returns the worker to the pool. Every request throws
/// <see cref="EngineFailureException"/> when the engine fails or reports an error.
/// </summary>
public interface IEngineSession : IEngineAdapter, ITablebase, IAsyncDisposable;
