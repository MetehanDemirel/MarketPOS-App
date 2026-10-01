using AutoUpdaterDotNET;
using Dapper;
using MarketPOS.Data;
using MarketPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MarketPOS.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly ICommand _internalCommand;
        public RelayCommand(Action<object> execute) { _internalCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(execute); }
        public RelayCommand(Action execute) { _internalCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(execute); }
        
        public event EventHandler? CanExecuteChanged
        {
            add => _internalCommand.CanExecuteChanged += value;
            remove => _internalCommand.CanExecuteChanged -= value;
        }
        
        public bool CanExecute(object? parameter) => _internalCommand.CanExecute(parameter);
        public void Execute(object? parameter) => _internalCommand.Execute(parameter);
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        // --- Core Properties ---
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { if (SetProperty(ref _selectedTabIndex, value)) OnTabChanged(); }
        }

        private string _currentTime; public string CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }
        private string _currentDay; public string CurrentDay { get => _currentDay; set => SetProperty(ref _currentDay, value); }
        private string _currentDate; public string CurrentDate { get => _currentDate; set => SetProperty(ref _currentDate, value); }

        private string _appVersion;
        public string AppVersion { get => _appVersion; set => SetProperty(ref _appVersion, value); }

        public ICommand CheckUpdateCommand { get; private set; }

        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;
            
            _db = new DatabaseService();
            StartClock();

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

            InitializeSalesCommands();
            InitializeInventoryCommands();
            InitializeCustomerCommands();
            InitializeReportCommands();

            CheckUpdateCommand = new RelayCommand(() => CheckForUpdates());

            LoadData();
            CheckForUpdates();
        }

        private void StartClock() 
        { 
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; 
            t.Tick += (s, e) => 
            { 
                var n = DateTime.Now; 
                CurrentTime = n.ToString("HH:mm"); 
                CurrentDay = n.ToString("dddd", new System.Globalization.CultureInfo("tr-TR")); 
                CurrentDate = n.ToString("dd/MM/yyyy"); 
            }; 
            t.Start(); 
        }

        private async void LoadData()
        {
            using var c = _db.GetConnection();

            var ct = (await c.QueryAsync<string>("SELECT Name FROM Categories ORDER BY Name")).ToList();
            ct.Insert(0, "TÜMÜ");
            Categories = new ObservableCollection<string>(ct);

            _allCustomersCache = (await _db.GetCustomersAsync()).ToList();
            FilterCustomers();

            SalesSearchInput = ""; 
            InventorySearchInput = ""; 
            
            _ = SearchSalesAsync();
            SearchInventory();
            
            OnPropertyChanged(nameof(FilteredCategories));
        }

        private void OnTabChanged()
        {
            if (SelectedTabIndex == 3)
            { 
                ReportStartDate = DateTime.Today; 
                ReportEndDate = DateTime.Today; 
                OnPropertyChanged(nameof(ReportStartDate)); 
                OnPropertyChanged(nameof(ReportEndDate)); 
                GenerateReport(); 
            }
            else if (SelectedTabIndex == 4)
            { 
                LogStartDate = DateTime.Today; 
                LogEndDate = DateTime.Today; 
                OnPropertyChanged(nameof(LogStartDate)); 
                OnPropertyChanged(nameof(LogEndDate)); 
                LoadLogs(); 
            }
        }

        private void CheckForUpdates()
        {
            string updateUrl = "https://raw.githubusercontent.com/MetehanDemirel/posappmarket/main/update.xml?v=" + DateTime.Now.Ticks;

            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            
            AutoUpdater.ReportErrors = false; 
            AutoUpdater.Start(updateUrl);
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.Error != null) return;

            if (args.IsUpdateAvailable)
            {
                if (ModernMsgBox.Show($"Yeni sürüm mevcut!\n\nYeni: {args.CurrentVersion}\nYüklü: {args.InstalledVersion}\n\nGüncellemek ister misiniz?", "Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        ToastNotification.Show("Güncelleme başlatılamadı: " + ex.Message, true);
                    }
                }
            }
        }

    }
}