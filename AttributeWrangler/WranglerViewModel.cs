using ArchestrA.GRAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AttributeWrangler
{
    public class WranglerViewModel :INotifyPropertyChanged
    {
        private bool _whatif;
        private Operation _operation;
        private string _replaceValue;
        private string _findValue;
        private string _attributePattern;
        private MxSecurityClassification _security;
        private MxPropertyLockedEnum _locked;
        private Dictionary<int, ArchestrAObject> _objects = new Dictionary<int, ArchestrAObject>();

        public bool WhatIf
        {
            get
            {
                return _whatif;
            }
            set
            {
                _whatif = value;
                OnPropertyChanged();
            }
        }

        public Operation Operation 
        {
            get
            {
                return _operation;
            }
            set
            {
                _operation = value;
                OnPropertyChanged();
            }
        }

        public string FindValue
        {
            get
            {
                return _findValue;
            }
            set
            {
                _findValue = value;
                OnPropertyChanged();
            }
        }

        public string ReplaceValue
        {
            get
            {
                return _replaceValue;
            }
            set
            {
                _replaceValue = value;
                OnPropertyChanged();
            }
        }

        public MxSecurityClassification Security
        {
            get
            {
                return _security;
            }
            set
            {
                _security = value;
                OnPropertyChanged();
            }
        }

        public MxPropertyLockedEnum Locked
        {
            get
            {
                return _locked;
            }
            set
            {
                _locked = value;
                OnPropertyChanged();
            }
        }

        public string AttributePattern
        {
            get
            {
                return _attributePattern;
            }
            set
            {
                _attributePattern = value;
                OnPropertyChanged();
            }
        }


        public List<ArchestrAObject> Objects
        {
            get
            {
                return _objects.Values.ToList();
            }
        }

        public bool AddObject(ArchestrAObject obj)
        {
            if (_objects.ContainsKey(obj.ObjectID))
            {
                return false;
            }
            else
            {
                _objects.Add(obj.ObjectID, obj);
                return true;
            }
        }

        public bool RemoveObject(ArchestrAObject obj)
        {
            if (_objects.ContainsKey(obj.ObjectID))
            {
                _objects.Remove(obj.ObjectID);
                return true;
            }
            else
                return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
