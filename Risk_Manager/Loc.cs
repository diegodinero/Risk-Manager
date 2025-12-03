namespace Risk_Manager
{
    // Minimal shim so loc.key("...") compiles and returns the original string.
    // Replace with real localization call if your host provides one.
    internal static class loc
    {
        public static string key(string text) => text ?? string.Empty;
    }
}