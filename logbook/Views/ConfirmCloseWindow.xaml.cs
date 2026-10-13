using System.ComponentModel;
using System.Windows;

namespace logbook.Views
{
    public partial class ConfirmCloseWindow : Window
    {
        /// <summary>
        /// true — пользователь выбрал «Сохранить».
        /// false — пользователь выбрал «Выйти без сохранения».
        /// Не имеет смысла, если DialogResult != true (крестик = отмена).
        /// </summary>
        public bool SaveBeforeClose { get; private set; }

        /// <summary>
        /// true, если пользователь подтвердил выход (любой из кнопок).
        /// false, если закрыл крестиком / Alt+F4 / Esc.
        /// </summary>
        public bool ConfirmedExit { get; private set; }

        public ConfirmCloseWindow()
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            // Крестик / Alt+F4 — отмена выхода
            Closing += ConfirmCloseWindow_Closing;
        }

        private void ConfirmCloseWindow_Closing(object sender, CancelEventArgs e)
        {
            // Если окно закрывается не через кнопки (DialogResult ещё не задан)
            // — считаем это отменой.
            if (DialogResult == null)
            {
                ConfirmedExit = false;
                // DialogResult останется null — вызывающий код должен это учесть
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClose = true;
            ConfirmedExit = true;
            DialogResult = true;
            Close();
        }

        private void ExitWithoutSave_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClose = false;
            ConfirmedExit = true;
            DialogResult = true;
            Close();
        }
    }
}