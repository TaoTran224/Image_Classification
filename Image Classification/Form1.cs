using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using System.IO;                    // Để làm việc với thư mục (Directory) và file (Path)
using Emgu.CV;                      // Thư viện gốc OpenCV (CvInvoke, Mat...)
using Emgu.CV.Structure;            // Để dùng Image<Gray, byte> hoặc Bgr
using Emgu.CV.ML;                   // Thư viện Machine Learning (KNearest, TrainData)
using Emgu.CV.CvEnum;               // Các hằng số (Inter.Linear, ThresholdType...)
using System.Drawing.Imaging;       // Để hỗ trợ chuyển đổi Bitmap sang dữ liệu OpenCV
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Image_Classification
{
    public partial class Form1 : Form
    {
        KNearest knn = new KNearest();
        Matrix<float> trainData;
        Matrix<float> trainLabels;
        string modelPath = Application.StartupPath + "\\trained_model.xml";

        string pathInput = "";
        string pathOutput = "";

        FolderBrowserDialog fbd = new FolderBrowserDialog();
        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Btn_SelectTrainingFolder_Click(object sender, EventArgs e)
        {

        }

        private void Btn_Output_Click(object sender, EventArgs e)
        {
            // Khởi tạo Dialog hiện đại
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            // THIẾT LẬP QUAN TRỌNG: 
            // Cho phép chọn thư mục (IsFolderPicker = true)
            dialog.IsFolderPicker = true;

            dialog.Title = "Chọn thư mục lưu hình ảnh pixel";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Lấy đường dẫn thư mục người dùng đã chọn
                string folderPath = dialog.FileName;

                // Hiển thị lên giao diện của bạn (ví dụ label hoặc textbox)
                Txt_DirOutput.Text = folderPath;

                // Lưu vào biến để dùng cho việc convert
                pathOutput = folderPath;
            }
 
        }

        private void Btn_SelectInput_Click(object sender, EventArgs e)
        {
            // Khởi tạo Dialog hiện đại
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            // THIẾT LẬP QUAN TRỌNG: 
            // Cho phép chọn thư mục (IsFolderPicker = true)
            dialog.IsFolderPicker = true;

            dialog.Title = "Chọn thư mục dữ liệu hình ảnh gốc";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Lấy đường dẫn thư mục người dùng đã chọn
                string folderPath = dialog.FileName;

                // Hiển thị lên giao diện của bạn (ví dụ label hoặc textbox)
                Txt_DirInput.Text = folderPath;

                // Lưu vào biến để dùng cho việc convert
                pathInput = folderPath;
            }
        }
    }
}
