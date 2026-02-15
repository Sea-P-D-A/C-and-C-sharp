namespace OS_Laba2
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtDirectory1;
        private TextBox txtDirectory2;
        private Button btnSearch;
        private ListBox lstResults;
        private Label label1;
        private Label label2;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtDirectory1 = new TextBox();
            this.txtDirectory2 = new TextBox();
            this.btnSearch = new Button();
            this.lstResults = new ListBox();
            this.label1 = new Label();
            this.label2 = new Label();

            // Form1
            this.Text = "Поиск общих подкаталогов";
            this.ClientSize = new Size(500, 400);

            // Элементы управления
            this.label1.Text = "Каталог 1:";
            this.label1.Location = new Point(20, 20);
            this.label1.Size = new Size(80, 20);

            this.txtDirectory1.Location = new Point(100, 20);
            this.txtDirectory1.Size = new Size(350, 20);

            this.label2.Text = "Каталог 2:";
            this.label2.Location = new Point(20, 50);
            this.label2.Size = new Size(80, 20);

            this.txtDirectory2.Location = new Point(100, 50);
            this.txtDirectory2.Size = new Size(350, 20);

            this.btnSearch.Text = "Найти общие подкаталоги";
            this.btnSearch.Location = new Point(150, 80);
            this.btnSearch.Size = new Size(200, 30);
            this.btnSearch.Click += new EventHandler(this.btnSearch_Click);

            this.lstResults.Location = new Point(20, 120);
            this.lstResults.Size = new Size(460, 250);

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] {
            this.label1, this.txtDirectory1,
            this.label2, this.txtDirectory2,
            this.btnSearch, this.lstResults
        });
        }
    }
}
