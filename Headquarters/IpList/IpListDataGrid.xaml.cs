﻿namespace Headquarters;

public partial class IpListDataGrid
{
    public IpListDataGrid()
    {
        InitializeComponent();
        DataContext = IpListDataGridViewModel.Instance;
    }
}