using logbook.Helpers;
using logbook.Models;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== УВЕДОМЛЕНИЯ ===========================
        // ============================================================

        private void OpenNotifications()
        {
            var window = new logbook.Views.NotificationsWindow(this)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        /// <summary>
        /// Обновляет уровень уведомлений (0 / 1 / 2).
        /// </summary>
        public void UpdateNotifications()
        {
            var resourceAlerts = BuildResourceAlerts();
            var oilAlerts = BuildOilAlerts();

            int total = resourceAlerts.Count + oilAlerts.Count;
            if (total == 0)
            {
                NotificationLevel = 0;
                return;
            }

            bool hasCritical = false;

            foreach (var a in resourceAlerts)
            {
                if (IsCriticalAlert(a)) { hasCritical = true; break; }
            }

            if (!hasCritical)
            {
                foreach (var a in oilAlerts)
                {
                    if (IsCriticalAlert(a)) { hasCritical = true; break; }
                }
            }

            NotificationLevel = hasCritical ? 2 : 1;
        }

        /// <summary>
        /// Проверяет, является ли уведомление критическим.
        /// </summary>
        private bool IsCriticalAlert(AlertMessage a)
        {
            if (a == null) return false;

            if (!string.IsNullOrWhiteSpace(a.Time) && a.Time.StartsWith("-"))
                return true;

            if (!string.IsNullOrWhiteSpace(a.Prefix))
            {
                if (a.Prefix.Contains("исчерпан") || a.Prefix.Contains("exhausted"))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(a.Suffix))
            {
                if (a.Suffix.Contains("срочно") || a.Suffix.Contains("urgently"))
                    return true;
            }

            return false;
        }
    }
}