using System;
using System.Windows;
using System.Windows.Controls;

namespace QRCodeGenerator
{
    /// <summary>
    /// Handles the display of client messages in the UI.
    /// </summary>
    public class MessageDisplayHandler
    {
        private readonly ListBox _messagesListBox;

        public MessageDisplayHandler(ListBox messagesListBox)
        {
            _messagesListBox = messagesListBox ?? throw new ArgumentNullException(nameof(messagesListBox));
        }

        /// <summary>
        /// Displays a client message in the messages listbox with timestamp and client ID.
        /// </summary>
        public void DisplayMessage(ClientMessageEventArgs e)
        {
            if (e == null)
                return;

            _messagesListBox.Dispatcher.Invoke(() =>
            {
                string displayText = FormatMessage(e);
                _messagesListBox.Items.Add(displayText);

                // Auto-scroll to the latest message
                if (_messagesListBox.Items.Count > 0)
                    _messagesListBox.ScrollIntoView(_messagesListBox.Items[_messagesListBox.Items.Count - 1]);
            });
        }

        /// <summary>
        /// Clears all messages from the listbox.
        /// </summary>
        public void ClearMessages()
        {
            _messagesListBox.Dispatcher.Invoke(() => _messagesListBox.Items.Clear());
        }

        /// <summary>
        /// Formats a client message with timestamp and client ID.
        /// </summary>
        private string FormatMessage(ClientMessageEventArgs e)
        {
            return $"[{e.Timestamp:HH:mm:ss}] {e.ClientId}: {e.Message}";
        }
    }
}
