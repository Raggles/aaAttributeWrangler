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
using System.Threading;

namespace AttributeWrangler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IGalaxy _galaxy;
        private Thread _t;
        private bool _abortOperation = false;
        private GRAccessApp _grAccess;
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private Dictionary<int, ArchestrAObject> _objects = new Dictionary<int, ArchestrAObject>();

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

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            gridControls.IsEnabled = false;
            _abortOperation = false;
            btnAbort.IsEnabled = true;
            btnGo.IsEnabled = false;
            spinner.Visibility = Visibility.Visible;
            
            _t = new Thread(() =>
            {
                try
                {
                    Go();

                    this.Dispatcher.Invoke(() =>
                    {
                        gridControls.IsEnabled = true;
                        btnAbort.IsEnabled = false;
                        btnGo.IsEnabled = true;
                        spinner.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            });
            _t.Start();
        }
        
        private void Go()
        {
            foreach (var obj in _model.Objects)
            {
                try
                {
                    if (obj.IsTemplate)
                    {
                        string[] tagname = new string[] { obj.Name };
                        _log.Debug(string.Format("Querying galaxy for {0}", obj.Name));
                        IgObjects queryResult = _galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsTemplate, ref tagname);
                        ICommandResult cmd = _galaxy.CommandResult;
                        if (!cmd.Successful)
                        {
                            _log.Warn("QueryObjectsByName Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                            continue;
                        }
                        ITemplate template = (ITemplate)queryResult[1];//can throw errors here
                        if (template.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                        {
                            _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", obj.Name, template.checkedOutBy));
                            continue;
                        }
                        _log.Debug(string.Format("Checking out {0}", obj.Name));
                        template.CheckOut();
                        _log.Debug(string.Format("Checked out {0}", obj.Name));

                        ProcessAttributes(obj, template.ConfigurableAttributes);
                        template.Save();
                        template.CheckIn();
                        if (_abortOperation)
                        {
                            _log.Warn("Operation was aborted");
                            return;
                        }
                    }
                    else
                    {
                        string[] tagname = new string[] { obj.Name };
                        IgObjects queryResult = _galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, ref tagname);
                        ICommandResult cmd = _galaxy.CommandResult;
                        if (!cmd.Successful)
                        {
                            _log.Warn("QueryObjectsByName Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                            continue;
                        }
                        IInstance instance = (IInstance)queryResult[1];//can throw errors here
                        if (instance.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                        {
                            _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", obj.Name, instance.checkedOutBy));
                            continue;
                        }
                        instance.CheckOut();
                        ProcessAttributes(obj, instance.ConfigurableAttributes);
                        instance.Save();
                        instance.CheckIn();
                        if (_abortOperation)
                        {
                            _log.Warn("Operation was aborted");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            }
        }

        private void ProcessAttributes(ArchestrAObject obj, IAttributes attributes)
        {
            Dictionary<string, IAttribute> attributesDict = new Dictionary<string, IAttribute>();
            Dictionary<string, IAttribute> filteredAttributesDict = new Dictionary<string, IAttribute>();
            foreach (IAttribute attribute in attributes)
            {
                //there will be duplicates, but not for what we are interested in
                if (!attributesDict.ContainsKey(attribute.Name))
                    attributesDict.Add(attribute.Name, attribute);
            }
            //filter attributes here
            Regex regex = new Regex(_model.AttributePattern);

            foreach (var kvp in attributesDict)
            {
                if (regex.IsMatch(kvp.Key))
                {
                    filteredAttributesDict.Add(kvp.Key, kvp.Value);
                }
            }
            try
            {
                ApplyActions(obj, filteredAttributesDict);
                
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        
        private void ApplyActions(ArchestrAObject obj, Dictionary<string, IAttribute> attributes)
        {
            foreach (var kvp in attributes)
            {
                try
                {
                    switch (_model.Operation)
                    {
                        case Operation.Find:
                            if (string.IsNullOrWhiteSpace(_model.FindValue))
                            {
                                _log.Info(string.Format("Found matching attribute [{0}] on object [{1}] with data type [{2}] and value [{3}]", kvp.Key, obj.Name, kvp.Value.DataType.ToString(), kvp.Value.value.GetString()));
                            }
                            else
                            {
                                string val = kvp.Value.value.GetString();
                                Regex r = new Regex(_model.FindValue);
                                if (r.IsMatch(val))
                                    _log.Info(string.Format("Found matching attribute [{0}] on object [{1}] with data type [{2}] and value [{3}]", kvp.Key, obj.Name, kvp.Value.DataType.ToString(), kvp.Value.value.GetString()));
                            }
                            break;
                        case Operation.SetLocked:
                            _log.Error(string.Format("Setting Lock status on attribute [{0}] on object [{1}] to [{2}]", kvp.Key, obj.Name, _model.Locked.ToString()));
                            if (!_model.WhatIf)
                            {
                                kvp.Value.SetLocked(_model.Locked);
                                ICommandResult cmd = kvp.Value.CommandResult;
                                if (!cmd.Successful)
                                {
                                    _log.Warn("SetLocked Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                                    continue;
                                }
                            }
                            break;
                        case Operation.SetSecurity:
                            _log.Error(string.Format("Setting security classification on attribute [{0}] on object [{1}] to [{2}]", kvp.Key, obj.Name, _model.Security.ToString()));
                            if (!_model.WhatIf)
                            {
                                kvp.Value.SetSecurityClassification(_model.Security);
                                ICommandResult cmd = kvp.Value.CommandResult;
                                if (!cmd.Successful)
                                {
                                    _log.Warn("SetLocked Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                                    continue;
                                }
                            }
                            break;
                        default:
                            switch (kvp.Value.DataType)
                            {
                                case MxDataType.MxReferenceType:
                                    GalaxyFunctions.UpdateMxReference(_model.WhatIf, obj, kvp.Value, _model.Operation, _model.ReplaceValue, _model.FindValue);
                                    break;
                                case MxDataType.MxString:
                                    GalaxyFunctions.UpdateMxString(_model.WhatIf, obj, kvp.Value, _model.Operation, _model.ReplaceValue, _model.FindValue);
                                    break;
                                case MxDataType.MxInteger:
                                    int intFind, intReplace;
                                    if (!int.TryParse(_model.FindValue, out intFind))
                                        _log.Error("Find value is not a valid integer");
                                    if (!int.TryParse(_model.ReplaceValue, out intReplace))
                                        _log.Error("Replace value is not a valid integer");
                                    GalaxyFunctions.UpdateMxInteger(_model.WhatIf, obj, kvp.Value, _model.Operation, intReplace, intFind);
                                    break;
                                case MxDataType.MxFloat:
                                    float fltFind, fltReplace;
                                    if (!float.TryParse(_model.FindValue, out fltFind))
                                        _log.Error("Find value is not a valid float");
                                    if (!float.TryParse(_model.ReplaceValue, out fltReplace))
                                        _log.Error("Replace value is not a valid float");
                                    GalaxyFunctions.UpdateMxFloat(_model.WhatIf, obj, kvp.Value, _model.Operation, fltReplace, fltFind);
                                    break;
                                case MxDataType.MxDouble:
                                    double dblFind, dblReplace;
                                    if (!double.TryParse(_model.FindValue, out dblFind))
                                        _log.Error("Find value is not a valid double");
                                    if (!double.TryParse(_model.ReplaceValue, out dblReplace))
                                        _log.Error("Replace value is not a valid double");
                                    GalaxyFunctions.UpdateMxDouble(_model.WhatIf, obj, kvp.Value, _model.Operation, dblReplace, dblFind);
                                    break;
                                case MxDataType.MxBoolean:
                                    bool bFind, bReplace;
                                    if (!bool.TryParse(_model.FindValue, out bFind))
                                        _log.Error("Find value is not a valid boolean");
                                    if (!bool.TryParse(_model.ReplaceValue, out bReplace))
                                        _log.Error("Replace value is not a valid float");
                                    GalaxyFunctions.UpdateMxBool(_model.WhatIf, obj, kvp.Value, _model.Operation, bReplace, bFind);
                                    break;
                                default:
                                    _log.Warn(string.Format("Attribute [{0}] on object [{1}] has a data type [{2}] which is not supported", kvp.Key, obj.Name, kvp.Value.DataType.ToString()));
                                    break;
                            }
                            break;
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
                    if (_model.AddObject(item))
                        lstObjects.Items.Add(item);
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
                    if (_model.AddObject(item))
                        lstObjects.Items.Add(item);
                }
            }
        }

        private void btnClearSelected_Click(object sender, RoutedEventArgs e)
        {
            List<ArchestrAObject> objectsToRemove = new List<ArchestrAObject>();
            foreach (ArchestrAObject item in (from ArchestrAObject i in lstObjects.SelectedItems select i).ToList())
            {
                _model.RemoveObject(item);
                lstObjects.Items.Remove(item);
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            _model.Objects.Clear();
            lstObjects.Items.Clear();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }
    }
}
