using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ArchestrA.GRAccess;

namespace AttributeWrangler
{
    public class ArchestrAObject
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static IGalaxy Galaxy { get; set; }
        public static GRAccessApp GrAccess { get; set; }

        public string Name { get; set; }
        public bool IsTemplate { get; set; }
        public ArchestrAObject Parent { get; set; }
        public List<ArchestrAObject> Children { get; set; } = new List<ArchestrAObject>();
        public int ObjectID { get; set; }
        public int AreaID { get; set; }
        public int ParentObjectID { get; set; }

        public IAttributes Attributes
        {
            get
            {
                if (GRAccessObject == null)
                    return null;
                if (IsTemplate)
                {
                    var template = (ITemplate)GRAccessObject;
                    if (template.CheckoutStatus == ECheckoutStatus.checkedOutToMe)
                        return template.ConfigurableAttributes;
                    return null;
                }
                else
                {
                    var instance = (IInstance)GRAccessObject;
                    if (instance.CheckoutStatus == ECheckoutStatus.checkedOutToMe)
                        return instance.ConfigurableAttributes;
                    return null;
                }
            }
        }

        private object GRAccessObject { get; set; }

        public bool AddPrimitive(IAttribute attribute, Primitive prim)
        {
            try
            {
                ICommandResult result = null;
                if (IsTemplate)
                {
                    var template = (ITemplate)GRAccessObject;
                    switch (prim)
                    {
                        case Primitive.scalingextension:
                            _log.Debug($"Adding scaling extension primitice to object {Name} attributes {attribute.Name}");
                            template.AddExtensionPrimitive("scalingextension", attribute.Name);
                            result = Galaxy.CommandResult;
                            break;
                    }

                }
                else
                {
                    var instance = (IInstance)GRAccessObject;
                    switch (prim)
                    {
                        case Primitive.scalingextension:
                            _log.Debug($"Adding scaling extension primitice to object {Name} attributes {attribute.Name}");
                            instance.AddExtensionPrimitive("scalingextension", attribute.Name);
                            result = Galaxy.CommandResult;
                            break;
                    }
                }
                return result?.Successful ?? false;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }

        public bool CheckIn(bool save)
        {
            try
            {
                if (IsTemplate)
                {
                    var template = (ITemplate)GRAccessObject;
                    if (save)
                    {
                        _log.Debug(string.Format("Saving {0}", Name));
                        template.Save();
                    }
                    _log.Debug(string.Format("Checking in {0}", Name));
                    template.CheckIn();
                    return true;
                }
                else
                {
                    var instance = (IInstance)GRAccessObject;
                    if (save)
                    {
                        _log.Debug(string.Format("Saving {0}", Name));
                        instance.Save();
                    }
                        _log.Debug(string.Format("Checking in {0}", Name));
                    instance.CheckIn();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }

        public bool Checkout()
        {
            try
            {
                string[] tagname = new string[] { Name };
                if (IsTemplate)
                {
                    _log.Debug(string.Format("Querying galaxy for {0}", Name));
                    IgObjects queryResult = Galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsTemplate, ref tagname);
                    ICommandResult cmd = Galaxy.CommandResult;
                    if (!cmd.Successful)
                    {
                        _log.Warn("QueryObjectsByName Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                        return false;
                    }
                    ITemplate template = (ITemplate)queryResult[1];//can throw errors here
                    if (template.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                    {
                        _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", Name, template.checkedOutBy));
                        return false;
                    }
                    _log.Debug(string.Format("Checking out {0}", Name));
                    template.CheckOut();
                    GRAccessObject = template;
                    _log.Debug(string.Format("Checked out {0}", Name));
                    return true;
                }
                else
                {
                    _log.Debug(string.Format("Querying galaxy for {0}", Name));
                    IgObjects queryResult = Galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, ref tagname);
                    ICommandResult cmd = Galaxy.CommandResult;
                    if (!cmd.Successful)
                    {
                        _log.Warn("QueryObjectsByName Failed:" + cmd.Text + " : " + cmd.CustomMessage);
                        return false;
                    }
                    IInstance instance = (IInstance)queryResult[1];//can throw errors here
                    if (instance.CheckoutStatus != ECheckoutStatus.notCheckedOut)
                    {
                        _log.Warn(string.Format("Object [{0}] is already checked out by [{1}]", Name, instance.checkedOutBy));
                        return false;
                    }
                    _log.Debug(string.Format("Checking out {0}", Name));
                    instance.CheckOut();
                    GRAccessObject = instance;
                    _log.Debug(string.Format("Checked out {0}", Name));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }

        public static string GetRootAttribute(string attribute)
        {
            return attribute.Substring(0, attribute.LastIndexOf('.'));
        }
    }

    public enum Operation
    {
        Find,
        FindReplace,
        FindUpdate,
        Update,
        SetLocked,
        SetSecurity
    }

    public enum Primitive
    {
        scalingextension
    }

    public class ArchestrACsvItem
    {
        public string Value { get; set; }
        public string Attribute { get; set; }
        public string Type { get; set; }
        public string Object { get; set; }
        public bool IsInput { get; set; }
    }

    public enum PickerMode
    {
        List,
        Template,
        Area
    }

    public class TreeViewModel
    {
        public ReadOnlyCollection<ObjectViewModel> Children;

        public TreeViewModel(List<ArchestrAObject> objects)
        {
            Children = new ReadOnlyCollection<ObjectViewModel>((from i in objects select new ObjectViewModel(i)).ToList());
        }
    }

    public class ObjectViewModel : INotifyPropertyChanged
    {
        private ArchestrAObject _object;
        private ObjectViewModel _parent;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyCollection<ObjectViewModel> Children { get; set; }

        public ObjectViewModel(ArchestrAObject obj) : this(obj, null) { }

        public ObjectViewModel(ArchestrAObject obj, ObjectViewModel parent)
        {
            _object = obj;
            _parent = parent;
            Children = new ReadOnlyCollection<ObjectViewModel>((from child in _object.Children select new ObjectViewModel(child, this)).ToList());
        }

        public ArchestrAObject aaObject
        {
            get { return _object; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get { return _object.Name; }
        }

        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
