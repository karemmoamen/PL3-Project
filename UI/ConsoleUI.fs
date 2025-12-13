namespace Project.UI

open System
open Project.Core
open Project.Services

module ConsoleUI =

    // Function to display menu when not logged in
    let showLoginMenu () =
        printfn "\n=== Store App - Login Menu ==="
        printfn "1. Login"
        printfn "2. Create a new user"
        printfn "3. Exit"
        printf "Choose an option: "

    // Function to display menu when logged in (role-based)
    let showUserMenu (user: User) =
        printfn "\n=== Store App - Welcome, %s (%s) ===" user.Username (RoleHelpers.roleToString user.Role)
        match user.Role with
        | Admin ->
            printfn "1. Add Product"
            printfn "2. Remove Product"
            printfn "3. Logout"
            printfn "4. Exit"
        | User ->
            printfn "1. View Products"
            printfn "2. View Cart"
            printfn "3. Add to Cart"
            printfn "4. Remove from Cart"
            printfn "5. Checkout"
            printfn "6. Logout"
            printfn "7. Exit"
        printf "Choose an option: "

    // Main loop for the console UI with session tracking
    let runConsoleUI (conn: Microsoft.Data.Sqlite.SqliteConnection) =
        let mutable currentUser: User option = None
        let mutable cart: Cart = []  // Immutable cart state
        let mutable productMap: Map<int, Product> = Map.empty  // Product catalog Map
        let mutable running = true
        
        // Load product map
        productMap <- ProductService.loadProductsIntoMap conn
        
        while running do
            match currentUser with
            | None ->
                // Not logged in: show login menu
                showLoginMenu()
                let choice = Console.ReadLine().Trim()
                match choice with
                | "1" -> 
                    currentUser <- UserService.loginUser conn
                    // Reset cart on login
                    cart <- []
                | "2" -> UserService.createUser conn
                | "3" -> 
                    running <- false
                    printfn "Exiting..."
                | _ -> printfn "Invalid option. Please try again."
            | Some user ->
                // Logged in: show user menu
                showUserMenu user
                let choice = Console.ReadLine().Trim()
                match choice with
                | "1" -> 
                    match user.Role with
                    | Admin -> 
                        ProductService.addProduct conn
                        // Reload product map after adding
                        productMap <- ProductService.loadProductsIntoMap conn
                    | User -> 
                        // View Products
                        let products = productMap |> Map.toList |> List.map snd
                        ProductService.displayProducts products
                | "2" -> 
                    match user.Role with
                    | Admin -> 
                        ProductService.removeProduct conn
                        // Reload product map after removal
                        productMap <- ProductService.loadProductsIntoMap conn
                    | User -> 
                        // View Cart
                        CartService.displayCart cart productMap
                | "3" -> 
                    match user.Role with
                    | Admin -> 
                        printfn "Logging out..."
                        currentUser <- None
                    | User -> 
                        // Add to Cart
                        printfn "\nEnter product ID to add:"
                        let productIdStr = Console.ReadLine().Trim()
                        match System.Int32.TryParse(productIdStr) with
                        | true, productId ->
                            if Map.containsKey productId productMap then
                                printfn "Enter quantity:"
                                let quantityStr = Console.ReadLine().Trim()
                                match System.Int32.TryParse(quantityStr) with
                                | true, quantity when quantity > 0 ->
                                    cart <- CartService.addToCart cart productId quantity
                                    let product = Map.find productId productMap
                                    printfn "Added %d x %s to cart." quantity product.Name
                                | _ -> printfn "Invalid quantity."
                            else
                                printfn "Product ID not found."
                        | _ -> printfn "Invalid product ID."
                | "4" -> 
                    match user.Role with
                    | Admin -> 
                        printfn "Exiting..."
                        running <- false
                    | User -> 
                        // Remove from Cart
                        if CartService.isEmpty cart then
                            printfn "Cart is empty."
                        else
                            CartService.displayCart cart productMap
                            printfn "\nEnter product ID to remove:"
                            let productIdStr = Console.ReadLine().Trim()
                            match System.Int32.TryParse(productIdStr) with
                            | true, productId ->
                                match CartService.getCartItem cart productId with
                                | Some item ->
                                    cart <- CartService.removeFromCart cart productId
                                    let product = Map.find productId productMap
                                    printfn "Removed %s from cart." product.Name
                                | None -> printfn "Product not in cart."
                            | _ -> printfn "Invalid product ID."
                | "5" -> 
                    match user.Role with
                    | Admin -> ()  // No 5 for admin
                    | User -> 
                        // Checkout
                        if CartService.isEmpty cart then
                            printfn "Cart is empty. Nothing to checkout."
                        else
                            CartService.displayCart cart productMap
                            printfn "\nProceed with checkout? (y/n)"
                            let confirm = Console.ReadLine().Trim().ToLower()
                            if confirm = "y" then
                                let total = CartService.calculateTotal cart productMap
                                printfn "\n=== Checkout Summary ==="
                                printfn "Total: $%.2f" total
                                printfn "\nCheckout completed!"
                                // Clear cart after checkout
                                cart <- []
                            else
                                printfn "Checkout cancelled."
                | "6" -> 
                    match user.Role with
                    | Admin -> ()  // No 6 for admin
                    | User -> 
                        printfn "Logging out..."
                        currentUser <- None
                        // Clear cart on logout
                        cart <- []
                | "7" -> 
                    match user.Role with
                    | Admin -> ()  // No 7 for admin
                    | User -> 
                        printfn "Exiting..."
                        running <- false
                | _ -> printfn "Invalid option. Please try again."