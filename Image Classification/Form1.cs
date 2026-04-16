using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; 
using OpenCvSharp;
using OpenCvSharp.ML;
using OpenCvSharp.Extensions;
using System.Drawing.Imaging;
using Microsoft.WindowsAPICodePack.Dialogs;

using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace Image_Classification
{
    public partial class Form1 : Form
    {
        UInt16 IMAGE_SIZE = 28;

        string modelPath = Application.StartupPath + "\\trained_model.xml";

        string pathInput = "";
        string pathOutput = "";
        string pathPixelData = "";

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


        private void ProcessImages(string inputDir, string outputDir)
        {
            // Quét tất cả file ảnh (.jpg, .png)
            var files = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                try
                {
                    // 1. Đọc ảnh Grayscale
                    using (Mat src = new Mat(file, ImreadModes.Grayscale))
                    using (Mat binary = new Mat())
                    {
                        // 2. Nhị phân hóa (Chữ trắng trên nền đen)
                        // Dùng Otsu để tự động tìm ngưỡng tối ưu
                        Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                        // 3. Resize và căn giữa (Padding) để thành 28x28
                        using (Mat finalImg = ResizeWithPadding(binary, IMAGE_SIZE, IMAGE_SIZE))
                        {
                            // 4. Lưu vào thư mục tương ứng với nhãn (tên thư mục cha)
                            string label = Path.GetFileName(Path.GetDirectoryName(file));
                            string saveDir = Path.Combine(outputDir, label);

                            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

                            finalImg.SaveImage(Path.Combine(saveDir, Path.GetFileName(file)));
                        }
                    }
                }
                catch { /* Bỏ qua file lỗi hoặc không phải định dạng ảnh */ }
            }
        }

        private Mat ResizeWithPadding(Mat src, int width, int height)
        {
            Mat result = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1, Scalar.Black);

            // Tính toán tỉ lệ để resize mà không làm méo chữ
            double scale = Math.Min((double)width / src.Width, (double)height / src.Height);
            int newW = (int)(src.Width * scale);
            int newH = (int)(src.Height * scale);

            using (Mat resized = new Mat())
            {
                Cv2.Resize(src, resized, new OpenCvSharp.Size(newW, newH));

                // Tính vị trí để đặt ảnh vào giữa khung đen
                int x = (width - newW) / 2;
                int y = (height - newH) / 2;

                // Chép ảnh đã resize vào giữa ma trận kết quả
                Rect roi = new Rect(x, y, newW, newH);
                resized.CopyTo(new Mat(result, roi));
            }

            return result;
        }

        private async void Btn_Convert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pathInput) || string.IsNullOrEmpty(pathOutput))
            {
                MessageBox.Show("Vui lòng chọn đường dẫn Input và Output!");
                return;
            }

            Btn_Convert.Enabled = false;

            await Task.Run(() =>
            {
                try
                {
                    // Lấy tất cả file ảnh, bất kể chữ hoa hay chữ thường
                    var allFiles = Directory.GetFiles(pathInput, "*.*", SearchOption.AllDirectories)
                                            .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                        s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

                    foreach (string file in allFiles)
                    {
                        // Sử dụng khối using để giải phóng tài nguyên C++ ngay lập tức
                        using (Mat src = new Mat(file, ImreadModes.Grayscale))
                        {
                            if (src.Empty()) continue;

                            using (Mat binary = new Mat())
                            {
                                // 1. Nhị phân hóa tự động bằng Otsu
                                Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                                // 2. Resize và thêm lề (Padding) để không làm méo chữ số
                                using (Mat finalImg = ResizeAndCenter(binary, 28, 28))
                                {
                                    // 3. Tạo cấu trúc thư mục đích (0, 1, 2...)
                                    string label = Path.GetFileName(Path.GetDirectoryName(file));
                                    string saveDir = Path.Combine(pathOutput, label);

                                    if (!Directory.Exists(saveDir))
                                        Directory.CreateDirectory(saveDir);

                                    // 4. Lưu ảnh
                                    string savePath = Path.Combine(saveDir, Path.GetFileName(file));
                                    finalImg.SaveImage(savePath);
                                }
                            }
                        }
                        // Ép giải phóng bộ nhớ sau mỗi ảnh để tránh lỗi Type Initializer do tràn RAM
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    this.Invoke(new Action(() => MessageBox.Show("Convert hoàn tất!")));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => MessageBox.Show("Lỗi thực thi: " + ex.Message)));
                }
            });

            btnConvert.Enabled = true;
        }


        private void Btn_Training_Click(object sender, EventArgs e)
        {

        }
    }
}
