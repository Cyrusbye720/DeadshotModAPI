namespace DeadshotModAPI;

/// <summary>
/// Supported keyboard keys for input callbacks.
/// </summary>
public enum Key
{
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    Digit0, Digit1, Digit2, Digit3, Digit4,
    Digit5, Digit6, Digit7, Digit8, Digit9,

    // Function keys
    F1, F2, F3, F4, F5, F6,
    F7, F8, F9, F10, F11, F12,

    // Navigation & editing
    UpArrow, DownArrow, LeftArrow, RightArrow,
    Home, End, PageUp, PageDown,
    Insert, Delete,

    // Modifiers
    LeftShift, RightShift,
    LeftCtrl, RightCtrl,
    LeftAlt, RightAlt,

    // Control & text editing
    Space, Enter, Escape, Tab, Backspace,

    // Symbols
    Minus, Equals, LeftBracket, RightBracket,
    Backslash, Semicolon, Apostrophe, Comma,
    Period, Slash, Backquote,

    // Numpad
    Numpad0, Numpad1, Numpad2, Numpad3, Numpad4,
    Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
    NumpadPlus, NumpadMinus, NumpadMultiply,
    NumpadDivide, NumpadPeriod, NumpadEnter,

    // Locks & system
    CapsLock, NumLock, ScrollLock,
    PrintScreen, Pause, Menu
}