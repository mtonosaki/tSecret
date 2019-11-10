using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace tSecret
{
    /// <summary>
    /// Utility of Xamarin Forms Controls
    /// </summary>
	public class XamarinUtil
    {
        /// <summary>
        /// 指定ページ以外のページ遷移を削除する
        /// </summary>
        /// <param name="Navigation"></param>
        /// <param name="currentPage"></param>
        public static void ClearPageTrace(INavigation Navigation, Page currentPage)
        {
            List<Page> pages = Navigation.NavigationStack.Where(a => a != currentPage).ToList();
            foreach (Page page in pages)
            {
                Navigation.RemovePage(page);
            }
        }
    }
}
