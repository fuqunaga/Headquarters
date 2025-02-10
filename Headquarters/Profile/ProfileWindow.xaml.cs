using System.Windows;

namespace Headquarters;

public partial class ProfileWindow : Window
{
    private static ProfileWindow? _instance;
    public static ProfileWindow Instance => _instance ??= new ProfileWindow();
    
    public ProfileWindow()
    {
        InitializeComponent();
    }
}