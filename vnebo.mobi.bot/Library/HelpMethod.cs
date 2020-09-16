using Microsoft.Win32;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vnebo.mobi.bot.Libs
{
    internal class HelpMethod
    {
        #region DLL IMPORT
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, uint wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        #endregion

        /// <summary>
        /// Инициализированная переменная класса <see cref="Random"/>.
        /// </summary>
        public static readonly Random getRandomNumber = new Random();

        /// <summary>
        /// Создаёт задачу которая, будет выполнена после случайной задержки.
        /// </summary>
        /// <param name="Minimum">Минимальное число задержки.</param>
        /// <param name="Maximum">Максимальное число задержки.</param>
        /// <returns>Задача, представляющая случайную временную задержку.</returns>
        public static async Task RandomDelay(int Minimum, int Maximum)
        {
            await Task.Delay(getRandomNumber.Next(Minimum, Maximum + 1));
        }

        /// <summary>
        /// Отправляет GET запрос на указанный URL-адрес.
        /// </summary>
        /// <param name="Url">Ссылка на страницу.</param>
        /// <param name="HttpClient">Ссылка на класс <see cref="HttpClient"/>.</param>
        /// <returns>Вернёт исходный код страницы.</returns>
        public static async Task<string> GET(string Url, HttpClient HttpClient)
        {
            string result;

            try
            {
                // Задержка
                await RandomDelay(300, 800);

                // Запускаем цикл
                do
                {
                    // Отправляем запрос
                    result = await HttpClient.GetAsync(Url).Result.Content.ReadAsStringAsync();

                    // Если в ответ получили "Слишком быстро"
                    if (result.Contains("Слишком быстро"))
                    {
                        // Перед повторной отправкой, делаем небольшую задержку
                        await RandomDelay(300, 800);
                    }
                }
                while (result.Contains("Слишком быстро"));
            }
            catch (Exception)
            {
                return "";
            }

            return result;
        }

        /// <summary>
        /// Устанавливает placeholder для текстовых полей.
        /// </summary>
        /// <param name="TextBox">Ссылка на экземпляр класса <see cref="TextBox"/>.</param>
        /// <param name="PlaceholderText">Текст placeholder.</param>
        public static void SetPlaceholder(TextBox TextBox, string PlaceholderText)
        {
            SendMessage(TextBox.Handle, 0x1500 + 1, 0, PlaceholderText);
        }

        /// <summary>
        /// Устанавливает размер последней вкладки <see cref="TabControl"/> минимального размера.
        /// </summary>
        /// <param name="TabControl">Ссылка на экземпляр <see cref="TabControl"/>.</param>
        public static void TabControlSmallWidth(TabControl TabControl)
        {
            TabControl.HandleCreated += (s, e) =>
            {
                _ = SendMessage(TabControl.Handle, 0x1300 + 49, IntPtr.Zero, (IntPtr)10);
            };
        }

        /// <summary>
        /// Добавляет приложение в автозагрузку Windows.
        /// </summary>
        /// <param name="Flag">True - Добавляет, False - Убирает</param>
        public static void AutoRun(bool Flag)
        {
            // Полный путь к файлу
            string fileFullPath = Application.ExecutablePath;
            // Получаем информацию об файле
            FileInfo fileInfo = new FileInfo(fileFullPath);
            // Получаем имя файла
            string fileName = fileInfo.Name.Replace(".exe", "");
            // Открываем ветку реестра
            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");

            try
            {
                if (Flag)
                {
                    registryKey.SetValue(fileName, fileFullPath);
                }
                else
                {
                    registryKey.DeleteValue(fileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Произошла ошибка, автозапуск невозможен.");
            }

            // Закрываем ветку реестра
            registryKey.Close();
        }

        /// <summary>
        /// Форматирует цифровую строку в красивый вид.
        /// </summary>
        /// <param name="Number">Число строкой.</param>
        /// <param name="Format_type">Укорачивает цифровую строку, true - 1.11k, false - 100,000</param>
        /// <returns></returns>
        public static string StringNumberFormat(string Number, bool Format_type = true)
        {
            if(Number.Length > 0)
            {
                // Если нужно укорачивать строку
                if (Format_type)
                {
                    // Создаём временные переменные
                    string number_text = "";
                    double number_double = Convert.ToDouble(Number);

                    // Если число меньше 1 000, то просто возвращаем.
                    if (number_double < 1000)
                    {
                        number_text = Number.ToString();
                    }
                    // Если число меньше 1 000 000 (тысячи), то возвращаем в конце букву "k"
                    else if (number_double < 1000000d)
                    {
                        number_text = (number_double / 1000d).ToString("#.##k");
                    }
                    // Если число меньше 1 000 000 000 (миллионы), то возвращаем в конце букву "m"
                    else if (number_double < 1000000000d)
                    {
                        number_text = (number_double / 1000000d).ToString("#.##m");
                    }
                    // Если число меньше 1 000 000 000 000 (миллиарды), то возвращаем в конце букву "g"
                    else if (number_double < 1000000000000d)
                    {
                        number_text = (number_double / 1000000000d).ToString("#.##g");
                    }
                    // Если число меньше 1 000 000 000 000 000 (триллионы), то возвращаем в конце букву "t"
                    else if (number_double < 1000000000000000d)
                    {
                        number_text = (number_double / 1000000000000d).ToString("#.##t");
                    }
                    else
                    {
                        number_text = (number_double / 1000000000000000d).ToString("#.##p");
                    }

                    return number_text.Trim();
                }

                return Convert.ToDouble(Number).ToString("#,##0", new CultureInfo("en-US")).Replace(" ", "");
            }

            return "0";
        }

        /// <summary>
        /// Метод, который конвертирует строку в логическое выражение.
        /// </summary>
        /// <param name="boolean">Строка логического типа.</param>
        /// <returns>Если строка ровна <see cref="true"/> (регистр неважен), вернется <see cref="true"/>, в остальных случаев вернется <see cref="false"/>.</returns>
        public static bool ToBoolean(string boolean)
        {
            return boolean.ToLower().Contains("true");
        }

        /// <summary>
        /// Отправляет строку в <see cref="RichTextBox"/> с поддержкой цвета и скрытие времени.
        /// </summary>
        /// <param name="Text">Строка.</param>
        /// <param name="BotID">Идентификатор бота (вкладки).</param>
        /// <param name="Form">Ссылка на <see cref="Form1"/>.</param>
        /// <param name="Color">Цвет текста.</param>
        /// <param name="ShowTime">True - Показывать время, False - Не показывать время.</param>
        public static void Log(string Text, int BotID, MainForm Form, Color Color = new Color(), bool ShowTime = true)
        {
            Form.Invoke((MethodInvoker)delegate
            {
                RichTextBox logs = FindControl.FindRichTextBox("richtextbox_log", BotID, Form);

                logs.SelectionColor = SystemColors.ControlDarkDark;
                logs.AppendText($" {(ShowTime ? $"[ { DateTime.Now:dd.MM.yyyy HH:mm:ss} ]" : "")}");
                logs.SelectionColor = Color;
                logs.AppendText($" {(Text.Length > 0 ? "--" : "")} {Text} {Environment.NewLine}");
                logs.ScrollToCaret();
            });
        }

        /// <summary>
        /// Отправляет строку в <see cref="ToolStrip"/> с поддержкой <see cref="Image"/>.
        /// </summary>
        /// <param name="Text">Строка.</param>
        /// <param name="BotID">Индентификатор бота (вкладки).</param>
        /// <param name="Form1">Ссылка на <see cref="Form1"/>.</param>
        /// <param name="Image">Картинка.</param>
        public static void StatusLog(string Text, int BotID, MainForm Form1, Image Image = null)
        {
            //
            //
            // ПЕРЕДЕЛАТЬ ДАННЫЙ МЕТОД!!!!
            // ПЕРЕДЕЛАТЬ ДАННЫЙ МЕТОД!!!!
            //
            //
            Form1.Invoke((MethodInvoker)delegate
            {
                FindControl.FindToolStrip("toolstrip_info_top", BotID, Form1).Items[0].Text = Text;
                FindControl.FindToolStrip("toolstrip_info_top", BotID, Form1).Items[0].Image = Image;
            });
        }
    }
}
