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
using ICSharpCode.AvalonEdit;

namespace Db_TeXML.Wpf
{
    public partial class HelpWindow : BaseWindow
    {
        #region Properties
        ArrayList commandsList = new ArrayList();
        #endregion

        #region Constructor(s)
        public HelpWindow()
        {
            InitializeComponent();

            TextEditor txbHelpInfo = new TextEditor();
            txbHelpInfo.FontFamily = new FontFamily("Courier New");
            txbHelpInfo.WordWrap = true;
            txbHelpInfo.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            txbHelpInfo.Width = 769.164;
            txbHelpInfo.Height = 451.333;
            txbHelpInfo.Text = Db_TeXML.Wpf.Resources.DbTeXML_Language_Specifications;
            txbHelpInfo.IsReadOnly = true;
            gridTxbHelpInfo.Children.Add(txbHelpInfo);
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
