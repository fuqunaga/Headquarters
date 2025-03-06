using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Headquarters;

public partial class ScriptRunPage : UserControl
{
    public ScriptRunPage()
    {
        InitializeComponent();
        MainGrid.IsVisibleChanged += MainGrid_IsVisibleChanged;
    }

    private void MainGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue) return;
        
        // レイアウト確定後に調整
        Dispatcher.BeginInvoke(AdjustRowHeights, DispatcherPriority.Loaded);
    }


    // 高さ調整ロジック
    // Row2(Output Field)の高さが50%未満にならないように調整
    private void AdjustRowHeights()
    {
        // Console.WriteLine($"{MainGrid.ActualHeight} {ParametersRow.ActualHeight} {Splitter.ActualHeight}");
        
        var totalHeight = MainGrid.ActualHeight;
        var minRow2Height = totalHeight * 0.5; // Row2の最小高さ（50%）
        var row0ContentHeight = ParametersRow.ActualHeight; // Row0のコンテンツ高さ
        var splitterHeight = Splitter.ActualHeight; // GridSplitterの高さ（固定値）

        // Row2が50%未満になる場合、Starで割合調整
        if (totalHeight - row0ContentHeight - splitterHeight < minRow2Height)
        {
            // Row0とRow2をStarで割合指定
            var availableHeight = totalHeight - splitterHeight; // Splitter分を除く
            RowDef0.Height = new GridLength(availableHeight - minRow2Height, GridUnitType.Star); // 残りをRow0に
            RowDef2.Height = new GridLength(minRow2Height, GridUnitType.Star);                   // Row2を50%に
        }
        else
        {
            // Row0はコンテンツサイズ、Row2は残り
            RowDef0.Height = GridLength.Auto;
            RowDef2.Height = new GridLength(1, GridUnitType.Star);
        }
    }
}