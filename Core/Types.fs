namespace Project.Core

// Role discriminated union
type Role = 
    | Admin
    | User

// User record
type User = {
    Id: int
    Username: string
    Password: string
    Role: Role
}

// Product record
type Product = {
    Id: int
    Name: string
    Description: string
    Price: decimal
    Category: string
    Stock: int
    Brand: string
    Rating: float
    ImageUrl: string
}

// CartItem - represents a product with quantity in cart
type CartItem = {
    ProductId: int
    Quantity: int
}

// Cart - immutable list of cart items
type Cart = CartItem list

// Role helpers module
module RoleHelpers =
    let roleToString (role: Role) =
        match role with
        | Admin -> "admin"
        | User -> "user"

    let stringToRole (str: string) =
        match str.ToLower() with
        | "admin" -> Admin
        | "user" -> User
        | _ -> failwith $"Invalid role: {str}"
