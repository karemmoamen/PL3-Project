namespace Tests

open Xunit
open Project.Services
open Project.Core

module CartServiceTests =

    [<Fact>]
    let ``addToCart adds new item`` () =
        let cart: Cart = []
        let cart2 = CartService.addToCart cart 1 2
        Assert.Equal(1, List.length cart2)
        let item = cart2 |> List.head
        Assert.Equal(1, item.ProductId)
        Assert.Equal(2, item.Quantity)

    [<Fact>]
    let ``addToCart updates quantity when exists`` () =
        let cart = [{ ProductId = 1; Quantity = 2 }]
        let cart2 = CartService.addToCart cart 1 3
        let item = cart2 |> List.find (fun i -> i.ProductId = 1)
        Assert.Equal(5, item.Quantity)

    [<Fact>]
    let ``removeFromCart removes item`` () =
        let cart = [{ ProductId = 1; Quantity = 2 }; { ProductId = 2; Quantity = 1 }]
        let cart2 = CartService.removeFromCart cart 1
        Assert.Equal(1, List.length cart2)
        Assert.True(cart2 |> List.exists (fun i -> i.ProductId = 2))

    [<Fact>]
    let ``updateQuantity removes when newQuantity <= 0`` () =
        let cart = [{ ProductId = 1; Quantity = 2 }]
        let cart2 = CartService.updateQuantity cart 1 0
        Assert.Empty(cart2)

    [<Fact>]
    let ``calculateTotal computes correct total`` () =
        let cart = [{ ProductId = 1; Quantity = 2 }; { ProductId = 2; Quantity = 1 }]
        let p1 = { Id = 1; Name = "A"; Description = ""; Price = 10.0m; Category = ""; Stock = 0; Brand = ""; Rating = 0.0; ImageUrl = "" }
        let p2 = { Id = 2; Name = "B"; Description = ""; Price = 5.5m; Category = ""; Stock = 0; Brand = ""; Rating = 0.0; ImageUrl = "" }
        let productMap = Map.ofList [ (1, p1); (2, p2) ]
        let total = CartService.calculateTotal cart productMap
        Assert.Equal(25.5m, total)

    [<Fact>]
    let ``updateQuantity changes to non-zero value`` () =
        let cart = [{ ProductId = 1; Quantity = 5 }]
        let cart2 = CartService.updateQuantity cart 1 3
        let item = cart2 |> List.find (fun i -> i.ProductId = 1)
        Assert.Equal(3, item.Quantity)

    [<Fact>]
    let ``functions are immutable - original cart unchanged`` () =
        let original = [{ ProductId = 1; Quantity = 2 }]
        let _ = CartService.addToCart original 2 1
        let _ = CartService.updateQuantity original 1 5
        let _ = CartService.removeFromCart original 1
        // original should remain unchanged
        Assert.Equal(1, List.length original)
        let item = original |> List.head
        Assert.Equal(2, item.Quantity)

    [<Fact>]
    let ``getCartItem returns Some for existing and None for missing`` () =
        let cart = [{ ProductId = 1; Quantity = 2 }]
        let some = CartService.getCartItem cart 1
        Assert.True(some.IsSome)
        let none = CartService.getCartItem cart 2
        Assert.True(none.IsNone)

    [<Fact>]
    let ``isEmpty returns correct values`` () =
        let empty: Cart = []
        Assert.True(CartService.isEmpty empty)
        let nonEmpty = [{ ProductId = 1; Quantity = 1 }]
        Assert.False(CartService.isEmpty nonEmpty)

    [<Fact>]
    let ``calculateTotal ignores missing products`` () =
        let cart = [{ ProductId = 1; Quantity = 1 }; { ProductId = 3; Quantity = 2 }]
        let p1 = { Id = 1; Name = "A"; Description = ""; Price = 7.0m; Category = ""; Stock = 0; Brand = ""; Rating = 0.0; ImageUrl = "" }
        let productMap = Map.ofList [ (1, p1) ]
        let total = CartService.calculateTotal cart productMap
        // only product 1 counted
        Assert.Equal(7.0m, total)

    [<Fact>]
    let ``getCartItemsWithProducts excludes missing products`` () =
        let cart = [{ ProductId = 1; Quantity = 1 }; { ProductId = 2; Quantity = 1 }]
        let p1 = { Id = 1; Name = "A"; Description = ""; Price = 3.0m; Category = ""; Stock = 0; Brand = ""; Rating = 0.0; ImageUrl = "" }
        let productMap = Map.ofList [ (1, p1) ]
        let list = CartService.getCartItemsWithProducts cart productMap
        Assert.Equal(1, List.length list)
