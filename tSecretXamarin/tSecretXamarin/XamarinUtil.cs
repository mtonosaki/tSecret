﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace tSecretXamarin
{
    /// <summary>
    /// Utility of Xamarin Forms Controls
    /// </summary>
	public class XamarinUtil
    {
        /// <summary>
        /// Delete other pages
        /// </summary>
        /// <param name="Navigation"></param>
        /// <param name="currentPage"></param>
        public static void ClearPageTrace(INavigation Navigation, Page currentPage)
        {
            var pages = Navigation.NavigationStack.Where(a => a != currentPage).ToList();
            foreach (Page page in pages)
            {
                Navigation.RemovePage(page);
            }
        }
    }
}
