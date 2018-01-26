using ArchestrA.GRAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        private string _galaxy;
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TreeViewModel Model;
        public List<ArchestrAObject> Result;

        public ObjectPicker()
        {
            InitializeComponent();
        }

        public ObjectPicker(string galaxy, PickerMode mode)
        {
            InitializeComponent();
            _galaxy = galaxy;
            _mode = mode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Model = new TreeViewModel(new List<ArchestrAObject>() { DatabasteFunctions.GetDerivationTree("localhost", _galaxy) });
            tvObjects.ItemsSource = Model.Children;
        }

        private void GetAreas()
        {
            
        }

        

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {

            Result = (from i in GetAllItems(Model) where i.IsSelected select i.aaObject).ToList();
            DialogResult = true;
            this.Close();
        }

        private List<ObjectViewModel> GetAllItems (TreeViewModel tvm)
        {
            List<ObjectViewModel> results = new List<ObjectViewModel>();
            foreach (var item in tvm.Children)
            {
                results.Add(item);
                results.AddRange(GetAllItems(item));
            }
            return results;
        }

        private List<ObjectViewModel> GetAllItems(ObjectViewModel ovm)
        {
            List<ObjectViewModel> results = new List<ObjectViewModel>();
            foreach (var item in ovm.Children)
            {
                results.Add(item);
                results.AddRange(GetAllItems(item));
            }
            return results;
        }

    }
}

