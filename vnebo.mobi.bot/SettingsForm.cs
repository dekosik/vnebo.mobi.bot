using System;
using System.Linq;
using System.Windows.Forms;

namespace vnebo.mobi.bot
{
    public partial class SettingsForm : Form
    {
        private static readonly IniFiles settings = new IniFiles(AppDomain.CurrentDomain.BaseDirectory + "settings.ini");

        public SettingsForm(int BotID)
        {
            InitializeComponent();

            // Название секции
            string section_name = $"USER_{BotID}";

            // Обновляем заголовок формы
            Text = $"Настройки {(settings.KeyExists(section_name, "LOGIN") ? $"( {settings.ReadString(section_name, "LOGIN")} )" : "")}";

            // Записываем в Tag имя ключа
            checkBox01.Tag = "COLLECT_COIN";
            checkBox02.Tag = "SELL_GOODS";
            checkBox03.Tag = "BUY_GOODS";
            checkBox04.Tag = "LIFT_UP";
            checkBox05.Tag = "FLOOR_OPEN";
            checkBox06.Tag = "QUESTS";
            checkBox07.Tag = "BUSINESS_TOURNAMENT";
            checkBox08.Tag = "HUMAN_JOBS";
            checkBox09.Tag = "HOSTEL_EVICT_LESS_9";
            checkBox10.Tag = "HOSTEL_EVICT_MINUS";
            checkBox11.Tag = "HOSTEL_EVICT_PLUS";

            // Прохоидмся по GroupBox
            foreach (GroupBox groupBox in Controls.OfType<GroupBox>())
            {
                // В GroupBox проходимся по CheckBox
                foreach (CheckBox checkBox in groupBox.Controls.OfType<CheckBox>())
                {
                    // Ставим обработчик событий
                    checkBox.CheckedChanged += (s, e) =>
                    {
                        settings.Write(section_name, (s as CheckBox).Tag.ToString(), (s as CheckBox).Checked.ToString().ToLower());
                    };

                    // Загружаем настройку для этого CheckBox
                    checkBox.Checked = settings.ReadBool(section_name, checkBox.Tag.ToString());
                }
            }

            // Подсказки
            toolTip1.SetToolTip(checkBox01, "Бот будет собирать выручку с этажей.");
            toolTip1.SetToolTip(checkBox02, "Бот будет выкладывать доставленный товар на этажах.");
            toolTip1.SetToolTip(checkBox03, "Бот будет закупать товар на этажах.\nПриоритет закупки товара: 3 - 2 - 1.");
            toolTip1.SetToolTip(checkBox04, "Бот будет поднимать посетителей в лифте.");
            toolTip1.SetToolTip(checkBox05, "Бот будет открывать построенные этажи.");
            toolTip1.SetToolTip(checkBox06, "Бот будет забирать выполненные ежедневные задания.");
            toolTip1.SetToolTip(checkBox07, "Бот будет получать награду в бизнес турнире.");
            toolTip1.SetToolTip(checkBox08, "Бот будет нанимать более опытных жителей на работу.");
            toolTip1.SetToolTip(checkBox09, "Бот будет выселять жителей ниже 9 уровня.");
            toolTip1.SetToolTip(checkBox10, "Бот будет выселять жителей со знаком (-).");
            toolTip1.SetToolTip(checkBox11, "Бот будет выселять жителей со знаком (+).");
        }
    }
}
