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
        private int raceTimeMins = 0;
        public RaceSimulator()
        {
            InitializeComponent();
        }

        private void SetupRaceButton_Click(object sender, RoutedEventArgs e)
        {
            RaceStartInfo newRace = SetupRace();
            
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, newRace);

            // Get the selected duration and unit.
            bool isMinuteRace =
                (DurationUnitBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() == "Minutes";

            if (isMinuteRace &&
                int.TryParse(DurationBox.Text, out int durationMinutes) &&
                durationMinutes > 0)
            {

                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToRaceStatus(durationMinutes, newRace);
            }
            else if (!isMinuteRace)
            {
                MessageBox.Show(
                    "The countdown timer currently supports minute-based races.",
                    "Lap-Based Race",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Please enter a valid race duration in minutes.",
                    "Invalid Duration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }


            // start the race with name, heat, and length
        }

        private RaceStartInfo SetupRace()
        {
            RaceStartInfo newRace = new RaceStartInfo();
            DateTime raceStartTime = DateTime.Now;
            newRace.RaceName = RaceNameBox.Text;
            newRace.RaceHeat = (HeatBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            newRace.canPauseRace = AllowPausingCheckBox.IsChecked == true;
            newRace.showFuelBar = IncludeFuelCheckBox.IsChecked == true;
            string raceTimeMins = DurationBox.Text;
            newRace.RaceDate = raceStartTime;
            return newRace; 
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ReturnToMainWindow();
        }
    }
}
