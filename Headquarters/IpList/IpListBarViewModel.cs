using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Headquarters
{
    public class IpListBarViewModel
    {
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

        private const string IpListFolder = @".\IpList";
        private const string IpListExtension = ".csv";


        public List<string> IpListFileList { get; } = [];
        public string SelectedIpListFile { get; set; }
        public Visibility ComboBoxVisibility { get; set; }

        public ICommand AddCommand { get; private set; }



        public void Initialize()
        {
            UpdateIpListFileList();

            AddCommand = new UiCommand() { Proc = Add };
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

        [SupportedOSPlatform("windows")]
        private static async void Add()
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

            }
        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
        }

    }
}
