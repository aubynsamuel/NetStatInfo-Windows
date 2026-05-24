namespace NetStatInfoWin.Services;

internal sealed class UsageLoadException(UsageLoadFailureKind kind, Exception? innerException = null)
    : Exception($"Unable to load attributed app usage: {kind}.", innerException)
{
    public UsageLoadFailureKind Kind { get; } = kind;
}
