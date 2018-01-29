using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for AdvancedSearch.xaml
    /// </summary>
    public partial class AdvancedSearch : Window
    {
        private string _galaxy;
        private string _node;

        public List<ArchestrAObject> Result { get; set; }

        public AdvancedSearch()
        {
            InitializeComponent();
        }

        public AdvancedSearch(string node, string galaxy)
        {
            _node = node;
            _galaxy = galaxy;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lstObjects.Items.Clear();

                bool checkParentArea = chkParentAreaOnly.IsChecked == true && txtArea.Text.Length > 0;
                bool checkTagName = txtObject.Text.Length > 0;
                bool checkParentTemplate = chkParentTemplateOnly.IsChecked == true && txtDerivedFrom.Text.Length > 0;
                bool checkAnyTemplate = chkParentTemplateOnly.IsChecked == false && txtDerivedFrom.Text.Length > 0;
                bool checkAnyArea = chkParentAreaOnly.IsChecked == false && txtArea.Text.Length > 0;
                bool instancesOnly = chkInstances.IsChecked == true;

                List<ArchestrAObject> areas = DatabasteFunctions.GetAreas(_node, _galaxy);
                List<ArchestrAObject> allObjects = DatabasteFunctions.GetAllObjects(_node, _galaxy);
                List<ArchestrAObject> filteredObjects = new List<ArchestrAObject>();
                List<ArchestrAObject> filteredAreas = new List<ArchestrAObject>();
                List<ArchestrAObject> filteredTemplates = new List<ArchestrAObject>();
                List<ArchestrAObject> results = new List<ArchestrAObject>();
                Dictionary<int, ArchestrAObject> allObjectsInAreas = new Dictionary<int, ArchestrAObject>();
                Dictionary<int, ArchestrAObject> allObjectsOfTemplates = new Dictionary<int, ArchestrAObject>();

                if (checkParentArea || checkAnyArea)
                {
                    Regex r = new Regex(txtArea.Text);

                    foreach (var item in areas)
                    {
                        if (r.IsMatch(item.Name))
                        {
                            filteredAreas.Add(item);
                        }
                    }
                }

                if (checkTagName)
                {
                    Regex r = new Regex(txtObject.Text);

                    foreach (var item in allObjects)
                    {
                        if (r.IsMatch(item.Name))
                        {
                            filteredObjects.Add(item);
                        }
                    }
                }
                else
                {
                    filteredObjects = allObjects;
                }

                if (checkParentTemplate || checkAnyTemplate)
                {
                    Regex r = new Regex(txtDerivedFrom.Text);

                    foreach (var item in allObjects)
                    {
                        if (item.IsTemplate)
                        {
                            if (r.IsMatch(item.Name))
                            {
                                filteredTemplates.Add(item);
                            }
                        }
                    }
                }

                if (checkAnyArea)
                {
                    foreach (var item in filteredAreas)
                    {
                        var res = DatabasteFunctions.GetAllObjectsInArea(_node, _galaxy, item.ObjectID);
                        foreach (var i in res)
                        {
                            if (!allObjectsInAreas.ContainsKey(i.ObjectID))
                            {
                                allObjectsInAreas.Add(i.ObjectID, i);
                            }
                        }

                    }
                }

                if (checkAnyTemplate)
                {
                    foreach (var item in filteredTemplates)
                    {
                        var res = DatabasteFunctions.GetAllObjectsDerivedFrom(_node, _galaxy, item.ObjectID);
                        foreach (var i in res)
                        {
                            if (!allObjectsOfTemplates.ContainsKey(i.ObjectID))
                            {
                                allObjectsOfTemplates.Add(i.ObjectID, i);
                            }
                        }

                    }
                }


                List<ArchestrAObject> workingResults = new List<ArchestrAObject>();
                if (checkParentArea)
                {
                    workingResults.AddRange(from i in filteredObjects where (from j in filteredAreas where j.ObjectID == i.AreaID select j).FirstOrDefault() != null select i);
                }
                else if (checkAnyArea)
                {
                    workingResults.AddRange(from i in filteredObjects where (from j in allObjectsInAreas where j.Value.ObjectID == i.ObjectID select j.Value).FirstOrDefault() != null select i);
                }
                else
                    workingResults = filteredObjects;

                List<ArchestrAObject> workingResults2 = new List<ArchestrAObject>();

                if (checkParentTemplate)
                {
                    workingResults2.AddRange(from i in workingResults where (from j in filteredTemplates where j.ObjectID == i.ParentObjectID select j).FirstOrDefault() != null select i);
                }
                else if (checkAnyTemplate)
                {
                    workingResults2.AddRange(from i in workingResults where (from j in allObjectsOfTemplates where j.Value.ObjectID == i.ObjectID select j.Value).FirstOrDefault() != null select i);
                }
                else
                    workingResults2 = workingResults;

                if (instancesOnly)
                {
                    results.AddRange(from i in workingResults2 where i.IsTemplate == false select i);
                }
                else
                {
                    results = workingResults2;
                }

                foreach (var item in results)
                {
                    lstObjects.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnAddSelected_Click(object sender, RoutedEventArgs e)
        {
            Result = (from ArchestrAObject i in lstObjects.SelectedItems select i).ToList();
            DialogResult = true;
            this.Close();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            lstObjects.SelectAll();
        }
    }
}
