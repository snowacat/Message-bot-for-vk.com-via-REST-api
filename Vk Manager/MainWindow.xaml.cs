using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace Vk_Manager
{
    public partial class MainWindow : Window
    {
        private const Int32 APP_ID = /*APPLICATION ID HERE*/;
        private const Int32 DELAY_KEEPER = 2000;
        private const Int32 LIMIT_MESSAGE_LETTERS = 100;
        
        private DateTime startTime;
        private Boolean run = false;
        private vk_api api;
        private String sidCaptcha = null;
        private String accessToken;

        private Thread threadKeeper;
        private Thread threadTimer;
        
        private Int32 scope = (int)(VkontakteScopeList.friends | VkontakteScopeList.messages  | VkontakteScopeList.notify | VkontakteScopeList.wall);
        private Int32 countReceive = 0;
        private Int32 countSend = 0;
        private Int32 userId = 0;

        public MainWindow()
        {
            InitializeComponent();
            webBrowser.Navigate(String.Format("http://api.vkontakte.ru/oauth/authorize?client_id={0}&scope={1}&display=popup&response_type=token", APP_ID, scope));
        }

        #region Permissions
        private enum VkontakteScopeList
        {
            notify = 1,
            friends = 2,
            photos = 4,
            audio = 8,
            video = 16,
            offers = 32,
            questions = 64,
            pages = 128,
            link = 256,
            notes = 2048,
            messages = 4096,
            wall = 8192,
            docs = 131072
        }
        #endregion

        #region Styles
        private void imgClose_MouseLeave(object sender, MouseEventArgs e)
        {
            imgClose.Opacity = 1;
        }

        private void imgClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            if (threadKeeper != null)
            {
                threadKeeper.Abort();
                threadTimer.Abort();
            }
        }

        private void imgClose_MouseMove(object sender, MouseEventArgs e)
        {
            imgClose.Opacity = 0.8;
        }

        private void Window_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void startImg_MouseMove(object sender, MouseEventArgs e)
        {
            startImg.Opacity = 0.8;
        }

        private void startImg_MouseLeave(object sender, MouseEventArgs e)
        {
            startImg.Opacity = 1;
        }

        private void stopImg_MouseMove(object sender, MouseEventArgs e)
        {
            stopImg.Opacity = 0.8;
        }

        private void stopImg_MouseLeave(object sender, MouseEventArgs e)
        {
            stopImg.Opacity = 1;
        }

        private void startImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Start();
        }

        #endregion

        private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (e.Uri.ToString().IndexOf("access_token") != -1)
            {
                Regex myReg = new Regex(@"(?<name>[\w\d\x5f]+)=(?<value>[^\x26\s]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in myReg.Matches(e.Uri.ToString()))
                {
                    if (m.Groups["name"].Value == "access_token")
                    {
                        accessToken = m.Groups["value"].Value;
                    }
                    else if (m.Groups["name"].Value == "user_id")
                    {
                        userId = Convert.ToInt32(m.Groups["value"].Value);
                    }
                }
            }
            if (accessToken != null)
            {
                firstGrid.Visibility = Visibility.Collapsed;
                secondGrid.Visibility = Visibility.Visible;
                threadKeeper = new Thread(RunKeeper);
                threadKeeper.Start();
                threadTimer = new Thread(RunTimer);
                threadTimer.Start();
            }
        }

        public void RunKeeper()
        {
            api = new vk_api(accessToken);
            Random random = new Random();

            while (true)
            {
                while (run == true)
                {
                    try
                    {
                        XmlDocument messages = api.GetNewMessages();
                        foreach (XmlNode node in messages.SelectNodes("response/message/body"))
                        {
                            countReceive++;
                            Dispatcher.Invoke(new Action(() =>
                            {
                                lblReseive.Content = "Receive: " + countReceive;
                                rtbHistory.AppendText("\nVK: " + node.InnerText);
                            }));
                        }

                        foreach (XmlNode node in messages.SelectNodes("response/message/uid"))
                        {
                            Int32 messageNumber = random.Next(0, 2);
                            String currentText = null;
                            
                            Dispatcher.Invoke(new Action(() =>
                            {
                                currentText = txbInput.Text;
                            }));

                            switch (messageNumber)
                            {
                                case 1:
                                    currentText = currentText + "\nI'm out with " + startTime.ToString();
                                    break;
                                case 2:
                                    currentText = currentText + "\nI'm out with " + startTime.ToString() + "\nCeep calm!";
                                    break;
                                default:
                                    currentText = currentText + "\nI'm out with " + startTime.ToString() + "\nBe happy!";
                                    break;
                            }

                            try
                            {
                                XmlDocument er = api.SendMessage(Convert.ToInt32(node.InnerText), currentText);
                                foreach (XmlNode sid_captcha in er.SelectNodes("error/captcha_sid"))
                                {
                                    sidCaptcha = sid_captcha.InnerText;
                                }
                                foreach (XmlNode link_captcha in er.SelectNodes("error/captcha_img"))
                                {
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        var source = new BitmapImage();
                                        source.BeginInit();
                                        source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                        source.CacheOption = BitmapCacheOption.OnLoad;
                                        source.UriSource = new Uri(link_captcha.InnerText);
                                        source.EndInit();
                                        imgCaptcha.Source = source;
                                        startImg.Visibility = Visibility.Visible;
                                        stopImg.Visibility = Visibility.Collapsed;
                                        imgCaptcha.Visibility = Visibility.Visible;
                                        txbCaptcha.Visibility = Visibility.Visible;
                                    }));

                                    run = false;
                                    userId = Convert.ToInt32(node.InnerText);
                                }

                                // Counters and history
                                if (sidCaptcha == null)
                                {
                                    countSend++;
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        lblSend.Content = "Send: " + countSend;
                                        rtbHistory.AppendText("\nYou: " + txbInput.Text);
                                    }));
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                    Thread.Sleep(DELAY_KEEPER);
                }
            }
        }

        private void txbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            txbInput.MaxLength = LIMIT_MESSAGE_LETTERS;
            txbInput.MaxLines = 1;

            if (txbInput.Text.Length == txbInput.MaxLength)
            {
                lblStatus.Content = "Limit letters has reached: " + txbInput.Text.Length.ToString() + "/" + txbInput.MaxLength;
            }
            else
            {
                if (txbInput.Text.Length == 0)
                {
                    lblStatus.Content = "";
                }
                else
                {
                    lblStatus.Content = "Limit letters: " + txbInput.Text.Length.ToString() + "/" + txbInput.MaxLength;
                }
            }

        }

        private void stopImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startImg.Visibility = Visibility.Visible;
            stopImg.Visibility = Visibility.Collapsed;
            run = false;
        }

        private void Start()
        {
            if (sidCaptcha == null)
            {
                startImg.Visibility = Visibility.Collapsed;
                stopImg.Visibility = Visibility.Visible;
                startTime = DateTime.Now;
                run = true;
            }
            else
            {
                if (txbCaptcha.Text != "")
                {
                    imgCaptcha.Visibility = Visibility.Collapsed;
                    txbCaptcha.Visibility = Visibility.Collapsed;
                    try
                    {
                        api.SendMessageWithCaptcha(userId, txbInput.Text + "\nI'm out with " + startTime.ToString(), txbCaptcha.Text, sidCaptcha);
                        startImg.Visibility = Visibility.Collapsed;
                        stopImg.Visibility = Visibility.Visible;
                        startTime = DateTime.Now;
                        sidCaptcha = null;
                        countSend++;
                        lblSend.Content = "Send messages: " + countSend;
                        run = true;
                    }
                    catch
                    {
                        lblStatus.Content = "Please, repeat.";
                    }
                }
                else
                {
                    lblStatus.Content = "Please, input captcha.";
                }
            }
        }

        private void RunTimer()
        {
            Int32 minutes = 0;
            Int32 seconds = 0;

            while (true)
            {
                while (run == true)
                {
                    if (seconds == 60)
                    {
                        minutes++;
                        seconds = 0;
                    }
                    try
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            if (seconds < 10)
                            {
                                if (minutes < 10)
                                {
                                    lblTimer.Content = "0" + minutes + ":0" + seconds;
                                }
                                else
                                {
                                    lblTimer.Content = minutes + ":0" + seconds;
                                }
                            }
                            else
                            {
                                if (minutes < 10)
                                {
                                    lblTimer.Content = "0" + minutes + ":" + seconds;
                                }
                                else
                                {
                                    lblTimer.Content = minutes + ":" + seconds;
                                }
                          
                            } 
                        }));
                    }
                    catch
                    {
                    }
                    Thread.Sleep(1000);
                    seconds++;
                }
                Thread.Sleep(1000);
            }
        }
    }
}
