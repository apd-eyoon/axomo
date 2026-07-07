using Microsoft.Extensions.Options;

namespace StoreCreditor.Tests;

internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; private set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;

    public void Set(T value) => CurrentValue = value;
}
