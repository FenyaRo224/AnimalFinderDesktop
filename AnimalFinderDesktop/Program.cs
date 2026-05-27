using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // СОЗДАЁМ ПАПКУ ДЛЯ ФОТО ПРИ ЗАПУСКЕ ПРОГРАММЫ
            string photosDir = Path.Combine(Application.StartupPath, "Photos");
            if (!Directory.Exists(photosDir))
            {
                Directory.CreateDirectory(photosDir);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Инициализация Supabase
            Task.Run(async () => await SupabaseService.GetClient()).GetAwaiter().GetResult();

            // Показываем форму входа
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;
            }

            Application.Run(new MainForm());
        }
    }
}