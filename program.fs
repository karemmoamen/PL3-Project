open System
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Microsoft.Data.Sqlite
open Project.Data
open Project.Core
open Project.UI
open Project.Services

[<EntryPoint>]
let main argv =
    // 1️⃣ Initialize the database (creates file & tables if not exist)
    Database.initializeDatabase()

    // 2️⃣ Open a connection
    let conn = Database.getConnection()
    conn.Open()
    
    // Seed sample products if none exist
    let existingProducts = ProductRepository.getAllProducts conn
    if existingProducts.IsEmpty then
        ProductService.seedSampleProducts conn

    // 3️⃣ Build Avalonia app
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .AfterSetup(fun _ ->
            match Avalonia.Application.Current with
            | null -> ()
            | app ->
                match app.ApplicationLifetime with
                | :? IClassicDesktopStyleApplicationLifetime as desktop ->
                    let mainWindow = MainWindow()
                    mainWindow.SetConnection(conn)
                    desktop.MainWindow <- mainWindow
                    
                    desktop.Exit.Add(fun _ ->
                        conn.Close()
                        printfn "Application closed.")
                | _ -> ())
        .StartWithClassicDesktopLifetime(argv)
        |> ignore
    
    0 // Exit code
