using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace Headquarters
{
    [SuppressMessage("Interoperability", "CA1416:プラットフォームの互換性を検証")]
    public class IpListBarViewModel
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

        
        private const string IpListFolder = @".\IpList";
        private const string IpListExtension = ".csv";


        private List<string> IpListFileList { get; } = [];
        public string SelectedIpListFile { get; set; }
        public Visibility ComboBoxVisibility { get; set; }

        public ICommand AddCommand { get; private set; }



        public void Initialize()
        {
            UpdateIpListFileList();

            AddCommand = new UiCommand() { Proc = ShowAddDialog };
        }


        private void UpdateIpListFileList()
        {
            IpListFileList.Clear();

            if (Directory.Exists(IpListFolder))
            {
                IpListFileList.AddRange(
                    Directory.GetFiles(IpListFolder)
                    .Where(filePath => Path.GetExtension(filePath) == IpListExtension)
                    .Select(Path.GetFileName)
                );
            }

            if ( IpListFileList.Count > 0 )
            {
                SelectedIpListFile = IpListFileList.FirstOrDefault();
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
                Title = "Add IpList file",
                Suffix = IpListExtension
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

            if (result != null && (bool)result)
            {
                AddIpListFile(vm.Name);
            }
        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AddIpListFile(string filePath)
        {
            var path = Path.Combine(IpListFolder, filePath);
            File.WriteAllTextAsync(path, SelectedIpListFile);
        }

    }
}
