namespace Project.Data

open System
open Microsoft.Data.Sqlite
open Project.Core

module ProductRepository =

    // Get all products from DB
    let getAllProducts (conn: SqliteConnection) : Product list =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "SELECT Id, Name, Description, Price, Category, Stock, Brand, Rating, ImageUrl FROM Products"
        use reader = cmd.ExecuteReader()
        let rec readProducts acc =
            if reader.Read() then
                let product = {
                    Id = reader.GetInt32(0)
                    Name = reader.GetString(1)
                    Description = if reader.IsDBNull(2) then "" else reader.GetString(2)
                    Price = reader.GetDecimal(3)
                    Category = reader.GetString(4)
                    Stock = reader.GetInt32(5)
                    Brand = if reader.IsDBNull(6) then "" else reader.GetString(6)
                    Rating = if reader.IsDBNull(7) then 0.0 else reader.GetDouble(7)
                    ImageUrl = if reader.IsDBNull(8) then "" else reader.GetString(8)
                }
                readProducts (product :: acc)
            else
                List.rev acc
        readProducts []

    // Insert a new product
    let insertProduct (conn: SqliteConnection) (product: Product) =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
        INSERT INTO Products (Name, Description, Price, Category, Stock, Brand, Rating, ImageUrl)
        VALUES (@name, @description, @price, @category, @stock, @brand, @rating, @imageUrl)
        """
        cmd.Parameters.AddWithValue("@name", product.Name) |> ignore
        cmd.Parameters.AddWithValue("@description", product.Description) |> ignore
        cmd.Parameters.AddWithValue("@price", product.Price) |> ignore
        cmd.Parameters.AddWithValue("@category", product.Category) |> ignore
        cmd.Parameters.AddWithValue("@stock", product.Stock) |> ignore
        cmd.Parameters.AddWithValue("@brand", product.Brand) |> ignore
        cmd.Parameters.AddWithValue("@rating", product.Rating) |> ignore
        cmd.Parameters.AddWithValue("@imageUrl", product.ImageUrl) |> ignore
        cmd.ExecuteNonQuery() |> ignore
        printfn "Product '%s' added successfully." product.Name

    // Delete a product
    let deleteProduct (conn: SqliteConnection) (productId: int) : bool =
        try
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "DELETE FROM Products WHERE Id = @id"
            cmd.Parameters.AddWithValue("@id", productId) |> ignore
            let rowsAffected = cmd.ExecuteNonQuery()
            if rowsAffected > 0 then
                printfn "Product with ID %d deleted successfully." productId
                true
            else
                printfn "No product found with ID %d." productId
                false
        with
        | :? SqliteException as ex when ex.SqliteErrorCode = 19 ->
            printfn "Failed to delete product %d: foreign key constraint." productId
            false
        | ex ->
            printfn "Unexpected error deleting product %d: %s" productId ex.Message
            false
