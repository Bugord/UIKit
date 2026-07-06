namespace UIKit.Runtime.Core
{
    public readonly struct PopupOptions
    {
        public bool CancelOnBackdropClick { get; init; }
        public bool CancelOnNavigationCancel { get; init; }

        public static PopupOptions Default =>
            new() {
                CancelOnBackdropClick = true,
                CancelOnNavigationCancel = true
            };

        public static PopupOptions Blocking =>
            new() {
                CancelOnBackdropClick = false,
                CancelOnNavigationCancel = false
            };
    }
}