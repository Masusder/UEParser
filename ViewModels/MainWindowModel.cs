using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using UEParser.Views;

namespace UEParser.ViewModels
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private object _selectedPage = new Home();
        private object _selectedCategory = "Home";
        private Control _currentPage = new Home();

        public object SelectedPage
        {
            get => _selectedPage;
            set
            {
                _selectedPage = value;
                OnPropertyChanged();
            }
        }

        public object SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                SetCurrentPage();
            }
        }

        public Control CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public MainWindowModel()
        {
            // Initialize with Home page
            CurrentPage = new Home();
        }

        private void SetCurrentPage()
        {
            if (SelectedCategory is NavigationViewItem nvi)
            {
                CurrentPage = nvi?.Tag?.ToString() switch
                {
                    "Home" => new Home(),
                    "Controllers" => new ParsingControllers(),
                    //"WebsiteUpdate" => new WebsiteUpdatePage(),
                    //"API" => new APIPage(),
                    //"Netease" => new NeteasePage(),
                    _ => new Home() // Default case
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
