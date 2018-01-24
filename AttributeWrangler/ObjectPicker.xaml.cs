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
using System.Windows.Shapes;

namespace AttributeWrangler
{
    /// <summary>
    /// Interaction logic for ObjectPicker.xaml
    /// </summary>
    public partial class ObjectPicker : Window
    {
        private PickerMode _mode;

        public ObjectPicker()
        {
            InitializeComponent();
        }

        public ObjectPicker(PickerMode mode)
        {
            InitializeComponent();
            _mode = mode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
