using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Laba1_2._2._7_OS
{
    public class ThreadSafeBuffer
    {

        private Queue<string> buffer;
        private Semaphore emptySlots;
        private Semaphore fullSlots;
        private Mutex mutex;
        private int maxSize;

        // Semaphore(initialCount, maximumCount)
        // - initialCount: начальное значение счетчика
        // - maximumCount: максимальное значение счетчика

        public ThreadSafeBuffer(int size)
        {
            buffer = new Queue<string>();
            emptySlots = new Semaphore(size, size); // Изначально все места свободны
            fullSlots = new Semaphore(0, size);    // Изначально нет заполненных мест
            mutex = new Mutex();
            maxSize = size;
        }

        public bool tryPut(string data, int timeout = 0)
        {

            if (!emptySlots.WaitOne(timeout))
                return false;
                   // Не удалось получить свободное место (буфер полон)

            mutex.WaitOne();    // Захватываем мьютекс


            try
            {
                buffer.Enqueue(data);
                return true;    // Успешно положили данные
            }
            finally
            {

                mutex.ReleaseMutex();   // Освобождаем поток
                fullSlots.Release();    // Увеличиваем счётчик семафора
            }
        }

        public string take()
        {

            fullSlots.WaitOne();


            mutex.WaitOne();

            try
            {
                return buffer.Dequeue();    // Шаг 3: Забираем данные из буфера
            }
            finally
            {
                mutex.ReleaseMutex();       // Освобождаем поток
                emptySlots.Release();        // Увеличиваем счётчик семафора
            }

        }

        public string getState()
        {
            mutex.WaitOne();
            try
            {
                return $"Элементов: {buffer.Count}/{maxSize}";
            }
            finally { mutex.ReleaseMutex(); }
        }

        public string[] getBufferContent()
        {
            mutex.WaitOne();
            try
            {
                return buffer.ToArray();
            }
            finally { mutex.ReleaseMutex(); }
        }

    }

    
}