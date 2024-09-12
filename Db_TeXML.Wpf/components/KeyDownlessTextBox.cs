using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace Db_TeXML.Wpf
{
    public class KeyDownlessTextBox : AutoSelectTextBox
    {
        public KeyDownlessTextBox() : base() { }

        private bool lastKeyPressedWasDownKey = false;
        public bool LastKeyPressedWasDownKey
        {
            get { return this.lastKeyPressedWasDownKey; }
            set { this.lastKeyPressedWasDownKey = value; }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                lastKeyPressedWasDownKey = true;
            }
            else
            {
                lastKeyPressedWasDownKey = false;
            }
            base.OnPreviewKeyDown(e);
        }
    }
}
