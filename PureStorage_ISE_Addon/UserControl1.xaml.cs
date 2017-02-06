using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.PowerShell.Host.ISE;
using PureStorage.Rest;
using Newtonsoft.Json;

namespace PureStorage_ISE_Addon
{
    public class psFlashArrayItem
    {
        public string Name { get; set; }

        public string RESTVersion { get; set; }

        public string Account { get; set; }

        public string PurityVersion { get; set; }

        public string Role { get; set; }
    }

    public class psVolumeItem
    {
        public string Name { get; set; }

        public long Size { get; set; }

        public string Serial { get; set; }

        public int Snapshots { get; set; }
    }

    public class psHostHostGroupItem
    {
        public string Name { get; set; }

        public int Ports { get; set; }

        public string Protocol { get; set; }
    }

    public class psSnapshotsItem
    {
        public string Name { get; set; }

        public string Serial { get; set; }

        public string Source { get; set; }
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class PsISETab : IAddOnToolHostObject
    {

        protected PureRestClient Client;

        public PsISETab()
        {
            InitializeComponent();
        }

        public ObjectModelRoot HostObject { get; set; }
        string protocol = null;
        string currFlashArray = null;

        //private bool HostFilter(object item)
        //{
        //    if (String.IsNullOrEmpty(txtFilterHosts.Text))
        //        return true;
        //    else
        //        return ((item as psHostHGGridView).Name.IndexOf(txtFilterHosts.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        //}

        //private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    CollectionViewSource.GetDefaultView(item.ItemsSource).Refresh();
        //}

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Client = PureRestClient.Create(
                txtUserName.Text, 
                pwdPwd.Password,
                txtFlashArray.Text, 
                "", 
                new PureRestClientOptions {
                    IgnoreCertificateError = true
                });
            HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("# Create connection to the FlashArray at " + txtFlashArray.Text + "\r\n");
            HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("$FlashArray = New-PfaArray -EndPoint " + txtFlashArray.Text + " -Credentials (Get-Credential) -IgnoreCertificateError \r\n\r\n");

            if (lvVolumes.HasItems)
            {
                lvHosts.Items.Clear();
                lvVolumes.Items.Clear();
                lvSnapshots.Items.Clear();
            }


            #region FlashArray Items
            var psFlashArrayGridView = new GridView();
                this.lvFlashArrays.View = psFlashArrayGridView;

                psFlashArrayGridView.Columns.Add(new GridViewColumn {
                    Header = "FlashArray",
                    DisplayMemberBinding = new Binding("Name")
                });

                psFlashArrayGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Purity Ver.",
                    Width = 50,
                    DisplayMemberBinding = new Binding("PurityVersion")
                });

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "REST Ver.",
                Width = 50,
                DisplayMemberBinding = new Binding("RESTVersion")
                });

                psFlashArrayGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Username",
                    DisplayMemberBinding = new Binding("Account")
                });

                psFlashArrayGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Role",
                    DisplayMemberBinding = new Binding("Role")
                });

            this.lvFlashArrays.Items.Add(new psFlashArrayItem { Name = Client.Array.GetName(), PurityVersion = Client.Array.GetAttributes().Version, RESTVersion = Client.RestApiVersion, Account = Client.Username, Role = Client.Role.ToString() });
                this.lvFlashArrays.View = psFlashArrayGridView;
            #endregion

            #region Snapshots Items
                var psSnapshotGridView = new GridView();
                this.lvSnapshots.View = psSnapshotGridView;

                psSnapshotGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Name",
                    DisplayMemberBinding = new Binding("Name")
                });

                psSnapshotGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Source",
                    DisplayMemberBinding = new Binding("Source")
                });

                psSnapshotGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Serial #",
                    DisplayMemberBinding = new Binding("Serial")
                });
            #endregion

            #region Volumes Items
                var psVolumeGridView = new GridView();
                this.lvVolumes.View = psVolumeGridView;

                psVolumeGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Name",
                    DisplayMemberBinding = new Binding("Name")
                });

                psVolumeGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Size (GB)",
                    DisplayMemberBinding = new Binding("Size")
                });

                psVolumeGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Snapshots",
                    DisplayMemberBinding = new Binding("Snapshots")
                });

                psVolumeGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Serial #",
                    DisplayMemberBinding = new Binding("Serial")
                });

                var psVolumes = Client.Volumes.List();
                foreach (var volumes in psVolumes)
                {
                    this.lvVolumes.Items.Add(new psVolumeItem { Name = volumes.Name, Size = (volumes.Size), Snapshots = Client.Volumes.GetSnapshots(volumes.Name).Count, Serial = volumes.Serial });
                }
                this.lvVolumes.View = psVolumeGridView;
            #endregion

            #region Host & Host Group Items
                GridView psHostHGGridView = new GridView();
                this.lvHosts.View = psHostHGGridView;

                psHostHGGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Name",
                    DisplayMemberBinding = new Binding("Name")
                });

                psHostHGGridView.Columns.Add(new GridViewColumn
                {
                    Header = "Protocol",
                    DisplayMemberBinding = new Binding("Protocol")
                });

                psHostHGGridView.Columns.Add(new GridViewColumn
                {
                    Header = "# Ports",
                    DisplayMemberBinding = new Binding("Ports")
                });

                var psHosts = Client.Hosts.List();
                foreach (var hosts in psHosts)
                {
                    if (hosts.WwnList.Count == 0)
                    {
                        protocol = "iSCSI";
                    }
                    else
                    {
                        protocol = "FC";
                    }

                this.lvHosts.Items.Add(new psHostHostGroupItem { Name = hosts.Name , Protocol = protocol, Ports = hosts.WwnList.Count});
                    //this.lvHosts.
                }
                this.lvHosts.View = psHostHGGridView;
            #endregion

            currFlashArray = txtFlashArray.Text;
            txtUserName.Clear();
            pwdPwd.Clear();
            txtFlashArray.Clear();
        }

        protected void DoDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //var track = ((ListViewItem)sender).Content as Track; //Casting back to the binded Track
        }

        private void lvVolumes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lvHosts.Items.Count > 0)
            {
                psVolumeItem volSnapshots = (psVolumeItem)lvVolumes.SelectedItems[0];
                var psSnapshots = Client.Volumes.GetSnapshots(volSnapshots.Name);

                foreach (var snapshots in psSnapshots)
                {
                    this.lvSnapshots.Items.Add(new psSnapshotsItem { Name = snapshots.Name, Source = snapshots.Source, Serial = snapshots.Serial });
                }
            }
        }

        //private void lvVolumes_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    psVolumeItem volume = (psVolumeItem)lvVolumes.SelectedItems[0];

        //    HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("# Create snapshot of " + volume.Name + "\r\n");
        //    HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("New-PfaVolume -Array '" + currFlashArray + "' -Source '" + volume.Name + "' -VolumeName '<NEW_VOLUME_NAME>' \r\n\r\n");

        //}


        private void lvFlashArrays_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region FlashArray Items
            var psFlashArrayGridView = new GridView();
            this.lvFlashArrays.View = psFlashArrayGridView;

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "FlashArray",
                DisplayMemberBinding = new Binding("Name")
            });

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "Purity Ver.",
                DisplayMemberBinding = new Binding("PurityVersion")
            });

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "REST Ver.",
                DisplayMemberBinding = new Binding("RESTVersion")
            });

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "Username",
                DisplayMemberBinding = new Binding("Account")
            });

            psFlashArrayGridView.Columns.Add(new GridViewColumn
            {
                Header = "Role",
                DisplayMemberBinding = new Binding("Role")
            });

            this.lvFlashArrays.Items.Add(new psFlashArrayItem { Name = Client.Array.GetName(), PurityVersion = Client.Array.GetAttributes().Version, RESTVersion = Client.RestApiVersion, Account = Client.Username, Role = Client.Role.ToString() });
            this.lvFlashArrays.View = psFlashArrayGridView;
            #endregion

            #region Snapshots Items
            var psSnapshotGridView = new GridView();
            this.lvSnapshots.View = psSnapshotGridView;

            psSnapshotGridView.Columns.Add(new GridViewColumn
            {
                Header = "Name",
                DisplayMemberBinding = new Binding("Name")
            });

            psSnapshotGridView.Columns.Add(new GridViewColumn
            {
                Header = "Source",
                DisplayMemberBinding = new Binding("Source")
            });

            psSnapshotGridView.Columns.Add(new GridViewColumn
            {
                Header = "Serial #",
                DisplayMemberBinding = new Binding("Serial")
            });
            #endregion

            #region Volumes Items
            var psVolumeGridView = new GridView();
            this.lvVolumes.View = psVolumeGridView;

            psVolumeGridView.Columns.Add(new GridViewColumn
            {
                Header = "Name",
                DisplayMemberBinding = new Binding("Name")
            });

            psVolumeGridView.Columns.Add(new GridViewColumn
            {
                Header = "Size (GB)",
                DisplayMemberBinding = new Binding("Size")
            });

            psVolumeGridView.Columns.Add(new GridViewColumn
            {
                Header = "Snapshots",
                DisplayMemberBinding = new Binding("Snapshots")
            });

            psVolumeGridView.Columns.Add(new GridViewColumn
            {
                Header = "Serial #",
                DisplayMemberBinding = new Binding("Serial")
            });

            var psVolumes = Client.Volumes.List();
            foreach (var volumes in psVolumes)
            {
                this.lvVolumes.Items.Add(new psVolumeItem { Name = volumes.Name, Size = (volumes.Size), Snapshots = Client.Volumes.GetSnapshots(volumes.Name).Count, Serial = volumes.Serial });
            }
            this.lvVolumes.View = psVolumeGridView;
            #endregion

            #region Host & Host Group Items
            GridView psHostHGGridView = new GridView();
            this.lvHosts.View = psHostHGGridView;

            psHostHGGridView.Columns.Add(new GridViewColumn
            {
                Header = "Name",
                DisplayMemberBinding = new Binding("Name")
            });

            psHostHGGridView.Columns.Add(new GridViewColumn
            {
                Header = "Protocol",
                DisplayMemberBinding = new Binding("Protocol")
            });

            psHostHGGridView.Columns.Add(new GridViewColumn
            {
                Header = "# Ports",
                DisplayMemberBinding = new Binding("Ports")
            });

            var psHosts = Client.Hosts.List();
            foreach (var hosts in psHosts)
            {
                if (hosts.WwnList.Count == 0)
                {
                    protocol = "iSCSI";
                }
                else
                {
                    protocol = "FC";
                }

                this.lvHosts.Items.Add(new psHostHostGroupItem { Name = hosts.Name, Protocol = protocol, Ports = hosts.WwnList.Count });
                //this.lvHosts.
            }
            this.lvHosts.View = psHostHGGridView;
            #endregion
        }

        private void lvSnapshots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvHosts.Items.Count > 0)
            {

                psSnapshotsItem snapshot = (psSnapshotsItem)lvSnapshots.SelectedItems[0];

                HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("# Create new volume from snapshot of " + snapshot.Name + "\r\n");
                HostObject.CurrentPowerShellTab.Files.SelectedFile.Editor.InsertText("New-PfaVolume -Array '" + currFlashArray + "' -Source '" + snapshot.Name + "' -VolumeName '<NEW_VOLUME_NAME>' \r\n\r\n");
            }
        }
    }
}
