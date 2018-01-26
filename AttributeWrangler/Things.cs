using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AttributeWrangler
{
    public class ArchestrAObject
    {
        public string Name { get; set; }
        public bool IsTemplate { get; set; }
        public ArchestrAObject Parent { get; set; }
        public List<ArchestrAObject> Children { get; set; } = new List<ArchestrAObject>();
        public int ObjectID { get; set; }
        public int AreaID { get; set; }
        public int ParentObjectID { get; set; }
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
