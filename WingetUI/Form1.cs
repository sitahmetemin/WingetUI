using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace WingetUI
{
    public partial class WingetUI : Form
    {
        List<string> appList = new List<string>();
        List<string> appNameAndVersionList = new List<string>();
        List<string> selectedAppList = new List<string>();

        public WingetUI() => InitializeComponent();

        private void AppLoad(object sender, EventArgs e) => GetAllAppList();

        private void btnCloseClick(object sender, EventArgs e) => this.Close();

        private void GetAllAppList()
        {
            var response = RunCommandWithResponse("winget search");

            Encoding Utf8 = Encoding.UTF8;
            byte[] utf8Bytes = Utf8.GetBytes(response);
            string goodDecode = Utf8.GetString(utf8Bytes);
            var replaced = Regex.Replace(goodDecode, " {2,}", " ");
            var splited = replaced.Split(Environment.NewLine).Skip(2).ToList();
            var removedHeader = splited.Skip(2).ToList();

            appList.AddRange(removedHeader);

            foreach (var item in appList)
            {
                if (!item.Contains("."))
                    continue;

                var spli = item.Split(" ");
                string appName = spli.First(q => q.Contains("."));
                string appVersion = spli.Last(q => q.Contains("."));
                appNameAndVersionList.Add($"{appName}|{appVersion}");
            }

            checkedListBoxApps.Items.AddRange(appNameAndVersionList.ToArray());
        }

        private async Task RunCommand(string command, bool dontCreateWindow = true)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    FileName = "powershell.exe",
                    Arguments = command,
                    CreateNoWindow = dontCreateWindow,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            proc.Start();
            Cursor.Current = Cursors.WaitCursor;

            richTextBoxLog.Clear();

            while (!proc.StandardOutput.EndOfStream)
            {
                richTextBoxLog.AppendText(await proc.StandardOutput.ReadLineAsync());
            }

            if (proc.StandardOutput.EndOfStream)
                MessageBox.Show("Success!");
            else
                MessageBox.Show("Fail!");
        }

        private string RunCommandWithResponse(string command)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    FileName = "powershell.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            StringBuilder stringBuilder = new StringBuilder();
            proc.Start();

            while (!proc.StandardOutput.EndOfStream)
                stringBuilder.AppendLine(proc.StandardOutput.ReadToEnd());

            return stringBuilder.ToString();
        }

        private void txtSearchEvent(object sender, KeyEventArgs e)
        {
            checkedListBoxApps.Items.Clear();
            var founded = appNameAndVersionList.Where(q => q.ToLower().Contains(txtSearch.Text.ToLower().ToString())).ToArray();
            checkedListBoxApps.Items.AddRange(founded);

            for (int count = 0; count < checkedListBoxApps.Items.Count; count++)
            {
                if (selectedAppList.Contains(checkedListBoxApps.Items[count].ToString()))
                {
                    checkedListBoxApps.SetItemChecked(count, true);
                }
            }
        }

        private void itemChecked(object sender, ItemCheckEventArgs e)
        {
            if (checkedListBoxApps.SelectedItem != null)
            {
                string selectedItem = checkedListBoxApps.SelectedItem.ToString();

                if (e.NewValue == CheckState.Checked)
                {
                    if (!selectedAppList.Any(q => q.Contains(selectedItem)))
                        selectedAppList.Add(selectedItem);
                }
                else if (e.NewValue == CheckState.Unchecked)
                {
                    if (selectedAppList.Any(q => q.Contains(selectedItem)))
                        selectedAppList.Remove(selectedItem);
                }
                else if (checkedListBoxApps.CheckedItems == null)
                    selectedAppList.Clear();
            }

        }

        private async void btnInstallEvent(object sender, EventArgs e)
        {
            if (selectedAppList.Count != 0)
            {
                DialogResult dialogResult = MessageBox.Show(string.Join(Environment.NewLine, selectedAppList), "Listedeki paketleri yüklemek istediðinizden emin misiniz?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                    await Install();
            }
            else
                MessageBox.Show("You must select at least one application.");
        }

        private async Task Install()
        {
            StringBuilder stringBuilder = new();

            foreach (var item in selectedAppList)
                stringBuilder.AppendLine($"winget install --id={item.Split("|").First()} -e; ");

            await RunCommand(stringBuilder.ToString());
        }
    }
}