using System.Windows;

namespace UIShowcase;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public List<string> ComboBoxList { get; set; } = ["ComboBoxItem1", "ComboBoxItem2", "ComboBoxItem3"];
    public List<string> ListBoxList { get; set; } = ["ListBoxList1", "ListBoxList2", "ListBoxList3"];
    public List<string> ListViewList { get; set; } = ["ListViewList1", "ListViewList2", "ListViewList3"];

    private static SortedDictionary<int, double> CreateSampleHistogram()
    {
        SortedDictionary<int, double> histogram = [];
        for (short i = 0; i < 10; i++)
        {
            histogram.Add(i, (Random.Shared.NextDouble() * 0.1 + Math.Sin(0.01 * i)) / 2.2);
        }
        return histogram;
    }

    public SortedDictionary<int, double> Histogram { get; } = CreateSampleHistogram();
}
