global using static GlobalExtensions;

internal static class GlobalExtensions
{
    public static bool Run(Action action)
    {
        action();
        return true;
    }
}
