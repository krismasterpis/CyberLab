using CyberLab.Resources.Libraries;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CyberLab.Resources.Popups
{
    /// <summary>
    /// Logika interakcji dla klasy OsaSettingsPopup.xaml
    /// </summary>
    public partial class OsaSettingsPopup : Window
    {
        private readonly OSA osa;

        public OsaSettingsPopup(OSA _Osa)
        {
            InitializeComponent();
            osa = _Osa;

            ResComboBox.SelectedIndex = Math.Clamp((int)osa.RESOLUTION_SETTING, 0, ResComboBox.Items.Count - 1);

            foreach (ComboBoxItem item in VbwComboBox.Items)
            {
                if (item.Content?.ToString() == ((int)osa.VBW_SETTING).ToString())
                {
                    VbwComboBox.SelectedItem = item;
                    break;
                }
            }

            PaTextBox.Text = osa.POINT_AVERAGE_COUNT.ToString();
            SaTextBox.Text = osa.SWEEP_AVERAGE_COUNT.ToString();
            SmoothComboBox.SelectedValue = osa.SMOOTHING_ENABLED ?? "OFF";

            foreach (ComboBoxItem item in SpComboBox.Items)
            {
                if (item.Content?.ToString() == osa.SAMPLING_POINTS_COUNT.ToString())
                {
                    SpComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void applyButt_Click(object sender, RoutedEventArgs e)
        {
            if (ResComboBox.SelectedIndex >= 0)
            {
                osa.RESOLUTION_SETTING = (OSA.ResolutionSetting)ResComboBox.SelectedIndex;
            }

            if (VbwComboBox.SelectedIndex >= 0)
            {
                var values = Enum.GetValues<OSA.VbwSetting>();
                if (VbwComboBox.SelectedIndex < values.Length)
                {
                    osa.VBW_SETTING = values[VbwComboBox.SelectedIndex];
                }
            }

            if (int.TryParse(PaTextBox.Text, out int paInt)) osa.POINT_AVERAGE_COUNT = paInt;
            if (int.TryParse(SaTextBox.Text, out int saInt)) osa.SWEEP_AVERAGE_COUNT = saInt;
            osa.SMOOTHING_ENABLED = (SmoothComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "OFF";
            if (int.TryParse((SpComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int spInt))
            {
                osa.SAMPLING_POINTS_COUNT = spInt;
            }

            string resStr = osa.RESOLUTION_SETTING switch
            {
                OSA.ResolutionSetting.Res_0_03_nm => "0.03",
                OSA.ResolutionSetting.Res_0_05_nm => "0.05",
                OSA.ResolutionSetting.Res_0_07_nm => "0.07",
                OSA.ResolutionSetting.Res_0_10_nm => "0.1",
                OSA.ResolutionSetting.Res_0_20_nm => "0.2",
                OSA.ResolutionSetting.Res_0_50_nm => "0.5",
                OSA.ResolutionSetting.Res_1_00_nm => "1.0",
                _ => "0.1"
            };
            osa.Write(AnritsuMs9740AScpiCommands.RESOLUTION + " " + resStr);

            string vbwStr = ((int)osa.VBW_SETTING).ToString();
            osa.Write(AnritsuMs9740AScpiCommands.VIDEO_BANDWIDTH + " " + vbwStr);

            if (osa.POINT_AVERAGE_COUNT > 0)
                osa.Write(AnritsuMs9740AScpiCommands.POINT_AVERAGE + " " + osa.POINT_AVERAGE_COUNT);
            else
                osa.Write(AnritsuMs9740AScpiCommands.POINT_AVERAGE + " OFF");

            osa.Write(AnritsuMs9740AScpiCommands.SWEEP_AVERAGE + " " + osa.SWEEP_AVERAGE_COUNT);
            osa.Write(AnritsuMs9740AScpiCommands.SMOOTHING + " " + osa.SMOOTHING_ENABLED);
            osa.Write(AnritsuMs9740AScpiCommands.SAMPLING_POINTS + " " + osa.SAMPLING_POINTS_COUNT);

            DialogResult = true;
            Close();
        }

        private void defaultButt_Click(object sender, RoutedEventArgs e)
        {
            ResComboBox.SelectedIndex = (int)OSA.ResolutionSetting.Res_0_10_nm;
            foreach (ComboBoxItem item in VbwComboBox.Items)
            {
                if (item.Content?.ToString() == "1000")
                {
                    VbwComboBox.SelectedItem = item;
                    break;
                }
            }
            PaTextBox.Text = "0";
            SaTextBox.Text = "1";
            SmoothComboBox.SelectedIndex = 0; // OFF
            foreach (ComboBoxItem item in SpComboBox.Items)
            {
                if (item.Content?.ToString() == "1001")
                {
                    SpComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }
}

