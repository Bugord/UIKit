namespace UIKit
{
    /// <summary>
    /// Where a screen opens. Screens stack and only the top one is shown; popups stack above
    /// every screen and stay visible underneath each other.
    /// </summary>
    public enum UILayer
    {
        Screen,
        Popup
    }
}
