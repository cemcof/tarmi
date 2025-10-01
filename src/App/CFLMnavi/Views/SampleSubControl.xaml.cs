using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Betrian.CflmNavi.App.ViewModels;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views
{
    public partial class SampleSubControl : ControlBase<SampleSubModuleViewModel>
    {
        public SampleSubControl()
        {
            InitializeComponent();
        }
    }
}
