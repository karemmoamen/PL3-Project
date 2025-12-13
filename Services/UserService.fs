namespace Project.Services

open System
open Project.Core
open Project.Data
open UserRepository

module UserService =

    // Function to create a new user interactively
    let createUser (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        printfn "Enter username:"
        let username = Console.ReadLine().Trim()
        if String.IsNullOrEmpty(username) then
            printfn "Username cannot be empty."
            ()
        else
            printfn "Enter password:"
            let password = Console.ReadLine().Trim()
            if String.IsNullOrEmpty(password) then
                printfn "Password cannot be empty."
                ()
            else
                printfn "Enter role (admin or user):"
                let roleStr = Console.ReadLine().Trim().ToLower()
                try
                    let role = RoleHelpers.stringToRole roleStr
                    let newUser = {
                        Id = 0 // Will be auto-assigned
                        Username = username
                        Password = password
                        Role = role
                    }
                    UserRepository.insertUser conn newUser
                    // Note: insertUser already prints success or skip message
                with
                | ex -> printfn "Error: %s" ex.Message

    // Function to login a user
    let loginUser (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        printfn "Enter username:"
        let username = Console.ReadLine().Trim()
        printfn "Enter password:"
        let password = Console.ReadLine().Trim()
        match UserRepository.authenticateUser conn username password with
        | Some user ->
            printfn "Login successful! Welcome, %s (%s)." user.Username (RoleHelpers.roleToString user.Role)
            Some user
        | None ->
            printfn "Login failed. Invalid username or password."
            None