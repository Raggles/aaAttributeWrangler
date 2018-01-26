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
        GRAccessApp _grAccess;
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<int, ArchestrAObject> Objects = new Dictionary<int, ArchestrAObject>();

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(GRAccessApp grAccess, IGalaxy galaxy)
        {
            _galaxy = galaxy;
            _grAccess = grAccess;
            InitializeComponent();
            _log.Info(string.Format("Connected to galaxy {0}", galaxy.Name));
        }

        private List<ArchestrAObject> GetObjectList()
        {
            return Objects.Values.ToList();
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            Go(chkWhatif.IsChecked ?? false);
        }

        private void Go(bool whatif)
        {

            var objects = GetObjectList();

            foreach (var obj in objects)
            {
                try
                {
                    if (obj.IsTemplate)
                    {
                        string[] tagname = new string[] { obj.Name };
                        IgObjects queryResult = _galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsTemplate, ref tagname);
                        ICommandResult cmd = _galaxy.CommandResult;
                        if (!cmd.Successful)
                        {
                            _log.Warn("QueryObjectsByName Failed for $UserDefined Template :" + cmd.Text + " : " + cmd.CustomMessage);
                            continue;
                        }
                        ITemplate userDefinedTemplate = (ITemplate)queryResult[1];//can throw errors here
                        if (userDefinedTemplate.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                        {
                            _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", obj.Name, userDefinedTemplate.checkedOutBy));
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
                    else
                    {
                        string[] tagname = new string[] { obj.Name };
                        IgObjects queryResult = _galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, ref tagname);
                        ICommandResult cmd = _galaxy.CommandResult;
                        if (!cmd.Successful)
                        {
                            _log.Warn("QueryObjectsByName Failed for $UserDefined Template :" + cmd.Text + " : " + cmd.CustomMessage);
                            continue;
                        }
                        IInstance userDefinedTemplate = (IInstance)queryResult[1];//can throw errors here
                        if (userDefinedTemplate.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                        {
                            _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", obj.Name, userDefinedTemplate.checkedOutBy));
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
                        try
                        {
                            ApplyActions(obj, filteredAttributes, whatif);
                            userDefinedTemplate.Save();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.ToString());
                        }
                        userDefinedTemplate.CheckIn();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }

            }

        }

        private void ApplyActions(ArchestrAObject obj, Dictionary<string, IAttribute> attributes, bool whatif)
        {
            foreach (var kvp in attributes)
            {
                try
                {
                    if (radFind.IsChecked == true)
                    {
                        _log.Info(string.Format("Found matching attribute [{0}] on object [{1}] with data type [{2}] and value [{3}]", kvp.Key, obj.Name, kvp.Value.DataType.ToString(), kvp.Value.value.GetString()));
                    }
                    else if (radFindReplace.IsChecked == true)
                    {
                        switch (kvp.Value.DataType)
                        {
                            case MxDataType.MxReferenceType:
                                IMxReference mxref = kvp.Value.value.GetMxReference();
                                if (mxref.FullReferenceString.Contains(txtValuePattern.Text))
                                {
                                    mxref.FullReferenceString = mxref.FullReferenceString.Replace(txtValuePattern.Text, txtReplaceValue.Text);
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), mxref.FullReferenceString));
                                    if (!whatif)
                                    {
                                        MxValue mxval = new MxValueClass();
                                        mxval.PutMxReference(mxref);
                                        kvp.Value.SetValue(mxval);
                                    }
                                }
                                break;

                            case MxDataType.MxString:
                                string strval = kvp.Value.value.GetString();
                                if (strval.Contains(txtValuePattern.Text))
                                {
                                    string newStr = strval.Replace(txtValuePattern.Text, txtReplaceValue.Text);
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), newStr));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutString(newStr);
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                        }


                    }
                    else if (radFindUpdate.IsChecked == true)
                    {
                        switch (kvp.Value.DataType)
                        {
                            case MxDataType.MxReferenceType:
                                IMxReference mxref = kvp.Value.value.GetMxReference();
                                if (mxref.FullReferenceString == txtValuePattern.Text)
                                {
                                    mxref.FullReferenceString = txtReplaceValue.Text;
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), mxref.FullReferenceString));
                                    if (!whatif)
                                    {
                                        MxValue mxval = new MxValueClass();
                                        mxval.PutMxReference(mxref);
                                        kvp.Value.SetValue(mxval);
                                    }
                                }
                                break;

                            case MxDataType.MxString:
                                string strval = kvp.Value.value.GetString();
                                if (strval == txtValuePattern.Text)
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutString(txtReplaceValue.Text);
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                            case MxDataType.MxInteger:
                                int intVal = kvp.Value.value.GetInteger();
                                if (intVal == int.Parse(txtValuePattern.Text))
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutInteger(int.Parse(txtReplaceValue.Text));
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                            case MxDataType.MxFloat:
                                float floatVal = kvp.Value.value.GetFloat();
                                if (floatVal == int.Parse(txtValuePattern.Text))
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutFloat(float.Parse(txtReplaceValue.Text));
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                            case MxDataType.MxDouble:
                                double doubleVal = kvp.Value.value.GetDouble();
                                if (doubleVal == int.Parse(txtValuePattern.Text))
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutDouble(double.Parse(txtReplaceValue.Text));
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                            case MxDataType.MxBoolean:
                                bool boolVal = kvp.Value.value.GetBoolean();
                                if (boolVal == bool.Parse(txtValuePattern.Text))
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutBoolean(bool.Parse(txtReplaceValue.Text));
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                        }
                    }
                    else if (radUpdate.IsChecked == true)
                    {
                        switch (kvp.Value.DataType)
                        {
                            case MxDataType.MxReferenceType:
                                IMxReference mxref = kvp.Value.value.GetMxReference();
                                mxref.FullReferenceString = txtReplaceValue.Text;
                                _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), mxref.FullReferenceString));
                                if (!whatif)
                                {
                                    MxValue mxval = new MxValueClass();
                                    mxval.PutMxReference(mxref);
                                    kvp.Value.SetValue(mxval);
                                }
                                break;

                            case MxDataType.MxString:
                                string strval = kvp.Value.value.GetString();
                                string newStr = txtReplaceValue.Text;
                                _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), newStr));
                                if (!whatif)
                                {
                                    MxValue newVal = new MxValueClass();
                                    newVal.PutString(newStr);
                                    kvp.Value.SetValue(newVal);
                                }
                                break;
                            case MxDataType.MxInteger:
                                int intVal = kvp.Value.value.GetInteger();
                                if (intVal == int.Parse(txtValuePattern.Text))
                                {
                                    _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                    if (!whatif)
                                    {
                                        MxValue newVal = new MxValueClass();
                                        newVal.PutInteger(int.Parse(txtReplaceValue.Text));
                                        kvp.Value.SetValue(newVal);
                                    }
                                }
                                break;
                            case MxDataType.MxFloat:
                                float floatVal = kvp.Value.value.GetFloat();
                                _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                if (!whatif)
                                {
                                    MxValue newVal = new MxValueClass();
                                    newVal.PutFloat(float.Parse(txtReplaceValue.Text));
                                    kvp.Value.SetValue(newVal);
                                }
                                break;
                            case MxDataType.MxDouble:
                                double doubleVal = kvp.Value.value.GetDouble();
                                _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                if (!whatif)
                                {
                                    MxValue newVal = new MxValueClass();
                                    newVal.PutDouble(double.Parse(txtReplaceValue.Text));
                                    kvp.Value.SetValue(newVal);
                                }
                                break;
                            case MxDataType.MxBoolean:
                                bool boolVal = kvp.Value.value.GetBoolean();
                                _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", kvp.Key, obj.Name, kvp.Value.value.GetString(), txtReplaceValue.Text));
                                if (!whatif)
                                {
                                    MxValue newVal = new MxValueClass();
                                    newVal.PutBoolean(bool.Parse(txtReplaceValue.Text));
                                    kvp.Value.SetValue(newVal);
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("Error while processing attribute {0} : {1}", kvp.Key, ex.ToString()));
                }
            }
        }
        
        private void btnAddFromDerivationTree_Click(object sender, RoutedEventArgs e)
        {
            var form = new ObjectPicker(_galaxy.Name, PickerMode.List);
            var result = form.ShowDialog();
            if (result == true)
            {
                foreach (var item in form.Result)
                {
                    if (!Objects.ContainsKey(item.ObjectID))
                    {
                        Objects.Add(item.ObjectID, item);
                        lstObjects.Items.Add(item);
                    }
                }

            }
        }

        private void btnAdvancedSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdvancedSearch("localhost", _galaxy.Name);
            if (dialog.ShowDialog() == true)
            {
                foreach (var item in dialog.Result)
                {
                    if (!Objects.ContainsKey(item.ObjectID))
                    {
                        Objects.Add(item.ObjectID, item);
                        lstObjects.Items.Add(item);
                    }
                }
            }
        }

        private void btnClearSelected_Click(object sender, RoutedEventArgs e)
        {
            List<ArchestrAObject> objectsToRemove = new List<ArchestrAObject>();
            foreach (ArchestrAObject item in lstObjects.SelectedItems)
            {
                Objects.Remove(item.ObjectID);
                objectsToRemove.Add(item);
            }
            foreach (var item in objectsToRemove)
            {
                lstObjects.Items.Remove(item);
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            Objects.Clear();
            lstObjects.Items.Clear();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }
    }
}
