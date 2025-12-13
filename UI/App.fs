namespace Project.UI

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Styling
open Avalonia.Themes.Fluent

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- ThemeVariant.Light

    override this.OnFrameworkInitializationCompleted() =
        // MainWindow will be set in program.fs
        ()


