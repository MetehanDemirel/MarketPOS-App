using MarketPOS.Data;
using MarketPOS.Services;
using Dapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MarketPOS.ViewModels
{
    public partial class MainViewModel
    {
        // --- Properties ---
        private ObservableCollection<Product> _salesProducts = new ObservableCollection<Product>();
        public ObservableCollection<Product> SalesProducts
        {
            get => _salesProducts;
            set { _salesProducts = value; OnPropertyChanged(); }
        }
        private ObservableCollection<CartItem>[] _carts = new ObservableCollection<CartItem>[3] 
        { 
            new ObservableCollection<CartItem>(), 
            new ObservableCollection<CartItem>(), 
            new ObservableCollection<CartItem>() 
        };
        private int _activeCartIndex = 0;

        public ObservableCollection<CartItem> Cart => _carts[_activeCartIndex];

        public int ActiveCartIndex
        {
            get => _activeCartIndex;
            set
            {
                if (_activeCartIndex != value && value >= 0 && value < 3)
                {
                    _activeCartIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Cart));
                    OnPropertyChanged(nameof(IsCart1Active));
                    OnPropertyChanged(nameof(IsCart2Active));
                    OnPropertyChanged(nameof(IsCart3Active));
                    UpdateTotal();
                }
            }
        }

        public bool IsCart1Active => _activeCartIndex == 0;
        public bool IsCart2Active => _activeCartIndex == 1;
        public bool IsCart3Active => _activeCartIndex == 2;

        private bool _isProductGridVisible = false;
        public bool IsProductGridVisible
        {
            get => _isProductGridVisible;
            set 
            { 
                _isProductGridVisible = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ProductColWidth));
            }
        }



        public string ProductColWidth => IsProductGridVisible ? "*" : "0";

        private bool _isCategoryView = true;
        public bool IsCategoryView
        {
             get => _isCategoryView;
             set { _isCategoryView = value; OnPropertyChanged(); }
        }

        public IEnumerable<string> FilteredCategories => Categories.Where(c => c != "TÜMÜ");

        private string _salesSearchInput;
        public string SalesSearchInput
        {
            get => _salesSearchInput;
            set
            {
                _salesSearchInput = value;
                OnPropertyChanged();
                CurrentSalesPage = 1; // RESET
            }
        }

        private int _currentSalesPage = 1;
        public int CurrentSalesPage
        {
            get => _currentSalesPage;
            set { _currentSalesPage = value; OnPropertyChanged(); }
        }

        private int _totalSalesPages = 1;
        public int TotalSalesPages
        {
            get => _totalSalesPages;
            set { _totalSalesPages = value; OnPropertyChanged(); }
        }

        private string _salesCategoryFilter = "TÜMÜ";
        public string SalesCategoryFilter
        {
            get => _salesCategoryFilter;
            set
            {
                _salesCategoryFilter = value;
                CurrentSalesPage = 1; // RESET
                OnPropertyChanged();
                TriggerSearch();
            }
        }
    
        private decimal _cartTotal;
        public decimal CartTotal
        {
            get => _cartTotal;
            set { _cartTotal = value; OnPropertyChanged(); }
        }

        // --- Commands ---
        public ICommand AddToCartCommand { get; private set; }
        public ICommand AddSpecificProductCommand { get; private set; }
        public ICommand OpenPaymentWindowCommand { get; private set; }
        public ICommand CheckoutVeresiyeCommand { get; private set; }
        public ICommand ClearCartCommand { get; private set; }
        public ICommand UndoLastCommand { get; private set; }
        public ICommand RemoveFromCartCommand { get; private set; }
        public ICommand IncreaseQtyCommand { get; private set; }
        public ICommand DecreaseQtyCommand { get; private set; }

        public ICommand FilterSalesCategoryCommand { get; private set; }
        public ICommand ToggleProductGridCommand { get; private set; }
        public ICommand AddQuickMoneyCommand { get; private set; }
        public ICommand BackToCategoriesCommand { get; private set; }
        public ICommand OpenCategoryCommand { get; private set; }

        public ICommand NextSalesPageCommand { get; private set; }
        public ICommand PrevSalesPageCommand { get; private set; }
        public ICommand SwitchCartCommand { get; private set; }
        public ICommand AddDiscountCommand { get; private set; }

        // --- Methods ---
        private void InitializeSalesCommands()
        {
             AddToCartCommand = new RelayCommand(_ => TryAddByBarcode());
             AddSpecificProductCommand = new RelayCommand(p => AddToCart((Product)p));
             OpenPaymentWindowCommand = new RelayCommand(_ => OpenPaymentWindow());
             CheckoutVeresiyeCommand = new RelayCommand(_ => ProcessCheckoutVeresiye());
             ClearCartCommand = new RelayCommand(_ => ClearCart());
             UndoLastCommand = new RelayCommand(_ => UndoLastTransaction());
             RemoveFromCartCommand = new RelayCommand(item => RemoveFromCart((CartItem)item));
             
             FilterSalesCategoryCommand = new RelayCommand(c => 
             {
                 SalesCategoryFilter = c.ToString(); 
                 IsCategoryView = false; // Switch to Product View
             });

             ToggleProductGridCommand = new RelayCommand(_ => IsProductGridVisible = !IsProductGridVisible);

             BackToCategoriesCommand = new RelayCommand(_ => IsCategoryView = true);

             // OpenCategoryCommand is essentially the same as FilterSalesCategory, but explicitly for the UI navigation
             OpenCategoryCommand = new RelayCommand(c =>
             {
                 SalesSearchInput = ""; // Clear search to show all products in category
                 CurrentSalesPage = 1;
                 SalesCategoryFilter = c.ToString();
                 IsCategoryView = false;
             });

             NextSalesPageCommand = new RelayCommand(_ => { if (CurrentSalesPage < TotalSalesPages) { CurrentSalesPage++; TriggerSearch(); } });
             PrevSalesPageCommand = new RelayCommand(_ => { if (CurrentSalesPage > 1) { CurrentSalesPage--; TriggerSearch(); } });

             SwitchCartCommand = new RelayCommand(param => 
             {
                 if (int.TryParse(param?.ToString(), out int idx))
                 {
                     ActiveCartIndex = idx;
                 }
             });

             AddQuickMoneyCommand = new RelayCommand(amount => 
             {
                 if (decimal.TryParse(amount?.ToString(), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out decimal val))
                 {
                     AddQuickMoneyToCart(val);
                 }
             });

             AddDiscountCommand = new RelayCommand(percentage => 
             {
                 if (int.TryParse(percentage?.ToString(), out int val))
                 {
                     ApplyDiscountToCart(val);
                 }
             });

             IncreaseQtyCommand = new RelayCommand(i => 
             { 
                 if (i is CartItem c) 
                 { 
                     if (c.Product.Stock > 0) 
                     { 
                         c.Quantity++; 
                         c.Product.Stock--; 
                         UpdateTotal(); 
                     } 
                     else 
                     {
                         ToastNotification.Show("Stokta kalmadı!", true); 
                     }
                 } 
             });

             DecreaseQtyCommand = new RelayCommand(i => 
             { 
                 if (i is CartItem c) 
                 { 
                     c.Product.Stock++; 
                     if (c.Quantity > 1) 
                         c.Quantity--; 
                     else 
                         Cart.Remove(c); 
                     
                     UpdateTotal(); 
                 } 
             });
        }

        private System.Threading.CancellationTokenSource _searchCts;

        private async void TriggerSearch()
        {
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // No delay needed for category click
                if (token.IsCancellationRequested) return;

                await SearchSalesAsync();
            }
            catch (System.Threading.Tasks.TaskCanceledException) { }
            catch (Exception) { }
        }

        private async System.Threading.Tasks.Task SearchSalesAsync()
        {
            string catFilter = SalesCategoryFilter;
            string searchInput = SalesSearchInput;

            await System.Threading.Tasks.Task.Run(() => 
            {
                try
                {
                   using var conn = _db.GetConnection();
                   string countQuery = "SELECT COUNT(*) FROM Products WHERE IsActive = 1";
                   string query = "SELECT * FROM Products WHERE IsActive = 1";
                   var p = new DynamicParameters();

                   if (catFilter != "TÜMÜ")
                   {
                       countQuery += " AND Category = @Category";
                       query += " AND Category = @Category";
                       p.Add("@Category", catFilter);
                   }

                   bool hasSearch = !string.IsNullOrWhiteSpace(searchInput);
                   if (hasSearch)
                   {
                       countQuery += " AND (Name LIKE @Search OR Barcode LIKE @Search)";
                       query += " AND (Name LIKE @Search OR Barcode LIKE @Search)";
                       p.Add("@Search", "%" + searchInput + "%");
                   }

                   int pageSize = 60;
                   int totalItems = Dapper.SqlMapper.ExecuteScalar<int>(conn, countQuery, p);
                   
                   int tp = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
                   Application.Current.Dispatcher.Invoke(() => TotalSalesPages = tp);
                   
                   int currentPageSafe = CurrentSalesPage;
                   if (currentPageSafe > tp) currentPageSafe = tp;
                   
                   int offset = (currentPageSafe - 1) * pageSize;

                   query += " ORDER BY Id DESC LIMIT @Limit OFFSET @Offset";
                   p.Add("@Limit", pageSize);
                   p.Add("@Offset", offset);

                   var results = Dapper.SqlMapper.Query<Product>(conn, query, p).ToList();

                   Application.Current.Dispatcher.Invoke(() =>
                   {
                       SalesProducts = new ObservableCollection<Product>(results);
                   });
                }
                catch (Exception) { }
            });
        }

        private async void TryAddByBarcode()
        {
             if (string.IsNullOrWhiteSpace(SalesSearchInput)) return;
             
             using var conn = _db.GetConnection();
             var product = await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Barcode = @B AND IsActive = 1", new { B = SalesSearchInput });

             if (product != null)
             {
                 AddToCart(product);
                 SalesSearchInput = "";
             }
             else
             {
                 ToastNotification.Show("Ürün bulunamadı!", true);
                 SalesSearchInput = ""; 
             }
        }

        private void AddToCart(Product p)
        {
            if (p.Stock <= 0)
            {
                ToastNotification.Show("Stok Yok", true);
                return;
            }

            var item = Cart.FirstOrDefault(i => i.Product.Id == p.Id);
            p.Stock--; 

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                Cart.Add(new CartItem { Product = p, Quantity = 1 });
            }
            UpdateTotal();
        }

        private void RemoveFromCart(CartItem item)
        {
            if (item == null) return;
            item.Product.Stock += item.Quantity; 
            Cart.Remove(item);
            UpdateTotal();
        }

        private void ClearCart()
        {
            foreach (var item in Cart)
            {
                item.Product.Stock += item.Quantity;
            }
            Cart.Clear();
            UpdateTotal();
        }

        public bool IsCartNotEmpty => Cart.Count > 0;

        private void UpdateTotal()
        {
            var discountItem = Cart.FirstOrDefault(x => x.Product.Barcode == "DISCOUNT");
            if (discountItem != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(discountItem.Product.Name, @"%(\d+)");
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal percentage))
                {
                    decimal subTotal = Cart.Where(x => x.Product.Barcode != "DISCOUNT").Sum(x => x.Total);
                    decimal discountAmount = -(subTotal * (percentage / 100m));
                    
                    if (discountItem.Product.Price != discountAmount)
                    {
                        discountItem.Product.Price = discountAmount;
                        int qty = discountItem.Quantity;
                        discountItem.Quantity = qty; 
                    }
                }
            }

            CartTotal = Cart.Sum(x => x.Total);
            OnPropertyChanged(nameof(IsCartNotEmpty));
        }

        private void OpenPaymentWindow()
        {
            if (Cart.Count == 0) return;
            var win = new PaymentWindow(CartTotal);
            win.ShowDialog();

            if (win.IsCompleted)
            {
                string method = PayMethods.Nakit;
                if (win.ResultCard > 0 && win.ResultCash > 0) method = PayMethods.Parcali;
                else if (win.ResultCard > 0) method = PayMethods.KrediKarti;

                SaveTransaction(method, TxTypes.Satis, 
                    method == PayMethods.Parcali ? $"Parçalı Ödeme\n(Nakit: {win.ResultCash:C2}, Kart: {win.ResultCard:C2})" : "Standart Satış", 
                    null, win.ResultCash, win.ResultCard);
            }
        }

        private void ProcessCheckoutVeresiye()
        {
            if (Cart.Count == 0) return;
            var w = new CustomerSelectionWindow(_allCustomersCache);
            w.ShowDialog();

            if (w.SelectedCustomer == null || Cart.Count == 0) return;

            SaveTransaction(PayMethods.Veresiye, TxTypes.Veresiye, $"Veresiye: {w.SelectedCustomer.Name}", w.SelectedCustomer.Id);
        }

        private async void SaveTransaction(string method, string type, string description, int? customerId, decimal receivedCash = 0, decimal receivedCard = 0)
        {
            var transaction = new Transaction
            {
                Date = DateTime.Now,
                TotalAmount = CartTotal,
                CostAmount = Cart.Sum(x => x.Product.Cost * x.Quantity),
                PaymentMethod = method,
                ReceivedCash = receivedCash,
                ReceivedCard = receivedCard,
                Type = type,
                Description = description,
                CustomerId = customerId
            };

            try
            {
                await _db.ProcessSaleTransactionAsync(transaction, Cart.ToList());

                await _db.AddLogAsync(TxTypes.Satis, $"{type}: {CartTotal:C2} ({method})");
                _ = _db.BackupDatabaseAsync();

                ToastNotification.Show("İşlem Başarılı");
                
                Cart.Clear();
                UpdateTotal();
                
                LoadData(); 
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Hata: " + ex.Message, true);
            }
        }

        private void AddQuickMoneyToCart(decimal amount)
        {
            var dummy = new Product
            {
                Id = -1, // Dummy ID
                Name = "Hızlı Ekle / Nakit",
                Barcode = "QUICK",
                Price = amount,
                Cost = 0,
                Stock = 999999,
                Category = "Hızlı"
            };
            
            Cart.Add(new CartItem { Product = dummy, Quantity = 1 });
            UpdateTotal();
        }

        private void ApplyDiscountToCart(int percentage)
        {
            var discountItem = Cart.FirstOrDefault(x => x.Product.Barcode == "DISCOUNT");
            if (discountItem == null)
            {
                var dummy = new Product
                {
                    Id = -2, 
                    Name = $"%{percentage} İndirim",
                    Barcode = "DISCOUNT",
                    Price = 0,
                    Cost = 0,
                    Stock = 999999,
                    Category = "İndirim"
                };
                Cart.Add(new CartItem { Product = dummy, Quantity = 1 });
            }
            else
            {
                discountItem.Product.Name = $"%{percentage} İndirim";
            }
            UpdateTotal();
        }

        private void UndoLastTransaction()
        {
            var w = new RefundWindow();
            w.ShowDialog();
            if (w.RefundCompleted)
            {
                LoadData();
                GenerateReport(); 
            }
        }
    }
}
