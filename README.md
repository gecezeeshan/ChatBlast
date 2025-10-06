WhatsApp Bulk Sender
====================
Author: Zeeshan Ali  
Version: 1.0.0  
Date: October 2025  

Overview
--------
WhatsApp Bulk Sender is a lightweight Windows desktop tool for sending WhatsApp messages to multiple contacts directly via WhatsApp Web.  
It supports sending **text messages**, **images**, **videos**, and **documents** using Chrome automation (Selenium).

No API setup or Meta Business Account is required — you just log in once via QR code.

---

System Requirements
-------------------
• Windows 10 or later  
• Google Chrome (latest version installed)  
• .NET 6 Desktop Runtime (download from: https://dotnet.microsoft.com/en-us/download/dotnet/6.0)  
• Stable internet connection  

---

Setup Instructions
------------------
1. **Extract** the provided ZIP file (WhatsAppBulkSender_App.zip) anywhere on your PC.  
2. Open the folder and **double-click `WhatsAppBulkSender.exe`**.  
3. Wait for Chrome to launch WhatsApp Web.  
4. Scan the **QR code** with your phone (only required once).  
5. After login, you’re ready to send messages.

---

Usage
-----
1. Click **“Load Excel”** and select an Excel file (first sheet, first column = phone numbers).  
   • Numbers can include country code or start with “+” (e.g. +971501234567).  
2. Type your **message** in the text box.  
3. (Optional) Click **“Attach Files”** to add images, videos, or documents.  
4. Set a delay (recommended: 2000–5000 ms for safe sending).  
5. Click **“Send”**.  
6. Monitor the **progress log** below for send results.

---

Features
--------
• ✅ Load phone numbers from Excel  
• ✅ Send text messages  
• ✅ Send images, videos, and files  
• ✅ Supports multiple attachments  
• ✅ Configurable send delay  
• ✅ QR login persists automatically for future sessions  
• ✅ Real-time progress updates and detailed logs  
• ✅ Automatic screenshots when errors occur  

---

Troubleshooting
---------------
If Chrome fails to start:
  → Ensure Google Chrome is installed and updated.  
  → Delete the user profile folder:
     `C:\Users\<YourName>\AppData\Local\WhatsAppBulkSender\ChromeProfile`

If messages don’t send:
  → Check that all numbers are valid and active on WhatsApp.  
  → Ensure QR login is complete.  
  → Increase the delay between messages.  
  → Reopen the app if Chrome stays open in background.

---

Support
-------
Developed by: **Zeeshan Ali**  
Email: [gecezeeshan@gmail.com]  
For code or modification requests, please refer to the included **SourceCode.zip** package.

---

Disclaimer
----------
This tool uses WhatsApp Web automation and is **not an official WhatsApp product**.  
Use responsibly and only send messages to contacts who have consented to receive them.  
Frequent bulk usage may trigger WhatsApp’s temporary restrictions.
