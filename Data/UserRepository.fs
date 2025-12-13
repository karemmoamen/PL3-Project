namespace Project.Data

open System
open Microsoft.Data.Sqlite
open Project.Core   // gives access to User and RoleHelpers

module UserRepository =

    // Helper to check if user exists
    let userExists (conn: SqliteConnection) (username: string) : bool =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "SELECT COUNT(*) FROM Users WHERE Username = @username"
        cmd.Parameters.AddWithValue("@username", username) |> ignore
        let count = cmd.ExecuteScalar() :?> int64
        count > 0L

    // Helper to insert a user safely
    let insertUser (conn: SqliteConnection) (user: User) =
        if not (userExists conn user.Username) then
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "INSERT INTO Users (Username, Password, Role) VALUES (@username, @password, @role)"
            cmd.Parameters.AddWithValue("@username", user.Username) |> ignore
            cmd.Parameters.AddWithValue("@password", user.Password) |> ignore
            cmd.Parameters.AddWithValue("@role", RoleHelpers.roleToString user.Role) |> ignore
            cmd.ExecuteNonQuery() |> ignore
            printfn "User '%s' created successfully." user.Username
        else
            printfn "User '%s' already exists, skipping insert." user.Username

    // Authenticate user by username and password
    let authenticateUser (conn: SqliteConnection) (username: string) (password: string) : User option =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "SELECT Id, Username, Password, Role FROM Users WHERE Username = @username AND Password = @password"
        cmd.Parameters.AddWithValue("@username", username) |> ignore
        cmd.Parameters.AddWithValue("@password", password) |> ignore
        use reader = cmd.ExecuteReader()
        if reader.Read() then
            let id = reader.GetInt32(0)
            let uname = reader.GetString(1)
            let pwd = reader.GetString(2)
            let roleStr = reader.GetString(3)
            let role = RoleHelpers.stringToRole roleStr
            Some { Id = id; Username = uname; Password = pwd; Role = role }
        else
            None