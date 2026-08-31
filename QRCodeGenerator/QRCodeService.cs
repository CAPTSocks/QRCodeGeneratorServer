using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QRCoder;

namespace QRCodeGenerator
{
    /// <summary>
    /// Provides QR code generation services.
    /// </summary>
    public class QRCodeService
    {
        /// <summary>
        /// Generates a QR code image from a payload and returns it as a WPF BitmapSource.
        /// </summary>
        public BitmapSource GenerateQRCode(string payload)
        {
            using var qrGenerator = new global::QRCoder.QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(payload, global::QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new global::QRCoder.QRCode(qrData);
            using Bitmap qrBitmap = qrCode.GetGraphic(20);

            IntPtr hBitmap = qrBitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr value);
    }
}
