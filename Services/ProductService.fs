namespace Project.Services

open System
open Project.Core
open Project.Data

module ProductService =

    // Load products into a Map for fast lookup (by ID)
    let loadProductsIntoMap (conn: Microsoft.Data.Sqlite.SqliteConnection) : Map<int, Product> =
        let products = ProductRepository.getAllProducts conn
        products |> List.map (fun p -> p.Id, p) |> Map.ofList

    // Pure filtering function suitable for unit testing (no GUI deps)
    let filterProducts (products: Product list) (searchText: string) (category: string) : Product list =
        let search = if isNull searchText then "" else searchText.Trim().ToLower()
        let selectedCategory = if isNull category then "All" else category
        products
        |> List.filter (fun p ->
            let nameMatch = String.IsNullOrEmpty(search) || p.Name.ToLower().Contains(search)
            let categoryMatch = (selectedCategory = "All") || (p.Category = selectedCategory)
            nameMatch && categoryMatch)

    // Display products in a formatted list
    let displayProducts (products: Product list) =
        if products.IsEmpty then
            printfn "No products available."
        else
            printfn "\n=== Product Catalog ==="
            products |> List.iteri (fun i p ->
                printfn "%d. %s - $%.2f (%s)" (i + 1) p.Name p.Price p.Category
                if not (String.IsNullOrEmpty p.Description) then
                    printfn "   Description: %s" p.Description
                // Stock intentionally not shown in console product listing (UI handles visibility)
                if not (String.IsNullOrEmpty p.Brand) then
                    printfn "   Brand: %s" p.Brand
                if p.Rating > 0.0 then
                    printfn "   Rating: %.1f" p.Rating
                printfn ""
            )

    // Seed some sample products (call once for testing)
    let seedSampleProducts (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        let sampleProducts = [
            { Id = 0; Name = "Laptop"; Description = "High-performance laptop"; Price = 999.99m; Category = "Electronics"; Stock = 10; Brand = "BrandA"; Rating = 4.5; ImageUrl = "" }
            { Id = 0; Name = "Book"; Description = "Programming book"; Price = 29.99m; Category = "Books"; Stock = 50; Brand = ""; Rating = 4.0; ImageUrl = "" }
            { Id = 0; Name = "Shoes"; Description = "Running shoes"; Price = 79.99m; Category = "Clothing"; Stock = 20; Brand = "BrandB"; Rating = 4.2; ImageUrl = "" }
        ]
        sampleProducts |> List.iter (ProductRepository.insertProduct conn)

    // Add a new product interactively
    let addProduct (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        printfn "Enter product name:"
        let name = Console.ReadLine().Trim()
        if String.IsNullOrEmpty(name) then
            printfn "Name cannot be empty."
        else
            printfn "Enter description (optional):"
            let description = Console.ReadLine().Trim()
            printfn "Enter price:"
            let priceStr = Console.ReadLine().Trim()
            match System.Decimal.TryParse(priceStr) with
            | true, price ->
                printfn "Enter category:"
                let category = Console.ReadLine().Trim()
                if String.IsNullOrEmpty(category) then
                    printfn "Category cannot be empty."
                else
                    printfn "Enter stock:"
                    let stockStr = Console.ReadLine().Trim()
                    match System.Int32.TryParse(stockStr) with
                    | true, stock ->
                        printfn "Enter brand:"
                        let brand = Console.ReadLine().Trim()
                        printfn "Enter rating (optional, e.g., 4.5):"
                        let ratingStr = Console.ReadLine().Trim()
                        let rating = if String.IsNullOrEmpty(ratingStr) then 0.0 else (match System.Double.TryParse(ratingStr) with | true, r -> r | _ -> 0.0)
                        printfn "Enter image URL (optional):"
                        let imageUrl = Console.ReadLine().Trim()
                        let product = {
                            Id = 0
                            Name = name
                            Description = description
                            Price = price
                            Category = category
                            Stock = 0
                            Brand = brand
                            Rating = rating
                            ImageUrl = imageUrl
                        }
                        if String.IsNullOrWhiteSpace(brand) then
                            printfn "Brand is required. Product not added."
                        else
                            ProductRepository.insertProduct conn product
                    | _ -> printfn "Invalid stock number."
            | _ -> printfn "Invalid price."

    // Remove a product interactively
    let removeProduct (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        let products = ProductRepository.getAllProducts conn
        if products.IsEmpty then
            printfn "No products to remove."
        else
            displayProducts products
            printfn "Enter the ID of the product to remove:"
            let idStr = Console.ReadLine().Trim()
            match System.Int32.TryParse(idStr) with
            | true, id ->
                let deleted = ProductRepository.deleteProduct conn id
                if deleted then
                    printfn "Product removed."
                else
                    printfn "Cannot remove product: it is referenced by other data."
            | _ -> printfn "Invalid ID."
