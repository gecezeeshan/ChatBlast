using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumKeys = OpenQA.Selenium.Keys;
using System.Diagnostics;
namespace WhatsAppBulkSender
{
    internal sealed class WhatsAppSender : IDisposable
    {
        private readonly ChromeDriver _driver;
        private readonly WebDriverWait _wait;

        public WhatsAppSender(bool headless)
        {
            var opts = new ChromeOptions();
            opts.PageLoadStrategy = PageLoadStrategy.Eager;

            // ✅ Stability options to prevent ChromeDriver crash
            opts.AddArgument("--remote-debugging-port=9222");
            opts.AddArgument("--no-sandbox");
            opts.AddArgument("--disable-dev-shm-usage");
            opts.AddArgument("--disable-gpu");
            opts.AddArgument("--disable-notifications");
            opts.AddArgument("--disable-extensions");
            opts.AddArgument("--disable-blink-features=AutomationControlled");
            opts.AddArgument("--start-maximized");

            // ✅ Use persistent profile to keep WhatsApp logged in
            var userDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WhatsAppBulkSender", "ChromeProfile"
            );
            opts.AddArgument($"--user-data-dir={userDir.Replace("\\", "/")}");

            // ✅ Headless mode (optional)
            if (headless)
            {
                opts.AddArgument("--headless=new");
                opts.AddArgument("--window-size=1200,900");
            }

            // ✅ Launch ChromeDriver with the improved config
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
                               || ElementExists(By.CssSelector("div[aria-placeholder='Search…']"))
                               || ElementExists(By.CssSelector("div[aria-label='Search input textbox']"));
                }
                catch { loggedIn = false; }

