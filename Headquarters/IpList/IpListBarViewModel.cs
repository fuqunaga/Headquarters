using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Headquarters
{
    public class IpListBarViewModel : ViewModelBase
    {
        private const string IpListFolder = @".\IPList";
        private const string DefaultIpListFileName = "iplist";
        private const string IpListFileExtension = ".csv";
        
        private static string DefaultIpListFilePath => FileNameToFullPath(DefaultIpListFileName);
        private static string FileNameToFullPath(string fileName) =>Path.Combine(IpListFolder, fileName);


        private IpListDataGridViewModel _ipListDataGridViewModel;
        private string _selectedIpListFileNameName;

        
        public List<string> IpListFileList { get; } = [];

        
        public void Initialize(IpListDataGridViewModel ipListDataGridViewModel)
        {
            // UpdateIpListFileList();
            // // SaveCommand = new UiCommand() { Proc = Save };
            // // LoadCommand = new UiCommand() { Proc = Load };
            // // OpenFolderCommand = new UiCommand() { Proc = OpenFolderCommand };
            //
            _ipListDataGridViewModel = ipListDataGridViewModel;
            _ipListDataGridViewModel.Load(DefaultIpListFilePath);
        }
        
        #if false
        
        private async void ShowAddDialog()
        {
            var viewModel = new NameDialogViewModel()
            {
                Title = "Add IP List file",
                Suffix = IpListFileExtension
            };

            var view = new NameDialog()
            {
                DataContext = viewModel
            };
            
            
            var binding = BindingOperations.GetBinding(view.NameTextBox, TextBox.TextProperty);
            if (binding != null)
            {
                var fileNotExistsValidationRule = new FileNotExistsValidationRule(IpListFolder);
                binding.ValidationRules.Add(fileNotExistsValidationRule);
            }

            var result = await DialogHost.Show(view, "RootDialog");
            if (result == null || !(bool)result) return;
            
            _ipListViewModel.Save(FileNameToFullPath($"{viewModel.Name}{IpListFileExtension}"));
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
#endif
    }
}
