using System;

namespace Laba1_2._2._7_OS
{
    public partial class Form1 : Form
    {

        private ThreadSafeBuffer buffer;
        private Consumer consumer;
        private List<Producer> producers;
        private Random random;
        private System.Windows.Forms.Timer producerTimer;

        // Элементы управления
        private TextBox txtBufferSize;
        private Button btnStart;
        private Button btnStop;
        private Button btnPauseConsumer;
        private Button btnResumeConsumer;
        private ListBox lstBufferContent;
        private ListBox lstEvents;
        private Label lblBufferState;
        private Label lblConsumerState;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();

            // Запускаем обновление состояния сразу
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += (s, e) => UpdateBufferState();
            timer.Start();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Producer-Consumer Demo";
            this.Size = new Size(800, 600);

            // Инициализация случайного генератора
            random = new Random();
            producers = new List<Producer>();

            // Создание таймера для генерации производителей
            producerTimer = new System.Windows.Forms.Timer();
            producerTimer.Interval = 3000; // Каждые 3 секунды
            producerTimer.Tick += ProducerTimer_Tick;

            CreateControls();
        }

        private void CreateControls()
        {
            // Поле для размера буфера
            var lblSize = new Label { Text = "Размер буфера:", Location = new Point(10, 10), Size = new Size(100, 20) };
            txtBufferSize = new TextBox { Text = "5", Location = new Point(120, 10), Size = new Size(50, 20) };

            // Кнопки управления
            btnStart = new Button { Text = "Старт", Location = new Point(180, 10), Size = new Size(80, 25) };
            btnStop = new Button { Text = "Стоп", Location = new Point(270, 10), Size = new Size(80, 25) };
            btnPauseConsumer = new Button { Text = "Пауза потребителя", Location = new Point(360, 10), Size = new Size(120, 25) };
            btnResumeConsumer = new Button { Text = "Возобновить потребителя", Location = new Point(490, 10), Size = new Size(150, 25) };

            // Метки состояния
            lblBufferState = new Label { Text = "Состояние буфера: Не запущено", Location = new Point(10, 40), Size = new Size(300, 20) };
            lblConsumerState = new Label { Text = "Состояние потребителя: Не запущено", Location = new Point(10, 65), Size = new Size(300, 20) };

            // Списки для отображения
            lstBufferContent = new ListBox { Location = new Point(10, 90), Size = new Size(350, 200) };
            lstEvents = new ListBox { Location = new Point(370, 90), Size = new Size(400, 200) };

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] {
                lblSize, txtBufferSize, btnStart, btnStop, btnPauseConsumer, btnResumeConsumer,
                lblBufferState, lblConsumerState, lstBufferContent, lstEvents
            });

            // Подписка на события
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnPauseConsumer.Click += BtnPauseConsumer_Click;
            btnResumeConsumer.Click += BtnResumeConsumer_Click;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                int bufferSize = int.Parse(txtBufferSize.Text);

                // Создаем буфер и потребителя
                buffer = new ThreadSafeBuffer(bufferSize);
                consumer = new Consumer(buffer, UpdateConsumerState, OnDataConsumed);

                // Запускаем потребителя
                consumer.Start();

                // Запускаем таймер для создания производителей
                producerTimer.Start();

                // Запускаем таймер для обновления состояния буфера
                var updateTimer = new System.Windows.Forms.Timer();
                updateTimer.Interval = 500;
                updateTimer.Tick += (s, args) => UpdateBufferState(); // Подписываем метод на событие таймера
                updateTimer.Start();

                btnStart.Enabled = false;
                AddEvent("Система запущена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            producerTimer.Stop();
            consumer?.Stop();

            // Останавливаем всех производителей
            foreach (var producer in producers)
            {
                producer.Stop();
            }
            producers.Clear();

            btnStart.Enabled = true;
            AddEvent("Система остановлена");
        }

        private void BtnPauseConsumer_Click(object sender, EventArgs e)
        {
            consumer?.Pause();
        }

        private void BtnResumeConsumer_Click(object sender, EventArgs e)
        {
            consumer?.Resume();
        }

        private void ProducerTimer_Tick(object sender, EventArgs e)
        {
            // Случайно решаем, создавать ли нового производителя
            if (random.Next(0, 100) < 60) // 60% вероятность
            {
                CreateNewProducer();
            }
        }

        private void CreateNewProducer()
        {
            string producerId = $"P{producers.Count + 1}";

            var producer = new Producer(producerId, buffer, OnProducerStateChanged);
            producers.Add(producer);
            producer.Start();

            AddEvent($"Создан новый производитель: {producerId}");
        }

        private void OnProducerStateChanged(string producerId, string state)
        {
            // Обновляем UI из потока UI
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(OnProducerStateChanged), producerId, state);
                return;
            }

            AddEvent($"{producerId}: {state}");

            // Если производитель завершил работу, удаляем его из списка
            if (state.Contains("Завершен") || state.Contains("Самоуничтожение"))
            {
                var producerToRemove = producers.Find(p => p.ToString().Contains(producerId));
                if (producerToRemove != null)
                {
                    producers.Remove(producerToRemove);
                }
            }
        }

        private void UpdateConsumerState(string state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateConsumerState), state);
                return;
            }

            lblConsumerState.Text = state;
        }

        private void OnDataConsumed(string data)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(OnDataConsumed), data);
                return;
            }

            AddEvent($"Потреблено: {data}");
        }

        private void UpdateBufferState()
        {
            if (buffer == null) return;

            if (InvokeRequired)
            {
                Invoke(new Action(UpdateBufferState));
                return;
            }

            lblBufferState.Text = $"Состояние буфера: {buffer.getState()}";

            // Обновляем содержимое буфера
            lstBufferContent.Items.Clear();
            var content = buffer.getBufferContent();
            foreach (var item in content)
            {
                lstBufferContent.Items.Add(item);
            }
        }

        private void AddEvent(string eventText)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddEvent), eventText);
                return;
            }

            lstEvents.Items.Add($"{DateTime.Now:HH:mm:ss} - {eventText}");
            lstEvents.TopIndex = lstEvents.Items.Count - 1; // Прокрутка к последнему элементу
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Корректное завершение при закрытии формы
            producerTimer?.Stop();
            consumer?.Stop();

            foreach (var producer in producers)
            {
                producer.Stop();
            }

            base.OnFormClosing(e);
        }
    }
}


/*Общая формулировка:
Реализовать задачу "Поставщик-потребитель". Поставщик генерирует данные и отправляет их в общий буфер.
Размер буфера ограничен потребитель забирает данные из буфера. поставщик не может положить, если в буфере нет свободных мест.
Потребитель не может взять данные, если буфер пуст. Поставщик и потребитель не могут одновременно работать с буфером.
создать Windows-приложение. 
Каждый поставщик и каждый потребитель - поток. 
Буфер описан как собственный класс, который внутри может соответствующий библиотечный контейнер.
Средства синхронизации встроены в буфер в команды "положить" и "взять". На форме должны отображаться состояния буфера и
состояния поставщика и потребителя, должна быть возможность  приостановить и возобновить поток.
 Представление буфера - Очередь
 Средства синхронизации - Семафоры и мьютексы 
 Задача: Создать многопоточное приложение с одним потоком - читателем удаляющим данные из буфера. 
Главный поток в случайный момент времени порождает потоки - писатели, которые в случайные моменты времени помещают данные в буфер, если в 
структуре имеется свободное место, или самоуничтожаются с соответствующим сообщение. Каждая пара читатель - писатель использует свой буфер
*/