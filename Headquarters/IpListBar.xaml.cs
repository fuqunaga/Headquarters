using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace Headquarters
{
    /// <summary>
    /// IpListBar.xaml の相互作用ロジック
    /// </summary>
    public partial class IpListBar : UserControl
    {
        private static readonly string _ipListFolder = @".\IpList";
        private static readonly string _ipListExtension = ".csv";


        public List<string> IpListFileList { get; } = [];
        public string SelectedIpListFile { get; set; }

        public IpListBar()
        {
            InitializeComponent();
            DataContext = this;
            UpdateIpListFileList();
            SelectedIpListFile = IpListFileList.FirstOrDefault();
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

            IpListComboBox.Visibility = IpListFileList.Any() 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }
}
