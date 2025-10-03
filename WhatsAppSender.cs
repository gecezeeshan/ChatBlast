using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumKeys = OpenQA.Selenium.Keys;

namespace WhatsAppBulkSender
{
    internal sealed class WhatsAppSender : IDisposable
    {
        private readonly ChromeDriver _driver;
        private readonly WebDriverWait _wait;

        public WhatsAppSender(bool headless)
        {
            var opts = new ChromeOptions();
            var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhatsAppBulkSender", "ChromeProfile");
            opts.AddArgument($"--user-data-dir={userDir.Replace("\\", "/")}");
            if (headless)
            {
                opts.AddArgument("--headless=new");
                opts.AddArgument("--window-size=1200,900");
            }
            opts.AddArgument("--disable-gpu");
            opts.AddArgument("--disable-notifications");
            opts.AddArgument("--no-sandbox");

            _driver = new ChromeDriver(opts);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        }

        public async Task EnsureLoggedInAsync(CancellationToken ct)
        {
            _driver.Navigate().GoToUrl("https://web.whatsapp.com/");
            var loggedIn = false;
            var start = DateTime.UtcNow;

            while (!loggedIn)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    loggedIn = ElementExists(By.XPath("//div[@role='textbox' and @contenteditable='true']"))
                               || ElementExists(By.CssSelector("div[aria-placeholder='Search???']"))
                               || ElementExists(By.CssSelector("div[aria-label='Search input textbox']"));
                }
                catch
                {
                    loggedIn = false;
                }

                if (!loggedIn)
                {
                    await Task.Delay(1000, ct);
                    if ((DateTime.UtcNow - start).TotalMinutes > 3)
                        throw new TimeoutException("Login timeout. Please scan the QR code to continue.");
                }
            }
        }

        public async Task<bool> SendMessageAsync(string phone, string message, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message))
                return false;

            string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitsOnly))
                return false;

            string targetUrl = $"https://web.whatsapp.com/send?phone={digitsOnly}&type=phone_number&app_absent=0";
            _driver.Navigate().GoToUrl(targetUrl);

            if (_driver.Url.Contains("wa.me/"))
            {
                bool continueLinkReady = await WaitUntilAsync(() =>
                    ElementExists(By.CssSelector("a[href^='https://web.whatsapp.com/send']")), TimeSpan.FromSeconds(10), ct);
                if (continueLinkReady)
                {
                    try
                    {
                        var handlesBefore = _driver.WindowHandles.ToList();
                        var continueLink = _driver.FindElement(By.CssSelector("a[href^='https://web.whatsapp.com/send']"));
                        continueLink.Click();
                        await Task.Delay(500, ct);
                        await WaitUntilAsync(() => _driver.Url.Contains("web.whatsapp.com"), TimeSpan.FromSeconds(15), ct);
                        var handlesAfter = _driver.WindowHandles.ToList();
                        if (handlesAfter.Count > handlesBefore.Count)
                            _driver.SwitchTo().Window(handlesAfter.Last());
                    }
                    catch
                    {
                        // ignore; fall back to later checks
                    }
                }
            }

            bool ready = await WaitUntilAsync(() =>
            {
                return ElementExists(By.CssSelector("footer div[contenteditable='true'][data-lexical-editor='true']"))
                       || ElementExists(By.CssSelector("footer [data-testid='conversation-compose-box-input']"))
                       || HasInvalidNumberBanner();
            }, TimeSpan.FromSeconds(45), ct);
            if (!ready)
            {
                TrySaveScreenshot(digitsOnly);
                return false;
            }
            if (HasInvalidNumberBanner())
                return false;

            try
            {
                var composer = TryFindComposer();
                if (composer == null)
                    return false;

                int outgoingBefore = CountOutgoingMessages();

                composer.Click();
                composer.SendKeys(SeleniumKeys.Control + "a");
                composer.SendKeys(SeleniumKeys.Delete);

                var lines = (message ?? string.Empty).Replace("\r\n", "\n").Split('\n');
                for (int idx = 0; idx < lines.Length; idx++)
                {
                    var line = lines[idx];
                    if (!string.IsNullOrEmpty(line))
                        composer.SendKeys(line);
                    if (idx < lines.Length - 1)
                        composer.SendKeys(SeleniumKeys.Shift + SeleniumKeys.Enter);
                }

                composer.SendKeys(SeleniumKeys.Enter);

                bool sent = await WaitUntilAsync(() =>
                {
                    if (CountOutgoingMessages() > outgoingBefore)
                        return true;
                    return ComposerLooksCleared();
                }, TimeSpan.FromSeconds(10), ct);

                if (!sent)
                    TrySaveScreenshot(digitsOnly);
                return sent;
            }
            catch
            {
                return false;
            }
        }

        private bool HasInvalidNumberBanner()
        {
            const string selector = "translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')";
            return ElementExists(By.XPath($"//*[contains({selector},'phone number shared via url is invalid')]"))
                   || ElementExists(By.XPath($"//*[contains({selector},'not a whatsapp user')]"))
                   || ElementExists(By.XPath($"//*[contains({selector},'does not exist on whatsapp')]"));
        }

        private IWebElement? TryFindComposer()
        {
            string[] selectors =
            {
                "footer div[contenteditable='true'][data-lexical-editor='true']",
                "footer [data-testid='conversation-compose-box-input']",
                "footer div[contenteditable='true']"
            };

            foreach (var selector in selectors)
            {
                var element = _driver.FindElements(By.CssSelector(selector)).FirstOrDefault();
                if (element != null)
                    return element;
            }

            return null;
        }

        private bool ComposerLooksCleared()
        {
            try
            {
                var composer = TryFindComposer();
                if (composer == null)
                    return false;

                var text = (composer.GetAttribute("innerText") ?? string.Empty)
                    .Replace("\u200b", string.Empty)
                    .Replace("\u200e", string.Empty)
                    .Replace("\u00a0", string.Empty)
                    .Trim();

                return string.IsNullOrEmpty(text);
            }
            catch
            {
                return false;
            }
        }

        private int CountOutgoingMessages()
        {
            try
            {
                var result = ((IJavaScriptExecutor)_driver).ExecuteScript("return document.querySelectorAll('[data-testid=\"msg-outgoing\"]').length;");
                return Convert.ToInt32(result ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        private bool ElementExists(By by)
        {
            try
            {
                _driver.FindElement(by);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                ct.ThrowIfCancellationRequested();
                if (condition())
                    return true;
                await Task.Delay(300, ct);
            }
            return false;
        }

        private bool WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                if (condition())
                    return true;
                Thread.Sleep(250);
            }
            return false;
        }

        private void TrySaveScreenshot(string phone)
        {
            try
            {
                var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                var path = $"WhatsAppFail_{phone}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                screenshot.SaveAsFile(path);
            }
            catch
            {
                // ignored
            }
        }

        public void Dispose()
        {
            try { _driver.Quit(); } catch { }
            try { _driver.Dispose(); } catch { }
        }
    }
}
