using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;

namespace Headquarters
{
    internal class IpListBarViewModel
    {
        public class UICommand : ICommand
        {
            public Action proc { get; set; }
            #region ICommand メンバ
            public bool CanExecute(object parameter)
            {
                return true;
            }
            public event EventHandler CanExecuteChanged;
            public void Execute(object parameter)
            {
                proc();
            }
            #endregion
        }

        private static readonly string _ipListFolder = @".\IpList";
        private static readonly string _ipListExtension = ".csv";


        public List<string> IpListFileList { get; } = [];
        public string SelectedIpListFile { get; set; }
        public Visibility ComboBoxVisibility { get; set; }

        public ICommand AddCommand { get; private set; }



        public void Initialize()
        {
            UpdateIpListFileList();

            AddCommand = new UICommand() { proc = Add };
        }


        private void UpdateIpListFileList()
        {
            IpListFileList.Clear();

            if (Directory.Exists(_ipListFolder))
            {
                IpListFileList.AddRange(
                    Directory.GetFiles(_ipListFolder)
                    .Where(filePath => Path.GetExtension(filePath) == _ipListExtension)
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
        private async void Add()
        {
            var vm = new NameDialogViewModel()
            {
                Title = "Add IpList:"
            };

            var view = new NameDialog()
            {
                DataContext = vm
            };

            var result = await DialogHost.Show(view, "RootDialog");
        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
        }

    }
}
