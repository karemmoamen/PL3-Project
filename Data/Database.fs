namespace Project.Data

open System
open System.IO
open Microsoft.Data.Sqlite
open Project.Core   // gives access to User and RoleHelpers

module Database =

    // Path to the database file
    let dbPath = "store.db"

    // Create and return a SQLite connection
    let getConnection () : SqliteConnection =
        let connString = $"Data Source={dbPath}"
        new SqliteConnection(connString)

    // SQL for creating all tables
    let private createTables (conn: SqliteConnection) =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS Users (
            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            Password TEXT NOT NULL,
            Role     TEXT NOT NULL CHECK(Role IN ('admin', 'user'))
        );

        CREATE TABLE IF NOT EXISTS Products (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT NOT NULL,
            Description TEXT,
            Price       REAL NOT NULL,
            Category    TEXT NOT NULL,
            Stock       INTEGER NOT NULL,
            Brand       TEXT,
            Rating      REAL,
            ImageUrl    TEXT
        );

        CREATE TABLE IF NOT EXISTS Carts (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId    INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY(UserId) REFERENCES Users(Id)
        );

        CREATE TABLE IF NOT EXISTS CartItems (
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            CartId    INTEGER NOT NULL,
            ProductId INTEGER NOT NULL,
            Quantity  INTEGER NOT NULL,
            FOREIGN KEY(CartId) REFERENCES Carts(Id),
            FOREIGN KEY(ProductId) REFERENCES Products(Id)
        );
        """
        cmd.ExecuteNonQuery() |> ignore

    // Initialize DB (called on program startup)
    let initializeDatabase () =
        let createNewFile =
            if not (File.Exists dbPath) then
                printfn "Creating new database file: %s" dbPath
                use f = File.Create(dbPath)
                true
            else false

        use conn = getConnection ()
        conn.Open()
        createTables conn

        if createNewFile then
            printfn "Database created and initialized."
        else
            printfn "Database already exists."

        conn.Close()
