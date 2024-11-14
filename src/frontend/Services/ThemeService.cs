﻿namespace frontend.Services;

public class ThemeService
{
    public bool State { get; set; }
    public event Action? StateChanged;

    public ThemeService()
    {
        State = true;
    }

    public void ToggleTheme()
    {
        State = !State;
        StateChanged?.Invoke();
    }
}