using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Popgun.Menus
{
    public class MenuItem
    {
        public enum LinkTypes
        {
            None,
            Screen,
            Menu,
            EnterIP,
            Exit
        }

        public LinkTypes LinkType = LinkTypes.None;
        public String LinkID = String.Empty;
        public String Parameter = null;
        public Image Image;
    }
}
