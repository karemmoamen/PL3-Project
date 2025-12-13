namespace Project.Services

open System
open Project.Core

module CartService =

    // Pure function: Add item to cart (immutable)
    // Returns a new cart with the item added or quantity updated
    let addToCart (cart: Cart) (productId: int) (quantity: int) : Cart =
        let existingItem = cart |> List.tryFind (fun item -> item.ProductId = productId)
        match existingItem with
        | Some item ->
            // Update quantity if item already exists
            cart 
            |> List.map (fun i -> 
                if i.ProductId = productId 
                then { i with Quantity = i.Quantity + quantity }
                else i)
        | None ->
            // Add new item
            { ProductId = productId; Quantity = quantity } :: cart

    // Pure function: Remove item from cart (immutable)
    // Returns a new cart without the specified item
    let removeFromCart (cart: Cart) (productId: int) : Cart =
        cart |> List.filter (fun item -> item.ProductId <> productId)

    // Pure function: Update quantity of an item in cart (immutable)
    let updateQuantity (cart: Cart) (productId: int) (newQuantity: int) : Cart =
        if newQuantity <= 0 then
            removeFromCart cart productId
        else
            cart 
            |> List.map (fun item -> 
                if item.ProductId = productId 
                then { item with Quantity = newQuantity }
                else item)

    // Pure function: Get item from cart by product ID
    let getCartItem (cart: Cart) (productId: int) : CartItem option =
        cart |> List.tryFind (fun item -> item.ProductId = productId)

    // Pure function: Calculate total price of cart
    // Takes a Map<int, Product> for product lookup
    let calculateTotal (cart: Cart) (productMap: Map<int, Product>) : decimal =
        cart
        |> List.fold (fun total item ->
            match Map.tryFind item.ProductId productMap with
            | Some product -> total + (product.Price * decimal item.Quantity)
            | None -> total) 0m

    // Pure function: Get cart items with full product details
    let getCartItemsWithProducts (cart: Cart) (productMap: Map<int, Product>) : (CartItem * Product) list =
        cart
        |> List.choose (fun item ->
            match Map.tryFind item.ProductId productMap with
            | Some product -> Some (item, product)
            | None -> None)

    // Display cart contents
    let displayCart (cart: Cart) (productMap: Map<int, Product>) =
        if cart.IsEmpty then
            printfn "\nYour cart is empty."
        else
            printfn "\n=== Shopping Cart ==="
            let itemsWithProducts = getCartItemsWithProducts cart productMap
            itemsWithProducts
            |> List.iteri (fun i (item, product) ->
                let subtotal = product.Price * decimal item.Quantity
                printfn "%d. %s x%d" (i + 1) product.Name item.Quantity
                printfn "   Price: $%.2f each, Subtotal: $%.2f" product.Price subtotal)
            
            let total = calculateTotal cart productMap
            printfn "\nTotal: $%.2f" total

    // Check if cart is empty
    let isEmpty (cart: Cart) : bool = cart.IsEmpty


