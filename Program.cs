using System.Text;

namespace WhatsAppBulkSender
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Register legacy encodings (needed for ExcelDataReader)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
