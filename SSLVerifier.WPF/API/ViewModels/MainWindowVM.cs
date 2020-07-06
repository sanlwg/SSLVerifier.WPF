﻿using System;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SSLVerifier.API.Extensions;
using SSLVerifier.API.Functions;
using SSLVerifier.API.MainLogic;
using SSLVerifier.API.ModelObjects;
using SSLVerifier.Core;
using SSLVerifier.Core.Processor;
using SSLVerifier.Views.Windows;
using SysadminsLV.WPF.OfficeTheme.Toolkit;
using SysadminsLV.WPF.OfficeTheme.Toolkit.Commands;
using SysadminsLV.WPF.OfficeTheme.Toolkit.ViewModels;

namespace SSLVerifier.API.ViewModels {
    class MainWindowVM : ViewModelBase {
        Boolean alreadyExitRaised, singleScan, isSaved = true, running;
        String lastSavedFile, tbServer, statusText = "Ready";
        ServerObject selectedItem;
        Int32 selectedIndex, threshold = 30, tbPort = 443;
        Double progress, progressWidth;
        ChainElement selectedTreeElement;
        Visibility progressVisible;

        public MainWindowVM() {
            initCommands();
        }

        void initCommands() {
            NewListCommand = new RelayCommand(newList);
            OpenListCommand = new RelayCommand(openList);
            SaveListCommand = new RelayCommand(saveList, canSaveList);
            SaveAsListCommand = new RelayCommand(saveAsList, canSaveList);
            CloseCommand = new RelayCommand<CancelEventArgs>(close);
            ViewCertificateCommand = new RelayCommand(viewCertificate, canViewCertificate);
            ViewChainCertificateCommand = new RelayCommand(ViewChainCertificate, canViewChainCertificate);
            AddServerCommand = new RelayCommand(addServer, canAddServer);
            RemoveServerCommand = new RelayCommand(removeServer, CanRemoveServer);
            StartSingleScanCommand = new RelayCommand(StartSingleScan, canStartSingleScan);
            StartSingleScan2Command = new RelayCommand(addServerAndScan, canAddServerAndScan);
            StartScanCommand = new RelayCommand(startScan, canStartScan);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ShowEntryProperties = new RelayCommand(showProperties, canShowProperties);
            ShowSettings = new RelayCommand(showSettings);
            SaveCsvCommand = new RelayCommand(saveAsCsv, canSaveCsv);
            ShowLicenseCommand = new RelayCommand(ShowLicense);
        }

        #region Commands
        public ICommand NewListCommand { get; set; }
        public ICommand OpenListCommand { get; set; }
        public ICommand SaveListCommand { get; set; }
        public ICommand SaveAsListCommand { get; set; }
        public ICommand SaveCsvCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand ViewCertificateCommand { get; set; }
        public ICommand ViewChainCertificateCommand { get; set; }
        public ICommand AddServerCommand { get; set; }
        public ICommand RemoveServerCommand { get; set; }
        public ICommand StartSingleScanCommand { get; set; }
        public ICommand StartSingleScan2Command { get; set; }
        public ICommand StartScanCommand { get; set; }
        public ICommand ShowAboutCommand { get; set; }
        public ICommand ShowLicenseCommand { get; set; }
        public ICommand ShowEntryProperties { get; set; }
        public ICommand ShowSettings { get; set; }
        #endregion

        public ServerListContainer ServerList { get; } = new ServerListContainer();


        public Boolean IsSaved {
            get => isSaved;
            set {
                isSaved = value;
                OnPropertyChanged(nameof(IsSaved));
            }
        }
        public String LastSavedFile {
            get => lastSavedFile;
            set {
                lastSavedFile = value;
                OnPropertyChanged(nameof(LastSavedFile));
            }
        }
        public String ServerName2 {
            get => tbServer;
            set {
                tbServer = value;
                OnPropertyChanged(nameof(ServerName2));
            }
        }
        public Int32 Port2 {
            get => tbPort;
            set {
                tbPort = value;
                OnPropertyChanged(nameof(Port2));
            }
        }

