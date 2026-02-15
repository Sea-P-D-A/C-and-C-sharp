using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laba1_2._2._7_OS
{
    public class Consumer
    {
        private ThreadSafeBuffer buffer;
        private Thread thread;
        private bool isRunning;
        private Action<string> onStateChanged;
        private Action<string> onDataConsumed;

        public Consumer(ThreadSafeBuffer targetBuffer, Action<string> stateCallback, Action<string> dataCallback)
        {
            buffer = targetBuffer;
            onStateChanged = stateCallback;
            onDataConsumed = dataCallback;
        }

        public void Start()
        {
            if (thread != null && thread.IsAlive)
                return;

            isRunning = true;
            thread = new Thread(() => WorkerMethod());
            thread.IsBackground = true; // Фоновый поток
            thread.Start();
            UpdateState("Запущен");
        }

        public void Stop()
        {
            isRunning = false;
            thread?.Join(1000); // Ждем завершения до 1 секунды
            UpdateState("Остановлен");
        }

        public void Pause()
        {
            // Для паузы можно использовать флаг, но в реальном коде нужна более сложная логика
            UpdateState("Приостановлен");
        }

        public void Resume()
        {
            UpdateState("Возобновлен");
        }

        private void WorkerMethod(int timeout = Timeout.Infinite)
        {
            while (isRunning)
            {
                try
                {
                    UpdateState("Ожидает данные...");

                    // Берем данные из буфера (ждет бесконечно)
                    string data = buffer.take();

                    if (data != null)
                    {
                        onDataConsumed?.Invoke(data);
                        UpdateState($"Обработал: {data}");
                    }

                    // Имитация обработки данных
                    Thread.Sleep(1000);
                }
                catch (ThreadAbortException)
                {
                    UpdateState("Прерван");
                    break;
                }
                catch (Exception ex)
                {
                    UpdateState($"Ошибка: {ex.Message}");
                    break;
                }
            }
            UpdateState("Завершен");
        }

        private void UpdateState(string state)
        {
            onStateChanged?.Invoke($"Потребитель: {state}");
        }
    }
}
