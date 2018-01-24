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
using ArchestrA.GRAccess;
using System.Text.RegularExpressions;

namespace AttributeWrangler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IGalaxy _galaxy;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(IGalaxy galaxy)
        {
            _galaxy = galaxy;
            InitializeComponent();
            Log(string.Format("Connected to galaxy {0}", galaxy.Name));
        }

        private void Log(string message)
        {
            txtLog.AppendText(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ": " + message + Environment.NewLine);
        }



        private List<aaObject> GetObjectList()
        {
            return null;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            Go(chkWhatif.IsChecked ?? false);
        }

        private void Go(bool whatif)
        {
            var objects = GetObjectList();
            //filter instances or templates

            foreach (var obj in objects)
            {
                if (obj.IsTemplate)
                {
                    string[] tagname = new string[] { obj.Name };
                    IgObjects queryResult = _galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsTemplate, ref tagname);
                    ICommandResult cmd = _galaxy.CommandResult;
                    if (!cmd.Successful)
                    {
                        Log("QueryObjectsByName Failed for $UserDefined Template :" + cmd.Text + " : " + cmd.CustomMessage);
                        continue;
                    }
                    ITemplate userDefinedTemplate = (ITemplate)queryResult[1];//can throw errors here
                    if (userDefinedTemplate.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                    {
                        Log(string.Format("Object [{0}] is already checked out by [{1}]", obj.Name, userDefinedTemplate.checkedOutBy));
                        continue;
                    }
                    userDefinedTemplate.CheckOut();

                    IAttributes attrs = userDefinedTemplate.ConfigurableAttributes;
                    //put all of the attributes into a dictionary so we can do linq queries
                    Dictionary<string, IAttribute> attributes = new Dictionary<string, IAttribute>();
                    Dictionary<string, IAttribute> filteredAttributes = new Dictionary<string, IAttribute>();
                    foreach (IAttribute attribute in attrs)
                    {
                        //there will be duplicates, but not for what we are interested in
                        if (!attributes.ContainsKey(attribute.Name))
                            attributes.Add(attribute.Name, attribute);
                    }
                    //filter attributes here
                    Regex regex = new Regex(txtAttributePattern.Text);

                    foreach (var kvp in attributes)
                    {
                        if (regex.IsMatch(kvp.Key))
                        {
                            filteredAttributes.Add(kvp.Key, kvp.Value);
                        }
                    }
                    ApplyActions(obj, filteredAttributes, whatif);
                    userDefinedTemplate.Save();
                    userDefinedTemplate.CheckIn();
                }
            }

            
        }
        private void ApplyActions(aaObject obj, Dictionary<string, IAttribute> attributes, bool whatif)
        {
            foreach (var kvp in attributes)
            {
                if (radFind.IsChecked == true)
                {

                }
                else if (radFindReplace.IsChecked == true)
                {
                    var dt = kvp.Value.value.GetDataType();
                    if (dt == MxDataType.MxReferenceType)
                    {
                        IMxReference mxref = kvp.Value.value.GetMxReference();
                        if (mxref.FullReferenceString.Contains(txtValuePattern.Text))
                        {

                            mxref.FullReferenceString = mxref.FullReferenceString.Replace(txtValuePattern.Text, txtReplaceValue.Text);
                            Log(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), mxref.FullReferenceString));
                            if (!whatif)
                            {
                                MxValue val = new MxValueClass();
                                val.PutMxReference(mxref);
                                kvp.Value.SetValue(val);
                            }
                        }
                    }
                    else if (dt == MxDataType.MxString)
                    {
                        string val = kvp.Value.value.GetString();
                        if (val.Contains(txtValuePattern.Text))
                        {
                            string newStr = val.Replace(txtValuePattern.Text, txtReplaceValue.Text);
                            Log(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), val));
                            if (!whatif)
                            {
                                MxValue newVal = new MxValueClass();
                                newVal.PutString(newStr);
                                kvp.Value.SetValue(newVal);
                            }
                        }
                    }
                    

                }
                else if (radFindUpdate.IsChecked == true)
                {

                }
                else if (radUpdate.IsChecked == true)
                {

                }
            }
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            switch (cmbObjectFilter.SelectedIndex)
            {
                case 0:
                    chkInstances.IsEnabled = true;
                    chkTemplates.IsEnabled = true;
                    break;
                case 1:
                    chkInstances.IsChecked = false;
                    chkInstances.IsEnabled = false;
                    chkTemplates.IsChecked = true;
                    chkTemplates.IsEnabled = false;
                    break;
                case 2:
                    chkInstances.IsChecked = true;
                    chkInstances.IsEnabled = false;
                    chkTemplates.IsChecked = false;
                    chkTemplates.IsEnabled = false;
                    break;
            }
        }
    }
}
