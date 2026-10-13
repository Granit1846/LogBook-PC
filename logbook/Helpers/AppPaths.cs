using System;
using System.IO;

namespace logbook.Helpers
{
    /// <summary>
    /// Пути к папке данных и резервным копиям.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>
        /// Папка данных: C:\Users\<user>\Documents\Logbook
        /// </summary>
        public static string DataFolder
        {
            get
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(docs, "Logbook");
            }
        }

        /// <summary>
        /// Папка для резервных копий: C:\Users\<user>\Documents\Logbook\backup
        /// </summary>
        public static string BackupFolder
        {
            get
            {
                return Path.Combine(DataFolder, "backup");
            }
        }

        /// <summary>
        /// Основной JSON.
        /// </summary>
        public static string DataFile
        {
            get
            {
                return Path.Combine(DataFolder, "work_data.json");
            }
        }

        /// <summary>
        /// Гарантирует, что папки существуют.
        /// </summary>
        public static void EnsureFoldersExist()
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            if (!Directory.Exists(BackupFolder))
                Directory.CreateDirectory(BackupFolder);
        }
    }
}