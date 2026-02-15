using System;
using System.Threading;

namespace Laba1_2._2._7_OS
{
    public class Producer
    {
        private ThreadSafeBuffer buffer;
        private string producerId;
        private Thread thread;
        private bool isRunning;
        private Action<string, string> onStateChanged;

        public Producer(string id, ThreadSafeBuffer targetBuffer, Action<string, string> stateCallback)
        {
            producerId = id;
            buffer = targetBuffer;
            onStateChanged = stateCallback;
        }

        public void Start()
        {
            if (thread != null && thread.IsAlive)
                return;

            isRunning = true;
            thread = new Thread(WorkerMethod);
            thread.Start();
            UpdateState("Запущен");
        }

        public void Stop()
        {
            isRunning = false;
            thread?.Join(1000);
            UpdateState("Остановлен");
        }

        private void WorkerMethod()
        {
            Random random = new Random();

            while (isRunning)
            {
                try
                {
                    // Генерируем случайные данные
                    string data = $"Данные_{producerId}_{DateTime.Now:HH:mm:ss}";

                    UpdateState($"Пытается положить: {data}");

                    // Пытаемся положить данные с таймаутом
                    bool success = buffer.tryPut(data, 2000);

                    if (success)
                    {
                        UpdateState($"Успешно добавлено: {data}");
                    }
                    else
                    {
                        UpdateState("Не удалось добавить - буфер полон. Самоуничтожение.");
                        isRunning = false;
                        break;
                    }

                    // Случайная задержка перед следующей операцией
                    Thread.Sleep(random.Next(1000, 3000));
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
            onStateChanged?.Invoke(producerId, state);
        }
    }
}