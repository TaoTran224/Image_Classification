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
        // Khai báo model KNN của OpenCvSharp.ML
        private KNearest knn;
        private SVM svm;
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

        private async void Btn_SelectClassify_Click(object sender, EventArgs e)
        {
            // 1. Chọn thư mục chứa ảnh cần nhận dạng
            //CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.IsFolderPicker = true;
            //if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            //string folderPath = dialog.FileName;
            //string[] files = Directory.GetFiles(folderPath, "*.*")
            //                  .Where(s => s.EndsWith(".PNG") || s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg")).ToArray();

            //if (files.Length == 0)
            //{
            //    MessageBox.Show("Thư mục không có ảnh!");
            //    return;
            //}

            //// 2. Kiểm tra model SVM
            //if (svm == null)
            //{
            //    string modelPath = Path.Combine(Application.StartupPath, "svm_model.yml");
            //    if (File.Exists(modelPath)) svm = SVM.Load(modelPath);
            //    else { MessageBox.Show("Hãy Train SVM trước!"); return; }
            //}

            //Btn_SelectInput.Enabled = false;
            //Lbl_ResultClassify.Text = "Đang quét dữ liệu...";

            //// 3. Chạy xử lý hàng loạt trên Task riêng để không treo giao diện
            //await Task.Run(() =>
            //{
            //    int count = 0;
            //    foreach (string file in files)
            //    {
            //        try
            //        {
            //            using (Mat src = new Mat(file, ImreadModes.Grayscale))
            //            using (Mat binary = new Mat())
            //            {
            //                // Tiền xử lý (Đảm bảo giống lúc Train)
            //                Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            //                using (Mat resized = ResizeAndCenter(binary, 28, 28))
            //                using (Mat floatImg = new Mat())
            //                {
            //                    resized.ConvertTo(floatImg, MatType.CV_32FC1);
            //                    using (Mat reshaped = floatImg.Reshape(1, 1))
            //                    {
            //                        // 1. SVM Dự đoán nhãn (0, 1, 2...)
            //                        float result = svm.Predict(reshaped);

            //                        // 2. Lấy giá trị thô để xác định độ tin cậy (Confidence)
            //                        // StatModel.Flags.RawOutput giúp lấy khoảng cách tới siêu phẳng
            //                        Mat decisionFunc = new Mat();
            //                        svm.Predict(reshaped, decisionFunc, StatModel.Flags.RawOutput);

            //                        // Giá trị confidence: Càng xa 0 càng tin cậy. 
            //                        // Nếu trị tuyệt đối quá nhỏ (< 0.5 chẳng hạn) là ảnh đang nằm ở vùng tranh chấp/lạ.
            //                        double confidence = Math.Abs(decisionFunc.At<double>(0, 0));

            //                        // 3. Đặt ngưỡng xác định ảnh lạ
            //                        // Bạn cần in giá trị này ra Console vài lần để chọn ngưỡng phù hợp (ví dụ 0.8)
            //                        double threshold = 0.1;

            //                        string fileName = Path.GetFileName(file);
            //                        if (confidence < threshold)
            //                        {
            //                            Console.WriteLine($"File: {fileName} --> KẾT QUẢ: KHÔNG XÁC ĐỊNH (Ảnh lạ - Conf: {confidence:F2})");
            //                        }
            //                        else
            //                        {
            //                            Console.WriteLine($"File: {fileName} --> Dự đoán: {result} (Conf: {confidence:F2})");
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch { /* Bỏ qua file lỗi */ }

            //        count++;
            //        this.Invoke(new Action(() => Lbl_ResultClassify.Text = $"Đang xử lý: {count}/{files.Length}"));
            //    }
            //});

            //Btn_SelectInput.Enabled = true;
            //Lbl_ResultClassify.Text = "Hoàn thành nhận dạng hàng loạt!";
            //MessageBox.Show($"Đã xử lý xong {files.Length} ảnh. Kiểm tra kết quả trong cửa sổ Output.");


            // 1. Chọn ảnh từ máy tính
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = ofd.FileName;
                    pictureBoxInput.Image = Image.FromFile(filePath);

                    // 2. Kiểm tra/Nạp model SVM
                    if (svm == null)
                    {
                        string modelPath = Path.Combine(Application.StartupPath, "svm_model.yml");
                        if (File.Exists(modelPath))
                            svm = SVM.Load(modelPath);
                        else
                        {
                            MessageBox.Show("Chưa tìm thấy file svm_model.yml. Hãy chạy Training trước!");
                            return;
                        }
                    }

                    // 3. Tiền xử lý ảnh (Phải khớp hoàn toàn với lúc Train)
                    using (Mat src = new Mat(filePath, ImreadModes.Grayscale))
                    using (Mat binary = new Mat())
                    {
                        // Nhị phân hóa (Dùng BinaryInv để lấy chữ trắng nền đen giống chuẩn)
                        Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                        // Resize và căn giữa về 28x28 (IMAGE_SIZE = 28)
                        using (Mat resized = ResizeAndCenter(binary, 28, 28))
                        using (Mat floatImg = new Mat())
                        {
                            // Chuyển sang Float 32-bit
                            resized.ConvertTo(floatImg, MatType.CV_32FC1);

                            // Trải phẳng thành vector 1 hàng x 784 cột
                            using (Mat reshaped = floatImg.Reshape(1, 1))
                            {
                                // 4. SVM Dự đoán
                                // Khác với KNN, SVM Predict trả về kết quả rất gọn gàng
                                float result = svm.Predict(reshaped);

                                // 5. Hiển thị kết quả lên giao diện
                                Lbl_ResultClassify.Text = "SVM Dự đoán: " + result.ToString();
                                Lbl_ResultClassify.ForeColor = Color.Blue;

                                MessageBox.Show($"Máy tính dự đoán đây là số: {result}", "Kết quả");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi nhận dạng SVM: " + ex.Message);
                }
            }
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

        private Mat ResizeAndCenter(Mat src, int targetWidth, int targetHeight)
        {
            // Tạo một khung đen 28x28 (Canvas)
            // Dùng đầy đủ tên OpenCvSharp.Size để tránh lỗi CS0104
            //Mat result = new Mat(new OpenCvSharp.Size(targetWidth, targetHeight), MatType.CV_8UC1, Scalar.Black);
            Mat result = new Mat(new OpenCvSharp.Size(targetWidth, targetHeight), MatType.CV_8UC1, Scalar.White);
            // Tính tỉ lệ resize để không làm méo đặc trưng của chữ số
            double scale = Math.Min((double)targetWidth / src.Width, (double)targetHeight / src.Height);
            int newW = (int)(src.Width * scale);
            int newH = (int)(src.Height * scale);

            using (Mat resized = new Mat())
            {
                Cv2.Resize(src, resized, new OpenCvSharp.Size(newW, newH));

                // Tính toán tọa độ x, y để đặt ảnh vào tâm của khung 28x28
                int x = (targetWidth - newW) / 2;
                int y = (targetHeight - newH) / 2;

                // Copy ảnh vào vùng chỉ định (ROI)
                Rect roi = new Rect(x, y, newW, newH);
                using (Mat targetRoi = new Mat(result, roi))
                {
                    resized.CopyTo(targetRoi);
                }
            }

            return result;
        }

        private async void Btn_Convert_Click(object sender, EventArgs e)
        {
            Btn_Convert.Enabled = false;
            // pathInput và pathOutput là các biến string chứa đường dẫn thư mục bạn đã chọn
            if (string.IsNullOrEmpty(pathInput) || string.IsNullOrEmpty(pathOutput))
            {
                MessageBox.Show("Vui lòng chọn đầy đủ thư mục đầu vào và đầu ra!", "Thông báo");
                return;
            }
            await Task.Run(() =>
            {
                try
                {
                    // Quét toàn bộ file ảnh, lọc lấy .jpg, .png, .bmp (không phân biệt hoa thường)
                    var files = Directory.GetFiles(pathInput, "*.*", SearchOption.AllDirectories)
                                         .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                     f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                     f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

                    foreach (string file in files)
                    {
                        // Sử dụng 'using' để giải phóng bộ nhớ C++ ngay lập tức sau mỗi ảnh
                        using (Mat src = new Mat(file, ImreadModes.Grayscale))
                        {
                            if (src.Empty()) continue;

                            using (Mat binary = new Mat())
                            {
                                // Nhị phân hóa Otsu: tự động tách chữ trắng trên nền đen
                                //Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                                Cv2.Threshold(src, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                                // Gọi hàm hỗ trợ Resize và căn giữa (padding)
                                using (Mat finalImg = ResizeAndCenter(binary, IMAGE_SIZE, IMAGE_SIZE))
                                {
                                    // Lấy tên thư mục cha làm nhãn (0, 1, 2...)
                                    string label = Path.GetFileName(Path.GetDirectoryName(file));
                                    string saveDir = Path.Combine(pathOutput, label);

                                    if (!Directory.Exists(saveDir))
                                        Directory.CreateDirectory(saveDir);

                                    // Lưu ảnh đã xử lý
                                    string savePath = Path.Combine(saveDir, Path.GetFileName(file));
                                    finalImg.SaveImage(savePath);
                                }
                            }
                        }
                    }
                    this.Invoke(new Action(() => MessageBox.Show("Đã convert xong toàn bộ dữ liệu!", "Thành công")));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => MessageBox.Show("Lỗi xử lý: " + ex.Message)));
                }
            });

            Txt_ConvertStatus.Text = "Sẵn sàng";

            Btn_Convert.Enabled = true;
        }


        private async void Btn_Training_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pathOutput) || !Directory.Exists(pathOutput))
            {
                MessageBox.Show("Vui lòng chọn thư mục Output chứa dữ liệu đã convert!");
                return;
            }

            Btn_Training.Enabled = false;
            Txt_TrainingStatus.Text = "Đang huấn luyện mô hình...";

            await Task.Run(() =>
            {
                try
                {
                    // KHAI BÁO TẠI ĐÂY để đảm bảo logic không bị lỗi context
                    List<float> trainingDataList = new List<float>();
                    List<int> labelsList = new List<int>();

                    string[] subDirs = Directory.GetDirectories(pathOutput);

                    foreach (string dir in subDirs)
                    {
                        string labelStr = Path.GetFileName(dir);
                        if (!int.TryParse(labelStr, out int label)) continue;

                        string[] files = Directory.GetFiles(dir, "*.png");
                        foreach (string file in files)
                        {
                            using (Mat img = new Mat(file, ImreadModes.Grayscale))
                            {
                                if (img.Empty()) continue;
                                using (Mat floatImg = new Mat())
                                {
                                    img.ConvertTo(floatImg, MatType.CV_32FC1);
                                    Mat reshaped = floatImg.Reshape(1, 1);

                                    // Nạp dữ liệu vào List
                                    // Cách sửa lỗi CS1501: Copy trực tiếp dữ liệu từ Mat vào mảng float
                                    float[] pixels = new float[reshaped.Cols];
                                    System.Runtime.InteropServices.Marshal.Copy(reshaped.Data, pixels, 0, pixels.Length);
                                    trainingDataList.AddRange(pixels);

                                    labelsList.Add(label);
                                }
                            }
                        }
                    }

                    if (trainingDataList.Count == 0) return;

                    // Chuyển sang Mat để Train
                    float[] trainDataArr = trainingDataList.ToArray();
                    int[] labelsArr = labelsList.ToArray();

                    using (Mat trainData = Mat.FromPixelData(labelsList.Count, 784, MatType.CV_32FC1, trainDataArr))
                    using (Mat trainLabels = Mat.FromPixelData(labelsList.Count, 1, MatType.CV_32SC1, labelsArr))
                    {
                        if (svm != null) svm.Dispose();
                        svm = SVM.Create();

                        // Cấu hình SVM - Đây là lý do SVM chính xác hơn KNN
                        svm.Type = SVM.Types.CSvc;
                        svm.KernelType = SVM.KernelTypes.Rbf; // Kernel RBF giúp phân loại cực tốt
                        svm.Gamma = 0.01;
                        svm.C = 10;
                        svm.TermCriteria = new TermCriteria(CriteriaTypes.MaxIter, 100, 1e-6);

                        svm.Train(trainData, SampleTypes.RowSample, trainLabels);
                        svm.Save("svm_model.yml");
                    }

                    this.Invoke(new Action(() => MessageBox.Show("Đã huấn luyện xong SVM!")));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() => MessageBox.Show("Lỗi: " + ex.Message)));
                }
            });

            Btn_Training.Enabled = true;
            Txt_TrainingStatus.Text = "Đã lưu Model.";
        }
    }
}
