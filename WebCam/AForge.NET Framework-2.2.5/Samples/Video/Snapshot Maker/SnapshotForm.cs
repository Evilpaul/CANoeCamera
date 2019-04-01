using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace SnapshotMaker
{
    public partial class SnapshotForm : Form
    {
        private static object _syncRoot = new object();

        public SnapshotForm()
        {
            InitializeComponent();
        }

        public void SetImage(Bitmap bitmap)
        {
            timeBox.Text = DateTime.Now.ToLongTimeString();

            lock (_syncRoot)
            {
                Bitmap old = (Bitmap)pictureBox.Image;
                pictureBox.Image = bitmap;

                if (old != null)
                {
                    old.Dispose();
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveFileDialog.FileName);
                ImageFormat format = ImageFormat.Jpeg;

                if (string.Compare(ext, ".bmp", true, CultureInfo.InvariantCulture) == 0)
                {
                    format = ImageFormat.Bmp;
                }
                else if (string.Compare(ext, ".png", true, CultureInfo.InvariantCulture) == 0)
                {
                    format = ImageFormat.Png;
                }

                try
                {
                    lock (_syncRoot)
                    {
                        Bitmap image = (Bitmap)pictureBox.Image;

                        image.Save(saveFileDialog.FileName, format);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed saving the snapshot.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
