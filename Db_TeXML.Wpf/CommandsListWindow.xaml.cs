using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.Collections;
using Db_TeXML.Wpf.parser;
using System.Windows.Media.Animation;

namespace Db_TeXML.Wpf
{
    public partial class CommandsListWindow : BaseWindow
    {
        #region Properties
        ArrayList commandsList = new ArrayList();
        #endregion

        #region Constructor(s)
        public CommandsListWindow()
        {
            InitializeComponent();
            
            foreach (var key in ParseEngine.TokenTypeToReflectionCallbackHash.Keys)
            {
                Hashtable subhash = (Hashtable)ParseEngine.TokenTypeToReflectionCallbackHash[key];

                foreach (string subkey in subhash.Keys)
                {
                    commandsList.Add(subkey);
                }
            }
            commandsList.Sort();

            foreach (string item in commandsList)
            {
                this.listCommands.Items.Add(item);
            }

            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width;
        }
        #endregion

        #region BaseWindow Drag Any
        private void BaseWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        #endregion

        #region BaseWindow Loaded
        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            double listItemHeight = 18.5;
            this.listCommands.Height = commandsList.Count * listItemHeight;
            this.Height = (commandsList.Count * listItemHeight) + this.topGrid.ActualHeight;

            //System.Windows.SystemParameters.PrimaryScreenHeight;
        }
        #endregion

        #region ButtonX Mouse In Out Click Events
        private void xButtonBlack_MouseEnter(object sender, MouseEventArgs e)
        {
            Storyboard sb = this.FindResource("xButtonIn") as Storyboard;
            sb.Begin();
        }

        private void xButtonBlack_MouseLeave(object sender, MouseEventArgs e)
        {
            Storyboard sb = this.FindResource("xButtonOut") as Storyboard;
            sb.Begin();
        }

        private void xButtonBlack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
        }
        #endregion
    }
}
