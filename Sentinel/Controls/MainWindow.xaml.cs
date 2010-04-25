﻿#region License
//
// © Copyright Ray Hayes
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.
//
#endregion

#region Using directives

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sentinel.Interfaces;
using Sentinel.Logs.Gui;
using Sentinel.Logs.Interfaces;
using Sentinel.Providers.Interfaces;
using Sentinel.Services;
using Sentinel.Support.Mvvm;
using Sentinel.Views.Interfaces;

#endregion

namespace Sentinel.Controls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private PreferencesWindow preferencesWindow;
        private static ServiceLocator services;

        public MainWindow()
        {
            InitializeComponent();
        }

        public ICommand Add { get; private set; }

        public ICommand Exit { get; private set; }

        public IUserPreferences Preferences { get; private set; }

        public IViewManager ViewManager { get; private set; }

        /// <summary>
        /// AddNewListener method provides a mechanism for the user to add additional
        /// listeners to the log-viewer.
        /// </summary>
        /// <param name="obj">Object to add as a new listener.</param>
        private void AddNewListener(object obj)
        {
            NewLoggerWizard wizard = new NewLoggerWizard();

            if (!wizard.Display(this)) return;

            NewLoggerSettings settings = wizard.Settings;

            // Create the logger.
            ILogManager logManager = services.Get<ILogManager>();
            ILogger log = logManager.Add(settings.LogName);

            // Create the frame view
            Debug.Assert(ViewManager != null,
                         "A ViewManager should be registed with sevice locator for the IViewManager interface");
            IWindowFrame frame = services.Get<IWindowFrame>();
            frame.Log = log;
            frame.SetViews(settings.Views);
            ViewManager.Viewers.Add(frame);

            // Create the providers.
            IProviderManager providerManager = services.Get<IProviderManager>();
            foreach (PendingProviderRecord providerRecord in settings.Providers)
            {
                ILogProvider provider = providerManager.Create(providerRecord.Info.Identifier,
                                                               providerRecord.Settings);
                provider.Logger = log;
                provider.Start();
            }

            // Add to the tab control.
            TabItem tab = new TabItem
                              {
                                  Header = log.Name,
                                  Content = frame
                              };
            tabControl.Items.Add(tab);
            tabControl.SelectedItem = tab;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Add = new DelegateCommand(AddNewListener);

            // Append version number to caption (to save effort of producing an about screen)
            Title = string.Format(
                "{0} ({1})",
                Title,
                Assembly.GetExecutingAssembly().GetName().Version);

            services = ServiceLocator.Instance;
            Preferences = services.Get<IUserPreferences>();
            ViewManager = services.Get<IViewManager>();

            // Maintaining column widths is proving difficult in Xaml alone, so 
            // add an observer here and deal with it in code.
            if (Preferences != null && Preferences is INotifyPropertyChanged)
            {
                (Preferences as INotifyPropertyChanged).PropertyChanged += PreferencesChanged;
            }

            DataContext = this;

            Exit = new DelegateCommand(ee => Close());

            // When a new item is added, select the newest one.
            ViewManager.Viewers.CollectionChanged +=
                (s, ee) =>
                    {
                        if (ee.Action == NotifyCollectionChangedAction.Add)
                        {
                            tabControl.SelectedIndex = tabControl.Items.Count - 1;
                        }
                    };

            Add.Execute(null);
        }

        private void PreferencesChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Preferences != null)
            {
                if (e.PropertyName == "Show")
                {
                    if (Preferences.Show)
                    {
                        preferencesWindow = new PreferencesWindow {Owner = this};
                        preferencesWindow.Show();
                    }
                    else if (preferencesWindow != null)
                    {
                        preferencesWindow.Close();
                        preferencesWindow = null;
                    }
                }
            }
        }
    }
}