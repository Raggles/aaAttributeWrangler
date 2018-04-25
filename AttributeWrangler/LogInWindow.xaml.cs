using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spinner.Visibility = Visibility.Visible;
                grid.IsEnabled = false;

                string nodeName = txtGalaxyNode.Text;
                string galaxyName = txtGalaxy.Text;
                string user = txtUsername.Text;
                string password = txtPassword.Text;

                Thread t = new Thread(() =>
                {
                    Login(nodeName, galaxyName, user, password);
                    this.Dispatcher.Invoke(() =>
                    {
                        grid.IsEnabled = true;
                        spinner.Visibility = Visibility.Hidden;
                    });

                });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Login(string nodeName, string galaxyName, string user, string password)
        {
            GRAccessApp grAccess;
            try
            {
                grAccess = new GRAccessAppClass();
            }
            catch (Exception ex)
            {
                DispatchMessagebox("Unable to initialize GRAccess.  This can have several causes - GRAcces.dll must a registered type library.  You must have an available license - if you only have one license then you must not have the IDE open while using this tool.  On some systems, you may have to run this tool as an administrator. \n Exception Details:\n\n" + ex.ToString());
                return;
            }

            _galaxies = grAccess.QueryGalaxies(nodeName);
            if (_galaxies == null || grAccess.CommandResult.Successful == false)
            {
                DispatchMessagebox("Unable to query galaxies on node " + nodeName);
                return;
            }
            IGalaxy galaxy = _galaxies[galaxyName];
            if (galaxy == null)
            {
                DispatchMessagebox(string.Format("Couldn't find galaxy {0} on node {1}", galaxyName, nodeName));
                return;
            }
            galaxy.Login(user, password);
            ICommandResult cmd = galaxy.CommandResult;
            if (!cmd.Successful)
            {
                DispatchMessagebox("Login to galaxy Failed :" + cmd.Text + " : " + cmd.CustomMessage);
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    new MainWindow(grAccess, galaxy).Show();
                    this.Close();
                    return;
                });
            }
        }

        private void DispatchMessagebox(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message);
            });
        }
    }
}
