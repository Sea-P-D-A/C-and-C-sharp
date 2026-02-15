using System;
using System.Windows.Forms;
using System.Threading;

namespace OS_Laba2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Инициализация компонентов при загрузке формы
        }

        // Обработчик события нажатия на кнопку поиска
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string dir1 = txtDirectory1.Text;
            string dir2 = txtDirectory2.Text;

            // Проверяем, что оба поля заполнены (не пустые и не null)
            if (string.IsNullOrEmpty(dir1) || string.IsNullOrEmpty(dir2))
            {
                MessageBox.Show("Укажите оба каталога");
                return;
            }

            // Создаем новый поток для выполнения поиска, чтобы не блокировать интерфейс
            Thread searchThread = new Thread(() =>
            {
                // Блок try-catch для обработки возможных исключений при поиске
                try
                {
                    // Создаем объект-компаратор для сравнения двух каталогов
                    DirectoryComparator comparator = new DirectoryComparator(dir1, dir2);
                    // Запускаем метод поиска общих подкаталогов (блокирующая операция)
                    comparator.FindCommonSubdirectories();

                    // Используем Invoke для безопасного обновления UI из другого потока
                    // Invoke гарантирует, что код выполнится в главном UI-потоке
                    this.Invoke(new Action(() =>
                    {
                        // Очищаем список результатов перед добавлением новых данных
                        lstResults.Items.Clear();

                        // Перебираем все найденные общие подкаталоги
                        foreach (string commonDir in comparator.CommonSubdirectories)
                        {
                            // Добавляем каждый общий подкаталог в список результатов
                            lstResults.Items.Add(commonDir);
                        }

                        // Проверяем, найдены ли общие подкаталоги
                        if (comparator.CommonSubdirectories.Count == 0)
                        {
                            MessageBox.Show("Общих подкаталогов не найдено");
                        }
                        else
                        {
                            MessageBox.Show($"Найдено {comparator.CommonSubdirectories.Count} общих подкаталогов");
                        }
                    }));
                }
                // Обработка любых исключений, которые могут возникнуть при поиске
                catch (Exception ex)
                {
                    // Также используем Invoke для безопасного показа ошибки в UI-потоке
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }));
                }
            });

            // Запускаем созданный поток (начинаем выполнение поиска)
            searchThread.Start();
        }
    }
}