                if (!loggedIn)
                {
                    await Task.Delay(1000, ct);
                    if ((DateTime.UtcNow - start).TotalMinutes > 3)
                        throw new TimeoutException("Login timeout. Please scan the QR code to continue.");
                }
            }
        }

        public async Task<bool> SendMessageAsync(string phone, string? message, string[]? filePaths, CancellationToken ct)
        {
            // 1️⃣ Basic input validation
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitsOnly)) return false;

            Console.WriteLine($"[Init] Opening chat for {digitsOnly}");

            // 2️⃣ Open WhatsApp chat
            string targetUrl = $"https://web.whatsapp.com/send?phone={digitsOnly}&type=phone_number&app_absent=0";
            _driver.Navigate().GoToUrl(targetUrl);

            // 3️⃣ Wait for chat to load
            bool ready = await WaitUntilAsync(
                () => ElementExists(By.CssSelector("footer div[contenteditable='true']")) || HasInvalidNumberBanner(),
                TimeSpan.FromSeconds(10), ct);

            if (!ready || HasInvalidNumberBanner())
            {
                Console.WriteLine("[Init] Invalid number or chat not ready.");
                return false;
            }

            try
            {
                // 4️⃣ If there are attachments
                if (filePaths != null && filePaths.Length > 0)
                {
                    foreach (var file in filePaths)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();

                        // Decide the WhatsApp menu type by extension
                        string attachType = ext switch
                        {
                            ".jpg" or ".jpeg" or ".png" or ".gif" or ".mp4" or ".mov" => "image",
                            ".pdf" or ".docx" or ".xlsx" or ".txt" or ".csv" or ".zip" => "document",
                            ".mp3" or ".aac" or ".wav" or ".ogg" or ".m4a" => "audio",
                            ".vcf" => "contact",
                            ".webp" => "sticker",
                            _ => "document"
                        };

                        Console.WriteLine($"[Attach] Sending {file} as {attachType}");

                        // 🔹 4.1 Click paperclip → select the proper submenu
                        var input = await ClickAttachMenuAsync(attachType, ct);
                        if (input == null)
                        {
                            Console.WriteLine($"[Attach] ❌ Could not open submenu for {attachType}");
                            TrySaveScreenshot($"{digitsOnly}_noAttachInput_{attachType}");
                            continue;
                        }

                        // 🔹 4.2 Upload file(s)
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", input);
                        input.SendKeys(file);
                        Console.WriteLine($"[Attach] File uploaded: {file}");

                        // 🔹 4.3 Wait for preview or editor to load
                        await WaitForMediaEditorAsync(ct);

                        // Optional caption if user provided message
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            var captionBox = FindCaptionBox();
                            if (captionBox != null)
                            {
                                captionBox.Click();
                                captionBox.SendKeys(message);
                                Console.WriteLine("[Attach] Caption added.");
                            }
                        }

                        // 🔹 4.4 Click Send button
                        if (await ClickSendButtonAsync(ct))
                        {
                            Console.WriteLine($"[Attach] ✅ Sent {Path.GetFileName(file)} successfully.");
                            await Task.Delay(1500, ct); // wait between messages
                        }
                        else
                        {
                            Console.WriteLine($"[Attach] ❌ Failed to send {file}");
                            TrySaveScreenshot($"{digitsOnly}_sendFail_{attachType}");
                        }
                    }

                    return true;
                }

                // 5️⃣ Text-only message flow
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine("[Text] Sending text message...");

                    var composer = TryFindComposer();
                    if (composer == null)
                    {
                        Console.WriteLine("[Text] ❌ No composer found!");
                        return false;
                    }

                    int outgoingBefore = CountOutgoingMessages();
                    composer.Click();

                    // Type and send
                    composer.SendKeys(message);
                    composer.SendKeys(SeleniumKeys.Enter);

                    bool sent = await WaitUntilAsync(
                        () => CountOutgoingMessages() > outgoingBefore || ComposerLooksCleared(),
                        TimeSpan.FromSeconds(3), ct);

                    Console.WriteLine("[Text] Sent = " + sent);
                    return sent;
                }

                Console.WriteLine("[Info] Nothing to send.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error] " + ex.Message);
                TrySaveScreenshot($"{digitsOnly}_exception");
                return false;
            }
        }


        /// <summary>
        /// Opens the WhatsApp attach menu (paperclip) and selects the right upload input.
        /// Works for image, document, audio, contact, or sticker.
        /// </summary>
        private async Task<IWebElement?> ClickAttachMenuAsync(string type, CancellationToken ct)
        {
            Console.WriteLine($"[AttachMenu] Opening attach menu for type: {type}");

            // Step 1️⃣: Click the main paperclip button
            var attachBtn = _driver.FindElements(By.CssSelector("button[title='Attach'], div[aria-label='Attach']")).FirstOrDefault();
            if (attachBtn == null)
            {
                Console.WriteLine("[AttachMenu] ❌ Paperclip button not found!");
                TrySaveScreenshot("noAttachBtn");
                return null;
            }

            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", attachBtn);
            await Task.Delay(600, ct);

            // Step 2️⃣: Find the correct input[type=file] based on type
            string[] iconHints = type.ToLower() switch
            {
                "document" => new[] { "document-filled-refreshed", "Document" },
                "image" or "video" => new[] { "media-filled-refreshed", "Photos", "videos" },
                "audio" => new[] { "ic-headphones-filled", "Audio" },
                "contact" => new[] { "person-filled-refreshed", "Contact" },
                "sticker" => new[] { "sticker-create-filled-refreshed", "Sticker" },
                _ => Array.Empty<string>()
            };

            // Step 3️⃣: Look for any visible <input type="file"> that matches these hints
            var allInputs = _driver.FindElements(By.CssSelector("li input[type='file']")).ToList();
            Console.WriteLine($"[AttachMenu] Found {allInputs.Count} file inputs in dropdown.");

            foreach (var input in allInputs)
            {
                try
                {
                    var liParent = input.FindElement(By.XPath("ancestor::li"));
                    string html = liParent.GetAttribute("innerHTML") ?? "";

                    if (iconHints.Any(h => html.Contains(h, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"[AttachMenu] ✅ Matched '{type}' via hint: {iconHints.First(h => html.Contains(h, StringComparison.OrdinalIgnoreCase))}");
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", input);
                        return input; // return input element directly so caller can sendKeys()
                    }
                }
                catch { /* ignore dead nodes */ }
            }

            Console.WriteLine($"[AttachMenu] ❌ No matching input found for type '{type}'");
            TrySaveScreenshot($"attachMenuFail_{type}_{DateTime.Now:HHmmss}");
            return null;
        }


        // 🔹 Wait until the media editor (preview window) appears
        private async Task WaitForMediaEditorAsync(CancellationToken ct)
        {
            await WaitUntilAsync(() =>
                ElementExists(By.CssSelector("div[data-testid='media-preview-container']")) ||
                ElementExists(By.CssSelector("div[data-testid='media-editor']")) ||
                ElementExists(By.CssSelector("div[role='dialog'][data-animate-modal-body='true']")),
                TimeSpan.FromSeconds(3), ct);
        }

        // 🔹 Click the “Send” button — works even if it’s inside Shadow DOM
        private async Task<bool> ClickSendButtonAsync(CancellationToken ct)
        {
            // 🟢 Look for both <button> and <div role="button"> variants of Send
            var sendBtn = _driver.FindElements(By.CssSelector(
                "button[aria-label='Send'], " +
                "div[aria-label='Send'][role='button'], " +
                "button[data-testid='media-send'], " +
                "span[data-icon='send'], " +
                "span[data-icon='wds-ic-send-filled'], " +
                "svg[title='wds-ic-send-filled']"))
                .LastOrDefault(el => el.Displayed && el.Enabled);

            if (sendBtn != null)
            {
                try
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", sendBtn);
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", sendBtn);
                    Console.WriteLine("[SendButton] ✅ Clicked via JS");
                    await Task.Delay(1000, ct);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SendButton] ⚠️ Click failed: " + ex.Message);
                    TrySaveScreenshot("send_click_fail");
                }
            }
            else
            {
                Console.WriteLine("[SendButton] ❌ Not found, trying deep shadow search...");

                // 🧩 Fallback: JS probe into shadow DOM (modern WhatsApp uses nested shadows)
                string jsShadowClick = @"
        const search = root => {
            const sel = ['button[aria-label=""Send""]', 'div[aria-label=""Send""][role=""button""]', 'span[data-icon=""wds-ic-send-filled""]'];
            for (const s of sel) {
                const el = root.querySelector(s);
                if (el) { el.click(); return true; }
            }
            for (const el of root.querySelectorAll('*')) {
                if (el.shadowRoot && search(el.shadowRoot)) return true;
            }
            return false;
        };
        return search(document);
    ";
                object ok = ((IJavaScriptExecutor)_driver).ExecuteScript(jsShadowClick);
                if (ok is bool && (bool)ok)
                {
                    Console.WriteLine("[SendButton] ✅ Clicked via shadow DOM fallback");
                    await Task.Delay(1000, ct);
                    return true;
                }
            }

            Console.WriteLine("[SendButton] ❌ Send button not found after all attempts");
            TrySaveScreenshot("no_send_button");
            return false;
        }



        private IWebElement? FindUsableFileInput()
        {
            // Prefer visible + enabled inputs; otherwise pick the last one WA injected.
            var inputs = _driver.FindElements(By.CssSelector("input[type='file']"));
            if (inputs.Count == 0) return null;

            // First pass: visible & enabled
            foreach (var inp in inputs)
            {
                try
                {
                    if (inp.Displayed && inp.Enabled) return inp;
                }
                catch { /* ignore stale */ }
            }
            // Fallback: last one (WA often adds the active input last)
            return inputs.LastOrDefault();
        }

        private IWebElement? FindCaptionBox()
        {
            // Common caption editors in media composer
            var selectors = new[]
            {
                "div[role='dialog'] div[contenteditable='true']",
                "div[data-testid='media-preview-container'] div[contenteditable='true']",
                "div[data-testid='media-editor'] div[contenteditable='true']"
            };
            foreach (var sel in selectors)
            {
                var el = _driver.FindElements(By.CssSelector(sel)).LastOrDefault();
                if (el != null && el.Displayed) return el;
            }
            return null;
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
                if (element != null) return element;
            }
            return null;
        }

        private bool TryPopulateComposer(IWebElement composer, string normalizedMessage)
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript(@"const el = arguments[0];
const text = arguments[1] ?? '';
el.focus();
const selection = window.getSelection();
selection.removeAllRanges();
const range = document.createRange();
range.selectNodeContents(el);
selection.addRange(range);
selection.deleteFromDocument();
if (text.length > 0) {
    document.execCommand('insertText', false, text);
} else {
    el.textContent = '';
}
el.dispatchEvent(new InputEvent('input', { bubbles: true }));
selection.removeAllRanges();", composer, normalizedMessage);
                return true;
            }
            catch { return false; }
        }

        private static string ReadComposerText(IWebElement composer)
        {
            return (composer.GetAttribute("innerText") ?? string.Empty)
                .Replace("\u200b", string.Empty)
                .Replace("\u200e", string.Empty)
                .Replace("\u00a0", string.Empty)
                .Replace("\r\n", "\n");
        }

        private static bool ComposerIsEmpty(IWebElement composer)
        {
            return string.IsNullOrEmpty(ReadComposerText(composer).Trim());
        }

        private bool ComposerLooksCleared()
        {
            try
            {
                var composer = TryFindComposer();
                if (composer == null) return false;
                return ComposerIsEmpty(composer);
            }
            catch { return false; }
        }

        private int CountOutgoingMessages()
        {
            try
            {
                var result = ((IJavaScriptExecutor)_driver)
                    .ExecuteScript("return document.querySelectorAll('[data-testid=\"msg-outgoing\"]').length;");
                return Convert.ToInt32(result ?? 0);
            }
            catch { return 0; }
        }

        private bool ElementExists(By by)
        {
            try { _driver.FindElement(by); return true; }
            catch { return false; }
        }

        private async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                ct.ThrowIfCancellationRequested();
                if (condition()) return true;
                await Task.Delay(300, ct);
            }
            return false;
        }

        private bool WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                if (condition()) return true;
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
            catch { /* ignored */ }
        }

        public void Dispose()
        {
            try { _driver.Quit(); } catch { }
            try { _driver.Dispose(); } catch { }
        }
    }
}
