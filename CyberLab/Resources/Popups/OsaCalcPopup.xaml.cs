using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberLab.Resources.Popups
{
    /// <summary>
    /// Logika interakcji dla klasy OsaCalcPopup.xaml
    /// </summary>
    public partial class OsaCalcPopup : Window
    {
        private char char1 = '\0';
        private char char2 = '\0';
        private readonly List<Button> topButtons = new();
        private readonly List<Button> botButtons = new();
        public string str = string.Empty;

        public OsaCalcPopup()
        {
            InitializeComponent();
            topButtons.AddRange(new[] { ButtA, ButtB, ButtC, ButtD, ButtE, ButtF, ButtG, ButtH, ButtI, ButtJ });
            botButtons.AddRange(new[] { ButtA2, ButtB2, ButtC2, ButtD2, ButtE2, ButtF2, ButtG2, ButtH2, ButtI2, ButtJ2 });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button butt && butt.Content != null)
            {
                char temp = butt.Content.ToString()![0];
                if (butt.Name.EndsWith("2"))
                {
                    char2 = temp;
                    if (char1 == char2)
                    {
                        char1 = '\0';
                    }
                }
                else
                {
                    char1 = temp;
                    if (char1 == char2)
                    {
                        char2 = '\0';
                    }
                }

                var accentBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                var whiteBrush = new SolidColorBrush(Colors.White);

                foreach (var button in botButtons)
                {
                    button.Foreground = (button.Content?.ToString()?[0] == char2) ? accentBrush : whiteBrush;
                }
                foreach (var button in topButtons)
                {
                    button.Foreground = (button.Content?.ToString()?[0] == char1) ? accentBrush : whiteBrush;
                }

                if (char1 == '\0' && char2 == '\0')
                {
                    textBlock.Text = "Calculation between traces ...";
                }
                else if (char1 == '\0')
                {
                    textBlock.Text = $"Calculation between traces ... and {char2}";
                }
                else if (char2 == '\0')
                {
                    textBlock.Text = $"Calculation between traces {char1} and ...";
                }
                else
                {
                    textBlock.Text = $"Calculation between traces {char1} and {char2}";
                }

                bool isValid = char.IsAsciiLetterUpper(char1) && char.IsAsciiLetterUpper(char2);
                okButt.Foreground = isValid ? accentBrush : whiteBrush;
            }
        }

        private void okButt_Click(object sender, RoutedEventArgs e)
        {
            if (char.IsAsciiLetterUpper(char1) && char.IsAsciiLetterUpper(char2))
            {
                str = $"{char1}-{char2}";
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}

