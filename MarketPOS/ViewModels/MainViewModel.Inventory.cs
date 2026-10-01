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
        private ObservableCollection<Product> _inventoryProducts = new ObservableCollection<Product>();
        public ObservableCollection<Product> InventoryProducts
        {
            get => _inventoryProducts;
            set { _inventoryProducts = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Product> AllProducts { get; set; } = new ObservableCollection<Product>(); // Cache for fast lookups
        public ObservableCollection<Product> DisplayedProducts { get; set; } = new ObservableCollection<Product>(); // For filtered view if needed
        private ObservableCollection<string> _categories = new ObservableCollection<string>();
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        // Inventory page search vars
        private string _inventorySearchInput;
        public string InventorySearchInput
        {
            get => _inventorySearchInput;
            set 
            { 
                _inventorySearchInput = value; 
                CurrentInventoryPage = 1;
                OnPropertyChanged(); 
            }
        }

        private string _inventoryNameSearchInput;
        public string InventoryNameSearchInput
        {
            get => _inventoryNameSearchInput;
            set { _inventoryNameSearchInput = value; OnPropertyChanged(); }
        }

        private int _currentInventoryPage = 1;
        public int CurrentInventoryPage
        {
            get => _currentInventoryPage;
            set { _currentInventoryPage = value; OnPropertyChanged(); }
        }

        private int _totalInventoryPages = 1;
        public int TotalInventoryPages
        {
            get => _totalInventoryPages;
            set { _totalInventoryPages = value; OnPropertyChanged(); }
        }

        private string _inventoryCategoryFilter = "TÜMÜ";
        public string InventoryCategoryFilter
        {
            get => _inventoryCategoryFilter;
            set
            {
                _inventoryCategoryFilter = value;
                CurrentInventoryPage = 1;
                OnPropertyChanged();
                SearchInventory();
            }
        }

        public ICommand OpenInventoryEditCommand { get; private set; }
        public ICommand SearchInventoryByNameCommand { get; private set; }
        public ICommand OpenEditProductCommand { get; private set; }
        public ICommand OpenAddProductCommand { get; private set; }
        public ICommand OpenCategoryManagerCommand { get; private set; }
        public ICommand DeleteProductCommand { get; private set; }
        
        public ICommand FilterInventoryCategoryCommand { get; private set; } // Renamed for Inventory exclusive

        public ICommand NextPageCommand { get; private set; }
        public ICommand PrevPageCommand { get; private set; }

        // --- Methods ---

        private void InitializeInventoryCommands()
        {
            OpenInventoryEditCommand = new RelayCommand(_ => { CurrentInventoryPage = 1; OpenInventoryEditByEnter(); });
            SearchInventoryByNameCommand = new RelayCommand(_ => { CurrentInventoryPage = 1; SearchInventory(); });
            OpenEditProductCommand = new RelayCommand(p => OpenEditProduct((Product)p));
            OpenAddProductCommand = new RelayCommand(_ => OpenAddProduct());
            OpenCategoryManagerCommand = new RelayCommand(_ => OpenCategoryManager());
            DeleteProductCommand = new RelayCommand(p => DeleteProduct(p));
            
            FilterInventoryCategoryCommand = new RelayCommand(c => 
            {
                 InventoryCategoryFilter = c.ToString();
            });

            NextPageCommand = new RelayCommand(_ => 
            {
                if (CurrentInventoryPage < TotalInventoryPages) { CurrentInventoryPage++; SearchInventory(); }
            });

            PrevPageCommand = new RelayCommand(_ => 
            {
                if (CurrentInventoryPage > 1) { CurrentInventoryPage--; SearchInventory(); }
            });
        }

        private void SearchInventory()
        {
            using var conn = _db.GetConnection();
            string countQuery = "SELECT COUNT(*) FROM Products WHERE IsActive = 1";
            string query = "SELECT * FROM Products WHERE IsActive = 1"; 
            
            var p = new DynamicParameters();

            if (InventoryCategoryFilter != "TÜMÜ")
            {
                countQuery += " AND Category = @Category";
                query += " AND Category = @Category";
                p.Add("@Category", InventoryCategoryFilter);
            }

            bool hasSearch = !string.IsNullOrWhiteSpace(InventorySearchInput);
            bool hasNameSearch = !string.IsNullOrWhiteSpace(InventoryNameSearchInput);

            if (hasSearch)
            {
                countQuery += " AND Barcode = @Search";
                query += " AND Barcode = @Search";
                p.Add("@Search", InventorySearchInput);
            }
            if (hasNameSearch)
            {
                countQuery += " AND Name LIKE @NameSearch";
                query += " AND Name LIKE @NameSearch";
                p.Add("@NameSearch", "%" + InventoryNameSearchInput + "%");
            }

            int pageSize = 30;
            int totalItems = Dapper.SqlMapper.ExecuteScalar<int>(conn, countQuery, p);
            
            TotalInventoryPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (CurrentInventoryPage > TotalInventoryPages) CurrentInventoryPage = TotalInventoryPages;

            int offset = (CurrentInventoryPage - 1) * pageSize;

            query += " ORDER BY Id DESC LIMIT @Limit OFFSET @Offset";
            p.Add("@Limit", pageSize);
            p.Add("@Offset", offset);

            var results = Dapper.SqlMapper.Query<Product>(conn, query, p).ToList();

            InventoryProducts = new ObservableCollection<Product>(results);
        }

        private void OpenInventoryEditByEnter()
        {
            if (string.IsNullOrWhiteSpace(InventorySearchInput)) 
            {
               SearchInventory(); // Refresh/Show All if empty
               return;
            }

            // Perform Search First
            SearchInventory();

            using var c = _db.GetConnection();
            // 1. Try Exact Barcode Match
            var product = Dapper.SqlMapper.QueryFirstOrDefault<Product>(c, "SELECT * FROM Products WHERE Barcode = @B", new { B = InventorySearchInput });

            if (product != null)
            {
                InventorySearchInput = "";
                OpenEditProduct(product);
                return;
            }

            // 2. If not found by barcode, check if the current filtered list has exactly one item
            if (InventoryProducts.Count == 1)
            {
                var p = InventoryProducts.First();
                InventorySearchInput = "";
                OpenEditProduct(p);
                return;
            }

            // Not found -> Open Add Product
            if (ModernMsgBox.Show("Ürün bulunamadı. Eklemek ister misiniz?", "Yeni Ürün", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                 string b = InventorySearchInput;
                 InventorySearchInput = "";
                 OpenAddProduct(b);
            }
        }

        private void OpenAddProduct(string initialBarcode = null)
        {
            var w = new AddProductWindow(Categories.Where(c => c != "TÜMÜ").ToList(), initialBarcode);
            w.ShowDialog();

            if (w.IsSaved)
            {
                try
                {
                    using var c = _db.GetConnection();

                    var existing = Dapper.SqlMapper.QueryFirstOrDefault<Product>(c, "SELECT * FROM Products WHERE Barcode = @B", new { B = w.NewProduct.Barcode });

                    if (existing != null)
                    {
                        if (existing.IsActive == false)
                        {
                            Dapper.SqlMapper.Execute(c, "UPDATE Products SET Name=@N, Category=@C, Price=@P, Cost=@Co, Stock=@S, IsActive=1 WHERE Id=@Id",
                                new
                                {
                                    N = w.NewProduct.Name,
                                    C = w.NewProduct.Category,
                                    P = w.NewProduct.Price,
                                    Co = w.NewProduct.Cost,
                                    S = w.NewProduct.Stock,
                                    Id = existing.Id
                                });

                            _db.AddLog("UrunEkleme", $"{w.NewProduct.Name} (Geri Getirildi)");
                            ModernMsgBox.Show("Eski bir kayıt bulundu. Ürün geri getirildi ve güncellendi.", "Bilgi");
                        }
                        else
                        {
                            ToastNotification.Show("Bu barkod zaten aktif bir üründe kullanılıyor!", true);
                            return;
                        }
                    }
                    else
                    {
                        Dapper.SqlMapper.Execute(c, "INSERT INTO Products (Barcode,Name,Category,Price,Cost,Stock,IsActive) VALUES (@B,@N,@C,@P,@Co,@S,1)",
                            new
                            {
                                B = w.NewProduct.Barcode,
                                N = w.NewProduct.Name,
                                C = w.NewProduct.Category,
                                P = w.NewProduct.Price,
                                Co = w.NewProduct.Cost,
                                S = w.NewProduct.Stock
                            });

                        _db.AddLog("UrunEkleme", $"{w.NewProduct.Name} oluşturuldu. (Fiyat: {w.NewProduct.Price:C2}, Stok: {w.NewProduct.Stock})");
                    }

                    LoadData();
                }
                catch (Exception ex)
                {
                    ModernMsgBox.Show("Hata oluştu: " + ex.Message, "Hata");
                }
            }
        }

        private void OpenEditProduct(Product p)
        {
            if (p == null) return;
            
            // Clone original values for comparison
            var originalName = p.Name;
            var originalPrice = p.Price;
            var originalStock = p.Stock;
            var originalCost = p.Cost;
            var originalCategory = p.Category;
            var originalBarcode = p.Barcode;

            var w = new EditProductWindow(p, Categories.Where(c => c != "TÜMÜ").ToList());
            w.ShowDialog();
            
            if (w.IsSaved)
            {
                var n = w.Product;
                _db.UpdateProduct(n);

                // Calculate Diff
                var changes = new List<string>();
                if (originalName != n.Name) changes.Add($"İsim: '{originalName}' -> '{n.Name}'");
                if (originalPrice != n.Price) changes.Add($"Fiyat: {originalPrice:C2} -> {n.Price:C2}");
                if (originalStock != n.Stock) changes.Add($"Stok: {originalStock} -> {n.Stock}");
                if (originalCost != n.Cost) changes.Add($"Maliyet: {originalCost:C2} -> {n.Cost:C2}");
                if (originalCategory != n.Category) changes.Add($"Kategori: {originalCategory} -> {n.Category}");
                if (originalBarcode != n.Barcode) changes.Add($"Barkod: {originalBarcode} -> {n.Barcode}");

                if (changes.Count > 0)
                {
                    _db.AddLog("UrunGuncelleme", $"{n.Name} güncellendi: " + string.Join(", ", changes));
                }
                else
                {
                     _db.AddLog("UrunGuncelleme", $"{n.Name} güncellendi (Değişiklik yok)");
                }

                LoadData();
            }
        }

        private void DeleteProduct(object p)
        {
            if (p is Product x && ModernMsgBox.Show("Sil?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                using var c = _db.GetConnection();
                Dapper.SqlMapper.Execute(c, "UPDATE Products SET IsActive=0 WHERE Id=@Id", new { Id = x.Id });
                _db.AddLog("UrunSilme", x.Name);
                LoadData();
            }
        }

        private void OpenCategoryManager()
        {
            var catModels = _db.GetCategoriesWithFlags().ToList();
            var w = new CategoryManagerWindow(catModels);
            w.OnAddCategory = (n) =>
            {
                try
                {
                    using var c = _db.GetConnection();
                    Dapper.SqlMapper.Execute(c, "INSERT INTO Categories (Name) VALUES (@N)", new { N = n });
                    _db.AddLog("KategoriEkleme", n);
                }
                catch { }
            };
            w.OnDeleteCategory = (n) =>
            {
                _db.DeleteCategory(n);
                _db.AddLog("KategoriSilme", n);
            };
            w.OnToggleUncounted = (name, isUncounted) =>
            {
                _db.SetCategoryUncounted(name, isUncounted);
            };
            w.ShowDialog();
            LoadData();
        }
    }
}
