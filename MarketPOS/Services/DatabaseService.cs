using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using MarketPOS.Data;

namespace MarketPOS.Services
{
    public class SqliteDecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(IDbDataParameter parameter, decimal value) => parameter.Value = value;
        public override decimal Parse(object value)
        {
            if (value == null || value is DBNull) return 0m;
            try { return Convert.ToDecimal(value); } 
            catch { return 0m; }
        }
    }

    public class DatabaseService
    {
        private static string DbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MarketDB.db");
        private string ConnectionString = $"Data Source={DbFile}";

        static DatabaseService()
        {
            SqlMapper.RemoveTypeMap(typeof(decimal));
            SqlMapper.AddTypeHandler(new SqliteDecimalHandler());
        }

        public DatabaseService() { InitializeDatabase(); }

        public IDbConnection GetConnection() => new SqliteConnection(ConnectionString);

        private void InitializeDatabase()
        {
            using var conn = GetConnection();
            conn.Open();
            conn.Execute("PRAGMA journal_mode = WAL;");

            conn.Execute(@"CREATE TABLE IF NOT EXISTS Products (Id INTEGER PRIMARY KEY AUTOINCREMENT, Barcode TEXT UNIQUE, Name TEXT, Category TEXT, Price DECIMAL, Cost DECIMAL, Stock INTEGER, IsActive INTEGER DEFAULT 1)");
            conn.Execute(@"CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name)");
            conn.Execute(@"CREATE TABLE IF NOT EXISTS Categories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT UNIQUE)");
            conn.Execute(@"CREATE TABLE IF NOT EXISTS Customers (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Phone TEXT, DebtBalance DECIMAL DEFAULT 0)");

            conn.Execute(@"CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                Date TEXT, 
                TotalAmount DECIMAL, 
                CostAmount DECIMAL DEFAULT 0, 
                PaymentMethod TEXT, 
                ReceivedCash DECIMAL DEFAULT 0, 
                ReceivedCard DECIMAL DEFAULT 0,
                Type TEXT, 
                Description TEXT, 
                CustomerId INTEGER)");

            conn.Execute(@"CREATE TABLE IF NOT EXISTS TransactionItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, TransactionId INTEGER, ProductId INTEGER, Quantity INTEGER, PriceAtSale DECIMAL, CostAtSale DECIMAL, FOREIGN KEY(TransactionId) REFERENCES Transactions(Id))");
            conn.Execute(@"CREATE TABLE IF NOT EXISTS SystemLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Date TEXT, ActionType TEXT, Description TEXT)");

            // Migrations
            var catCols = conn.Query<dynamic>("PRAGMA table_info(Categories)").Select(c => (string)c.name).ToList();
            if (!catCols.Contains("IsUncounted"))
                conn.Execute("ALTER TABLE Categories ADD COLUMN IsUncounted INTEGER DEFAULT 0");

            var tiCols = conn.Query<dynamic>("PRAGMA table_info(TransactionItems)").Select(c => (string)c.name).ToList();
            if (!tiCols.Contains("Category"))
                conn.Execute("ALTER TABLE TransactionItems ADD COLUMN Category TEXT DEFAULT ''");

            var emptyCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM TransactionItems WHERE (Category IS NULL OR Category = '') AND ProductId > 0");
            if (emptyCount > 0)
            {
                conn.Execute(@"UPDATE TransactionItems 
                               SET Category = (SELECT Category FROM Products WHERE Products.Id = TransactionItems.ProductId) 
                               WHERE (Category IS NULL OR Category = '') AND ProductId > 0");
            }

            var tCols = conn.Query<dynamic>("PRAGMA table_info(Transactions)").Select(c => (string)c.name).ToList();
            if (!tCols.Contains("IsPaid"))
                conn.Execute("ALTER TABLE Transactions ADD COLUMN IsPaid INTEGER DEFAULT 0");


            var catCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories");
            if (catCount == 0) { conn.Execute("INSERT INTO Categories (Name) VALUES ('Genel')"); conn.Execute("INSERT INTO Categories (Name) VALUES ('G覺da')"); }

            // Migration for unifying string constants
            conn.Execute($"UPDATE Transactions SET Type = '{TxTypes.Satis}' WHERE Type = 'Sat覺��'");
            conn.Execute($"UPDATE Transactions SET Type = '{TxTypes.Iade}' WHERE Type = '襤ade'");
            conn.Execute($"UPDATE SystemLogs SET ActionType = '{TxTypes.Satis}' WHERE ActionType = 'Sat覺��'");
            conn.Execute($"UPDATE SystemLogs SET ActionType = '{TxTypes.Iade}' WHERE ActionType = '襤ade'");
            
            conn.Execute($"UPDATE Transactions SET PaymentMethod = '{PayMethods.KrediKarti}' WHERE PaymentMethod = 'Kredi Kart覺'");
            conn.Execute($"UPDATE Transactions SET PaymentMethod = '{PayMethods.Parcali}' WHERE PaymentMethod = 'Par癟al覺'");
        }


        public IEnumerable<CategoryModel> GetCategoriesWithFlags()
        {
            using var conn = GetConnection();
            return conn.Query<CategoryModel>("SELECT Id, Name, IsUncounted FROM Categories ORDER BY Name");
        }

        public void SetCategoryUncounted(string name, bool isUncounted)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE Categories SET IsUncounted = @U WHERE Name = @N", new { U = isUncounted ? 1 : 0, N = name });
        }

        public List<string> GetUncountedCategoryNames()
        {
            using var conn = GetConnection();
            return conn.Query<string>("SELECT Name FROM Categories WHERE IsUncounted = 1").ToList();
        }

        public IEnumerable<TransactionItem> GetTransactionItemsByDateRange(DateTime start, DateTime end)
        {
            using var conn = GetConnection();
            return conn.Query<TransactionItem>(@"
                SELECT ti.* FROM TransactionItems ti
                JOIN Transactions t ON ti.TransactionId = t.Id
                WHERE t.Date >= @S AND t.Date <= @E AND t.Type IN (@Satis, @Veresiye, @Iade)",
                new { S = start.ToString("yyyy-MM-dd HH:mm:ss"), E = end.ToString("yyyy-MM-dd HH:mm:ss"), Satis = TxTypes.Satis, Veresiye = TxTypes.Veresiye, Iade = TxTypes.Iade });
        }

        public IEnumerable<Transaction> GetRefundableTransactions()
        {
            using var conn = GetConnection();
            return conn.Query<Transaction>($"SELECT * FROM Transactions WHERE Type IN ('{TxTypes.Satis}', '{TxTypes.Veresiye}') ORDER BY Id DESC LIMIT 50");
        }

        public Transaction GetTransactionById(int id)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<Transaction>("SELECT * FROM Transactions WHERE Id = @Id", new { Id = id });
        }

        public void RefundSpecificTransaction(int originalTransId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                var original = conn.QueryFirstOrDefault<Transaction>("SELECT * FROM Transactions WHERE Id = @Id", new { Id = originalTransId }, tran);
                if (original == null) throw new Exception("襤��lem bulunamad覺.");

                decimal cashRefunded = original.ReceivedCash;
                decimal cardRefunded = original.ReceivedCard;
                decimal debtToCancel = 0;

                if (original.Type == TxTypes.Veresiye && original.CustomerId.HasValue)
                {
                    decimal currentDebt = conn.QuerySingleOrDefault<decimal>("SELECT DebtBalance FROM Customers WHERE Id = @Id", new { Id = original.CustomerId.Value }, tran);
                    if (currentDebt > 0)
                    {
                        debtToCancel = Math.Min(currentDebt, original.TotalAmount);
                    }
                    
                    decimal extraCashToRefund = original.TotalAmount - debtToCancel;
                    cashRefunded += extraCashToRefund;
                }

                string desc = $"襤ade: Fi�� #{original.Id}";
                int refundId = conn.QuerySingle<int>(@"
                    INSERT INTO Transactions (Date, TotalAmount, CostAmount, PaymentMethod, ReceivedCash, ReceivedCard, Type, Description, CustomerId) 
                    VALUES (@D, @T, @C, @M, @RC, @RCD, @Type, @Desc, @CustId);
                    SELECT last_insert_rowid();",
                    new
                    {
                        D = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        T = -original.TotalAmount,
                        C = -original.CostAmount,
                        M = original.PaymentMethod,
                        RC = -cashRefunded,   // Nakit iadesi (Includes refunded Veresiye cash)
                        RCD = -cardRefunded,  // Kart iadesi
                        Type = TxTypes.Iade,
                        Desc = desc,
                        CustId = original.CustomerId
                    }, tran);

                var items = conn.Query<TransactionItem>("SELECT * FROM TransactionItems WHERE TransactionId = @Id", new { Id = original.Id }, tran);
                foreach (var i in items)
                {
                    conn.Execute("UPDATE Products SET Stock = Stock + @Q WHERE Id = @Id", new { Q = i.Quantity, Id = i.ProductId }, tran);
                    conn.Execute("INSERT INTO TransactionItems (TransactionId, ProductId, Quantity, PriceAtSale, CostAtSale) VALUES (@TrId, @PId, @Q, @P, @C)",
                        new { TrId = refundId, PId = i.ProductId, Q = i.Quantity, P = i.PriceAtSale, C = i.CostAtSale }, tran);
                }

                if (debtToCancel > 0 && original.CustomerId.HasValue)
                {
                    conn.Execute("UPDATE Customers SET DebtBalance = DebtBalance - @Amount WHERE Id = @Id",
                        new { Amount = debtToCancel, Id = original.CustomerId.Value }, tran);
                }

                tran.Commit();
            }
            catch { tran.Rollback(); throw; }
        }

        public void DeleteLog(int id)
        {
            using var conn = GetConnection();
            conn.Execute("DELETE FROM SystemLogs WHERE Id = @Id", new { Id = id });
        }

        public void UpdateLog(SystemLog log)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE SystemLogs SET Date = @D, ActionType = @A, Description = @Desc WHERE Id = @Id",
                new { D = log.Date.ToString("yyyy-MM-dd HH:mm:ss"), A = log.ActionType, Desc = log.Description, Id = log.Id });
        }

        public int AddCustomer(Customer c)
        {
            using var conn = GetConnection();
            return conn.QuerySingle<int>("INSERT INTO Customers (Name, Phone, DebtBalance) VALUES (@N, @P, @D); SELECT last_insert_rowid();",
                new { N = c.Name, P = c.Phone, D = c.DebtBalance });
        }

        public void AddTransaction(Transaction t)
        {
            using var conn = GetConnection();
            conn.Execute(@"INSERT INTO Transactions 
                (Date, TotalAmount, CostAmount, PaymentMethod, ReceivedCash, ReceivedCard, Type, Description, CustomerId) 
                VALUES (@Date, @Total, @Cost, @Method, @Cash, @Card, @Type, @Desc, @CustId)",
                new
                {
                    Date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                    Total = t.TotalAmount,
                    Cost = t.CostAmount,
                    Method = t.PaymentMethod,
                    Cash = t.ReceivedCash,
                    Card = t.ReceivedCard,
                    Type = t.Type,
                    Desc = t.Description,
                    CustId = t.CustomerId
                });
        }

        public void UpdateCustomerDebt(int customerId, decimal amountToAdd)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE Customers SET DebtBalance = DebtBalance + @Amount WHERE Id = @Id", new { Amount = amountToAdd, Id = customerId });
        }

        public IEnumerable<Customer> GetCustomers()
        {
            using var conn = GetConnection();
            return conn.Query<Customer>("SELECT * FROM Customers ORDER BY Name");
        }

        public void DeleteCustomer(int id)
        {
            using var conn = GetConnection();
            conn.Execute("DELETE FROM Customers WHERE Id = @Id", new { Id = id });
        }

        public void DeleteCategory(string name)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                conn.Execute("DELETE FROM Products WHERE Category = @C", new { C = name }, tran);
                conn.Execute("DELETE FROM Categories WHERE Name = @N", new { N = name }, tran);
                tran.Commit();
            }
            catch { tran.Rollback(); throw; }
        }

        public void UpdateProduct(Product p)
        {
            using var conn = GetConnection();
            conn.Execute(@"UPDATE Products SET Barcode=@B, Name=@N, Category=@C, Price=@P, Cost=@Co, Stock=@S WHERE Id=@Id",
                new { B = p.Barcode, N = p.Name, C = p.Category, P = p.Price, Co = p.Cost, S = p.Stock, Id = p.Id });
        }

        public IEnumerable<TransactionDetailItem> GetTransactionDetails(int transactionId)
        {
            using var conn = GetConnection();
            return conn.Query<TransactionDetailItem>(@"
                SELECT 
                    COALESCE(p.Name, CASE WHEN ti.ProductId = -2 THEN 'Indirim' WHEN ti.ProductId = -1 THEN 'Hizli Ekle' ELSE 'Silinmis 鈜�n' END) as Name, 
                    ti.Quantity, 
                    ti.PriceAtSale 
                FROM TransactionItems ti 
                LEFT JOIN Products p ON ti.ProductId = p.Id 
                WHERE ti.TransactionId = @Id", new { Id = transactionId });
        }

        public void AddLog(string action, string desc)
        {
            using var conn = GetConnection();
            conn.Execute("INSERT INTO SystemLogs (Date, ActionType, Description) VALUES (@D, @A, @Desc)", new { D = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), A = action, Desc = desc });
        }
        public void AddFinancialEntry(decimal amount, string type, string description)
        {
            using var conn = GetConnection();
            decimal finalTotal = type == TxTypes.Gider ? -Math.Abs(amount) : Math.Abs(amount);

            conn.Execute(@"INSERT INTO Transactions 
                (Date, TotalAmount, CostAmount, PaymentMethod, ReceivedCash, ReceivedCard, Type, Description, CustomerId) 
                VALUES (@D, @T, 0, @Method, @Cash, 0, @Type, @Desc, NULL)",
                new
                {
                    D = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    T = finalTotal,
                    Method = PayMethods.Nakit,
                    Cash = finalTotal,
                    Type = type,
                    Desc = description
                });
        }

        public IEnumerable<Transaction> GetTransactionsByDate(DateTime start, DateTime end)
        {
            using var conn = GetConnection();
            return conn.Query<Transaction>("SELECT * FROM Transactions WHERE Date >= @S AND Date <= @E ORDER BY Date DESC", new { S = start.ToString("yyyy-MM-dd HH:mm:ss"), E = end.ToString("yyyy-MM-dd HH:mm:ss") });
        }

        public IEnumerable<SystemLog> GetLogsByDate(DateTime start, DateTime end)
        {
            using var conn = GetConnection();
            return conn.Query<SystemLog>("SELECT * FROM SystemLogs WHERE Date >= @S AND Date <= @E ORDER BY Id DESC", new { S = start.ToString("yyyy-MM-dd HH:mm:ss"), E = end.ToString("yyyy-MM-dd HH:mm:ss") });
        }

        public void BackupDatabase()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Yedekler");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var datePattern = $"Yedek_{DateTime.Now:yyyy-MM-dd}*";
                if (Directory.GetFiles(folder, datePattern).Length > 0) return;

                using var conn = GetConnection();
                conn.Execute($"VACUUM INTO '{Path.Combine(folder, $"Yedek_{DateTime.Now:yyyy-MM-dd_HH-mm}.db")}'");
            }
            catch { }
        }
        public void ProcessSaleTransaction(Transaction transaction, List<CartItem> cartItems)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                int transactionId = conn.QuerySingle<int>(@"
                    INSERT INTO Transactions (Date, TotalAmount, CostAmount, PaymentMethod, ReceivedCash, ReceivedCard, Type, Description, CustomerId) 
                    VALUES (@Date, @Total, @Cost, @Method, @Cash, @Card, @Type, @Desc, @CustId);
                    SELECT last_insert_rowid();",
                    new
                    {
                        Date = transaction.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        Total = transaction.TotalAmount,
                        Cost = transaction.CostAmount,
                        Method = transaction.PaymentMethod,
                        Cash = transaction.ReceivedCash,
                        Card = transaction.ReceivedCard,
                        Type = transaction.Type,
                        Desc = transaction.Description,
                        CustId = transaction.CustomerId
                    }, tran);

                foreach (var item in cartItems)
                {
                    conn.Execute(@"
                        INSERT INTO TransactionItems (TransactionId, ProductId, Quantity, PriceAtSale, CostAtSale, Category) 
                        VALUES (@TrId, @PId, @Q, @P, @C, @Cat)",
                        new
                        {
                            TrId = transactionId,
                            PId = item.Product.Id,
                            Q = item.Quantity,
                            P = item.Product.Price,
                            C = item.Product.Cost,
                            Cat = item.Product.Category ?? ""
                        }, tran);

                    conn.Execute("UPDATE Products SET Stock = @S WHERE Id = @Id",
                        new { S = item.Product.Stock, Id = item.Product.Id }, tran);
                }

                if (transaction.Type == TxTypes.Veresiye && transaction.CustomerId.HasValue)
                {
                    conn.Execute("UPDATE Customers SET DebtBalance = DebtBalance + @A WHERE Id = @Id",
                        new { A = transaction.TotalAmount, Id = transaction.CustomerId.Value }, tran);
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        public IEnumerable<Transaction> GetTransactionsByCustomerId(int customerId)
        {
            using var conn = GetConnection();
            return conn.Query<Transaction>("SELECT * FROM Transactions WHERE CustomerId = @Id ORDER BY Id DESC LIMIT 50", new { Id = customerId });
        }

        public IEnumerable<Transaction> GetUnpaidReceiptsByCustomerId(int customerId)
        {
            using var conn = GetConnection();
            return conn.Query<Transaction>($"SELECT * FROM Transactions WHERE CustomerId = @Id AND Type = '{TxTypes.Veresiye}' AND IsPaid = 0 ORDER BY Id ASC", new { Id = customerId });
        }

        public void MarkTransactionAsPaid(int transactionId)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE Transactions SET IsPaid = 1 WHERE Id = @Id", new { Id = transactionId });
        }

        public void DeleteTransaction(int transactionId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                var t = conn.QueryFirstOrDefault<Transaction>("SELECT * FROM Transactions WHERE Id = @Id", new { Id = transactionId }, tran);
                if (t == null) return;

                var items = conn.Query<TransactionItem>("SELECT * FROM TransactionItems WHERE TransactionId = @Id", new { Id = transactionId }, tran);
                foreach (var i in items)
                {
                    conn.Execute("UPDATE Products SET Stock = Stock + @Q WHERE Id = @Id", new { Q = i.Quantity, Id = i.ProductId }, tran);
                }

                if (t.CustomerId.HasValue)
                {
                    if (t.Type == TxTypes.Veresiye)
                    {
                         conn.Execute("UPDATE Customers SET DebtBalance = DebtBalance - @A WHERE Id = @Id", new { A = t.TotalAmount, Id = t.CustomerId.Value }, tran);
                    }
                    else if (t.Type == TxTypes.Tahsilat)
                    {
                        conn.Execute("UPDATE Customers SET DebtBalance = DebtBalance + @A WHERE Id = @Id", new { A = t.TotalAmount, Id = t.CustomerId.Value }, tran);
                    }
                }

                conn.Execute("DELETE FROM TransactionItems WHERE TransactionId = @Id", new { Id = transactionId }, tran);
                conn.Execute("DELETE FROM Transactions WHERE Id = @Id", new { Id = transactionId }, tran);

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        public IEnumerable<UncountedItemGroup> GetUncountedSoldItemsByDateRange(DateTime start, DateTime end)
        {
            using var conn = GetConnection();
            var uncountedCats = GetUncountedCategoryNames();
            if (uncountedCats.Count == 0) return Enumerable.Empty<UncountedItemGroup>();

            string query = @"
                SELECT p.Name as ProductName, SUM(ti.Quantity) as TotalQuantity, SUM(ti.Quantity * ti.PriceAtSale) as TotalAmount
                FROM TransactionItems ti
                JOIN Transactions t ON ti.TransactionId = t.Id
                JOIN Products p ON ti.ProductId = p.Id
                WHERE t.Date >= @Start AND t.Date <= @End
                AND ti.Category IN @Cats
                GROUP BY p.Name
                ORDER BY TotalQuantity DESC";

            return conn.Query<UncountedItemGroup>(query, new { Start = start.ToString("yyyy-MM-dd HH:mm:ss"), End = end.ToString("yyyy-MM-dd HH:mm:ss"), Cats = uncountedCats });
        }

        // --- ASYNC METHODS ---
        public Task<IEnumerable<CategoryModel>> GetCategoriesWithFlagsAsync() => Task.Run(() => GetCategoriesWithFlags());
        public Task SetCategoryUncountedAsync(string name, bool isUncounted) => Task.Run(() => SetCategoryUncounted(name, isUncounted));
        public Task<List<string>> GetUncountedCategoryNamesAsync() => Task.Run(() => GetUncountedCategoryNames());
        public Task<IEnumerable<TransactionItem>> GetTransactionItemsByDateRangeAsync(DateTime start, DateTime end) => Task.Run(() => GetTransactionItemsByDateRange(start, end));
        public Task<IEnumerable<Transaction>> GetRefundableTransactionsAsync() => Task.Run(() => GetRefundableTransactions());
        public Task<Transaction> GetTransactionByIdAsync(int id) => Task.Run(() => GetTransactionById(id));
        public Task RefundSpecificTransactionAsync(int originalTransId) => Task.Run(() => RefundSpecificTransaction(originalTransId));
        public Task DeleteLogAsync(int id) => Task.Run(() => DeleteLog(id));
        public Task UpdateLogAsync(SystemLog log) => Task.Run(() => UpdateLog(log));
        public Task<int> AddCustomerAsync(Customer c) => Task.Run(() => AddCustomer(c));
        public Task AddTransactionAsync(Transaction t) => Task.Run(() => AddTransaction(t));
        public Task UpdateCustomerDebtAsync(int customerId, decimal amountToAdd) => Task.Run(() => UpdateCustomerDebt(customerId, amountToAdd));
        public Task<IEnumerable<Customer>> GetCustomersAsync() => Task.Run(() => GetCustomers());
        public Task DeleteCustomerAsync(int id) => Task.Run(() => DeleteCustomer(id));
        public Task DeleteCategoryAsync(string name) => Task.Run(() => DeleteCategory(name));
        public Task UpdateProductAsync(Product p) => Task.Run(() => UpdateProduct(p));
        public Task<IEnumerable<TransactionDetailItem>> GetTransactionDetailsAsync(int transactionId) => Task.Run(() => GetTransactionDetails(transactionId));
        public Task AddLogAsync(string action, string desc) => Task.Run(() => AddLog(action, desc));
        public Task AddFinancialEntryAsync(decimal amount, string type, string description) => Task.Run(() => AddFinancialEntry(amount, type, description));
        public Task<IEnumerable<Transaction>> GetTransactionsByDateAsync(DateTime start, DateTime end) => Task.Run(() => GetTransactionsByDate(start, end));
        public Task<IEnumerable<SystemLog>> GetLogsByDateAsync(DateTime start, DateTime end) => Task.Run(() => GetLogsByDate(start, end));
        public Task BackupDatabaseAsync() => Task.Run(() => BackupDatabase());
        public Task ProcessSaleTransactionAsync(Transaction transaction, List<CartItem> cartItems) => Task.Run(() => ProcessSaleTransaction(transaction, cartItems));
        public Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(int customerId) => Task.Run(() => GetTransactionsByCustomerId(customerId));
        public Task<IEnumerable<Transaction>> GetUnpaidReceiptsByCustomerIdAsync(int customerId) => Task.Run(() => GetUnpaidReceiptsByCustomerId(customerId));
        public Task MarkTransactionAsPaidAsync(int transactionId) => Task.Run(() => MarkTransactionAsPaid(transactionId));
        public Task DeleteTransactionAsync(int transactionId) => Task.Run(() => DeleteTransaction(transactionId));
        public Task<IEnumerable<UncountedItemGroup>> GetUncountedSoldItemsByDateRangeAsync(DateTime start, DateTime end) => Task.Run(() => GetUncountedSoldItemsByDateRange(start, end));

    }
}


