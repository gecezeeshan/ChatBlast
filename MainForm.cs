using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelDataReader;

namespace WhatsAppBulkSender
{
    public partial class MainForm : Form
    {
        private List<string> _numbers = new();
        private CancellationTokenSource? _cts;
        private string[]? _selectedFiles;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Select Excel file (.xlsx or .xls)",
                    Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*"
                };
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                using var stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var result = reader.AsDataSet();

                // Take first sheet, first column as phone numbers
                var table = result.Tables[0];
                var raw = new List<string>();
                foreach (DataRow row in table.Rows)
                {
                    if (row.ItemArray.Length == 0) continue;
                    var val = Convert.ToString(row.ItemArray[0])?.Trim();
                    if (!string.IsNullOrWhiteSpace(val)) raw.Add(val);
                }

                _numbers = NormalizeNumbers(raw);
                lblLoaded.Text = $"Loaded: {_numbers.Count} numbers";
                btnSend.Enabled = _numbers.Count > 0;
                Log($"File loaded. {raw.Count} rows -> {_numbers.Count} valid numbers.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static List<string> NormalizeNumbers(IEnumerable<string> raw)
        {
            // Basic sanitizer: keep digits only; WhatsApp direct links require numbers without '+'
            List<string> cleaned = new();
            foreach (var r in raw)
            {
                var digitsOnly = new string(r.Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(digitsOnly)) continue;
                cleaned.Add(digitsOnly);
            }
            // de-duplicate
            return cleaned.Distinct().ToList();
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (_numbers.Count == 0)
            {
                MessageBox.Show(this, "Please load numbers from Excel first.", "No numbers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMessage.Text) && (_selectedFiles == null || _selectedFiles.Length == 0))
            {
                MessageBox.Show(this, "Please enter a message or select at least one attachment.", "Nothing to send", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSend.Enabled = false;
            btnLoad.Enabled = false;
            btnAttach.Enabled = false;
            btnClearAttachments.Enabled = false;
            btnCancel.Enabled = true;

            _cts = new CancellationTokenSource();
            progressBar.Value = 0;
            progressBar.Maximum = _numbers.Count;
            Log("Launching WhatsApp Web. If prompted, scan the QR code once.");

            try
            {
                using var senderSvc = new WhatsAppSender(chkHeadless.Checked);
                await senderSvc.EnsureLoggedInAsync(_cts.Token);

                int delayMs = (int)numDelayMs.Value;
                string message = txtMessage.Text.Trim();

                int success = 0, fail = 0, i = 0;
                foreach (var phone in _numbers)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    i++;
                    try
                    {
                        bool sent = await senderSvc.SendMessageAsync(phone, message, _selectedFiles, _cts.Token);
                        if (sent)
                        {
                            success++;
                            Log($"[{i}/{_numbers.Count}] [OK] Sent to {phone}");
                        }
                        else
                        {
                            fail++;
                            Log($"[{i}/{_numbers.Count}] [X] Not delivered: {phone}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log("Cancelled by user.");
                        break;
                    }
                    catch (Exception exOne)
                    {
                        fail++;
                        Log($"[{i}/{_numbers.Count}] [!] Failed: {phone} -> {exOne.Message}");
                    }
                    progressBar.Value = Math.Min(progressBar.Maximum, i);
                    await Task.Delay(delayMs, _cts.Token);
                }

                Log($"Done. Success: {success}, Failed: {fail}.");
            }
            catch (OperationCanceledException)
            {
                Log("Operation cancelled.");
            }
            catch (Exception ex)
            {
                Log("Fatal error: " + ex.Message);
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
                btnLoad.Enabled = true;
                btnAttach.Enabled = true;
                btnClearAttachments.Enabled = true;
                btnCancel.Enabled = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select files to attach",
                Filter = "All Files|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                _selectedFiles = ofd.FileNames;
                lblAttachments.Text = $"{_selectedFiles.Length} file(s) selected";
                Log($"Attached {_selectedFiles.Length} file(s).");
            }
            else
            {
                _selectedFiles = null;
                lblAttachments.Text = "No files selected";
            }
        }

        private void btnClearAttachments_Click(object sender, EventArgs e)
        {
            _selectedFiles = null;
            lblAttachments.Text = "No files selected";
            Log("Attachments cleared.");
        }

        private void Log(string msg)
        {
            lstStatus.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            lstStatus.TopIndex = lstStatus.Items.Count - 1;
        }
    }
}
