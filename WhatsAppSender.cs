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

        private IWebElement? FindAttachButton()
        {
            var selectors = new[]
            {
        "button[data-testid='attach-menu-plus']",
        "div[data-testid='attach-menu-plus']",
        "button[aria-label='Attach']",
        "div[aria-label='Attach']",
        "button[aria-label='Menu'] svg path[d*='M18']", // newer "+" icon SVG pattern
        "span[data-icon='clip']",
        "button[title='Attach']",
        "div[title='Attach']",
        "div[role='button'][aria-label*='Attach']",
        "div[aria-label='Add attachment']",
        "button[aria-label='Add attachment']",
        "div[data-testid='attach']"
    };

            foreach (var sel in selectors)
            {
                try
                {
                    var el = _driver.FindElements(By.CssSelector(sel)).FirstOrDefault();
                    if (el != null && el.Displayed && el.Enabled)
                    {
                        Console.WriteLine($"[Attach] Found via selector: {sel}");
                        return el;
                    }
                    else
                    {
                        Console.WriteLine($"[Attach] Tried selector: {sel} -> Not found/displayed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Attach] Selector {sel} failed: {ex.Message}");
                }
            }

            // JS-based fallback: detect "+" icon dynamically
            try
            {
                var jsAttach = (IWebElement)((IJavaScriptExecutor)_driver).ExecuteScript(@"
            return [...document.querySelectorAll('button,div')].find(e =>
                e.textContent.trim() === '+' ||
                e.getAttribute('aria-label')?.toLowerCase().includes('attach') ||
                e.getAttribute('title')?.toLowerCase().includes('attach')
            );
        ");
                if (jsAttach != null)
                {
                    Console.WriteLine("[Attach] JS fallback found element.");
                    return jsAttach;
                }
            }
            catch { }

            Console.WriteLine("[Attach] Attach button not found after all selectors.");
            return null;
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




        private void JsLog(string msg)
        {
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript($@"
            if (!window.__wa_debug) {{
                const box = document.createElement('div');
                box.id = '__wa_debug';
                box.style.cssText = `
                    position:fixed;bottom:10px;left:10px;
                    background:rgba(0,0,0,0.7);color:#0f0;
                    font:12px monospace;padding:8px;z-index:999999;
                    max-height:200px;overflow:auto;width:300px;border-radius:4px;
                `;
                document.body.appendChild(box);
                window.__wa_debug = box;
            }}
            const el = window.__wa_debug;
            const line = document.createElement('div');
            line.textContent = '[C#] ' + new Date().toLocaleTimeString() + ' - ' + {System.Text.Json.JsonSerializer.Serialize(msg)};
            el.appendChild(line);
            el.scrollTop = el.scrollHeight;
        ");
            }
            catch { /* no-op */ }
        }

public async Task<bool> SendMessageAsync(string phone, string? message, string[]? filePaths, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(phone)) return false;

    string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
    if (string.IsNullOrEmpty(digitsOnly)) return false;

    JsLog("Opening chat for " + digitsOnly);
    string targetUrl = "https://web.whatsapp.com/send?phone=" + digitsOnly + "&type=phone_number&app_absent=0";
    _driver.Navigate().GoToUrl(targetUrl);

    JsLog("Waiting for composer...");
    bool ready = await WaitUntilAsync(
        () => ElementExists(By.CssSelector("footer div[contenteditable='true']")) || HasInvalidNumberBanner(),
        TimeSpan.FromSeconds(45), ct);

    JsLog("Composer ready = " + ready);
    if (!ready || HasInvalidNumberBanner())
    {
        JsLog("Invalid number or composer not found.");
        return false;
    }

    try
    {
        // ===== CASE 1: Attachments =====
        if (filePaths != null && filePaths.Length > 0)
        {
            Console.WriteLine($"[Attach] Trying to send {filePaths.Length} file(s) to {digitsOnly}.");

            var attachBtn = FindAttachButton();
            if (attachBtn == null)
            {
                Console.WriteLine("[Attach] No attach button found — saving screenshot...");
                TrySaveScreenshot(digitsOnly + "_noAttach");
                return false;
            }

            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", attachBtn);
                attachBtn.Click();
            }
            catch
            {
                Console.WriteLine("[Attach] Normal click failed, trying JS click...");
                try { ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", attachBtn); } catch { }
            }

            await Task.Delay(800, ct);

            var fileInput = FindUsableFileInput();
            if (fileInput == null)
            {
                Console.WriteLine("[Attach] No file input found — saving screenshot...");
                TrySaveScreenshot(digitsOnly + "_noFileInput");
                return false;
            }

            fileInput.SendKeys(string.Join("\n", filePaths));
            Console.WriteLine($"[Attach] Uploaded {filePaths.Length} file(s).");

            // Wait for editor or preview
            bool editorReady = await WaitUntilAsync(
                () => ElementExists(By.CssSelector("div[role='dialog'], div[data-testid='media-editor']")),
                TimeSpan.FromSeconds(10), ct);

            if (!editorReady)
            {
                Console.WriteLine("[Attach] ⚠️ Media editor not detected — saving screenshot.");
                TrySaveScreenshot(digitsOnly + "_noMediaEditor");
            }

            // Optional caption
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

            // === NEW FIX: Synthetic click for modern Send button ===
            string jsClickSend =
                "return (function(){ " +
                "  const sels=['div[role=\"button\"][aria-label=\"Send\"]','button[aria-label=\"Send\"]','[data-testid=\"media-send\"]','span[data-icon=\"wds-ic-send-filled\"]'];" +
                "  function find(root){ " +
                "    for (let sel of sels){ let el=root.querySelector(sel); if(el) return el; }" +
                "    for (let n of root.querySelectorAll('*')){ if(n.shadowRoot){ let f=find(n.shadowRoot); if(f) return f; } }" +
                "    return null;" +
                "  }" +
                "  const el=find(document);" +
                "  if(el){ " +
                "    const rect=el.getBoundingClientRect();" +
                "    const evt=new MouseEvent('click',{bubbles:true,cancelable:true,view:window,clientX:rect.left+5,clientY:rect.top+5});" +
                "    el.dispatchEvent(evt);" +
                "    console.log('✅ Synthetic click fired on Send');" +
                "    return true;" +
                "  }" +
                "  return false;" +
                "})();";

            object ok = ((IJavaScriptExecutor)_driver).ExecuteScript(jsClickSend);
            if (ok is bool clicked && clicked)
            {
                Console.WriteLine("[Attach] ✅ Synthetic click dispatched successfully.");
                await Task.Delay(3000, ct);

                // Confirm send by checking for new outgoing media
                bool sent = await WaitUntilAsync(
                    () => CountOutgoingMessages() > 0,
                    TimeSpan.FromSeconds(5), ct);

                Console.WriteLine(sent ? "[Attach] ✅ Media sent confirmed." : "[Attach] ❌ Media send unconfirmed.");
                return sent;
            }
            else
            {
                Console.WriteLine("[Attach] ❌ Send button not found — saving screenshot.");
                TrySaveScreenshot(digitsOnly + "_noSendButton");
                return false;
            }
        }

        // ===== CASE 2: Text-only =====
        if (!string.IsNullOrWhiteSpace(message))
        {
            JsLog("Sending text-only message...");

            var composer = TryFindComposer();
            if (composer == null)
            {
                JsLog("Composer not found!");
                return false;
            }

            int outgoingBefore = CountOutgoingMessages();
            composer.Click();

            JsLog("Typing message...");
            var normalizedMessage = message.Replace("\r\n", "\n");
            var populated = TryPopulateComposer(composer, normalizedMessage);

            if (!populated || ComposerIsEmpty(composer))
            {
                JsLog("Fallback typing...");
                composer.SendKeys(SeleniumKeys.Control + "a");
                composer.SendKeys(SeleniumKeys.Delete);

                foreach (var line in normalizedMessage.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(line))
                        composer.SendKeys(line);
                    composer.SendKeys(SeleniumKeys.Shift + SeleniumKeys.Enter);
                }
            }

            composer.SendKeys(SeleniumKeys.Enter);
            JsLog("Enter pressed, waiting for confirmation...");

            bool sentText = await WaitUntilAsync(
                () => CountOutgoingMessages() > outgoingBefore || ComposerLooksCleared(),
                TimeSpan.FromSeconds(10), ct);

            JsLog("Text message sent = " + sentText);
            return sentText;
        }

        JsLog("Nothing to send.");
        return false;
    }
    catch (Exception ex)
    {
        JsLog("Exception: " + ex.Message);
        TrySaveScreenshot(digitsOnly + "_error");
        return false;
    }
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
