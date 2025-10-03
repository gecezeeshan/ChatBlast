# WhatsApp Bulk Sender (Demo)

> **Disclaimer**: This tool automates WhatsApp Web through Selenium for educational and personal-use purposes.
> It is **not** an official WhatsApp API. Automating messages may violate WhatsApp's Terms of Service and
> could lead to throttling or account bans. Use responsibly: only message contacts who have consented.
> I am not responsible for any account restrictions or damages.

## What it does
- Windows Forms app that opens WhatsApp Web and sends a single message to many phone numbers
- One-time QR scan to login (your Chrome user profile is persisted so you don't scan again)
- Load phone numbers from the first column of an Excel file (`.xlsx`/`.xls`)
- Configurable delay between sends
- Minimal UI with progress bar and status log
- Robust per-number try/catch and cancellation

## Requirements
- Windows 10/11
- Visual Studio 2022 or newer
- .NET 6 Desktop Runtime / SDK
- Google Chrome installed
- Internet connection (for WhatsApp Web)
- NuGet packages will restore automatically:
  - Selenium.WebDriver
  - Selenium.Support
  - Selenium.WebDriver.ChromeDriver
  - ExcelDataReader
  - ExcelDataReader.DataSet

## Build & Run
1. Open `WhatsAppBulkSender.sln` in Visual Studio.
2. Build -> Rebuild Solution (restores NuGet packages automatically).
3. Run (F5) to start the app.

## Usage
1. Click **Load Excel…** and choose a file with phone numbers in the first column.
   - Numbers should include country code (e.g., `+9715xxxxxxx` or `9715xxxxxxx`).
   - Duplicates are removed automatically.
2. Type or paste your message in the **Message to broadcast** box.
3. (Optional) Adjust **Delay between sends** (default 1200 ms).
4. (Optional) Tick **Run Chrome headless** to run without a visible window (first login requires visible window to scan QR).
5. Click **Send**.
6. On first run, Chrome opens `web.whatsapp.com`. Scan the QR code with your phone.
7. Watch the status log and progress bar update as messages are sent.

## Excel Format
- First sheet, first column is read as phone numbers.
- Any extra columns are ignored.

## Persistence
- The app uses a Chrome user-data directory at:
  `%LOCALAPPDATA%\WhatsAppBulkSender\ChromeProfile`
- This keeps your WhatsApp Web login session so you only scan QR once per machine/profile.

## Tips and Rate Limits
- Add a reasonable delay (e.g., 1200–3000 ms) to reduce the chance of rate limiting.
- Avoid very large batches. Consider splitting into smaller groups.
- Prefer messaging known contacts with consent to avoid being reported.

## Troubleshooting
- **Login timeout**: Make sure the QR is scanned within ~3 minutes.
- **No Chrome**: Install Chrome and try again.
- **Invalid numbers**: Some numbers are not on WhatsApp; these show as warnings.
- **Element changes**: WhatsApp Web changes DOM occasionally. If sending breaks,
  update selectors in `WhatsAppSender.cs`.

## Packaging
You can publish a single-file EXE:
```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained=false
```
This produces an exe under `bin\Release\net6.0-windows\win-x64\publish`.

---

## Security & Ethics
- Use only with contacts who have opted in.
- Comply with local laws and WhatsApp policies."# ChatBlast" 
