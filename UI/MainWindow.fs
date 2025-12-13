namespace Project.UI

open System
open System.Collections.ObjectModel
open Avalonia.Controls
open Avalonia.Controls.Templates
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Microsoft.Data.Sqlite
open Project.Core
open Project.Data
open Project.Services

type MainWindow() as this =
    inherit Window()

    let mutable conn: SqliteConnection option = None
    let mutable currentUser: User option = None
    let mutable cart: Cart = []  // Immutable cart state
    let mutable productMap: Map<int, Product> = Map.empty  // Product catalog Map
    
    // UI Components - Login
    let mutable loginPanel: Panel = Unchecked.defaultof<Panel>
    let mutable usernameBox: TextBox = Unchecked.defaultof<TextBox>
    let mutable passwordBox: TextBox = Unchecked.defaultof<TextBox>
    let mutable roleBox: ComboBox = Unchecked.defaultof<ComboBox>
    let mutable statusText: TextBlock = Unchecked.defaultof<TextBlock>
    
    // UI Components - Main View
    let mutable mainPanel: Panel = Unchecked.defaultof<Panel>
    let mutable welcomeText: TextBlock = Unchecked.defaultof<TextBlock>
    
    // UI Components - Products
    let mutable productListBox: ListBox = Unchecked.defaultof<ListBox>
    let products = ObservableCollection<Product>()
    
    // UI Components - Search / Filter
    let mutable searchBox: TextBox = Unchecked.defaultof<TextBox>
    let mutable categoryFilterBox: ComboBox = Unchecked.defaultof<ComboBox>
    let mutable searchBtn: Button = Unchecked.defaultof<Button>
    let mutable clearFilterBtn: Button = Unchecked.defaultof<Button>
    
    // UI Components - Cart
    let mutable cartListBox: ListBox = Unchecked.defaultof<ListBox>
    let cartItems = ObservableCollection<string>()
    let mutable cartTotalText: TextBlock = Unchecked.defaultof<TextBlock>
    
    // UI Components - Admin
    let mutable adminPanel: StackPanel = Unchecked.defaultof<StackPanel>

    do
        this.InitializeComponent()

    member private this.InitializeComponent() =
        this.Title <- "Store App"
        this.Width <- 1000.0
        this.Height <- 700.0
        
        let mainGrid = Grid()
        mainGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
        mainGrid.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
        mainGrid.RowDefinitions.Add(RowDefinition(Height = GridLength(1.0, GridUnitType.Star)))
        
        // Status bar
        statusText <- TextBlock()
        statusText.Text <- "Welcome to Store App"
        statusText.Margin <- Avalonia.Thickness(10.0)
        statusText.FontSize <- 12.0
        Grid.SetRow(statusText, 0)
        mainGrid.Children.Add(statusText)
        
        // Login Panel
        loginPanel <- this.CreateLoginPanel()
        Grid.SetRow(loginPanel, 1)
        mainGrid.Children.Add(loginPanel)
        
        // Main Panel (hidden initially)
        mainPanel <- this.CreateMainPanel()
        Grid.SetRow(mainPanel, 1)
        mainPanel.IsVisible <- false
        mainGrid.Children.Add(mainPanel)
        
        this.Content <- mainGrid

    member private this.CreateLoginPanel() =
        let panel = StackPanel()
        panel.HorizontalAlignment <- HorizontalAlignment.Center
        panel.VerticalAlignment <- VerticalAlignment.Center
        panel.Margin <- Avalonia.Thickness(50.0)
        panel.Spacing <- 15.0
        
        let title = TextBlock()
        title.Text <- "Store App - Login"
        title.FontSize <- 24.0
        title.FontWeight <- FontWeight.Bold
        title.HorizontalAlignment <- HorizontalAlignment.Center
        panel.Children.Add(title)
        
        usernameBox <- TextBox()
        usernameBox.Watermark <- "Username"
        usernameBox.Width <- 300.0
        panel.Children.Add(usernameBox)
        
        passwordBox <- TextBox()
        passwordBox.Watermark <- "Password"
        passwordBox.PasswordChar <- '*'
        passwordBox.Width <- 300.0
        panel.Children.Add(passwordBox)
        
        let loginBtn = Button()
        loginBtn.Content <- "Login"
        loginBtn.Width <- 300.0
        loginBtn.Click.Add(fun _ -> this.OnLoginClick())
        panel.Children.Add(loginBtn)
        
        let separator = TextBlock()
        separator.Text <- "--- OR ---"
        separator.HorizontalAlignment <- HorizontalAlignment.Center
        panel.Children.Add(separator)
        
        let createTitle = TextBlock()
        createTitle.Text <- "Create New User"
        createTitle.FontSize <- 16.0
        createTitle.HorizontalAlignment <- HorizontalAlignment.Center
        panel.Children.Add(createTitle)
        
        roleBox <- ComboBox()
        roleBox.ItemsSource <- ["admin"; "user"]
        roleBox.SelectedIndex <- 1
        roleBox.Width <- 300.0
        panel.Children.Add(roleBox)
        
        let createBtn = Button()
        createBtn.Content <- "Create User"
        createBtn.Width <- 300.0
        createBtn.Click.Add(fun _ -> this.OnCreateUserClick())
        panel.Children.Add(createBtn)
        
        panel

    member private this.CreateMainPanel() =
        let panel = DockPanel()
        panel.Margin <- Avalonia.Thickness(10.0)
        
        // Top bar with welcome and logout
        let topBar = StackPanel()
        topBar.Orientation <- Orientation.Horizontal
        topBar.HorizontalAlignment <- HorizontalAlignment.Stretch
        DockPanel.SetDock(topBar, Dock.Top)
        topBar.Margin <- Avalonia.Thickness(0.0, 0.0, 0.0, 10.0)
        
        welcomeText <- TextBlock()
        welcomeText.Text <- ""
        welcomeText.FontSize <- 18.0
        welcomeText.FontWeight <- FontWeight.Bold
        welcomeText.Margin <- Avalonia.Thickness(0.0, 0.0, 20.0, 0.0)
        welcomeText.VerticalAlignment <- VerticalAlignment.Center
        topBar.Children.Add(welcomeText)
        
        let logoutBtn = Button()
        logoutBtn.Content <- "Logout"
        logoutBtn.Click.Add(fun _ -> this.Logout())
        topBar.Children.Add(logoutBtn)
        
        panel.Children.Add(topBar)
        
        // Content area - SplitView for User role
        let contentArea = Grid()
        
        // Admin controls
        adminPanel <- StackPanel()
        adminPanel.Orientation <- Orientation.Horizontal
        adminPanel.Spacing <- 10.0
        adminPanel.Margin <- Avalonia.Thickness(0.0, 0.0, 0.0, 10.0)
        
        let addBtn = Button()
        addBtn.Content <- "Add Product"
        addBtn.Click.Add(fun _ -> this.OnAddProductClick())
        adminPanel.Children.Add(addBtn)
        
        let removeBtn = Button()
        removeBtn.Content <- "Remove Product"
        removeBtn.Click.Add(fun _ -> this.OnRemoveProductClick())
        adminPanel.Children.Add(removeBtn)
        
        let refreshBtn = Button()
        refreshBtn.Content <- "Refresh Products"
        refreshBtn.Click.Add(fun _ -> this.LoadProducts())
        adminPanel.Children.Add(refreshBtn)
        
        adminPanel.IsVisible <- false
        
        // User view - Products and Cart side by side
        let userGrid = Grid()
        userGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(2.0, GridUnitType.Star)))
        userGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(10.0)))
        userGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.5, GridUnitType.Star)))
        
        // Products section
        let productsPanel = StackPanel()
        productsPanel.Margin <- Avalonia.Thickness(5.0)
        
        let productsTitle = TextBlock()
        productsTitle.Text <- "Product Catalog"
        productsTitle.FontSize <- 18.0
        productsTitle.FontWeight <- FontWeight.Bold
        productsTitle.Margin <- Avalonia.Thickness(0.0, 0.0, 0.0, 10.0)
        productsPanel.Children.Add(productsTitle)
        
        // Search / Filter controls
        let filterRow = StackPanel()
        filterRow.Orientation <- Orientation.Horizontal
        filterRow.Spacing <- 8.0
        filterRow.Margin <- Avalonia.Thickness(0.0, 0.0, 0.0, 10.0)

        searchBox <- TextBox()
        searchBox.Watermark <- "Search by name..."
        searchBox.Width <- 240.0
        filterRow.Children.Add(searchBox)

        categoryFilterBox <- ComboBox()
        categoryFilterBox.Width <- 160.0
        categoryFilterBox.ItemsSource <- ["All"]
        categoryFilterBox.SelectedIndex <- 0
        filterRow.Children.Add(categoryFilterBox)

        searchBtn <- Button()
        searchBtn.Content <- "Search"
        searchBtn.Click.Add(fun _ -> this.ApplyProductFilter())
        filterRow.Children.Add(searchBtn)

        clearFilterBtn <- Button()
        clearFilterBtn.Content <- "Clear"
        clearFilterBtn.Click.Add(fun _ ->
            searchBox.Text <- ""
            if categoryFilterBox <> null then categoryFilterBox.SelectedIndex <- 0
            this.ApplyProductFilter())
        filterRow.Children.Add(clearFilterBtn)

        productsPanel.Children.Add(filterRow)
        
        productListBox <- ListBox()
        productListBox.ItemsSource <- products
        productListBox.ItemTemplate <- 
            FuncDataTemplate<obj>(
                System.Func<obj, Avalonia.Controls.INameScope, Control>(fun data _ ->
                    match data with
                    | :? Product as product ->
                        let panel = StackPanel()
                        panel.Margin <- Avalonia.Thickness(5.0)
                        panel.Background <- Brushes.WhiteSmoke
                        
                        let nameText = TextBlock()
                        nameText.FontSize <- 16.0
                        nameText.FontWeight <- FontWeight.Bold
                        nameText.Text <- product.Name
                        panel.Children.Add(nameText)
                        
                        let priceText = TextBlock()
                        priceText.FontSize <- 14.0
                        priceText.Foreground <- Brushes.DarkGreen
                        priceText.Text <- sprintf "$%.2f" product.Price
                        panel.Children.Add(priceText)
                        
                        let detailsText = TextBlock()
                        detailsText.FontSize <- 12.0
                        detailsText.Text <- product.Category
                        panel.Children.Add(detailsText)
                        
                        if not (String.IsNullOrEmpty(product.Description)) then
                            let descText = TextBlock()
                            descText.FontSize <- 11.0
                            descText.Foreground <- Brushes.Gray
                            descText.Text <- product.Description
                            descText.TextWrapping <- TextWrapping.Wrap
                            panel.Children.Add(descText)
                        
                        // Add to cart button
                        let addToCartBtn = Button()
                        addToCartBtn.Content <- "Add to Cart"
                        addToCartBtn.Margin <- Avalonia.Thickness(0.0, 5.0, 0.0, 0.0)
                        addToCartBtn.Click.Add(fun _ -> this.OnAddToCartFromProduct(product.Id))
                        panel.Children.Add(addToCartBtn)
                        
                        panel :> Control
                    | _ -> TextBlock(Text = "Invalid item") :> Control))
        
        productsPanel.Children.Add(productListBox)
        Grid.SetColumn(productsPanel, 0)
        userGrid.Children.Add(productsPanel)
        
        // Cart section
        let cartPanel = StackPanel()
        cartPanel.Margin <- Avalonia.Thickness(5.0)
        cartPanel.Background <- Brushes.LightGray
        
        let cartTitle = TextBlock()
        cartTitle.Text <- "Shopping Cart"
        cartTitle.FontSize <- 18.0
        cartTitle.FontWeight <- FontWeight.Bold
        cartTitle.Margin <- Avalonia.Thickness(0.0, 0.0, 0.0, 10.0)
        cartPanel.Children.Add(cartTitle)
        
        cartListBox <- ListBox()
        cartListBox.ItemsSource <- cartItems
        cartListBox.Height <- 400.0
        cartListBox.ItemTemplate <- 
            FuncDataTemplate<obj>(
                System.Func<obj, Avalonia.Controls.INameScope, Control>(fun data _ ->
                    match data with
                    | :? string as itemText ->
                        let panel = StackPanel()
                        panel.Margin <- Avalonia.Thickness(5.0)
                        panel.Background <- Brushes.White
                        
                        let textBlock = TextBlock()
                        textBlock.Text <- itemText
                        textBlock.TextWrapping <- TextWrapping.Wrap
                        panel.Children.Add(textBlock)
                        
                        // Extract product ID from text (hacky but works)
                        // Format: "ProductName xQuantity - $Price"
                        let parts = itemText.Split([|" (ID: "|], StringSplitOptions.None)
                        if parts.Length > 1 then
                            let idPart = parts.[1].Replace(")", "")
                            match System.Int32.TryParse(idPart) with
                            | true, productId ->
                                let removeBtn = Button()
                                removeBtn.Content <- "Remove"
                                removeBtn.Margin <- Avalonia.Thickness(0.0, 5.0, 0.0, 0.0)
                                removeBtn.Click.Add(fun _ -> this.OnRemoveFromCart(productId))
                                panel.Children.Add(removeBtn)
                            | _ -> ()
                        
                        panel :> Control
                    | _ -> TextBlock(Text = "Invalid item") :> Control))
        
        cartPanel.Children.Add(cartListBox)
        
        cartTotalText <- TextBlock()
        cartTotalText.FontSize <- 16.0
        cartTotalText.FontWeight <- FontWeight.Bold
        cartTotalText.Margin <- Avalonia.Thickness(0.0, 10.0, 0.0, 10.0)
        cartPanel.Children.Add(cartTotalText)
        
        let checkoutBtn = Button()
        checkoutBtn.Content <- "Checkout"
        checkoutBtn.FontSize <- 14.0
        checkoutBtn.Height <- 40.0
        checkoutBtn.Click.Add(fun _ -> this.OnCheckoutClick())
        cartPanel.Children.Add(checkoutBtn)
        
        Grid.SetColumn(cartPanel, 2)
        userGrid.Children.Add(cartPanel)
        
        // Add panels to main content (don't add them to `contentArea` to avoid
        // assigning multiple visual parents to the same control)
        let mainContent = StackPanel()
        mainContent.Children.Add(adminPanel)
        mainContent.Children.Add(userGrid)

        panel.Children.Add(mainContent)
        
        panel

    member private this.OnLoginClick() =
        match conn with
        | Some c ->
            let username = usernameBox.Text.Trim()
            let password = passwordBox.Text.Trim()
            
            if String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password) then
                statusText.Text <- "Please enter username and password"
            else
                match UserRepository.authenticateUser c username password with
                | Some user ->
                    currentUser <- Some user
                    cart <- []  // Reset cart on login
                    this.LoadProducts()
                    this.UpdateCartDisplay()
                    this.ShowMainView()
                    statusText.Text <- sprintf "Logged in as %s" user.Username
                | None ->
                    statusText.Text <- "Login failed. Invalid credentials."
        | None ->
            statusText.Text <- "Database connection not available"

    member private this.OnCreateUserClick() =
        match conn with
        | Some c ->
            let username = usernameBox.Text.Trim()
            let password = passwordBox.Text.Trim()
            
            if String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password) then
                statusText.Text <- "Please enter username and password"
            else
                try
                    let roleStr = if roleBox.SelectedItem <> null then roleBox.SelectedItem.ToString() else "user"
                    let role = RoleHelpers.stringToRole roleStr
                    let newUser = { Id = 0; Username = username; Password = password; Role = role }
                    UserRepository.insertUser c newUser
                    statusText.Text <- sprintf "User '%s' created successfully" username
                    usernameBox.Text <- ""
                    passwordBox.Text <- ""
                with
                | ex -> statusText.Text <- sprintf "Error: %s" ex.Message
        | None ->
            statusText.Text <- "Database connection not available"

    member private this.ShowMainView() =
        loginPanel.IsVisible <- false
        mainPanel.IsVisible <- true
        
        match currentUser with
        | Some user ->
            welcomeText.Text <- sprintf "Welcome, %s (%s)" user.Username (RoleHelpers.roleToString user.Role)
            adminPanel.IsVisible <- (user.Role = Admin)
        | None -> ()

    member private this.Logout() =
        currentUser <- None
        cart <- []
        loginPanel.IsVisible <- true
        mainPanel.IsVisible <- false
        usernameBox.Text <- ""
        passwordBox.Text <- ""
        products.Clear()
        cartItems.Clear()
        statusText.Text <- "Logged out"

    member private this.LoadProducts() =
        match conn with
        | Some c ->
            productMap <- ProductService.loadProductsIntoMap c
            products.Clear()
            let productList = productMap |> Map.toList |> List.map snd
            for p in productList do
                products.Add(p)
            // populate categories for filter combo box
            let categories = productList |> List.map (fun p -> p.Category) |> List.filter (fun s -> not (String.IsNullOrEmpty s)) |> List.distinct
            let categoryItems = ["All"] @ categories
            try
                if categoryFilterBox <> null then
                    categoryFilterBox.ItemsSource <- categoryItems
                    categoryFilterBox.SelectedIndex <- 0
            with
            | _ -> ()

            statusText.Text <- sprintf "Loaded %d products" productList.Length
        | None -> ()

    member private this.UpdateCartDisplay() =
        cartItems.Clear()
        let itemsWithProducts = CartService.getCartItemsWithProducts cart productMap
        for (item, product) in itemsWithProducts do
            let subtotal = product.Price * decimal item.Quantity
            let itemText = sprintf "%s x%d - $%.2f (ID: %d)" product.Name item.Quantity subtotal item.ProductId
            cartItems.Add(itemText)
        
        let total = CartService.calculateTotal cart productMap
        if cart.IsEmpty then
            cartTotalText.Text <- "Cart is empty"
        else
            cartTotalText.Text <- sprintf "Total: $%.2f" total

    member private this.OnAddToCartFromProduct(productId: int) =
        // Simple quantity dialog - add 1 by default, but could be enhanced
        cart <- CartService.addToCart cart productId 1
        this.UpdateCartDisplay()
        match Map.tryFind productId productMap with
        | Some product -> statusText.Text <- sprintf "Added %s to cart" product.Name
        | None -> statusText.Text <- "Product added to cart"

    member private this.ApplyProductFilter() =
        let searchText = if isNull searchBox.Text then "" else searchBox.Text.Trim().ToLower()
        let selectedCategory =
            if categoryFilterBox <> null && categoryFilterBox.SelectedItem <> null then
                categoryFilterBox.SelectedItem.ToString()
            else
                "All"

        let allProducts = productMap |> Map.toList |> List.map snd
        let filtered =
            allProducts
            |> List.filter (fun p ->
                let nameMatch = String.IsNullOrEmpty(searchText) || p.Name.ToLower().Contains(searchText)
                let categoryMatch = (selectedCategory = "All") || (p.Category = selectedCategory)
                nameMatch && categoryMatch)

        products.Clear()
        for p in filtered do products.Add(p)
        statusText.Text <- sprintf "Showing %d of %d products" filtered.Length allProducts.Length

    member private this.OnRemoveFromCart(productId: int) =
        cart <- CartService.removeFromCart cart productId
        this.UpdateCartDisplay()
        match Map.tryFind productId productMap with
        | Some product -> statusText.Text <- sprintf "Removed %s from cart" product.Name
        | None -> statusText.Text <- "Item removed from cart"

    member private this.OnCheckoutClick() =
        if CartService.isEmpty cart then
            statusText.Text <- "Cart is empty. Nothing to checkout."
        else
            let total = CartService.calculateTotal cart productMap
            let message = sprintf "Total: $%.2f\n\nProceed with checkout?" total
            // Simple message box (could be enhanced with proper dialog)
            statusText.Text <- sprintf "Checkout completed! Total: $%.2f" total
            cart <- []
            this.UpdateCartDisplay()

    member private this.OnAddProductClick() =
        let dialog = Window()
        dialog.Title <- "Add Product"
        dialog.Width <- 400.0
        dialog.Height <- 500.0
        
        let panel = StackPanel()
        panel.Margin <- Avalonia.Thickness(20.0)
        panel.Spacing <- 10.0
        
        let nameBox = TextBox()
        nameBox.Watermark <- "Product Name"
        panel.Children.Add(nameBox)
        
        let descBox = TextBox()
        descBox.Watermark <- "Description"
        descBox.Height <- 60.0
        panel.Children.Add(descBox)
        
        let priceBox = TextBox()
        priceBox.Watermark <- "Price (e.g., 99.99)"
        panel.Children.Add(priceBox)
        
        let categoryBox = TextBox()
        categoryBox.Watermark <- "Category"
        panel.Children.Add(categoryBox)
        
        // Stock is managed in the database but not editable via this form
        
        let brandBox = TextBox()
        brandBox.Watermark <- "Brand"
        panel.Children.Add(brandBox)
        
        let ratingBox = TextBox()
        ratingBox.Watermark <- "Rating (optional, e.g., 4.5)"
        panel.Children.Add(ratingBox)
        
        let btnPanel = StackPanel()
        btnPanel.Orientation <- Orientation.Horizontal
        btnPanel.HorizontalAlignment <- HorizontalAlignment.Right
        btnPanel.Spacing <- 10.0
        
        let saveBtn = Button()
        saveBtn.Content <- "Save"
        saveBtn.Click.Add(fun _ ->
            try
                match conn with
                | Some c ->
                    let price = Decimal.Parse(priceBox.Text)
                    // stock removed from form; use default 0
                    let stock = 0
                    let rating = if String.IsNullOrEmpty(ratingBox.Text) then 0.0 else Double.Parse(ratingBox.Text)
                    let product = {
                        Id = 0
                        Name = nameBox.Text
                        Description = descBox.Text
                        Price = price
                        Category = categoryBox.Text
                        Stock = stock
                        Brand = brandBox.Text
                        Rating = rating
                        ImageUrl = ""
                    }
                    // Validate required fields
                    if String.IsNullOrWhiteSpace(product.Brand) then
                        statusText.Text <- "Brand is required"
                    else
                        ProductRepository.insertProduct c product
                    this.LoadProducts()
                    dialog.Close()
                    statusText.Text <- sprintf "Product '%s' added successfully" product.Name
                | None -> ()
            with
            | ex -> statusText.Text <- sprintf "Error: %s" ex.Message)
        
        let cancelBtn = Button()
        cancelBtn.Content <- "Cancel"
        cancelBtn.Click.Add(fun _ -> dialog.Close())
        
        btnPanel.Children.Add(cancelBtn)
        btnPanel.Children.Add(saveBtn)
        panel.Children.Add(btnPanel)
        
        dialog.Content <- panel
        dialog.ShowDialog(this) |> ignore

    member private this.OnRemoveProductClick() =
        match productListBox.SelectedItem with
        | :? Product as product ->
            match conn with
            | Some c ->
                let deleted = ProductRepository.deleteProduct c product.Id
                if deleted then
                    this.LoadProducts()
                    statusText.Text <- sprintf "Product '%s' removed" product.Name
                else
                    statusText.Text <- sprintf "Cannot remove '%s': referenced by other data." product.Name
            | None -> ()
        | _ ->
            statusText.Text <- "Please select a product to remove"

    member this.SetConnection(connection: SqliteConnection) =
        conn <- Some connection

