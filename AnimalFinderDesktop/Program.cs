using System;
using System.Windows.Forms;
using AnimalFinderDesktop.Forms;
using AnimalFinderDesktop.Services;
using System.Threading.Tasks;

namespace AnimalFinderDesktop
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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