        public Int32 Threshold {
            get => threshold;
            set {
                threshold = value;
                OnPropertyChanged(nameof(Threshold));
            }
        }
        public Boolean Running {
            get => running;
            set {
                running = value;
                OnPropertyChanged(nameof(Running));
            }
        }
        public String StatusText {
            get => statusText;
            set {
                statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
        public Visibility ProgressVisible {
            get => progressVisible;
            set {
                progressVisible = value;
                OnPropertyChanged(nameof(ProgressVisible));
            }
        }
        public Double Progress {
            get => progress;
            set {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
        public Double ProgressWidth {
            get => progressWidth;
            set {
                progressWidth = value;
                OnPropertyChanged(nameof(ProgressWidth));
            }
        }
        public ServerObject SelectedItem {
            get => selectedItem;
            set {
                selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
        public Int32 SelectedIndex {
            get => selectedIndex;
            set {
                selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }
        public ChainElement SelectedTreeItem {
            get => selectedTreeElement;
            set {
                selectedTreeElement = value;
                OnPropertyChanged(nameof(SelectedTreeItem));
            }
        }

        void newList(Object obj) {
            if (testSaved()) {
                LastSavedFile = String.Empty;
                IsSaved = true;
                ServerList.Servers.Clear();
            }
        }
        void openList(Object obj) {
            if (testSaved()) {
                OpenFileDialog dlg = new OpenFileDialog {
                    FileName = "ServerList.xml",
                    DefaultExt = ".xml",
                    Filter = "Server list files (.xml)|*.xml"
                };
                Boolean? result = dlg.ShowDialog();
                if (result != true) { return; }
                try {
                    XmlObject list = XmlRoutine.Deserialize(dlg.FileName);
                    if (list == null) { return; }
                    ServerList.Servers.Clear();
                    foreach (ServerObject item in list.ServerObjects) {
                        ServerList.Servers.Add(item);
                    }
                    IsSaved = true;
                } catch (Exception e) {
                    MsgBox.Show("XML Read error", e.Message);
                }
            }
        }
        void saveList(Object obj) {
            if (String.IsNullOrEmpty(LastSavedFile)) { saveAsList(null); }
            try {
                XmlRoutine.Serialize(ServerList.Servers, LastSavedFile);
                IsSaved = true;
            } catch (Exception e) {
                MsgBox.Show("XML Write error", e.Message);
            }
        }
        void saveAsList(Object obj) {
            SaveFileDialog dlg = new SaveFileDialog {
                FileName = "ServerList.xml",
                DefaultExt = ".xml",
                Filter = "Server list files (.xml)|*.xml"
            };
            Boolean? result = dlg.ShowDialog();
            if (result == true) {
                LastSavedFile = dlg.FileName;
                try {
                    XmlRoutine.Serialize(ServerList.Servers, LastSavedFile);
                    IsSaved = true;
                } catch (Exception e) {
                    MsgBox.Show("XML Write error", e.Message);
                }
            }
        }
        void saveAsCsv(Object obj) {
            SaveFileDialog dlg = new SaveFileDialog {
                FileName = "ServerList.csv",
                DefaultExt = ".xml",
                Filter = "Comma separated value file  (.csv)|*.csv"
            };
            Boolean? result = dlg.ShowDialog();
            if (result == true) {
                LastSavedFile = dlg.FileName;
                try {
                    ServerList.Servers.SaveAsCSV(dlg.FileName);
                } catch (Exception e) {
                    MsgBox.Show("CSV Write error", e.Message);
                }
            }
        }
        void close(CancelEventArgs e) {
            if (e == null) {
                // exit button pressed
                if (IsSaved) {
                    Application.Current.Shutdown();
                } else {
                    MessageBoxResult mbxResult = MsgBox.Show(
                        "SSL Certificate Verifier",
                        "Current server was modified. Save changes?",
                        MessageBoxImage.Warning,
                        MessageBoxButton.YesNoCancel);
                    switch (mbxResult) {
                        case MessageBoxResult.Yes:
                            saveList(null);
                            if (IsSaved) {
                                Application.Current.MainWindow.Close();
                            }
                            break;
                        case MessageBoxResult.No:
                            alreadyExitRaised = true;
                            Application.Current.MainWindow.Close();
                            break;
                    }
                }
            } else {
                // fired from X button or Alt+F4 gesture
                if (IsSaved) {
                    e.Cancel = false;
                } else {
                    // stub statement to determine whether the event was raised from button.
                    if (alreadyExitRaised) {
                        e.Cancel = false;
                    } else {
                        MessageBoxResult mbxResult = MsgBox.Show(
                            "SSL Certificate Verifier",
                            "Current server was modified. Save changes?",
                            MessageBoxImage.Warning,
                            MessageBoxButton.YesNoCancel);
                        switch (mbxResult) {
                            case MessageBoxResult.Yes:
                                saveList(null);
                                e.Cancel = !IsSaved;
                                break;
                            case MessageBoxResult.No:
                                e.Cancel = false;
                                break;
                            case MessageBoxResult.Cancel:
                                e.Cancel = true;
                                break;
                        }
                    }
                }
            }
        }
        void showProperties(Object obj) {
            IServerProxy old = SelectedItem.Proxy;
            var dlg = WindowsUI.ShowWindowDialog<ServerEntryProperties>(SelectedItem);
            if (dlg.MustSave) {
                SelectedItem.Proxy = old;
            }
        }
        Boolean canShowProperties(Object obj) {
            return SelectedItem != null;
        }
        Boolean canSaveCsv(Object obj) {
            return ServerList.Servers.Count > 0;
        }
        Boolean canSaveList(Object obj) {
            return ServerList.Servers.Count > 0;
        }

        void viewCertificate(Object obj) {
            X509Certificate2UI.DisplayCertificate(SelectedItem.Certificate);
        }
        Boolean canViewCertificate(Object obj) {
            if (SelectedItem == null) { return false; }
            return SelectedItem.Certificate != null && !IntPtr.Zero.Equals(SelectedItem.Certificate.Handle);
        }
        public void ViewChainCertificate(Object obj) {
            X509Certificate2UI.DisplayCertificate(SelectedTreeItem.Certificate);
        }
        Boolean canViewChainCertificate(Object obj) {
            return SelectedTreeItem?.Certificate != null;
        }

        public void StartSingleScan(Object obj) {
            singleScan = true;
            startScan(null);
        }
        Boolean canStartSingleScan(Object obj) {
            return SelectedIndex >= 0;
        }
        void addServerAndScan(Object obj) {
            ServerObject server = new ServerObject { ServerAddress = ServerName2.Trim().ToLower().Replace("https://", null), Port = Port2 };
            ServerList.Servers.Add(server);
            IsSaved = false;
            ServerName2 = String.Empty;
            SelectedIndex = ServerList.Servers.Count - 1;
            StartSingleScan(null);
        }
        Boolean canAddServerAndScan(Object obj) {
            return !String.IsNullOrEmpty(ServerName2) &&
                !String.IsNullOrWhiteSpace(ServerName2) &&
                !ServerList.Servers.Contains(new ServerObject { ServerAddress = ServerName2.Trim().ToLower().Replace("https://", null), Port = Port2 });
        }
        void prepareScan() {
            foreach (ServerObject server in ServerList.Servers) {
                if (singleScan) {
                    server.CanProcess = false;
                } else {
                    server.ItemStatus = ServerStatusEnum.Unknown;
                }
            }
            if (singleScan) {
                ServerList.Servers[SelectedIndex].CanProcess = true;
                ServerList.Servers[SelectedIndex].ItemStatus = ServerStatusEnum.Unknown;
            }
        }
        void startScan(Object obj) {
            Running = true;
            ProgressVisible = Visibility.Visible;
            ProgressWidth = 100;
            StatusText = "working...";
            prepareScan();
            BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
            var certVerifier = new CertProcessor {
                Config = new CertProcessorConfig {
                    Threshold = Threshold
                }
            };

            worker.DoWork += certVerifier.StartScan;
            worker.RunWorkerCompleted += endScan;
            worker.ProgressChanged += scanReportChanged;
            // reset statuses
            BackgroundObject arg = new BackgroundObject(ServerList.Servers) {
                SingleScan = singleScan
            };
            worker.RunWorkerAsync(arg);
            CommandManager.InvalidateRequerySuggested();
        }
        void scanReportChanged(Object sender, ProgressChangedEventArgs e) {
            if (e.UserState == null) {
                Progress = e.ProgressPercentage;
            } else {
                switch (((ReportObject)e.UserState).Action) {
                    case "Clear":
                        ServerList.Servers[((ReportObject)e.UserState).Index].Tree.Clear();
                        break;
                    case "Add":
                        ServerList.Servers[((ReportObject)e.UserState).Index].Tree.Add(((ReportObject)e.UserState).NewTree);
                        break;
                }
            }
        }
        void endScan(Object Sender, RunWorkerCompletedEventArgs e) {
            Running = singleScan = false;
            ProgressVisible = Visibility.Hidden;
            ProgressWidth = 0;
            foreach (ServerObject server in ServerList.Servers) {
                server.CanProcess = false;
            }
            ((BackgroundWorker)Sender).Dispose();
            StatusText = "ready";

        }
        Boolean canStartScan(Object obj) {
            return !Running && ServerList.Servers.Count > 0;
        }

        static void addServer(Object obj) {
            WindowsUI.ShowWindow<AddServerWindow>();
        }
        Boolean canAddServer(Object obj) {
            return !Running;
        }
        void removeServer(Object obj) {
            ServerList.Servers.RemoveAt(SelectedIndex);
            IsSaved = false;
        }
        Boolean CanRemoveServer(Object obj) {
            return SelectedIndex > -1;
        }

        static void ShowAbout(Object obj) {
            WindowsUI.ShowWindow<About>();
        }
        static void ShowLicense(Object obj) {
            WindowsUI.ShowWindow<LicenseWindow>();
        }
        void showSettings(Object obj) {
            WindowsUI.ShowWindowDialog<SettingsWindow>();
        }

        Boolean testSaved() {
            if (!IsSaved) {
                MessageBoxResult mbxResult = MsgBox.Show(
                    "SSL Certificate Verifier",
                    "Current server list was modified. Save changes?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNoCancel);
                switch (mbxResult) {
                    case MessageBoxResult.Yes:
                        saveList(null);
                        break;
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            return IsSaved;
        }
        public Boolean AddServerItem(String name, Int32 port) {
            ServerObject obj = new ServerObject { ServerAddress = name.Trim().ToLower().Replace("https://", null), Port = port };
            if (ServerList.Servers.Contains(obj)) {
                MsgBox.Show("Error", "Entered server name already in the list.");
                return false;
            }
            ServerList.Servers.Add(obj);
            IsSaved = false;
            return true;
        }
    }
}