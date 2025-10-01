using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Betrian.WPF;

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

    public SortedDictionary<int, double> Histogram
    {
        get
        {
            SortedDictionary<int, double> histogram = [];
            Random random = new(42);
            for (short i = 0; i < 10; i++)
            {
                histogram.Add(i, (random.NextDouble() * 0.1 + Math.Sin(0.01 * i)) / 2.2);
            }
            return histogram;
        }
    }
}
