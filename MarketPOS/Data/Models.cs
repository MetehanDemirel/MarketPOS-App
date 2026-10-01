using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MarketPOS.Data
{
    // --- MÜŞTERİ ---
    public class Customer : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";

        private decimal _debtBalance;
        public decimal DebtBalance
        {
            get => _debtBalance;
            set => SetProperty(ref _debtBalance, value);
        }
    }

    // --- ÜRÜN ---
    public class Product : ObservableObject
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Genel";
        public bool IsActive { get; set; } = true;

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { if (SetProperty(ref _price, value)) { OnPropertyChanged(nameof(Profit)); OnPropertyChanged(nameof(DisplayName)); } }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set { if (SetProperty(ref _cost, value)) { OnPropertyChanged(nameof(Profit)); } }
        }

        private int _stock;
        public int Stock
        {
            get => _stock;
            set { if (SetProperty(ref _stock, value)) { OnPropertyChanged(nameof(DisplayName)); } }
        }

        public decimal Profit => Price - Cost;
        public string DisplayName => $"{Name} ({Price:C2})";
    }

    public class CategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsUncounted { get; set; }
        public override string ToString() => Name;
    }

    // --- İŞLEM (REVİZE EDİLDİ) ---
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CostAmount { get; set; }

        // Ödeme Yöntemi string olarak "Parçalı", "Nakit" vs. tutulabilir
        public string PaymentMethod { get; set; } = "Nakit";

        // YENİ: Parçalı ödeme takibi için
        public decimal ReceivedCash { get; set; } // Alınan Nakit
        public decimal ReceivedCard { get; set; } // Çekilen Kart

        public string Type { get; set; } = TxTypes.Satis;
        public string Description { get; set; }
        public int? CustomerId { get; set; }
        public bool IsPaid { get; set; }

        public string TypeColor
        {
            get
            {
                return Type switch
                {
                    TxTypes.Iade => "#FEE2E2",
                    TxTypes.Veresiye => "#FEF9C3",
                    TxTypes.Tahsilat => "#CCFBF1",
                    TxTypes.Gider => "#FFE4E6",
                    TxTypes.KasaGiris => "#DBEAFE",
                    TxTypes.Satis => "#DCFCE7",
                    _ => "#FFFFFF"
                };
            }
        }

        public string DisplayType
        {
            get
            {
                return Type switch
                {
                    TxTypes.Iade => "İade",
                    TxTypes.Veresiye => "Veresiye",
                    TxTypes.Tahsilat => "Tahsilat",
                    TxTypes.Gider => "Gider",
                    TxTypes.KasaGiris => "Kasa Giriş",
                    TxTypes.Satis => "Satış",
                    _ => Type
                };
            }
        }

        public string DisplayPaymentMethod
        {
            get
            {
                return PaymentMethod switch
                {
                    PayMethods.Nakit => "Nakit",
                    PayMethods.KrediKarti => "Kredi Kartı",
                    PayMethods.Parcali => "Parçalı",
                    PayMethods.Veresiye => "Veresiye",
                    _ => PaymentMethod
                };
            }
        }
    }

    public class TransactionItem { public int Id { get; set; } public int TransactionId { get; set; } public int ProductId { get; set; } public int Quantity { get; set; } public decimal PriceAtSale { get; set; } public decimal CostAtSale { get; set; } public string Category { get; set; } = ""; }

    public class UncountedItemGroup
    {
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionDetailItem { public string Name { get; set; } public int Quantity { get; set; } public decimal PriceAtSale { get; set; } public decimal Total => Quantity * PriceAtSale; }

    public class SystemLog
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string ActionType { get; set; }
        public string Description { get; set; }

        public string DisplayActionType
        {
            get
            {
                return ActionType switch
                {
                    TxTypes.Iade => "İade",
                    TxTypes.Veresiye => "Veresiye",
                    TxTypes.Tahsilat => "Tahsilat",
                    TxTypes.Gider => "Gider",
                    TxTypes.KasaGiris => "Kasa Giriş",
                    TxTypes.Satis => "Satış",
                    _ => ActionType
                };
            }
        }

        public string LogColor => ActionType switch
        {
            "UrunEkleme" => "#DCFCE7",
            "KategoriEkleme" => "#E0E7FF",
            "MusteriEkleme" => "#CCFBF1",
            "UrunSilme" => "#FEE2E2",
            "KategoriSilme" => "#FEE2E2",
            _ => "#FFFFFF"
        };
    }

    public class CartItem : ObservableObject
    {
        public Product Product { get; set; }
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }
        public decimal Total => Product.Price * Quantity;
    }
}