using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Tono.Gui.Uwp;
using tSecretCommon;
using tSecretCommon.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace tSecretUwp
{
    public sealed partial class NoteListPage : Page
    {
        public NoteListPage()
        {
            this.InitializeComponent();

            ConfigUtil.Set("LoginUtc", DateTime.UtcNow.ToString());
            Refresh();
        }

        private bool IsShowAll
        {
            get => ShowDeleted.IsChecked ?? false;
            set => ShowDeleted.IsChecked = value;
        }

        private void Refresh()
        {
            var dat = new ObservableCollection<Note>();
            foreach (Note note in NotePersister.Current
                                    .Where(rec => IsShowAll || rec.IsDeleted == false)
                                    .OrderBy(rec => $"{rec.CaptionRubi1}--{rec.CaptionRubi}")
            )
            {
                dat.Add(note);
            }
            lvMain.ItemsSource = dat;
        }

        /// <summary>
        /// Inplementation of Sync event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Sync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KurukuruIn();
                await syncProc();
                KurukuruOut();
                await new MessageDialog($"クラウドと同期しました。", "tSecret").ShowAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"クラウド同期に失敗しました。\r\n{ex.Message}", "tSecret").ShowAsync();
                log(null);
                KurukuruOut();
            }
        }

        /// <summary>
        /// Data syncronize between Local PC and Cloud storage
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task syncProc()
        {
            log("Cloud sync...");
            await NotePersister.Current.Sync();
            Refresh();
            log(null);
        }

        private void KurukuruIn()
        {
        }

        private void KurukuruOut()
        {
        }

        private void log(string str)
        {
            if (str == null)
            {
                StatusBar.Text = "tSecret (c)2019-2020 Manabu Tonosaki Allrights reserved.";
            }
            else
            {
                StatusBar.Text = str;
            }
        }

        /// <summary>
        /// move to detail page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvMain_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(NoteEntryPage), lvMain.SelectedItem);
        }
    }
}
