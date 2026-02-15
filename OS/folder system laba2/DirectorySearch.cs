using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace OS_Laba2
{
    public class DirectorySearchWorker
    {
        // Приватные поля класса для хранения состояния
        private string _directoryPath;          // Путь к каталогу для поиска
        private ManualResetEvent _completedEvent; // Событие синхронизации для уведомления о завершении
        private List<string> _subdirectoryNames;  // Список найденных имен подкаталогов
        private Exception _error;                 // Исключение, если произошла ошибка

        // Конструктор класса - инициализирует рабочий объект
        public DirectorySearchWorker(string directoryPath, ManualResetEvent completedEvent)
        {
            _directoryPath = directoryPath;
            _completedEvent = completedEvent;
            _subdirectoryNames = new List<string>();
        }

        // Свойство только для чтения - предоставляет доступ к списку имен подкаталогов
        public List<string> SubdirectoryNames => _subdirectoryNames;

        // Свойство только для чтения - предоставляет доступ к информации об ошибке
        public Exception Error => _error;

        // Основной метод, который выполняется в рабочем потоке
        public void StartSearch()
        {
            try
            {
                // Рекурсивно получаем все подкаталоги в указанном пути
                // GetAllDirectories возвращает IEnumerable<string> с полными путями
                foreach (string subDir in GetAllDirectories(_directoryPath))
                {
                    // Извлекаем только имя каталога из полного пути
                    // Например: из "C:\Folder\SubFolder" получаем "SubFolder"
                    string dirName = Path.GetFileName(subDir);

                    // Проверяем, нет ли уже этого имени в списке (избегаем дубликатов)
                    if (!_subdirectoryNames.Contains(dirName))
                    {
                        _subdirectoryNames.Add(dirName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Если произошло исключение, сохраняем его для последующей обработки
                _error = ex;
            }
            finally
            {
                // Этот блок выполнится в любом случае - был успех или ошибка
                // Устанавливаем событие в сигнальное состояние - уведомляем о завершении работы
                _completedEvent.Set();
            }
        }

        #region FindFirstFile/FindNextFile Implementation
        // Регион содержит всю логику работы с WinAPI функциями поиска файлов

        // Константа, определяющая максимальную длину пути в Windows
        private const int MAX_PATH = 260;

        // Структура WIN32_FIND_DATA для хранения информации о найденном файле/каталоге
        [Serializable]  // Атрибут для возможности сериализации структуры
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]  // Указываем последовательное расположение в памяти
        [BestFitMapping(false)]  // Отключаем автоматическое преобразование символов
        private struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;    // Атрибуты файла/каталога
            public FILETIME ftCreationTime;           // Время создания
            public FILETIME ftLastAccessTime;         // Время последнего доступа
            public FILETIME ftLastWriteTime;          // Время последней записи
            public int nFileSizeHigh;                 // Старшая часть размера файла
            public int nFileSizeLow;                  // Младшая часть размера файла
            public int dwReserved0;                   // Зарезервировано
            public int dwReserved1;                   // Зарезервировано
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]  // Маршалинг строки как встроенного массива
            public string cFileName;                  // Имя файла/каталога
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]        // Маршалинг альтернативного имени (8.3)
            public string cAlternate;                 // Альтернативное имя в формате 8.3
        }

        // Импорт функции FindFirstFile из kernel32.dll
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindFirstFile(
            string lpFileName,           // Шаблон для поиска (может содержать * и ?)
            out WIN32_FIND_DATA lpFindFileData  // Структура для информации о первом найденном элементе
        );

        // Импорт функции FindNextFile из kernel32.dll
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FindNextFile(
            IntPtr hFindFile,            // Дескриптор поиска, полученный от FindFirstFile
            out WIN32_FIND_DATA lpFindFileData  // Структура для информации о следующем найденном элементе
        );

        // Импорт функции FindClose из kernel32.dll для освобождения ресурсов
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(
            IntPtr hFindFile  // Дескриптор поиска для закрытия
        );

        // Константа для невалидного дескриптора
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // Вспомогательный метод для формирования пути поиска
        private static string MakePath(string path)
        {
            // Добавляем "*" к пути для поиска всех элементов в каталоге
            // Например: "C:\Folder" -> "C:\Folder\*"
            return Path.Combine(path, "*");
        }

        // Метод для получения только каталогов в указанном пути
        private static IEnumerable<string> GetDirectories(string path)
        {
            // Вызываем внутренний метод с флагом isGetDirs = true
            return GetInternal(path, true);
        }

        // Внутренний метод для перебора элементов в каталоге
        private static IEnumerable<string> GetInternal(string path, bool isGetDirs)
        {
            // Структура для хранения информации о найденном элементе
            WIN32_FIND_DATA findData;

            // Вызываем FindFirstFile для начала поиска и получения дескриптора
            IntPtr findHandle = FindFirstFile(MakePath(path), out findData);

            // Проверяем, валиден ли дескриптор
            if (findHandle == INVALID_HANDLE_VALUE)
                // Если невалиден, создаем исключение с кодом последней ошибки Win32
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                // Цикл do-while для перебора всех найденных элементов
                do
                {
                    // Проверяем, является ли текущий элемент каталогом или файлом
                    // в зависимости от флага isGetDirs
                    if (isGetDirs ? (findData.dwFileAttributes & FileAttributes.Directory) != 0
                                  : (findData.dwFileAttributes & FileAttributes.Directory) == 0)
                    {
                        // Если элемент соответствует критериям, возвращаем его имя
                        // yield return делает метод итератором
                        yield return findData.cFileName;
                    }
                }
                // Продолжаем цикл, пока FindNextFile возвращает true (есть еще элементы)
                while (FindNextFile(findHandle, out findData));
            }
            finally
            {
                // Этот блок выполнится в любом случае - гарантированно закрываем дескриптор
                FindClose(findHandle);
            }
        }

        // Рекурсивный метод для получения всех подкаталогов (включая вложенные)
        public static IEnumerable<string> GetAllDirectories(string path)
        {
            // Перебираем все подкаталоги первого уровня в указанном пути
            foreach (string subDir in GetDirectories(path))
            {
                // Пропускаем специальные каталоги "." (текущий) и ".." (родительский)
                if (subDir == ".." || subDir == ".")
                    continue;

                // Формируем полный путь к подкаталогу
                string relativePath = Path.Combine(path, subDir);

                // Возвращаем путь текущего подкаталога
                yield return relativePath;

                // Рекурсивно вызываем себя для обхода вложенных подкаталогов
                // и возвращаем каждый найденный путь
                foreach (string subDir2 in GetAllDirectories(relativePath))
                    yield return subDir2;
            }
        }

        #endregion
    }
    public class DirectoryComparator
    {
        // Приватные поля для хранения путей к каталогам
        private string _firstDirectory;
        private string _secondDirectory;
        // Список для хранения общих имен подкаталогов
        private List<string> _commonSubdirectories;

        public DirectoryComparator(string firstDirectory, string secondDirectory)
        {
            _firstDirectory = firstDirectory;
            _secondDirectory = secondDirectory;
            _commonSubdirectories = new List<string>();
        }

        // Публичное свойство только для чтения - предоставляет доступ к списку общих подкаталогов
        public List<string> CommonSubdirectories => _commonSubdirectories;

        // Основной метод поиска общих подкаталогов
        public void FindCommonSubdirectories()
        {
            // Создаем массив из 2 элементов для событий синхронизации
            ManualResetEvent[] waitHandles = new ManualResetEvent[2];
            // Создаем первое событие синхронизации в несигнальном состоянии (false)
            waitHandles[0] = new ManualResetEvent(false);
            // Создаем второе событие синхронизации в несигнальном состоянии (false)
            waitHandles[1] = new ManualResetEvent(false);

            // Создаем рабочие объекты для поиска каталоге
            // Передаем им пути и событие синхронизации
            DirectorySearchWorker worker1 = new DirectorySearchWorker(_firstDirectory, waitHandles[0]);
            DirectorySearchWorker worker2 = new DirectorySearchWorker(_secondDirectory, waitHandles[1]);

            // Создаем потоки, которе будут выполнять метод StartSearch
            Thread thread1 = new Thread(new ThreadStart(worker1.StartSearch));
            Thread thread2 = new Thread(new ThreadStart(worker2.StartSearch));

            // Запускаем потоки
            thread1.Start();
            thread2.Start();

            // Текущий поток (searchThread) блокируется здесь и ждет,
            // пока оба рабочих потока не завершат поиск
            // WaitHandle.WaitAll ожидает сигнала от всех событий в массиве
            WaitHandle.WaitAll(waitHandles);

            // Проверяем, возникла ли ошибка в рабочем потоке
            // Если да, пробрасываем исключение дальше
            if (worker1.Error != null)
                throw worker1.Error;
            if (worker2.Error != null)
                throw worker2.Error;

            // Перебираем все имена подкаталогов из первого каталога
            foreach (string dirName in worker1.SubdirectoryNames)
            {
                // Проверяем, содержится ли текущее имя во втором списке подкаталогов
                if (worker2.SubdirectoryNames.Contains(dirName))
                {
                    // Если имя найдено в обоих каталогах, добавляем его в список общих
                    _commonSubdirectories.Add(dirName);
                }
            }

            // Освобождаем системные ресурсы, занятые событиями синхронизации
            waitHandles[0].Close();
            waitHandles[1].Close();
        }
    }
}