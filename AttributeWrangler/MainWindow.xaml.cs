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
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Win32;
using Newtonsoft.Json;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(GRAccessApp grAccess, IGalaxy galaxy)
        {
            _galaxy = galaxy;
            _grAccess = grAccess;
            ArchestrAObject.Galaxy = galaxy;
            ArchestrAObject.GrAccess = grAccess;
            InitializeComponent();
            _log.Info(string.Format("Connected to galaxy {0}", galaxy.Name));
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            if (tabMain.SelectedIndex == 0)
            {
                StartOperation();
                _t = new Thread(() =>
                {
                    try
                    {
                        Go();
                        _log.Info("All done");
                        FinishOperation();                        
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                    }
                });
                _t.Start();
            }
            else if (tabMain.SelectedIndex ==1)
            {
                string[] files = (from string i in lstFiles.Items select i).ToArray();
                if (files.Length != 0)
                {
                    StartOperation();
                    _t = new Thread(() =>
                    {
                        CsvUpdate(files);
                        _log.Info("All done");
                        FinishOperation();
                    });
                    _t.Start();
                }
            }
        }

        private void Go()
        {
            foreach (var obj in _model.Objects)
            {
                try
                {
                    obj.Checkout();
                    try
                    {
                        ProcessAttributes(obj, obj.Attributes);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                    }

                    obj.CheckIn(!_model.WhatIf);
                    if (_abortOperation)
                    {
                        _log.Warn("Operation was aborted");
                        return;
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
            if (attributes == null)
            {
                _log.Error(string.Format("Configurable attributes on {0} was null.  Possibly being updated by checkin from other object", obj.Name));
                return;
            }
            foreach (var searchParams in _model.SearchParameters)
            {
                Dictionary<string, IAttribute> attributesDict = new Dictionary<string, IAttribute>();
                Dictionary<string, IAttribute> filteredAttributesDict = new Dictionary<string, IAttribute>();
                foreach (IAttribute attribute in attributes)
                {
                    //there will be duplicates, but not for what we are interested in
                    if (!attributesDict.ContainsKey(attribute.Name))
                        attributesDict.Add(attribute.Name, attribute);
                }

                Regex regex = new Regex(searchParams.AttributePattern);

                foreach (var kvp in attributesDict)
                {
                    if (regex.IsMatch(kvp.Key))
                    {
                        filteredAttributesDict.Add(kvp.Key, kvp.Value);
                    }
                }
                ApplyActions(obj, filteredAttributesDict, searchParams);
            }
        }
        
        private void ApplyActions(ArchestrAObject obj, Dictionary<string, IAttribute> attributes, SearchParametersViewModel searchParams)
        {
            foreach (var kvp in attributes)
            {
                try
                {
                    switch (searchParams.Operation)
                    {
                        case Operation.Find:
                            if (string.IsNullOrWhiteSpace(searchParams.FindValue))
                            {
                                _log.Info(string.Format("Found matching attribute [{0}] on object [{1}] with data type [{2}] and value [{3}]", kvp.Key, obj.Name, kvp.Value.DataType.ToString(), kvp.Value.value.GetString()));
                            }
                            else
                            {
                                string val = kvp.Value.value.GetString();
                                string pattern = searchParams.FindValue.Replace("~%obj", obj.Name);
                                Regex r = new Regex(pattern);
                                if (r.IsMatch(val))
                                    _log.Info(string.Format("Found matching attribute [{0}] on object [{1}] with data type [{2}] and value [{3}]", kvp.Key, obj.Name, kvp.Value.DataType.ToString(), kvp.Value.value.GetString()));
                            }
                            break;
                        case Operation.SetLocked:
                            _log.Error(string.Format("Setting Lock status on attribute [{0}] on object [{1}] to [{2}]", kvp.Key, obj.Name, searchParams.Locked.ToString()));
                            if (!_model.WhatIf)
                            {
                                kvp.Value.SetLocked(searchParams.Locked);
                                ICommandResult cmd = kvp.Value.CommandResult;
                                if (!cmd.Successful)
                                {
                                    _log.Warn("SetLocked Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                                    continue;
                                }
                            }
                            break;
                        case Operation.SetSecurity:
                            _log.Error(string.Format("Setting security classification on attribute [{0}] on object [{1}] to [{2}]", kvp.Key, obj.Name, searchParams.Security.ToString()));
                            if (!_model.WhatIf)
                            {
                                kvp.Value.SetSecurityClassification(searchParams.Security);
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
                                    GalaxyFunctions.UpdateMxReference(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, searchParams.ReplaceValue, searchParams.FindValue);
                                    break;
                                case MxDataType.MxString:
                                    GalaxyFunctions.UpdateMxString(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, searchParams.ReplaceValue, searchParams.FindValue);
                                    break;
                                case MxDataType.MxInteger:
                                    int intFind, intReplace;
                                    if (!int.TryParse(searchParams.FindValue, out intFind) && searchParams.Operation != Operation.Update)
                                    {
                                        _log.Error("Find value is not a valid integer");
                                        return;
                                    }
                                    if (!int.TryParse(searchParams.ReplaceValue, out intReplace))
                                    {
                                        _log.Error("Replace value is not a valid integer");
                                        return;
                                    }
                                    GalaxyFunctions.UpdateMxInteger(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, intReplace, intFind);
                                    break;
                                case MxDataType.MxFloat:
                                    float fltFind, fltReplace;
                                    if (!float.TryParse(searchParams.FindValue, out fltFind) && searchParams.Operation != Operation.Update)
                                    {
                                        _log.Error("Find value is not a valid float");
                                        return;
                                    }
                                    if (!float.TryParse(searchParams.ReplaceValue, out fltReplace))
                                    {
                                        _log.Error("Replace value is not a valid float");
                                        return;
                                    }
                                    GalaxyFunctions.UpdateMxFloat(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, fltReplace, fltFind);
                                    break;
                                case MxDataType.MxDouble:
                                    double dblFind, dblReplace;
                                    if (!double.TryParse(searchParams.FindValue, out dblFind) && searchParams.Operation != Operation.Update)
                                    {
                                        _log.Error("Find value is not a valid double");
                                        return;
                                    }
                                    if (!double.TryParse(searchParams.ReplaceValue, out dblReplace))
                                    {
                                        _log.Error("Replace value is not a valid double");
                                        return;
                                    }
                                    GalaxyFunctions.UpdateMxDouble(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, dblReplace, dblFind);
                                    break;
                                case MxDataType.MxBoolean:
                                    bool bFind, bReplace;
                                    if (!bool.TryParse(searchParams.FindValue, out bFind) && searchParams.Operation != Operation.Update)
                                    {
                                        _log.Error("Find value is not a valid boolean");
                                        return;
                                    }
                                    if (!bool.TryParse(searchParams.ReplaceValue, out bReplace))
                                    {
                                        _log.Error("Replace value is not a valid float");
                                        return;
                                    }
                                    GalaxyFunctions.UpdateMxBool(_model.WhatIf, obj.Name, kvp.Value, searchParams.Operation, bReplace, bFind);
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
            _model.ClearObjects();
            lstObjects.Items.Clear();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void btnAbort_Click(object sender, RoutedEventArgs e)
        {
            _abortOperation = true;
            lblAbort.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tabMain.IsEnabled == false)
            {
                MessageBox.Show("Please wait for any pending operations to complete.");
                e.Cancel = true;
            }
        }

        public void CsvUpdate(string[] files)
        {
            List<ArchestrACsvItem> Items = new List<ArchestrACsvItem>();
            foreach (var file in files)
            {
                try
                {
                    if (_abortOperation)
                    {
                        _log.Warn("Operation was aborted");
                        return;
                    }
                    using (CachedCsvReader csv = new CachedCsvReader(new StreamReader(file), true))
                    {
                        while (csv.ReadNextRecord())
                        {
                            ArchestrACsvItem i = new ArchestrACsvItem();
                            i.Object = csv["Object"];
                            i.Attribute = csv["Attribute"];
                            i.Type = csv["Type"];
                            i.Value = csv["Value"];
                            if (i.Type.ToUpper() == "DI" || i.Type.ToUpper() == "AI" || i.Type.ToUpper() == "CO" || i.Type.ToUpper() == "INPUT" || i.Type.ToUpper() == "DI?" || i.Type.ToUpper() == "AI?" || i.Type.ToUpper() == "CO?")
                            {
                                i.IsInput = true;
                                i.Attribute += ".InputSource";
                            }
                            else if (i.Type.ToUpper() == "DO" || i.Type.ToUpper() == "AO" || i.Type.ToUpper() == "OUTPUT" || i.Type.ToUpper() == "DO?" || i.Type.ToUpper() == "AO?")
                            {
                                i.IsInput = false;
                                i.Attribute += ".OutputDest";
                            }
                            else if (i.Type.ToUpper() != "SCALING")
                                continue;
                            Items.Add(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            }
            var query = from i in Items group i by i.Object;

            foreach (var group in query)
            {
                try
                {
                    ArchestrAObject o = new ArchestrAObject() { Name = group.Key, IsTemplate = false };
                    if (_abortOperation)
                    {
                        _log.Warn("Operation was aborted");
                        return;
                    }
                    if (!o.Checkout())
                        continue;
                    var attributes = o.Attributes;
                    if (attributes == null)
                        return;
                    foreach (var a2item in group)
                    {
                        try
                        {
                            if (a2item.Type.ToLower() == "scaling")
                            {
                                IAttribute attrib = attributes[a2item.Attribute];
                                if (attrib == null)
                                {
                                    attrib = attributes[ArchestrAObject.GetRootAttribute(a2item.Attribute)];
                                    if (attrib != null)
                                    {
                                        if (!o.AddPrimitive(attrib, Primitive.scalingextension))
                                            continue;
                                        attributes = o.Attributes;
                                        attrib = attributes[a2item.Attribute];
                                    }
                                }
                                if (attrib != null)
                                { 
                                    GalaxyFunctions.UpdateMxValue(_model.WhatIf, group.Key, attrib,  a2item.Value);
                                }
                            }
                            else
                            {
                                IAttribute attrib = attributes[a2item.Attribute];
                                if (attrib != null)
                                {
                                    _log.Debug("Attribute is a UDA");
                                    GalaxyFunctions.UpdateMxReference(_model.WhatIf, group.Key, attrib, Operation.Update, a2item.Value);
                                }
                                else
                                {
                                    //maybe its a field attribute?
                                    attrib = attributes[a2item.Attribute.Replace("InputSource", "Input.InputSource").Replace("OutputDest", "Output.OutputDest")];
                                    if (attrib != null)
                                    {
                                        _log.Debug("Attribute is a Field Attribute");
                                        GalaxyFunctions.UpdateMxReference(_model.WhatIf, group.Key, attrib, Operation.Update, a2item.Value);
                                    }
                                    else
                                    {
                                        _log.Warn("Could not locate attribute on object! Is the IO extension enabled?  Is the attribute name spelled correctly?");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.ToString());
                        }
                    }
                    o.CheckIn(!_model.WhatIf);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            }
        }

        private void SelectCsvFilesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog
            {
                Filter = "csv files|*.csv"
            };
            if (d.ShowDialog() == true)
            {
                foreach (var file in d.FileNames)
                {
                    lstFiles.Items.Add(file);
                }
            }
        }

        private void ClearFilesButton_Click(object sender, RoutedEventArgs e)
        {
            lstFiles.Items.Clear();
        }

        private void StartOperation()
        {
            Dispatcher.Invoke(() =>
            {
                _abortOperation = false;
                spinner.Visibility = Visibility.Visible;
                btnAbort.IsEnabled = true;
                tabMain.IsEnabled = false;
            });
        }

        private void FinishOperation()
        {
            Dispatcher.Invoke(() =>
            {
                _abortOperation = false;
                spinner.Visibility = Visibility.Hidden;
                btnAbort.IsEnabled = false;
                tabMain.IsEnabled = true;
                lblAbort.Visibility = Visibility.Hidden;
            });
        }
    }
}
