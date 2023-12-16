using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace Headquarters
{
    public class IpListBarViewModel : INotifyPropertyChanged
    {
        #region Type Define
        
        private class UiCommand : ICommand
        {
            public Action Proc { get; set; }
            #region ICommand メンバ
            public bool CanExecute(object parameter)
            {
                return true;
            }
            public event EventHandler CanExecuteChanged;
            public void Execute(object parameter)
            {
                Proc();
            }
            #endregion
        }
        
        #endregion

        
        private const string IpListFolder = @".\IPList";
        private const string DefaultIpListFileName = "iplist";
        private const string IpListFileExtension = ".csv";
        
        private static string DefaultIpListFilePath => FileNameToFullPath(DefaultIpListFileName);
        private static string FileNameToFullPath(string fileName) =>Path.Combine(IpListFolder, fileName);
        
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        #endregion

        private IPListViewModel _ipListViewModel;
        private string _selectedIpListFileNameName;

        
        
        public List<string> IpListFileList { get; } = [];

        public string SelectedIpListFileName
        {
            get => _selectedIpListFileNameName;
            set => SetField(ref _selectedIpListFileNameName, value);
        }
        public Visibility ComboBoxVisibility { get; set; }

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }
        
        
        public void Initialize(IPListViewModel ipListViewModel)
        {
            UpdateIpListFileList();
            AddCommand = new UiCommand() { Proc = ShowAddDialog };
            RemoveCommand = new UiCommand() { Proc = ShowRemoveDialog };

            _ipListViewModel = ipListViewModel;
            _ipListViewModel.Load(SelectedIpListFileName ?? DefaultIpListFilePath);
        }

        private void UpdateIpListFileList()
        {
            IpListFileList.Clear();

            if (Directory.Exists(IpListFolder))
            {
                IpListFileList.AddRange(
                    Directory.GetFiles(IpListFolder)
                    .Where(filePath => Path.GetExtension(filePath) == IpListFileExtension)
                    .Select(Path.GetFileName)
                );
            }

            if ( IpListFileList.Count > 0 )
            {
                SelectedIpListFileName = IpListFileList.FirstOrDefault();
                ComboBoxVisibility = Visibility.Visible;
            }
            else
            {
                ComboBoxVisibility = Visibility.Collapsed;
            }
        }
        
        private async void ShowAddDialog()
        {
            var vm = new NameDialogViewModel()
            {
                Title = "Add IP List file",
                Suffix = IpListFileExtension
            };

            var view = new NameDialog()
            {
                DataContext = vm
            };
            
            
            var binding = BindingOperations.GetBinding(view.NameTextBox, TextBox.TextProperty);
            if (binding != null)
            {
                var fileNotExistsValidationRule = new FileNotExistsValidationRule(IpListFolder);
                binding.ValidationRules.Add(fileNotExistsValidationRule);
            }

            var result = await DialogHost.Show(view, "RootDialog");
            if (result == null || !(bool)result) return;
            
            _ipListViewModel.Save(FileNameToFullPath($"{vm.Name}{IpListFileExtension}"));
            UpdateIpListFileList();
        }

        private async void ShowRemoveDialog()
        {
            var vm = new NameDialogViewModel
            {
                Title = "Remove?",
                Name = SelectedIpListFileName
            };
            
            var view = new NameDialog
            {
                DataContext = vm,
                NameTextBox =
                {
                    IsEnabled = false
                }
            };

            var result = await DialogHost.Show(view, "RootDialog");
            if (result == null || !(bool)result) return;
            
            File.Delete(FileNameToFullPath(SelectedIpListFileName));
            UpdateIpListFileList();
        }


    }
}
