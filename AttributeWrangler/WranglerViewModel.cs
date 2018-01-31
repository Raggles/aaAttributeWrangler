using ArchestrA.GRAccess;
using MicroMvvm;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AttributeWrangler
{
    public class SearchParametersViewModel: INotifyPropertyChanged
    {
        private Operation _operation;
        private string _replaceValue = "";
        private string _findValue = "";
        private string _attributePattern = "";
        private MxSecurityClassification _security;
        private MxPropertyLockedEnum _locked;

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

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }



    public class WranglerViewModel :INotifyPropertyChanged
    {
        private bool _whatif;
        private Dictionary<int, ArchestrAObject> _objects = new Dictionary<int, ArchestrAObject>();
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private SearchParametersViewModel _selectedItem;

        public SearchParametersViewModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public WranglerViewModel()
        {
            SearchParameters.Add(new SearchParametersViewModel());
            SelectedItem = SearchParameters[0];
        }

        [JsonIgnore]
        public ICommand AddSearchParameters { get { return new RelayCommand<SearchParametersViewModel>(AddSearchParametersExecute, CanAddSearchParametersExecute); } }

        [JsonIgnore]
        public ICommand DeleteSearchParameters { get { return new RelayCommand<SearchParametersViewModel>(DeleteSearchParametersExecute, CanDeleteSearchParametersExecute); } }

        [JsonIgnore]
        public ICommand LoadSearchParameters { get { return new RelayCommand<SearchParametersViewModel>(LoadSearchParametersExecute); } }

        private void LoadSearchParametersExecute(SearchParametersViewModel obj)
        {
            OpenFileDialog d = new OpenFileDialog();
            if (d.ShowDialog() == true)
            {
                try
                {
                    SearchParametersViewModel v = JsonConvert.DeserializeObject(File.ReadAllText(d.FileName), typeof(SearchParametersViewModel)) as SearchParametersViewModel;
                    
                    if (v != null)
                    {
                        SearchParameters[SearchParameters.IndexOf(obj)] = v;
                        SelectedItem = v;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            }
            }

        [JsonIgnore]
        public ICommand SaveSearchParameters { get { return new RelayCommand<SearchParametersViewModel>(SaveSearchParametersExecute); } }

        private void SaveSearchParametersExecute(SearchParametersViewModel obj)
        {
            SaveFileDialog d = new SaveFileDialog();
            if (d.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(d.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
            }
        }

        private bool CanDeleteSearchParametersExecute(SearchParametersViewModel obj)
        {
            return SearchParameters.Count > 1;
        }

        private void DeleteSearchParametersExecute(SearchParametersViewModel obj)
        {
            int index = SearchParameters.IndexOf(obj);
            SearchParameters.Remove(obj);
            if (index >= SearchParameters.Count)
                index--;
            SelectedItem = SearchParameters[index];
        }

        private void AddSearchParametersExecute(SearchParametersViewModel obj)
        {
            SearchParameters.Add(new SearchParametersViewModel());
            SelectedItem = SearchParameters[SearchParameters.Count - 1];
        }

        private bool CanAddSearchParametersExecute(SearchParametersViewModel obj)
        {
            return SearchParameters.Count < 10;
        }

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

        public ObservableCollection<SearchParametersViewModel> SearchParameters { get; set; } = new ObservableCollection<SearchParametersViewModel>();
        
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

        internal void ClearObjects()
        {
            _objects.Clear();
        }
    }
}
