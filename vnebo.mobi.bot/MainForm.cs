using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using vnebo.mobi.bot.Libs;
using vnebo.mobi.bot.Properties;

namespace vnebo.mobi.bot
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Количество вкладкок (а так же идентификатор вкладки и компонентов).
        /// </summary>
        private int AccountCount = 0;

        /// <summary>
        /// Количество аккаунтов, нужна для подсчета сколько аккаунтов на данный момент.
        /// </summary>
        private int Account = 0;

        /// <summary>
        /// Максимальное число доступное для создание аккаунтов.
        /// </summary>
        private readonly int maxAccount = 50;

        /// <summary>
        /// Версия приложения.
        /// </summary>
        private readonly string v = "v1.1";
        private readonly string d = "(300920)";

        /// <summary>
        /// Текст кнопки "ЗАПУСТИТЬ БОТА".
        /// </summary>
        public static string BUTTON_TEXT_START = "ЗАПУСТИТЬ БОТА";

        /// <summary>
        /// Текст кнопки "ОСТАНОВИТЬ БОТА".
        /// </summary>
        public static string BUTTON_TEXT_STOP = "ОСТАНОВИТЬ БОТА";

        /// <summary>
        /// Стандартный аватар.
        /// </summary>
        private static readonly string AVATAR_DEFAULT = "man_no";

        /// <summary>
        /// Путь для файла настроек.
        /// </summary>
        private static readonly string settings_path = AppDomain.CurrentDomain.BaseDirectory + "settings.ini";

        /// <summary>
        /// Путь для временного файла настроек.
        /// </summary>
        private static readonly string settings_path_temp = AppDomain.CurrentDomain.BaseDirectory + "settings.temp";

        /// <summary>
        /// Глобальный класс настроек, запись, чтение и т.д
        /// </summary>
        private static readonly IniFiles settings = new IniFiles(settings_path);

        public MainForm()
        {
            InitializeComponent();

            // Загружаем из ресурсов аватары в ImageList
            foreach (DictionaryEntry entry in Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true))
            {
                string key = (string)entry.Key;

                if (key.Contains("player"))
                {
                    imageList1.Images.Add(key, (Image)entry.Value);
                }

                if (key == "man_no")
                {
                    imageList1.Images.Add(key, (Image)entry.Value);
                }
            }

            // Изменяем максимальный размер формы
            MaximumSize = new Size(Width, Height);

            // Хак на уменьшения размера последней вкладки
            HelpMethod.TabControlSmallWidth(tabControl1);

            // Заголовок окна
            Text = $"Бот для мобильной браузерной игры \"Небоскребы\"";

            // Заголовок в меню "О программе"
            toolStripMenuItem10.Text = $"Created by DeKoSiK - {v} {d}";

            // Заголовок иконки в трее
            notifyIcon1.Text = $"Бот для мобильной браузерной игры \"Небоскребы\"";

            #if DEBUG
                // Показываем текущую версию
                Version.Text = $"{v} {d}";
            #else
                // Проверяем актуальность версии
                CheckVersion();
            #endif
        }

        private void CheckVersion(string url = "https://github.com/dekosik/vnebo.mobi.bot/releases/latest")
        {
            // Запускаем поток
            Task.Run(async () =>
            {
                // Показываем текущую версию
                Version.Text = $"{v} {d}";

                // Выполняем GET запрос
                string githubResult = await HelpMethod.GET(url, new HttpClient());

                if (githubResult.Length > 0)
                {
                    // Проверяем актуальность версии
                    if (githubResult.Contains($"/tag/{v}"))
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            Version.ToolTipText = "Вы используете последнюю версию.";
                            Version.ForeColor = Color.Green;
                        });
                    }
                    else
                    {
                        // Парсим ссылку на скачивание
                        string url_download = new Regex("content=\"/dekosik/vnebo.mobi.bot/releases/tag/(.*?)\"").Match(githubResult).Groups[1].Value;

                        Invoke((MethodInvoker)delegate
                        {
                            Version.ToolTipText = url_download.Length > 0 ? $"Ваша версия устарела.\nНажмите чтобы перейти к скачиванию {url_download}." : $"Ваша версия устарела.\nНажмите чтобы перейти к скачиванию последней версии.";
                            Version.ForeColor = Color.Red;
                        });

                        // Ставим обработчик событий на клик
                        Version.Click += (s, e) =>
                        {
                            Process.Start(url_download.Length > 0 ? $"https://github.com/dekosik/vnebo.mobi.bot/releases/download/{url_download}/vnebo.mobi.bot.exe" : url);
                        };
                    }
                }
                else
                {
                    Invoke((MethodInvoker)delegate
                    {
                        Version.ToolTipText = "Не удалось проверить актуальность версии.";
                        Version.ForeColor = SystemColors.ControlDark;
                    });
                }
            });
        }

        private void CreateTemplate(TabPage tabPage)
        {
            ToolStrip toolstrip_info_top = new ToolStrip
            {
                AutoSize = false,
                BackColor = Color.White,
                Dock = DockStyle.None,
                Font = new Font("Segoe UI", 9F),
                GripStyle = ToolStripGripStyle.Hidden,
                Location = new Point(6, 3),
                RenderMode = ToolStripRenderMode.System,
                Size = new Size(612, 25),
                Name = $"toolstrip_info_top{AccountCount}"
            };

            ToolStripLabel toolstriplabel_status_log = new ToolStripLabel
            {
                Size = new Size(115, 22),
                Text = "",
                Name = $"toolstriplabel_status_log{AccountCount}"
            };

            ToolStripLabel toolstriplabel_coin_session = new ToolStripLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 0, 0, 0),
                Image = Resources.coins,
                Size = new Size(121, 25),
                Text = "0 собрано",
                Name = $"toolstriplabel_coin_session{AccountCount}",
                Tag = new Dictionary<string, string>()
                {
                    ["COIN_CURRENT"] = "0",
                    ["COIN_SESSION"] = "0",
                    ["COIN_STATUS"] = "false"
                }
            };

            ToolStripLabel toolstriplabel_baks_session = new ToolStripLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 0, 0, 0),
                Image = Resources.baks,
                Size = new Size(109, 25),
                Text = "0 собрано",
                Name = $"toolstriplabel_baks_session{AccountCount}",
                Tag = new Dictionary<string, string>()
                {
                    ["BAKS_CURRENT"] = "0",
                    ["BAKS_SESSION"] = "0",
                    ["BAKS_STATUS"] = "false"
                }
            };

            ToolStripLabel toolstriplabel_keys_session = new ToolStripLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(4, 0, 0, 0),
                Image = Resources.keys,
                Size = new Size(126, 25),
                Text = "0 собрано",
                Name = $"toolstriplabel_keys_session{AccountCount}",
                Tag = new Dictionary<string, string>()
                {
                    ["KEYS_CURRENT"] = "0",
                    ["KEYS_SESSION"] = "0",
                    ["KEYS_STATUS"] = "false"
                }
            };

            ToolStrip toolstrip_info_bottom = new ToolStrip
            {
                AutoSize = false,
                BackColor = Color.White,
                Dock = DockStyle.None,
                Font = new Font("Segoe UI", 9F),
                GripStyle = ToolStripGripStyle.Hidden,
                Location = new Point(6, 264),
                RenderMode = ToolStripRenderMode.System,
                Size = new Size(612, 25),
                Name = $"toolstrip_info_bottom{AccountCount}",
            };

            ToolStripLabel toolstriplabel_nickname = new ToolStripLabel
            {
                Image = Resources.star,
                Margin = new Padding(5, 0, 5, 0),
                Size = new Size(75, 25),
                Text = "Уровень: 0",
                Name = $"toolstriplabel_nickname{AccountCount}"
            };

            ToolStripLabel toolstriplabel_floor_count = new ToolStripLabel
            {
                Image = Resources.hd_nebo,
                Margin = new Padding(4, 0, 0, 0),
                Size = new Size(84, 25),
                Text = "Этажей: 0",
                Name = $"toolstriplabel_floor_count{AccountCount}"
            };

            ToolStripLabel toolstriplabel_keys = new ToolStripLabel
            {
                Image = Resources.keys,
                Alignment = ToolStripItemAlignment.Right,
                Size = new Size(104, 25),
                Text = "Ключей: 0",
                Name = $"toolstriplabel_keys{AccountCount}"
            };

            ToolStripLabel toolstriplabel_baks = new ToolStripLabel
            {
                Image = Resources.baks,
                Alignment = ToolStripItemAlignment.Right,
                Size = new Size(111, 25),
                Text = "Баксов: 0",
                Name = $"toolstriplabel_baks{AccountCount}"
            };

            ToolStripLabel toolstriplabel_coins = new ToolStripLabel
            {
                Image = Resources.coins,
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(0, 0, 4, 0),
                Size = new Size(157, 25),
                Text = "Монет: 0",
                Name = $"toolstriplabel_coins{AccountCount}"
            };

            GroupBox groupbox1 = new GroupBox
            {
                Location = new Point(6, 24),
                Size = new Size(211, 166),
                TabStop = false
            };

            GroupBox groupbox2 = new GroupBox
            {
                Location = new Point(7, 74),
                Size = new Size(198, 82),
                Text = "Интервал повторов ( мин )",
                TabStop = false
            };

            TextBox textbox_login = new TextBox
            {
                Location = new Point(7, 17),
                Size = new Size(198, 22),
                TabStop = false,
                MaxLength = 16,
                Name = $"textbox_login{AccountCount}"
            };

            TextBox textbox_password = new TextBox
            {
                Location = new Point(7, 46),
                Size = new Size(198, 22),
                PasswordChar = '*',
                TabStop = false,
                Name = $"textbox_password{AccountCount}"
            };

            Label label1 = new Label
            {
                AutoSize = true,
                Location = new Point(8, 25),
                Size = new Size(21, 13),
                Text = "ОТ",
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label label2 = new Label
            {
                AutoSize = true,
                Location = new Point(9, 54),
                Size = new Size(23, 13),
                Text = "ДО",
                TextAlign = ContentAlignment.MiddleLeft
            };

            NumericUpDown numericupdown_interval_from = new NumericUpDown
            {
                Location = new Point(38, 22),
                Maximum = new decimal(new int[] { 1000, 0, 0, 0 }),
                Minimum = new decimal(new int[] { 1, 0, 0, 0 }),
                Size = new Size(154, 22),
                Value = new decimal(new int[] { 10, 0, 0, 0 }),
                TabStop = false,
                Name = $"numericupdown_interval_from{AccountCount}",
                Tag = AccountCount
            };

            NumericUpDown numericupdown_interval_do = new NumericUpDown
            {
                Location = new Point(38, 51),
                Maximum = new decimal(new int[] { 1000, 0, 0, 0 }),
                Minimum = new decimal(new int[] { 1, 0, 0, 0 }),
                Size = new Size(154, 22),
                Value = new decimal(new int[] { 20, 0, 0, 0 }),
                TabStop = false,
                Name = $"numericupdown_interval_do{AccountCount}",
                Tag = AccountCount
            };

            Button button_start = new Button
            {
                Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold),
                Location = new Point(6, 194),
                Size = new Size(211, 34),
                Text = BUTTON_TEXT_START,
                UseVisualStyleBackColor = true,
                TabStop = false,
                Name = $"button_start{AccountCount}",
                Tag = AccountCount
            };

            Button button_show_settings = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(6, 232),
                Size = new Size(211, 27),
                Text = "ОТКРЫТЬ НАСТРОЙКИ",
                UseVisualStyleBackColor = true,
                TabStop = false,
                Name = $"button_show_settings{AccountCount}"
            };

            RichTextBox richtextbox_log = new RichTextBox
            {
                BackColor = SystemColors.ControlLightLight,
                Location = new Point(223, 31),
                ReadOnly = true,
                Size = new Size(395, 230),
                TabStop = false,
                Name = $"richtextbox_log{AccountCount}"
            };

            // Добавляем элементы в верхний ToolStrip
            toolstrip_info_top.Items.AddRange(new ToolStripItem[]
            {
                toolstriplabel_status_log,
                toolstriplabel_coin_session,
                toolstriplabel_baks_session,
                toolstriplabel_keys_session
            });

            // Добавляем элементы в нижний ToolStrip
            toolstrip_info_bottom.Items.AddRange(new ToolStripItem[]
            {
                toolstriplabel_nickname,
                toolstriplabel_floor_count,
                toolstriplabel_keys,
                toolstriplabel_baks,
                toolstriplabel_coins
            });

            // Добавляем элементы на GroupBox
            groupbox1.Controls.AddRange(new Control[]
            {
                textbox_login,
                textbox_password,
                groupbox2
            });

            // Добавляем элементы на GroupBox
            groupbox2.Controls.AddRange(new Control[]
            {
                label1,
                label2,
                numericupdown_interval_from,
                numericupdown_interval_do
            });

            // Добавляем элементы на вкладку
            tabPage.Controls.AddRange(new Control[]
            {
                toolstrip_info_top,
                toolstrip_info_bottom,
                groupbox1,
                button_start,
                button_show_settings,
                richtextbox_log
            });

            // Устанавливаем Placeholder
            HelpMethod.SetPlaceholder(textbox_login, "Ваш логин");
            HelpMethod.SetPlaceholder(textbox_password, "Ваш пароль");

            // Обработчики событий
            textbox_login.KeyUp += (s, e) =>
            {
                settings.Write($"USER_{tabPage.Tag}", "LOGIN", (s as TextBox).Text);
                tabControl1.TabPages[tabControl1.SelectedIndex].Text = (s as TextBox).Text.Length > 0 ? $"{(s as TextBox).Text}" : "Новый персонаж";
            };

            textbox_password.KeyUp += (s, e) =>
            {
                settings.Write($"USER_{tabPage.Tag}", "PASSWORD", (s as TextBox).Text);
            };

            numericupdown_interval_from.ValueChanged += (s, e) =>
            {
                NumericUpDown numericUpDown = (NumericUpDown)s; int botID = (int)numericUpDown.Tag;

                // Максимальное значения ОТ = ДО
                numericUpDown.Maximum = FindControl.FindNumericUpDown("numericupdown_interval_do", botID, this).Value;

                // Записываем значения в файл сохранений
                settings.Write($"USER_{botID}", "INTERVAL_FROM", numericUpDown.Value.ToString());
            };

            numericupdown_interval_do.ValueChanged += (s, e) =>
            {
                NumericUpDown numericUpDown = (NumericUpDown)s; int botID = (int)numericUpDown.Tag;

                // Максимальное значение ОТ = ДО
                FindControl.FindNumericUpDown("numericupdown_interval_from", botID, this).Maximum = numericUpDown.Value;
                // Записываем значения в файл сохранений
                settings.Write($"USER_{botID}", "INTERVAL_DO", numericUpDown.Value.ToString());
            };

            button_start.Click += (s, e) =>
            {
                Button button = (Button)s;

                // Если текст кнопки равен "ОСТАНОВИТЬ БОТА"
                // То меняет на "ЗАПУСТИТЬ БОТА" и выходим из метода
                if (button.Text == BUTTON_TEXT_STOP)
                {
                    button.Text = BUTTON_TEXT_START;
                    return;
                }

                // ЗАПУСКАЕМ БОТА
                BOT_START(Convert.ToInt32(button.Tag));
            };

            button_show_settings.Click += (s, e) =>
            {
                SettingsForm settings = new SettingsForm((int)tabControl1.SelectedTab.Tag);
                settings.ShowDialog();
            };

            // Подсказки
            toolTip1.SetToolTip(button_start, "Пока бот выполняет работу, остановить его невозможно.");
            toolTip1.SetToolTip(numericupdown_interval_from, "Тут можно выбрать интервал повторов.\r\nНапример: Бот выберет рандомное число от 10 до 20 минут до следующего старта.");
            toolTip1.SetToolTip(numericupdown_interval_do, "Тут можно выбрать интервал повторов.\r\nНапример: Бот выберет рандомное число от 10 до 20 минут до следующего старта.");
        }

        private Dictionary<string, string> ReadSettings(int BotID)
        {
            return new Dictionary<string, string>()
            {
                ["LOGIN"] = settings.ReadString($"USER_{BotID}", "LOGIN"),
                ["PASSWORD"] = settings.ReadString($"USER_{BotID}", "PASSWORD"),
                ["COLLECT_COIN"] = settings.ReadString($"USER_{BotID}", "COLLECT_COIN"),
                ["SELL_GOODS"] = settings.ReadString($"USER_{BotID}", "SELL_GOODS"),
                ["BUY_GOODS"] = settings.ReadString($"USER_{BotID}", "BUY_GOODS"),
                ["FLOOR_OPEN"] = settings.ReadString($"USER_{BotID}", "FLOOR_OPEN"),
                ["LIFT_UP"] = settings.ReadString($"USER_{BotID}", "LIFT_UP"),
                ["QUESTS"] = settings.ReadString($"USER_{BotID}", "QUESTS"),
                ["HOSTEL_EVICT_LESS_9"] = settings.ReadString($"USER_{BotID}", "HOSTEL_EVICT_LESS_9"),
                ["HOSTEL_EVICT_MINUS"] = settings.ReadString($"USER_{BotID}", "HOSTEL_EVICT_MINUS"),
                ["HOSTEL_EVICT_PLUS"] = settings.ReadString($"USER_{BotID}", "HOSTEL_EVICT_PLUS"),
                ["BUSINESS_TOURNAMENT"] = settings.ReadString($"USER_{BotID}", "BUSINESS_TOURNAMENT"),
                ["HUMAN_JOBS"] = settings.ReadString($"USER_{BotID}", "HUMAN_JOBS"),
                ["BUY_BAKS_FOR_COIN"] = settings.ReadString($"USER_{BotID}", "BUY_BAKS_FOR_COIN"),
                ["VENDORS_HUMANS"] = settings.ReadString($"USER_{BotID}", "VENDORS_HUMANS")
            };
        }

        private void CheckStop(int BotID, Button Button, NumericUpDown Interval_From, NumericUpDown Interval_Do)
        {
            if (Button.Text != BUTTON_TEXT_START)
            {
                BOT_START(BotID);
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    Button.Text = BUTTON_TEXT_START;
                    Interval_From.Enabled = true;
                    Interval_Do.Enabled = true;
                });

                HelpMethod.StatusLog("", BotID, this);
            }
        }

        private void BOT_START(int BotID)
        {
            // Создаём новый HttpClient
            HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://vnebo.mobi") };

            // Получаем ссылки на компоненты
            Button button_start = FindControl.FindButton("button_start", BotID, this);
            NumericUpDown interval_from = FindControl.FindNumericUpDown("numericupdown_interval_from", BotID, this);
            NumericUpDown interval_do = FindControl.FindNumericUpDown("numericupdown_interval_do", BotID, this);

            // Отключаем компоненты
            Invoke((MethodInvoker)delegate
            {
                button_start.Text = BUTTON_TEXT_STOP;
                button_start.Enabled = false;
                interval_from.Enabled = false;
                interval_do.Enabled = false;
            });

            // Получаем логин и пароль
            Dictionary<string, string> account_settings = ReadSettings(BotID);

            // Устанавливаем UserAgent для HttpClient
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/66.0");

            // Проверяем логин и пароль на пустоту
            if (account_settings["LOGIN"].Length > 0 & account_settings["PASSWORD"].Length > 0)
            {
                // Запускаем основной поток
                Task.Run(async () =>
                {
                    HelpMethod.StatusLog("Авторизация...", BotID, this, Resources.auth);

                    // Авторизация
                    string AuthorizationResult = await BotEngine.Authorization(account_settings["LOGIN"], account_settings["PASSWORD"], httpClient);

                    // Если успешно авторизовались
                    if (AuthorizationResult.Contains("Выход"))
                    {
                        // Разворачиваем этажи
                        string result = await HelpMethod.GET("/home?wicket:interface=:15:expandTowerLink::ILinkListener::", httpClient);

                        // Парсим ссылку на гостиницу
                        Match hostel_parse_url = new Regex("<a href=\"floor/([0-9]*?)/([0-9]*?)\"><img class=\"flogo\" alt=\"\" src=\"/images/icons/floor/new/hotel.png\"/></a>").Match(result);

                        // Парсим ссылку на профиль
                        string profile_url = new Regex("href=\"(tower/id/[0-9]*.?)\"><span>").Match(result).Groups[1].Value;

                        // Заносим в переменную ссылку на гостиницу
                        string hostel_url = $"/floor/{hostel_parse_url.Groups[1].Value}/{hostel_parse_url.Groups[2].Value}";

                        // Обновляем статистику
                        await BotEngine.Statistics(profile_url, BotID, httpClient, this, settings);

                        // Если есть построенные этажи и включена опция "Открывать построенные этажи"
                        if (result.Contains("Открыть этаж!") & HelpMethod.ToBoolean(account_settings["FLOOR_OPEN"]))
                        {
                            // Открываем этажи
                            await BotEngine.FloorOpen(result, BotID, httpClient, this);
                        }

                        // Если включена опция "Нанимать более опытных жителей."
                        if (HelpMethod.ToBoolean(account_settings["HUMAN_JOBS"]))
                        {
                            // Нанимаем более опытных
                            await BotEngine.HumanJobs(hostel_url, BotID, httpClient, this);
                        }

                        // Делаем несколько прогонов сбора выручки, выкладки товара, закупки и развозки лифта
                        for (int i = 1; i <= 2; i++)
                        {
                            // Если включена опция "Собирать выручку"
                            if (HelpMethod.ToBoolean(account_settings["COLLECT_COIN"]))
                            {
                                // Собираем выручку
                                await BotEngine.CollectCoins(BotID, httpClient, this);
                            }

                            // Если включена опция "Выкладывать товар"
                            if (HelpMethod.ToBoolean(account_settings["SELL_GOODS"]))
                            {
                                // Выкладываем товар
                                await BotEngine.SellGoods(BotID, httpClient, this);
                            }

                            // Если включена опция "Закупать товар"
                            if (HelpMethod.ToBoolean(account_settings["BUY_GOODS"]))
                            {
                                // Закупаем товар 
                                await BotEngine.BuyGoods(BotID, httpClient, this);
                            }

                            // Если включена опция "Доставлять посетителей"
                            if (HelpMethod.ToBoolean(account_settings["LIFT_UP"]))
                            {
                                // Лифт
                                await BotEngine.Lift(BotID, httpClient, this);
                            }
                        }

                        // Если включена одна из опций [Выселять жителей ниже 9 уровня, Выселять со знаком (-), Выселять со знаком (+)]
                        if (HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_LESS_9"]) || HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_MINUS"]) || HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_PLUS"]))
                        {
                            // Выселяем жителей
                            await BotEngine.HostelEvict(hostel_url, BotID, httpClient, this, HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_LESS_9"]), HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_MINUS"]), HelpMethod.ToBoolean(account_settings["HOSTEL_EVICT_PLUS"]));
                        }

                        // Если включена опция "Получать награду за бизнес турнир"
                        if (HelpMethod.ToBoolean(account_settings["BUSINESS_TOURNAMENT"]))
                        {
                            // Проверяем бизнес турнир
                            await BotEngine.BusinessTournament(BotID, httpClient, this);
                        }

                        // Если включена опция "Забирать ежедневные задания"
                        if (HelpMethod.ToBoolean(account_settings["QUESTS"]))
                        {
                            // Задания
                            await BotEngine.Quests(BotID, httpClient, this);
                        }

                        // Если включена опция "Выкупать баксы за монеты"
                        if (HelpMethod.ToBoolean(account_settings["BUY_BAKS_FOR_COIN"]))
                        {
                            // Выкупаем баксы за монеты
                            await BotEngine.BuyBaksForCoin(BotID, httpClient, this);
                        }

                        // Если включена опция "Нанимать жителей на бирже труда"
                        if (HelpMethod.ToBoolean(account_settings["VENDORS_HUMANS"]))
                        {
                            // Нанимаем
                            await BotEngine.VendorsHumans(BotID, httpClient, this);
                        }

                        // Обновляем статистику
                        await BotEngine.Statistics(profile_url, BotID, httpClient, this, settings);

                        // Получаем рандомный интервал ожидания
                        int interval_sec = HelpMethod.getRandomNumber.Next(
                            (Convert.ToInt32(interval_from.Value) * 60),
                            (Convert.ToInt32(interval_do.Value) * 60)
                        + 1);

                        // Ожидание
                        await BotEngine.Sleep(BotID, button_start, this, interval_sec);

                        // Новая пустая строка
                        HelpMethod.Log("", BotID, this, ShowTime: false);

                        // Проверяем не остановлен ли бот
                        CheckStop(BotID, button_start, interval_from, interval_do);
                    }
                    else if (AuthorizationResult == "error")
                    {
                        // Ожидание
                        await BotEngine.Sleep(BotID, button_start, this, 60);

                        // Проверяем не остановлен ли бот
                        CheckStop(BotID, button_start, interval_from, interval_do);
                    }
                    else
                    {
                        HelpMethod.Log("Неправильный логин или пароль.", BotID, this, Color.Red);

                        // Меняем текст кнопки (ЗАПУСТИТЬ БОТА), разблокируем кнопку и интервалы ОТ и ДО
                        Invoke((MethodInvoker)delegate
                        {
                            button_start.Enabled = true;
                            button_start.Text = BUTTON_TEXT_START;
                            interval_from.Enabled = true;
                            interval_do.Enabled = true;
                        });
                    }
                });
            }
            else
            {
                HelpMethod.Log("Логин или пароль не могут быть пустыми.", BotID, this, Color.Red);

                // Меняем текст кнопки (ЗАПУСТИТЬ БОТА), разблокируем кнопку и интервалы ОТ и ДО
                Invoke((MethodInvoker)delegate
                {
                    button_start.Enabled = true;
                    button_start.Text = BUTTON_TEXT_START;
                    interval_from.Enabled = true;
                    interval_do.Enabled = true;
                });
            }
        }

        private TabPage AddPage(int LastIndex = 0, bool Default = true, string Login = "Новый персонаж", string Avatar = "", int IntervalFrom = 10, int IntervalDo = 20, string StatLevel = "0", string StatFloor = "0", string StatCoin = "0", string StatBaks = "0", string StatKeys = "0")
        {

            if (Account < maxAccount)
            {
                // Увеличиваем количество вкладкок
                AccountCount++;

                // Увеличиваем количество аккаунтов
                Account++;

                // Проверяем на пустату аватар
                Avatar = Avatar.Length > 0 ? Avatar : AVATAR_DEFAULT;

                // Генерируем новую вкладку
                TabPage tabPage = new TabPage
                {
                    Text = Login.Length > 0 ? Login : "Новый персонаж",
                    Name = $"tabPage{AccountCount}",
                    BackColor = Color.White,
                    ToolTipText = "Для удаления профиля, нажмите несколько раз по вкладке.",
                    Tag = AccountCount,
                    ImageIndex = imageList1.Images.IndexOfKey(Avatar)
                };

                // Ставим обработчик событий на двойной клик
                tabPage.DoubleClick += TabControl1_DoubleClick;

                // Добавляем шаблон на вкладку
                CreateTemplate(tabPage);

                // Если новая вкладка создается пользователем
                if (Default)
                {
                    // Сохраняем профиль со стандартными настройками
                    settings.Write($"USER_{AccountCount}", "LOGIN", "");
                    settings.Write($"USER_{AccountCount}", "PASSWORD", "");
                    settings.Write($"USER_{AccountCount}", "INTERVAL_FROM", "10");
                    settings.Write($"USER_{AccountCount}", "INTERVAL_DO", "20");
                    settings.Write($"USER_{AccountCount}", "AVATAR", AVATAR_DEFAULT);
                    settings.Write($"USER_{AccountCount}", "STAT_LEVEL", "0");
                    settings.Write($"USER_{AccountCount}", "STAT_FLOOR", "0");
                    settings.Write($"USER_{AccountCount}", "STAT_COIN", "0");
                    settings.Write($"USER_{AccountCount}", "STAT_BAKS", "0");
                    settings.Write($"USER_{AccountCount}", "STAT_KEYS", "0");
                    settings.Write($"USER_{AccountCount}", "COLLECT_COIN", "true");
                    settings.Write($"USER_{AccountCount}", "SELL_GOODS", "true");
                    settings.Write($"USER_{AccountCount}", "BUY_GOODS", "true");
                    settings.Write($"USER_{AccountCount}", "FLOOR_OPEN", "true");
                    settings.Write($"USER_{AccountCount}", "LIFT_UP", "true");
                    settings.Write($"USER_{AccountCount}", "QUESTS", "true");
                    settings.Write($"USER_{AccountCount}", "HOSTEL_EVICT_LESS_9", "true");
                    settings.Write($"USER_{AccountCount}", "HOSTEL_EVICT_MINUS", "true");
                    settings.Write($"USER_{AccountCount}", "HOSTEL_EVICT_PLUS", "false");
                    settings.Write($"USER_{AccountCount}", "BUSINESS_TOURNAMENT", "true");
                    settings.Write($"USER_{AccountCount}", "HUMAN_JOBS", "true");
                    settings.Write($"USER_{AccountCount}", "BUY_BAKS_FOR_COIN", "false");
                    settings.Write($"USER_{AccountCount}", "VENDORS_HUMANS", "false");

                    // Добавляем вкладку
                    tabControl1.TabPages.Insert(LastIndex, tabPage);
                }
                else
                {
                    // Добавляем вкладку
                    tabControl1.TabPages.Insert(tabControl1.TabCount - 1, tabPage);

                    // Загружаем логин и пароль
                    FindControl.FindTextBox("textbox_login", AccountCount, this).Text = settings.ReadString($"USER_{AccountCount}", "LOGIN");
                    FindControl.FindTextBox("textbox_password", AccountCount, this).Text = settings.ReadString($"USER_{AccountCount}", "PASSWORD");

                    // Загружаем интервал от и до
                    FindControl.FindNumericUpDown("numericupdown_interval_from", AccountCount, this).Value = IntervalFrom;
                    FindControl.FindNumericUpDown("numericupdown_interval_do", AccountCount, this).Value = IntervalDo;

                    // Загружаем статистику
                    FindControl.FindToolStrip("toolstrip_info_bottom", AccountCount, this).Items[4].Text = $"Монет: {HelpMethod.StringNumberFormat(StatCoin)}";
                    FindControl.FindToolStrip("toolstrip_info_bottom", AccountCount, this).Items[3].Text = $"Баксов: {HelpMethod.StringNumberFormat(StatBaks, false)}";
                    FindControl.FindToolStrip("toolstrip_info_bottom", AccountCount, this).Items[2].Text = $"Ключей: {HelpMethod.StringNumberFormat(StatKeys, false)}";
                    FindControl.FindToolStrip("toolstrip_info_bottom", AccountCount, this).Items[0].Text = $"Уровень: {HelpMethod.StringNumberFormat(StatLevel, false)}";
                    FindControl.FindToolStrip("toolstrip_info_bottom", AccountCount, this).Items[1].Text = $"Этажей: {HelpMethod.StringNumberFormat(StatFloor, false)}";
                }

                // Делаем вкладку выбранной
                tabControl1.SelectedTab = tabPage;

                // Возвращаем вкладку, чтобы с ней можно было работать из вне.
                return tabPage;
            }

            return null;
        }

        private void TabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            int lastIndex = tabControl1.TabCount - 1;

            if (e.Button == MouseButtons.Left)
            {
                if (tabControl1.GetTabRect(lastIndex).Contains(e.Location))
                {
                    if (Account < maxAccount)
                    {
                        _ = AddPage(lastIndex);
                        return;
                    }

                    MessageBox.Show($"Нельзя создать больше {maxAccount} вкладок!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void TabControl1_DoubleClick(object sender, EventArgs e)
        {
            int selectIndex = tabControl1.SelectedIndex - 1;

            if (tabControl1.GetTabRect(selectIndex + 1).Contains(((MouseEventArgs)e).Location))
            {
                if (((MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    DialogResult result = MessageBox.Show("Вы действительно хотите удалить профиль?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        // Удаляем бота из сохранения
                        settings.DeleteSection($"USER_{(int)tabControl1.TabPages[tabControl1.SelectedIndex].Tag}");

                        // Удаляем вкладку
                        tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                        tabControl1.SelectedIndex = selectIndex == -1 ? 0 : selectIndex;

                        // Убавляем количество аккаунтов
                        Account--;

                        // Если вкладок 0, то создаем новый пустой профиль
                        if (tabControl1.TabCount == 1)
                        {
                            AddPage();
                        }
                    }
                }
            }
        }

        private void TabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex == tabControl1.TabCount - 1)
            {
                e.Cancel = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Читаем файл настроек
            string read_file = File.Exists(settings_path) ? File.ReadAllText(settings_path) : "";

            // Если файл настроек не пустой
            if (read_file.Length > 0)
            {
                // Инициализируем временный файл настроек
                IniFiles settings_temp = new IniFiles(settings_path_temp);

                // Запускаем цикл перебора всех секций
                foreach (Match item in new Regex(@"\[.*\]").Matches(read_file))
                {
                    // Получаем название секции
                    string section_name = item.Value.Replace("[", "").Replace("]", "");

                    // Если секция - это глобальные настройки
                    if (section_name.Contains("GLOBAL"))
                    {
                        // Записываем настройки во временный файл
                        settings_temp.Write(section_name, "AUTO_START", settings.ReadString(section_name, "AUTO_START"));
                    }

                    // Если секция - это профиль
                    if (section_name.Contains("USER"))
                    {
                        // Читаем основные настройки профиля
                        string login = settings.ReadString(section_name, "LOGIN");
                        string password = settings.ReadString(section_name, "PASSWORD");
                        int interval_from = settings.ReadInt(section_name, "INTERVAL_FROM");
                        int interval_do = settings.ReadInt(section_name, "INTERVAL_DO");
                        string stat_level = settings.ReadString(section_name, "STAT_LEVEL");
                        string stat_floor = settings.ReadString(section_name, "STAT_FLOOR");
                        string stat_coin = settings.ReadString(section_name, "STAT_COIN");
                        string stat_baks = settings.ReadString(section_name, "STAT_BAKS");
                        string stat_keys = settings.ReadString(section_name, "STAT_KEYS");
                        string avatar = settings.ReadString(section_name, "AVATAR");
                        bool collect_coin = settings.ReadBool(section_name, "COLLECT_COIN");
                        bool sell_goods = settings.ReadBool(section_name, "SELL_GOODS");
                        bool buy_goods = settings.ReadBool(section_name, "BUY_GOODS");
                        bool floor_open = settings.ReadBool(section_name, "FLOOR_OPEN");
                        bool lift_up = settings.ReadBool(section_name, "LIFT_UP");
                        bool quests = settings.ReadBool(section_name, "QUESTS");
                        bool hostel_evict_less_9 = settings.ReadBool(section_name, "HOSTEL_EVICT_LESS_9");
                        bool hostel_evict_minus = settings.ReadBool(section_name, "HOSTEL_EVICT_MINUS");
                        bool hostel_evict_plus = settings.ReadBool(section_name, "HOSTEL_EVICT_PLUS");
                        bool business_tournament = settings.ReadBool(section_name, "BUSINESS_TOURNAMENT");
                        bool human_jobs = settings.ReadBool(section_name, "HUMAN_JOBS");
                        bool buy_baks_for_coin = settings.ReadBool(section_name, "BUY_BAKS_FOR_COIN");
                        bool vendors_humans = settings.ReadBool(section_name, "VENDORS_HUMANS");


                        // Записываем основные настройки профиля во временный файл
                        settings_temp.Write($"USER_{AccountCount + 1}", "LOGIN", login);
                        settings_temp.Write($"USER_{AccountCount + 1}", "PASSWORD", password);
                        settings_temp.Write($"USER_{AccountCount + 1}", "INTERVAL_FROM", interval_from.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "INTERVAL_DO", interval_do.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "AVATAR", avatar);
                        settings_temp.Write($"USER_{AccountCount + 1}", "STAT_LEVEL", stat_level.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "STAT_FLOOR", stat_floor.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "STAT_COIN", stat_coin.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "STAT_BAKS", stat_baks.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "STAT_KEYS", stat_keys.ToString());
                        settings_temp.Write($"USER_{AccountCount + 1}", "COLLECT_COIN", collect_coin.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "SELL_GOODS", sell_goods.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "BUY_GOODS", buy_goods.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "FLOOR_OPEN", floor_open.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "LIFT_UP", lift_up.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "QUESTS", quests.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "HOSTEL_EVICT_LESS_9", hostel_evict_less_9.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "HOSTEL_EVICT_MINUS", hostel_evict_minus.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "HOSTEL_EVICT_PLUS", hostel_evict_plus.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "BUSINESS_TOURNAMENT", business_tournament.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "HUMAN_JOBS", human_jobs.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "BUY_BAKS_FOR_COIN", buy_baks_for_coin.ToString().ToLower());
                        settings_temp.Write($"USER_{AccountCount + 1}", "VENDORS_HUMANS", vendors_humans.ToString().ToLower());

                        // Добавляем новую вкладку
                        AddPage(Default: false, Login: login, Avatar: avatar, IntervalFrom: interval_from, IntervalDo: interval_do, StatLevel: stat_level, StatFloor: stat_floor, StatCoin: stat_coin, StatBaks: stat_baks, StatKeys: stat_keys);
                    }

                    if (Account >= maxAccount)
                    {
                        break;
                    }
                }

                // Удаляем основной файл настроек
                File.Delete(settings_path);

                // Переименновываем временный файл настроек в основной
                if (File.Exists(settings_path_temp))
                {
                    File.Move(settings_path_temp, settings_path);
                }

                // Автоматический старт
                if (settings.KeyExists("GLOBAL", "AUTO_START"))
                {
                    // Получаем значение
                    bool auto_start = settings.ReadBool("GLOBAL", "AUTO_START");

                    // Если включено
                    if (auto_start)
                    {
                        // Меняем настройку
                        toolStripMenuItem5.Checked = auto_start;

                        // Запускаем всех ботов
                        toolStripMenuItem3.PerformClick();

                        // Скрываем приложее в трей
                        WindowState = FormWindowState.Minimized;
                    }
                }

                return;
            }

            // Создаём новый профиль, если в настройках их нет
            AddPage();
        }

        private void ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            // Запускаем всех ботов
            for (int i = 1; i <= AccountCount; i++)
            {
                Button button = FindControl.FindButton("button_start", i, this);

                if (button.Text == BUTTON_TEXT_START & button.Enabled)
                {
                    BOT_START(i);
                }
            }
        }

        private void ToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            // Останавливаем всех ботов
            for (int i = 1; i <= AccountCount; i++)
            {
                Button button = FindControl.FindButton("button_start", i, this);

                if (button.Text == BUTTON_TEXT_STOP)
                {
                    button.Text = BUTTON_TEXT_START;
                }
            }
        }

        private void ToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            // Меняем Checked
            toolStripMenuItem5.Checked = !toolStripMenuItem5.Checked;

            // Сохраняем
            settings.Write("GLOBAL", "AUTO_START", toolStripMenuItem5.Checked.ToString().ToLower());

            // Добавляем в автозагрузку
            HelpMethod.AutoRun(toolStripMenuItem5.Checked);
        }

        private void NotifyIcon1_Click(object sender, EventArgs e)
        {
            // Если была нажата правая кнопка мыши
            if (((MouseEventArgs)e).Button == MouseButtons.Right)
            {
                // Показываем форму
                Show();

                // Разворачиваем приложение
                WindowState = FormWindowState.Normal;

                // Показываем значек в панели задач
                ShowInTaskbar = true;

                // Скрываем иконку из трее
                notifyIcon1.Visible = false;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Если окно было свернуто
            if (WindowState == FormWindowState.Minimized)
            {
                // Прячем из панели задач
                ShowInTaskbar = false;

                // Прячем форму
                Hide();

                // Показываем иконку в трее
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ToolStripMenuItem8_Click(object sender, EventArgs e)
        {
            Process.Start("https://vk.cc/azGobB");
        }

        private void ToolStripMenuItem9_Click(object sender, EventArgs e)
        {
            Process.Start("https://vk.cc/azGocp");
        }
    }
}
