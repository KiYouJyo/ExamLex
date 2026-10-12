namespace ExamLex.Services;
public static class WindowPinService
{
    private static Action<bool>? _apply;
    public static bool IsPinned { get; private set; }
    public static event Action<bool>? PinnedChanged;
    public static void Configure(Action<bool> apply)
    {
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _apply(IsPinned);
    }
    public static void SetPinned(bool pinned)
    {
        if (IsPinned == pinned) return;
        _apply?.Invoke(pinned);
        IsPinned = pinned;
        PinnedChanged?.Invoke(pinned);
    }
}
