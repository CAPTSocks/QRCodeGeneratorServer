using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QRCodeGenerator
{
    /// <summary>
    /// Interaction logic for RaceSimulator.xaml
    /// </summary>
    public partial class RaceSimulator : UserControl
    {
        public RaceSimulator()
        {
            InitializeComponent();
        }

        private void StartRaceButton_Click(object sender, RoutedEventArgs e)
        {
            string name = RaceNameBox.Text;
            string heat = (HeatBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string length = DurationBox.Text;
            string unit = (DurationUnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // start the race with name, heat, and length
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // close the popup / window
        }
    }
}
