using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using vnebo.mobi.bot.Properties;

namespace vnebo.mobi.bot.Libs
{
    internal class BotEngine
    {
        /// <summary>
        /// Метод, который авторизуется в игре.
        /// </summary>
        /// <param name="Login">Логин пользователя.</param>
        /// <param name="Password">Пароль пользователя.</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        public static async Task<string> Authorization(string Login, string Password, HttpClient HttpClient)
        {
            try
            {
                // Отправляем запрос на страницу логина
                string result = await HelpMethod.GET("/login", HttpClient);

                // Парсим скрытое поле
                string hidden_input = new Regex("<input type=\"hidden\" name=\"(.*?)\" id=").Match(result).Groups[1].Value;

                // Генерируем POST запрос
                FormUrlEncodedContent parameters = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(hidden_input, ""),
                    new KeyValuePair<string, string>("login", Login),
                    new KeyValuePair<string, string>("password", Password),
                    new KeyValuePair<string, string>(":submit", "Вход")
                });

                // Отправляем запрос
                return await HttpClient.PostAsync("/login?wicket:interface=:0:loginForm:loginForm::IFormSubmitListener::", parameters).Result.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return "error";
            }
        }

        /// <summary>
        /// Метод, который собирает выручку с этажей.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task CollectCoins(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Идём на страницу сбора выручки
            string result = await HelpMethod.GET("/floors/0/5", HttpClient);

            // Если есть проданный товар
            if (result.Contains("Товар продан!"))
            {
                HelpMethod.StatusLog("Собираем выручку...", BotID, Form, Resources.st_sold);

                // Общая сумма профита и количество этажей на которых собрано
                int coin = 0, floorCount = 0;

                // Запускаем цикл
                do
                {
                    // Парсим первую ссылку этажа и первый профит с этажа
                    string url = new Regex("wicket:interface=:([0-9]*?):floors:([0-9]*?):floorPanel:state:action::ILinkListener::").Match(result).Value;
                    string coin_string = new Regex("<img src=\"/images/icons/st_sold.png\" width=\"16\" height=\"16\" alt=\"o\"/><span>(.*?)</span>").Match(result).Groups[1].Value.Replace("&#039;", "");

                    // Если строка профита не пустая, прибавляем её к общему профиту
                    if (coin_string.Length > 0)
                    {
                        coin += Convert.ToInt32(coin_string);
                    }

                    // Прибавляем количество этажей
                    floorCount++;

                    // Забираем выручку
                    result = await HelpMethod.GET($"/?{url}", HttpClient);
                }
                while (result.Contains("Товар продан!"));

                // Логируем
                HelpMethod.Log($"Этажей, на которых собрана выручка: {floorCount}", BotID, Form);
            }
        }

        /// <summary>
        /// Метод, который выкладывает товар на этажах.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task SellGoods(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Идём на страницу доставки товара
            string result = await HelpMethod.GET("/floors/0/3", HttpClient);

            // Если есть доставленный товар
            if (result.Contains("Товар доставлен!"))
            {
                HelpMethod.StatusLog("Выкладываем товар...", BotID, Form, Resources.st_stocked);

                // Количество этажей
                int floorCount = 0;

                // Запускаем цикл
                do
                {
                    // Парсим первую ссылку сбора выручки
                    string url = new Regex("wicket:interface=:([0-9]*?):floors:([0-9]*?):floorPanel:state:action::ILinkListener::").Match(result).Value;

                    // Прибавляем количество этажей
                    floorCount++;

                    // Выкладываем товар
                    result = await HelpMethod.GET($"/?{url}", HttpClient);
                }
                while (result.Contains("Товар доставлен!"));

                // Логируем
                HelpMethod.Log($"Этажей, на которых выложен товар: {floorCount}", BotID, Form);
            }
        }

        /// <summary>
        /// Метод, который закупает товар на этажах.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task BuyGoods(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Идём на страницу закупки товара
            string result = await HelpMethod.GET("/floors/0/2", HttpClient);

            // Если можно закупить товар
            if (result.Contains("Можно закупить товар"))
            {
                HelpMethod.StatusLog("Закупаем товар...", BotID, Form, Resources.st_empty_plus);

                // Количество этажей
                int floorCount = 0;

                // Запускаем цикл
                do
                {
                    // Парсим ссылку первого этажа
                    Match url_floor = new Regex("floor/([0-9]*?)/([0-9]*?)\">").Match(result);

                    // Составляем ссылку на этаж
                    string url = $"/floor/{url_floor.Groups[1].Value}/{url_floor.Groups[2].Value}";

                    // Переходим на этаж
                    result = await HelpMethod.GET(url, HttpClient);

                    // Переменная для ссылки на закупку товара
                    string url_purchase;

                    // Проверяем где нужно закупить, приоритет 3 - 2 - 1
                    if (result.Contains("productC"))
                    {
                        url_purchase = new Regex("wicket:interface=:[0-9]*?:floorPanel:productC:emptyState:action:link::ILinkListener::").Match(result).Value;
                    }
                    else if (result.Contains("productB"))
                    {
                        url_purchase = new Regex("wicket:interface=:[0-9]*?:floorPanel:productB:emptyState:action:link::ILinkListener::").Match(result).Value;
                    }
                    else
                    {
                        url_purchase = new Regex("wicket:interface=:[0-9]*?:floorPanel:productA:emptyState:action:link::ILinkListener::").Match(result).Value;
                    }

                    // Прибавляем количество этажей
                    floorCount++;

                    // Закупаем товар на этаже
                    result = await HelpMethod.GET($"/?{url_purchase}", HttpClient);
                }
                while (result.Contains("Можно закупить товар"));

                // Логируем
                HelpMethod.Log($"Этажей, на которых закуплен товар: {floorCount}", BotID, Form);
            }
        }

        /// <summary>
        /// Метод, который развозит посетителей в лифте.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task Lift(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Проверяем лифт
            string result = await HelpMethod.GET("/lift", HttpClient);

            // Если есть посетители
            if (result.Contains("Поднять лифт"))
            {
                // Количество посетителей, заработано монет, заработано баксов
                int visitors_count = 0;

                // Запускаем цикл
                do
                {
                    HelpMethod.StatusLog("Поднимаем лифт...", BotID, Form, Resources.tb_lift);

                    // Переменная в которой будет храниться ссылка (поднять лифт или получить чаевые)
                    string url;

                    // Если нужно поднять лифт
                    if (result.Contains("Поднять лифт"))
                    {
                        // Парсим ссылку для подъёма на этаж
                        url = new Regex("lift/(.*?)\">Поднять").Match(result).Groups[1].Value;

                        // Поднимаем на этаж
                        result = await HelpMethod.GET($"/lift/{url}", HttpClient);
                    }

                    // Если нужно получить чаевые
                    if (result.Contains("Получить чаевые"))
                    {
                        // Парсим ссылку для получения чаевых
                        url = new Regex("../..(.*?)\"><span>Получить").Match(result).Groups[1].Value;

                        // Забираем чаевые
                        result = await HelpMethod.GET($"{url}", HttpClient);

                        // Прибавляем количество посетителей
                        visitors_count++;
                    }
                }
                while (result.Contains("Поднять лифт"));

                // Логируем
                HelpMethod.Log($"Доставили посетителей: {visitors_count}", BotID, Form);
            }
        }

        /// <summary>
        /// Метод, который забирает выполненные ежедневные задания.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task Quests(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Переходим на страницу заданий
            string result = await HelpMethod.GET("/quests", HttpClient);

            // Если есть выполненные задания
            if (result.Contains("Получить награду"))
            {
                HelpMethod.StatusLog("Забираем ежедневные награды...", BotID, Form, Resources.quests);

                // Переменные для хранения монет и баксов
                int quests_count = 0, quests_coin = 0, quests_baks = 0;

                // Запускаем цикл
                do
                {
                    // Парсим первую ссылку задания
                    string url = new Regex(@"\?(.*?)"">Получить").Match(result).Groups[1].Value;

                    // Парсим количество монет и баксов заработаных с задания
                    string coin_string = new Regex("<img src=\"/images/icons/st_sold.png\" width=\"16\" height=\"16\" alt=\"o\"/><span>(.*?)</span>").Match(result).Groups[1].Value;
                    string baks_string = new Regex(@"<img src=""/images/icons/mn_gold\.png"" width=""16"" height=""16"" alt=""\$""/><span>([0-9]?)</span>").Match(result).Groups[1].Value;

                    // Если сумма монет не пустая, прибавляем её к общиму профиту монет
                    if (coin_string.Length > 0)
                    {
                        quests_coin += Convert.ToInt32(coin_string.Replace("&#039;", ""));
                    }

                    // Если сумма баксов не пустая, прибавляем её к общиму профиту баксов
                    if (baks_string.Length > 0)
                    {
                        quests_baks += Convert.ToInt32(baks_string.Replace("&#039;", ""));
                    }

                    // Прибавляем общее количество заданий которые забарали
                    quests_count++;

                    // Забираем задание
                    result = await HelpMethod.GET($"/quests?{url}", HttpClient);
                }
                while (result.Contains("Получить награду"));

                // Логируем
                HelpMethod.Log($"Забрали ежедневных заданий: {quests_count}", BotID, Form);
            }
        }

        /// <summary>
        /// Метод, который выселяет жителей из гостиницы.
        /// </summary>
        /// <param name="hostel_url">Ссылка на гостиницу.</param>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        /// <param name="hostel_evict_less_9">Выселять жителей ниже 9 уровня.</param>
        /// <param name="hostel_evict_minus">Выселять жителей со знаком (-).</param>
        /// <param name="hostel_evict_plus">Выселять жителей со знаком (+).</param>
        public static async Task HostelEvict(string hostel_url, int BotID, HttpClient HttpClient, MainForm Form, bool hostel_evict_less_9 = false, bool hostel_evict_minus = false, bool hostel_evict_plus = false)
        {
            if (hostel_url.Length > 0)
            {
                HelpMethod.StatusLog("Проверяем гостиницу...", BotID, Form, Resources.man_minus);

                // Заходим в гостиницу
                string result = await HelpMethod.GET(hostel_url, HttpClient);

                // Раскрываем список жителей в гостинице, если это нужно
                if (result.Contains("Раскрыть список"))
                {
                    // Парсим ссылку, для раскрытие списка
                    string expandResidentsLink = new Regex(@"\?wicket:interface=:[0-9]*?:floorPanel:expandResidentsLink::ILinkListener::").Match(result).Value;

                    // Проверяем то что ссылка не пустая и раскрываем список
                    if (expandResidentsLink.Length > 0)
                    {
                        result = await HelpMethod.GET(expandResidentsLink, HttpClient);
                    }
                }

                // Навсякий случай проверим
                if (result.Contains("Свернуть список"))
                {
                    // Переменные
                    int human_evict_count = 0;

                    // Запускаем цикл выселения
                    foreach (object item in new Regex("<li>.*?</li>", RegexOptions.Singleline).Matches(result))
                    {
                        string li = item.ToString();

                        // Если пустая ячейка, пропускаем
                        if (li.Contains("Свободное место"))
                        {
                            continue;
                        }

                        // Парсим уровень жителя
                        string level = new Regex("<span class=\".*?\">([0-9])</span>").Match(li).Groups[1].Value;
                        // Парсим (-)
                        string major = new Regex("<span class=\"major\">(.*?)</span>").Match(li).Groups[1].Value;
                        // Парсим (+)
                        string amount = new Regex("<span class=\"amount\">(.*?)</span>").Match(li).Groups[1].Value;
                        // Парсим ссылку на жителя
                        string human_url = new Regex("(/human/[0-9]*.?/[0-9]*.?/[0-9]*.?/[0-9]*.?)\">").Match(li).Groups[1].Value;

                        // Если важные переменные не пустые
                        if (level.Length > 0 & human_url.Length > 0)
                        {
                            // Если выполняем любое из этих действий, оповещаем через статус
                            if (Convert.ToInt32(level) < 9 & hostel_evict_less_9 || major.Length > 0 & hostel_evict_minus || amount.Length > 0 & hostel_evict_plus)
                            {
                                HelpMethod.StatusLog("Выселяем жителей...", BotID, Form, Resources.man_minus);
                            }

                            // Житель меньше 9 уровня и включена опция "Выселять жителей ниже 9 уровня" и у жителя нет знака (+)
                            if (Convert.ToInt32(level) < 9 & hostel_evict_less_9 & amount.Length == 0)
                            {
                                // Переходим на страницу жителя
                                result = await HelpMethod.GET(human_url, HttpClient);

                                // Парсим ссылку на выселенения
                                string evictLink = new Regex(@"/\?(.*?)"">Выселить").Match(result).Groups[1].Value;

                                // Если ссылка на выселенения не пустая
                                if (evictLink.Length > 0)
                                {
                                    // Выселяем жителя
                                    _ = await HelpMethod.GET($"/?{evictLink}", HttpClient);

                                    // Прибавляем к общему количеству выселенных жителей
                                    human_evict_count++;
                                }
                            }
                            // Если житель со знаком (-) и включена опция "Выселять со знаком (-)"
                            else if (major.Length > 0 & hostel_evict_minus)
                            {
                                // Переходим на страницу жителя
                                result = await HelpMethod.GET(human_url, HttpClient);

                                // Парсим ссылку на выселенения
                                string evictLink = new Regex(@"/\?(.*?)"">Выселить").Match(result).Groups[1].Value;

                                // Если ссылка на выселенения не пустая
                                if (evictLink.Length > 0)
                                {
                                    // Выселяем жителя
                                    _ = await HelpMethod.GET($"/?{evictLink}", HttpClient);

                                    // Прибавляем к общему количеству выселенных жителей
                                    human_evict_count++;
                                }
                            }
                            // Если житель со знаком (+) и включена опция "Выселять со знаком (+)"
                            else if (amount.Length > 0 & hostel_evict_plus)
                            {
                                // Переходим на страницу жителя
                                result = await HelpMethod.GET(human_url, HttpClient);

                                // Парсим ссылку на выселенения
                                string evictLink = new Regex(@"/\?(.*?)"">Выселить").Match(result).Groups[1].Value;

                                // Если ссылка на выселенения не пустая
                                if (evictLink.Length > 0)
                                {
                                    // Выселяем жителя
                                    _ = await HelpMethod.GET($"/?{evictLink}", HttpClient);

                                    // Прибавляем к общему количеству выселенных жителей
                                    human_evict_count++;
                                }
                            }
                        }
                    }

                    // Если выселенных больше 0
                    if (human_evict_count > 0)
                    {
                        HelpMethod.Log($"Выселили жителей: {human_evict_count}", BotID, Form);
                    }
                }
            }
        }

        /// <summary>
        /// Метод, который забирает награды за бизнес турнир.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task BusinessTournament(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Проверяем бизнес турнир
            string result = await HelpMethod.GET("/inspectors", HttpClient);

            // Если можно получить награду
            if (result.Contains("Получить награду"))
            {
                HelpMethod.StatusLog("Получаем награду за бизнес турнир...", BotID, Form, Resources.chart_pie);

                // Парсим ссылку, чтобы забрать награды
                string prize_link = new Regex("<a class=\"btn60 btng\" href=\"(.*?)\">Получить награду!</a>").Match(result).Groups[1].Value.Replace("&amp;", "&");

                // Если ссылка на получение наград не пустая
                if (prize_link.Length > 0)
                {
                    // Забираем награды
                    result = await HelpMethod.GET($"/{prize_link}", HttpClient);

                    // Если кнопка исчезла
                    if (!result.Contains("Получить награду"))
                    {
                        // Логируем
                        HelpMethod.Log($"Получили награду за бизнес турнир.", BotID, Form);
                    }
                }
            }
        }

        /// <summary>
        /// Метод, который нанимает более опытных жителей на работу.
        /// </summary>
        /// <param name="hostel_url">Ссылка на гостиницу.</param>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task HumanJobs(string hostel_url, int BotID, HttpClient HttpClient, MainForm Form)
        {
            // Если ссылка на гостиницу не пустая
            if (hostel_url.Length > 0)
            {
                HelpMethod.StatusLog("Ищем опытных работников...", BotID, Form, Resources.man_plus);

                // Идём в гостиницу
                string result = await HelpMethod.GET(hostel_url, HttpClient);

                // Парсим все HTML-Блоки жителей
                MatchCollection human_list = new Regex("<li>.*?</li>", RegexOptions.Singleline).Matches(result);

                // Переменная для общего хранения устроенных на работу
                int humans_job_found = 0;

                // Перебираем каждого жителя гостиницы
                foreach (object human in human_list)
                {
                    // Если у пользователя есть знак (+)
                    if (human.ToString().Contains("(+)"))
                    {
                        HelpMethod.StatusLog("Пытаемся нанять опытных работников...", BotID, Form, Resources.man_plus);

                        // Парсим ссылку на жителя
                        string human_path = new Regex("(/human/[0-9]*.?/[0-9]*.?/[0-9]*.?/[0-9]*.?)\">").Match(human.ToString()).Groups[1].Value;

                        // Переходим на жителя
                        result = await HelpMethod.GET(human_path, HttpClient);

                        // Парсим ссылку кнопки "Найти работу"
                        string find_job_path = new Regex(@"\.\./\.\./\.\./\.\.(.*?)"">Найти").Match(result).Groups[1].Value;
                        string human_level = new Regex("<strong>([0-9])</strong>").Match(result).Groups[1].Value;

                        // Переходим на поиск работы
                        result = await HelpMethod.GET(find_job_path, HttpClient);

                        // Парсим ссылку кнопки "Устроить на работу"
                        string get_job_path = new Regex(@"<img src=""/images/icons/sml_happy.png"" alt="""" height=""16"" width=""16""/> <a class=""tdu"" href=""\.\./\.\./\.\./\.\./(.*?)"">устроить на работу").Match(result).Groups[1].Value;

                        // Если есть кнопка "Устроить на работу"
                        if (get_job_path.Length > 0)
                        {
                            // Устраиваем на работу
                            _ = await HelpMethod.GET(get_job_path, HttpClient);

                            // Прибавляем к количеству найденых работу жителей
                            humans_job_found++;
                        }
                        else
                        {
                            // Парсим все html-блоки этажей
                            MatchCollection floor_list = new Regex("<li>.*?</li>", RegexOptions.Singleline).Matches(result);

                            // Перебираем этажи
                            foreach (object floor in floor_list)
                            {
                                if (floor.ToString().Contains("Работа мечты, но мест нет"))
                                {
                                    // Парсим ссылку на этаж
                                    string floor_url = new Regex(@"<a class=""flhdr"" href=""\.\./\.\./\.\./\.\.(.*?)"">").Match(result).Groups[1].Value;

                                    // Переходим на этаж
                                    result = await HelpMethod.GET(floor_url, HttpClient);

                                    // Парсим HTML-Блоки жителей этажа
                                    MatchCollection floor_humans_list = new Regex(@"<li class=""\w{2}"">(.*?)</li>", RegexOptions.Singleline).Matches(result);

                                    // Перебираем жителей этажа
                                    foreach (object floor_human in floor_humans_list)
                                    {
                                        // Парсим уровень и ссылку на жителя
                                        string floor_human_level = new Regex(@"<span class=""\w{2}"">([0-9])</span>").Match(floor_human.ToString()).Groups[1].Value;
                                        string floor_human_path = new Regex(@"<a class=""btn"" href=""\.\./\.\.(.*?)"">").Match(floor_human.ToString()).Groups[1].Value;

                                        // Если житель не счастливый
                                        if (!floor_human.ToString().Contains("sml_happy"))
                                        {
                                            // Переходим на жителя
                                            result = await HelpMethod.GET(floor_human_path, HttpClient);

                                            // Парсим ссылку на увольнение
                                            string human_dismiss_path = new Regex(@"<a class=""btnw"" href=""\.\./\.\./\.\./\.\./(.*?)"">Уволить</a>").Match(result).Groups[1].Value;

                                            // Пробуем уволить
                                            result = await HelpMethod.GET($"/{human_dismiss_path}", HttpClient);

                                            // Если уволили
                                            if (result.Contains("Уволена из") || result.Contains("Уволен из"))
                                            {
                                                // Получаем ссылку на этаж с которого был уволен житель
                                                string floor_dismiss_path = new Regex(@"<a class=""\w{2}"" href=""(.*?)""><span>(.*?)</span></a>").Match(result).Groups[1].Value;

                                                // Переходим на этаж с которого уволили
                                                result = await HelpMethod.GET($"/{floor_dismiss_path}", HttpClient);

                                                // Парсим ссылку на поиск нового работника 
                                                string floor_id = new Regex(@"<a class=""btn"" href=""\.\./\.\./humans/floor/(.*?)"">").Match(result).Groups[1].Value;

                                                // Переходим нанимать жителя
                                                result = await HelpMethod.GET($"/humans/floor/{floor_id}", HttpClient);

                                                // Парсим ссылку кнопки "принять на работу", но если она работа мечты
                                                string dream_job_path = new Regex(@"<img src=""/images/icons/sml_happy\.png"" alt="""" height=""16"" width=""16""/> <a class=""tdu"" href=""\.\./\.\./(.*?)"">принять на работу</a>").Match(result).Groups[1].Value;

                                                // Нанимаем
                                                result = await HelpMethod.GET($"/{dream_job_path}", HttpClient);

                                                // Прибавляем к количеству найденых работу жителей
                                                humans_job_found++;

                                                // Выходим из цикла
                                                break;
                                            }
                                            // Если не смогли
                                            else
                                            {
                                                // Выходим из цикла, т.к смысла нет пытаться, т.к скорее всего товар закупается
                                                break;
                                            }
                                        }
                                        // Иначе житель счатливый
                                        else
                                        {
                                            // Если уровень нового жителя больше или равен предыдущего
                                            if (Convert.ToInt32(human_level) > Convert.ToInt32(floor_human_level))
                                            {
                                                // Переходим на жителя
                                                result = await HelpMethod.GET(floor_human_path, HttpClient);

                                                // Парсим ссылку на увольнение
                                                string human_dismiss_path = new Regex(@"<a class=""btnw"" href=""\.\./\.\./\.\./\.\./(.*?)"">Уволить</a>").Match(result).Groups[1].Value;

                                                // Пробуем уволить
                                                result = await HelpMethod.GET($"/{human_dismiss_path}", HttpClient);

                                                // Если уволили
                                                if (result.Contains("Уволена из") || result.Contains("Уволен из"))
                                                {
                                                    // Получаем ссылку на этаж с которого был уволен житель
                                                    string floor_dismiss_path = new Regex(@"<a class=""\w{2}"" href=""(.*?)""><span>(.*?)</span></a>").Match(result).Groups[1].Value;

                                                    // Переходим на этаж с которого уволили
                                                    result = await HelpMethod.GET($"/{floor_dismiss_path}", HttpClient);

                                                    // Парсим ссылку на поиск нового работника 
                                                    string floor_id = new Regex(@"<a class=""btn"" href=""\.\./\.\./humans/floor/(.*?)"">").Match(result).Groups[1].Value;

                                                    // Переходим нанимать жителя
                                                    result = await HelpMethod.GET($"/humans/floor/{floor_id}", HttpClient);

                                                    // Парсим ссылку кнопки "принять на работу", но если она работа мечты
                                                    string dream_job_path = new Regex(@"<img src=""/images/icons/sml_happy\.png"" alt="""" height=""16"" width=""16""/> <a class=""tdu"" href=""\.\./\.\./(.*?)"">принять на работу</a>").Match(result).Groups[1].Value;

                                                    // Нанимаем
                                                    result = await HelpMethod.GET($"/{dream_job_path}", HttpClient);

                                                    // Прибавляем к количеству найденых работу жителей
                                                    humans_job_found++;

                                                    // Выходим из цикла
                                                    break;
                                                }
                                                // Если не смогли
                                                else
                                                {
                                                    // Выходим из цикла, т.к смысла нет пытаться, т.к скорее всего товар закупается
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    // Выходим из этого цикла, т.к этаж нашли
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                if (humans_job_found > 0)
                {
                    HelpMethod.Log($"Наняли новых работников: {humans_job_found}", BotID, Form);
                }
            }
        }

        /// <summary>
        /// Метод, который открывает новый этажи.
        /// </summary>
        /// <param name="Result">Исходный код ответа.</param>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        public static async Task FloorOpen(string Result, int BotID, HttpClient HttpClient, MainForm Form)
        {
            // Общее количество открытых этажей
            int floor_open_count = 0;

            // Запускаем цикл открытией этажей
            do
            {
                // Парсим ссылку на открытие этажа
                string floor_open_url = new Regex("<a class=\"tdu\" href=\"(.*?)\">Открыть этаж!").Match(Result).Groups[1].Value;

                // Если спарсенная ссылка не пустая
                if (floor_open_url.Length > 0)
                {
                    HelpMethod.StatusLog("Открываем новые этажи...", BotID, Form, Resources.st_builded);

                    // Открываем этаж
                    Result = await HelpMethod.GET($"/{floor_open_url}", HttpClient);

                    // Прибавляем общее количество открытых этажей
                    floor_open_count++;
                }
            }
            while (Result.Contains("Открыть этаж!"));

            // Логируем
            HelpMethod.Log($"Открыли этажей: {floor_open_count}", BotID, Form);
        }

        /// <summary>
        /// Метод, который выкупает баксы за монеты.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        /// <returns></returns>
        public static async Task BuyBaksForCoin(int BotID, HttpClient HttpClient, MainForm Form)
        {
            HelpMethod.StatusLog("Анализ ситуации...", BotID, Form, Resources.thinking);

            // Переходим в обменник
            string result = await HelpMethod.GET("/change", HttpClient);

            // Проверяяем возможность обмена
            if (result.Contains("Выкупить за"))
            {
                HelpMethod.StatusLog("Выкупаем баксы за монеты...", BotID, Form, Resources.baks);

                // Парсим ссылку для выкупа
                string url = new Regex("<a class=\"tdu\" href=\"(.*?)\"><span>Выкупить").Match(result).Groups[1].Value;

                // Проверяем успешность парсинга
                if (url.Length > 0)
                {
                    // Переходим по ссылке
                    result = await HelpMethod.GET($"/{url}", HttpClient);

                    // Проверяем есть ли подтверждение
                    if (result.Contains("Подтверждение"))
                    {
                        // Парсим ссылку на подверждение
                        url = new Regex("<a class=\"btng cnfrm\" href=\"(.*?)\">Да").Match(result).Groups[1].Value;

                        // Переходим по ссылки подтверждения
                        result = await HelpMethod.GET($"/{url}", HttpClient);

                        if (result.Contains("Баксов получено"))
                        {
                            HelpMethod.Log("Выкупили все баксы за монеты.", BotID, Form);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Метод, который нанимает в бирже труда жителей со знаком (+).
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        /// <returns></returns>
        public static async Task VendorsHumans(int BotID, HttpClient HttpClient, MainForm Form)
        {
            // Переходим в биржу труда
            string result = await HelpMethod.GET("/vendor/humans", HttpClient);

            // Количество нанятых жителей
            int vendor_humans = 0;

            // Парсим цифру бесплатного количество обновлений
            string update_count = new Regex("осталось ([0-9]) раз").Match(result).Groups[1].Value;

            // Если есть бесплатные попытки обновлений
            if (update_count.Length > 0)
            {
                HelpMethod.StatusLog("Ищем жителей на бирже труда...", BotID, Form);

                // Запускаем цикл
                for (int i = 1; i <= Convert.ToInt32(update_count); i++)
                {
                    // Проходимся по жителям
                    foreach (object item in new Regex("<li>.*?</li>", RegexOptions.Singleline).Matches(result))
                    {
                        string li_humans = item.ToString();

                        // Если у жителя есть знак (+)
                        if (li_humans.Contains("(+)"))
                        {
                            // Парсим ссылку, чтобы нанять жителя
                            string url = new Regex(@"\?(.*?)""><span>Нанять").Match(li_humans).Groups[1].Value;

                            // Нанимаем жителя
                            result = await HelpMethod.GET($"/?{url}", HttpClient);

                            // Подтверждение
                            if (result.Contains("Подтверждение"))
                            {
                                // Парсим ссылку на подтверждение
                                url = new Regex("href=\"(.*?)\">Да").Match(result).Groups[1].Value;

                                // Переходим
                                result = await HelpMethod.GET($"/{url}", HttpClient);

                                if (result.Contains("Новый житель"))
                                {
                                    // Прибавляем количество нанятных жителей
                                    vendor_humans++;

                                    // Переходим обратно в биржу труда
                                    result = await HelpMethod.GET("/vendor/humans", HttpClient);
                                }
                            }
                        }
                    }

                    // Парсим ссылку кнопки "Обновить"
                    string update_url = new Regex("href=\"(.*?)\">Обновить").Match(result).Groups[1].Value;

                    // Переходим
                    result = await HelpMethod.GET($"/{update_url}", HttpClient);
                }

                if (vendor_humans > 0)
                {
                    HelpMethod.Log($"Наняли жителей на бирже труда: {vendor_humans}", BotID, Form);
                }
            }
        }

        /// <summary>
        /// Метод, который собирает награды в осеннем марафоне.
        /// </summary>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        /// <returns></returns>
        public static async Task AutumnMarathon(int BotID, HttpClient HttpClient, MainForm Form)
        {
            // Переменная для хранения ссылки
            string doneTasksUrl;

            // Запускаем цикл
            do
            {
                // Переходим на страницу осеннего марафона
                string result = await HelpMethod.GET("/tasks", HttpClient);

                // Парсим выполненные задания
                doneTasksUrl = new Regex("<a class=\"btng btn60\" href=\"(.*?)\">Получить награду!</a>").Match(result).Groups[1].Value;

                // Если ссылка не пустая
                if(doneTasksUrl.Length > 0)
                {
                    HelpMethod.StatusLog("Получаем награды...", BotID, Form, Resources.leaf_l);

                    // Забираем награду
                    await HelpMethod.GET($"/{doneTasksUrl}", HttpClient);
                }
            }
            while (doneTasksUrl.Length > 0);
        }

        /// <summary>
        /// Метод, который обновляет основную статистику и за сессию.
        /// </summary>
        /// <param name="profile_url">Ссылка на профиль.</param>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="HttpClient">Экземпляр <see cref="HttpClient"/>.</param>
        /// <param name="Form">Экземпляр <see cref="MainForm"/>.</param>
        /// <param name="Settings">Экземпляр <see cref="IniFiles"/>.</param>
        public static async Task Statistics(string profile_url, int BotID, HttpClient HttpClient, MainForm Form, IniFiles Settings)
        {
            HelpMethod.StatusLog("Обновляем статистику...", BotID, Form, Resources.update);

            // Переходим на главную
            string result_home = await HelpMethod.GET("/home", HttpClient);
            string result_profile = await HelpMethod.GET($"/{profile_url}", HttpClient);

            // Парсим данный из профиля
            string coin = new Regex(@"<img src=""/images/icons/mn_iron\.png"" width=""16"" height=""16"" alt=""o""/><span>(.*?)</span>").Match(result_profile).Groups[1].Value;
            string baks = new Regex(@"<img src=""/images/icons/mn_gold\.png"" width=""16"" height=""16"" alt=""\$""/><span>(.*?)</span>").Match(result_profile).Groups[1].Value;
            string floor = new Regex("Этажей: <strong class=\"white\">([0-9].*?)</strong>").Match(result_profile).Groups[1].Value;
            string level = new Regex("Уровень: <strong class=\"white\">([0-9].*?)</strong>").Match(result_profile).Groups[1].Value;
            string avatar = new Regex("/images/icons/user/(.*?).png").Match(result_profile).Groups[1].Value;

            // Парсим данные на главной
            string keys = new Regex(@"<img alt="""" src=""/images/icons/key\.png"" width=""16"" height=""16""/><span>(.*?)</span>").Match(result_home).Groups[1].Value;

            // Получаем ссылку на ToolStrip
            ToolStrip toolstrip_info_top = FindControl.FindToolStrip("toolstrip_info_top", BotID, Form);

            // Коллекция (*TYPE_CURRENT, *TYPE_SESSION, *TYPE_STATUS)
            Dictionary<string, string> coin_dictionary = (Dictionary<string, string>)toolstrip_info_top.Items[1].Tag;
            Dictionary<string, string> baks_dictionary = (Dictionary<string, string>)toolstrip_info_top.Items[2].Tag;
            Dictionary<string, string> keys_dictionary = (Dictionary<string, string>)toolstrip_info_top.Items[3].Tag;

            // Если строка с монетами не пустая
            if (coin.Length > 0)
            {
                // Очищаем строку от бяки
                coin = coin.Replace("&#039;", "");

                // Обновляем текущее количество монет
                FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[4].Text = $"Монет: {HelpMethod.StringNumberFormat(coin)}";
                FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[4].ToolTipText = $"Монет: {HelpMethod.StringNumberFormat(coin, false)}";

                // Высчитываем монеты
                if (HelpMethod.ToBoolean(coin_dictionary["COIN_STATUS"]))
                {
                    coin_dictionary["COIN_SESSION"] = (Convert.ToDouble(coin) - Convert.ToDouble(coin_dictionary["COIN_CURRENT"])).ToString();
                }
                else
                {
                    coin_dictionary["COIN_CURRENT"] = coin;
                    coin_dictionary["COIN_STATUS"] = "true";
                }
            }

            // Если строка с баксами не пустая
            if (baks.Length > 0)
            {
                // Очищаем строку от бяки
                baks = baks.Replace("&#039;", "");

                // Обновляем текущее количество баксов
                FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[3].Text = $"Баксов: {HelpMethod.StringNumberFormat(baks, false)}";

                // Высчитываем баксы
                if (HelpMethod.ToBoolean(baks_dictionary["BAKS_STATUS"]))
                {
                    baks_dictionary["BAKS_SESSION"] = (Convert.ToDouble(baks) - Convert.ToDouble(baks_dictionary["BAKS_CURRENT"])).ToString();
                }
                else
                {
                    baks_dictionary["BAKS_CURRENT"] = baks;
                    baks_dictionary["BAKS_STATUS"] = "true";
                }
            }

            // Если строка с ключами не пустая
            if (keys.Length > 0)
            {
                // Очищаем строку от бяки
                keys = keys.Replace("&#039;", "");

                // Обновляем текущее количество ключей
                FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[2].Text = $"Ключей: {HelpMethod.StringNumberFormat(keys, false)}";

                // Высчитываем баксы
                if (HelpMethod.ToBoolean(keys_dictionary["KEYS_STATUS"]))
                {
                    keys_dictionary["KEYS_SESSION"] = (Convert.ToDouble(keys) - Convert.ToDouble(keys_dictionary["KEYS_CURRENT"])).ToString();
                }
                else
                {
                    keys_dictionary["KEYS_CURRENT"] = keys;
                    keys_dictionary["KEYS_STATUS"] = "true";
                }
            }

            // Обновляем TAG
            toolstrip_info_top.Items[1].Tag = coin_dictionary;
            toolstrip_info_top.Items[2].Tag = baks_dictionary;
            toolstrip_info_top.Items[3].Tag = keys_dictionary;

            // Обновляем текст
            toolstrip_info_top.Items[1].Text = $"{HelpMethod.StringNumberFormat(coin_dictionary["COIN_SESSION"])} собрано";
            toolstrip_info_top.Items[1].ToolTipText = $"{HelpMethod.StringNumberFormat(coin_dictionary["COIN_SESSION"], false)} собрано";
            toolstrip_info_top.Items[2].Text = $"{HelpMethod.StringNumberFormat(baks_dictionary["BAKS_SESSION"], false)} собрано";
            toolstrip_info_top.Items[3].Text = $"{HelpMethod.StringNumberFormat(keys_dictionary["KEYS_SESSION"], false)} собрано";

            // Обновляем логин и количество построенных этажей
            FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[0].Text = $"Уровень: {HelpMethod.StringNumberFormat(level, false)}";
            FindControl.FindToolStrip("toolstrip_info_bottom", BotID, Form).Items[1].Text = $"Этажей: {HelpMethod.StringNumberFormat(floor, false)}";

            // Обновляем аватар
            Form.Invoke((MethodInvoker)delegate
            {
                // Ссылка на вкладку
                TabPage tabPage = FindControl.FindTabPage("tabPage", BotID, Form);

                // Обновляем текст вкладки и картинку
                tabPage.Text = $"{FindControl.FindTextBox("textbox_login", BotID, Form).Text}";
                tabPage.ImageIndex = Form.imageList1.Images.IndexOfKey($"{avatar.Replace("-", "_")}");

                // Сохраняем аватар, уровень, количество этажей, монет, баксов и ключей
                Settings.Write($"USER_{BotID}", "AVATAR", avatar.Replace("-", "_"));
                Settings.Write($"USER_{BotID}", "STAT_LEVEL", level);
                Settings.Write($"USER_{BotID}", "STAT_FLOOR", floor);
                Settings.Write($"USER_{BotID}", "STAT_COIN", coin);
                Settings.Write($"USER_{BotID}", "STAT_BAKS", baks);
                Settings.Write($"USER_{BotID}", "STAT_KEYS", keys);
            });
        }

        /// <summary>
        /// Запускает задачу ожидания.
        /// </summary>
        /// <param name="BotID">Индентификатор бота (вкладки).</param>
        /// <param name="Button">Ссылка на экземпляр класса <see cref="Button"/>.</param>
        /// <param name="Interval">Интервал ожидания, в секундах.</param>
        /// <param name="Form">Ссылка на экземпляр класса <see cref="MainForm"/>.</param>
        public static async Task Sleep(int BotID, Button Button, MainForm Form, int Interval = 60)
        {
            // Инициализируем таймер ожидания
            DateTime taskStop = DateTime.Now.AddSeconds(Interval);

            // Возвращаем доступность кнопки
            Form.Invoke(new Action(() => Button.Enabled = true));

            // Запускам цикл ожидания
            while (true)
            {
                // Получаем текущие время
                DateTime now = DateTime.Now;

                // Если время прошло, выходим из цикла
                if (now.Hour == taskStop.Hour && now.Minute == taskStop.Minute && now.Second == taskStop.Second || Button.Text.Contains(MainForm.BUTTON_TEXT_START))
                {
                    break;
                }

                // Обновляем лог
                HelpMethod.StatusLog($"Повтор через {taskStop.Subtract(now):mm} мин : {taskStop.Subtract(now):ss} сек", BotID, Form, Resources.hd_nebo);

                // Задержка
                await Task.Delay(100);
            }
        }
    }
}
