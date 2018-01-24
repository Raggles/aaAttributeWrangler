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
using System.Windows.Shapes;
using ArchestrA.GRAccess;

namespace AttributeWrangler
{
    /// <summary>
    /// Interaction logic for LogInWindow.xaml
    /// </summary>
    public partial class LogInWindow : Window
    {
        private IGalaxies _galaxies;

        public LogInWindow()
        {
            InitializeComponent();
        }

        private void txtGalaxyNode_LostFocus(object sender, RoutedEventArgs e)
        {
            LoadGalaxies();
        }

        private void txtGalaxyNode_TextChanged(object sender, TextChangedEventArgs e)
        {
            //LoadGalaxies();
        }

        private void LoadGalaxies()
        {
        }
        
        private void DoLogin()
        {
            try
            {
                GRAccessApp grAccess;
                string nodeName = txtGalaxyNode.Text;

                try
                {
                    grAccess = new GRAccessAppClass();
                }
                catch
                {
                    MessageBox.Show("Unable to initialize GRAccess.  Do you have it installed?");
                    this.Close();
                    return;
                }
                _galaxies = grAccess.QueryGalaxies(nodeName);

                if (_galaxies == null || grAccess.CommandResult.Successful == false)
                {
                    MessageBox.Show("Unable to query galaxies on node " + nodeName);
                }
                else
                {
                    string username = txtUsername.Text;
                    string password = txtPassword.Text;
                   
                    IGalaxy galaxy = _galaxies[txtGalaxy.Text];
                    galaxy.Login(username, password);
                    ICommandResult cmd = galaxy.CommandResult;
                    if (!cmd.Successful)
                    {
                        MessageBox.Show("Login to galaxy Failed :" + cmd.Text + " : " + cmd.CustomMessage);
                        return;
                    }
                    //TODO: check sql permissions here too
                    new MainWindow(galaxy).Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }
    }
}
