﻿using Lunalipse.Common.Interfaces.ILpsUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunalipse.Pages.ConfigPage.Structures
{
    public class PlaylistStruc : LpsDetailedListItem
    {
        public string UUID { get; set; }
        public PlaylistStruc()
        {
            base.DetailedIcon = "PlayList";
        }
    }
}
