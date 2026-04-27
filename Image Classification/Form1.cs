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
using Basler.Pylon;
using System.Threading;

using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace Image_Classification
{
    public partial class Form1 : Form
    {
        private Camera camera = null;
        private PixelDataConverter converter = new PixelDataConverter();
        private bool isStreaming = false;  // Trạng thái nút Live Video
        private bool isCapturing = false;  // Trạng thái khóa để chụp ảnh tĩn

        private Point detectedCircleCenter = new Point(0, 0); // Tâm vòng tròn phát hiện được
        private int detectedCircleRadius = 0;                  // Bán kính phát hiện được
        private bool isCircleDetected = false;                 // Cờ bật/tắt hiển thị vòng tròn đỏ

        private Point realtimeCenter = new Point(0, 0);
        private int realtimeRadius = 0;
        private bool isTracking = false; // Cờ để bật/tắt chế độ tự động theo dõi

        private List<CircleSegment> detectedCircles = new List<CircleSegment>();
        DateTime lastFrameTime = DateTime.Now;

        //basler 958x685
        UInt16 IMAGE_SIZE = 480;
        //private SVM svm;
        //string modelPath = Application.StartupPath + "\\trained_model.xml";

        string pathInput = "";
        string pathOutput = "";
        //string pathPixelData = "";

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            InitCamera();
            Pic_Box.SizeMode = PictureBoxSizeMode.Zoom;
        }
        // Đây là phương thức xử lý sự kiện mỗi khi camera có khung hình mới
        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            if (isCapturing) { e.GrabResult?.Dispose(); return; }

            try
            {
                using (IGrabResult result = e.GrabResult)
                {
                    if (result != null && result.GrabSucceeded)
                    {
                        // 1. Chuyển đổi hiển thị
                        Bitmap bitmap = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppRgb);
                        BitmapData bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        converter.OutputPixelFormat = PixelType.BGRA8packed;
                        converter.Convert(bmd.Scan0, bmd.Stride * bitmap.Height, result);
                        bitmap.UnlockBits(bmd);

                        if (isTracking)
                        {
                            using (Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap))
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(9, 9), 2, 2);

                                // Tìm vòng tròn
                                var circles = Cv2.HoughCircles(gray, HoughModes.Gradient, 1, 80, 100, 25, 10, 400);

                                lock (detectedCircles)
                                {
                                    detectedCircles.Clear();
                                    detectedCircles.AddRange(circles);
                                }

                                // --- ĐOẠN CODE CHỈNH SỬA ĐỂ HIỂN THỊ TOÀN BỘ TỌA ĐỘ ---
                                Tbox_coordinates.BeginInvoke(new MethodInvoker(() => {
                                    if (circles.Length > 0)
                                    {
                                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                                        sb.AppendLine($"--- Đang theo dõi: {circles.Length} vật thể ---");
                                        sb.AppendLine("------------------------------------------");

                                        for (int i = 0; i < circles.Length; i++)
                                        {
                                            float x = circles[i].Center.X;
                                            float y = circles[i].Center.Y;
                                            float r = circles[i].Radius;

                                            // Hiển thị chi tiết: Thứ tự - Tọa độ tâm - Bán kính
                                            // {i+1,2} giúp căn lề số thứ tự thẳng hàng
                                            sb.AppendLine($"Vật thể {i + 1,2}: Tâm[X:{x:F0}, Y:{y:F0}] - Bán kính R:{r:F1}");
                                        }

                                        Tbox_coordinates.Text = sb.ToString();

                                        // Tự động cuộn xuống cuối để xem dữ liệu mới nhất
                                        Tbox_coordinates.SelectionStart = Tbox_coordinates.Text.Length;
                                        Tbox_coordinates.ScrollToCaret();
                                    }
                                    else
                                    {
                                        Tbox_coordinates.Text = "Đang quét hệ thống... Chưa phát hiện vật thể hình tròn.";
                                    }
                                }));
                            }
                        }

                        // 2. Cập nhật UI
                        Pic_Box.BeginInvoke(new MethodInvoker(() => {
                            Pic_Box.Image?.Dispose();
                            Pic_Box.Image = bitmap;
                            Pic_Box.Invalidate(); // Gọi Paint để vẽ toàn bộ vòng tròn
                        }));
                    }
                }
                DateTime now = DateTime.Now;
                double actualFps = 1000.0 / (now - lastFrameTime).TotalMilliseconds;
                lastFrameTime = now;

                // Hiển thị lên một Label khác hoặc TextBox
                this.Invoke(new MethodInvoker(() => {
                    Lbl_ActualFPS.Text = $"Actual FPS: {actualFps:F1}";
                }));
            }
            catch { }
        }

        private void ConfigFPS(double desiredFps)
        {
            try
            {
                if (camera != null && camera.IsOpen)
                {
                    // 1. Phải bật chế độ cho phép điều khiển Frame Rate
                    camera.Parameters[PLCamera.AcquisitionFrameRateEnable].TrySetValue(true);

                    // 2. Lấy giới hạn FPS tối đa mà camera có thể đạt được (phụ thuộc vào Exposure Time)
                    double maxFps = camera.Parameters[PLCamera.AcquisitionFrameRate].GetMaximum();
                    double minFps = camera.Parameters[PLCamera.AcquisitionFrameRate].GetMinimum();

                    // 3. Ràng buộc giá trị nhập vào để tránh lỗi
                    if (desiredFps > maxFps) desiredFps = maxFps;
                    if (desiredFps < minFps) desiredFps = minFps;

                    // 4. Ghi giá trị FPS mới vào camera
                    camera.Parameters[PLCamera.AcquisitionFrameRate].SetValue(desiredFps);

                    // Hiển thị lên màn hình để kiểm tra
                    Console.WriteLine($"FPS hiện tại: {desiredFps} / Max: {maxFps}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể chỉnh FPS: " + ex.Message);
            }
        }

        private void InitCamera()
        {
            try
            {
                camera = new Camera();
                camera.Open();

                if (camera.StreamGrabber.IsGrabbing) camera.StreamGrabber.Stop();

                // 1. Cấu hình Decimation (Nếu bạn vẫn muốn dùng chia 4 chiều dọc)
                camera.Parameters[PLCamera.DecimationHorizontal].TrySetValue(4);
                camera.Parameters[PLCamera.DecimationVertical].TrySetValue(4);

                // 2. Thiết lập độ phân giải tối đa sau khi Decimation
                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

                // 3. CÀI ĐẶT FPS (Giải quyết lỗi không config được)
                // Lưu ý: Phải giảm ExposureTime trước để không bị khóa FPS
                camera.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off);
                camera.Parameters[PLCamera.ExposureTime].TrySetValue(10000.0); // 10ms

                camera.Parameters[PLCamera.AcquisitionFrameRateEnable].TrySetValue(true);
                camera.Parameters[PLCamera.AcquisitionFrameRate].TrySetValue(10.0); // Set 30 FPS

                // 4. Các cấu hình khác
                camera.Parameters[PLCamera.PixelFormat].TrySetValue(PLCamera.PixelFormat.Mono8);
                camera.Parameters[PLCamera.CenterX].TrySetValue(true);
                camera.Parameters[PLCamera.CenterY].TrySetValue(true);

                camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo: " + ex.Message);
            }
        }

        private void Btn_SelectClassify_Click(object sender, EventArgs e)
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

        private Mat ResizeAndCenter(Mat src, int targetWidth, int targetHeight)
        {
            // Tạo một khung đen 28x28 (Canvas)
            // Dùng đầy đủ tên OpenCvSharp.Size để tránh lỗi CS0104
            Mat result = new Mat(new OpenCvSharp.Size(targetWidth, targetHeight), MatType.CV_8UC1, Scalar.Black);
            //Mat result = new Mat(new OpenCvSharp.Size(targetWidth, targetHeight), MatType.CV_8UC1, Scalar.White);
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


        private void Btn_Training_Click(object sender, EventArgs e)
        {

        }

        private void Btn_RecVideo_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isStreaming)
                {
                    isStreaming = true;
                    isCapturing = false;
                    Btn_RecVideo.Text = "Stop Video";
                    if (!camera.StreamGrabber.IsGrabbing)
                        camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                }
                else
                {
                    isStreaming = false;
                    Btn_RecVideo.Text = "Start Video";
                    camera.StreamGrabber.Stop();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Btn_RecImage_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra an toàn để tránh lỗi Null
            if (Pic_Box.Image == null)
            {
                MessageBox.Show("Không có dữ liệu ảnh. Hãy bật Video trước!", "Thông báo");
                return;
            }

            try
            {
                int w = Pic_Box.Image.Width;
                int h = Pic_Box.Image.Height;

                if (MessageBox.Show($"Lưu ảnh Mono8 ({w}x{h})?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Bitmap Image|*.bmp" })
                    {
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            // Dùng lock để tránh việc luồng OnImageGrabbed thay đổi ảnh khi đang copy
                            lock (Pic_Box)
                            {
                                // Bước A: Tạo một Bitmap trung gian từ PictureBox
                                using (Bitmap tempBmp = new Bitmap(Pic_Box.Image))
                                {
                                    // Bước B: Tạo Bitmap 8-bit đích
                                    Bitmap bmp8bit = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

                                    // Bước C: Thiết lập Bảng màu (Sửa lỗi Index was outside the bounds)
                                    ColorPalette pal = bmp8bit.Palette;
                                    if (pal.Entries.Length < 256)
                                    {
                                        // Nếu hệ thống trả về palette thiếu, ta không lưu được
                                        throw new Exception("Hệ thống không khởi tạo đủ bảng màu 256.");
                                    }

                                    for (int i = 0; i < 256; i++)
                                    {
                                        pal.Entries[i] = Color.FromArgb(i, i, i);
                                    }
                                    bmp8bit.Palette = pal; // Gán ngược lại palette đã chỉnh sửa

                                    // Bước D: Copy dữ liệu điểm ảnh (Dùng LockBits để an toàn và nhanh)
                                    BitmapData srcData = tempBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                                    BitmapData destData = bmp8bit.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                                    unsafe
                                    {
                                        byte* pSrc = (byte*)srcData.Scan0;
                                        byte* pDest = (byte*)destData.Scan0;

                                        for (int y = 0; y < h; y++)
                                        {
                                            for (int x = 0; x < w; x++)
                                            {
                                                // Lấy kênh Red (hoặc Green/Blue vì là ảnh xám) làm giá trị 8-bit
                                                // pSrc[0]=B, pSrc[1]=G, pSrc[2]=R, pSrc[3]=A
                                                pDest[x] = pSrc[x * 4 + 2];
                                            }
                                            pSrc += srcData.Stride;
                                            pDest += destData.Stride;
                                        }
                                    }

                                    bmp8bit.UnlockBits(destData);
                                    tempBmp.UnlockBits(srcData);

                                    // Bước E: Lưu file
                                    bmp8bit.Save(sfd.FileName, ImageFormat.Bmp);
                                    bmp8bit.Dispose();
                                }
                            }
                            MessageBox.Show("Lưu thành công! Dung lượng file ~644KB.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu ảnh: " + ex.Message);
            }
        }

        private void Btn_Capture_Click(object sender, EventArgs e)
        {
            if (Pic_Box.Image == null) return;

            // Tạm thời tắt vòng tròn cũ
            isCircleDetected = false;
            Pic_Box.Invalidate();

            try
            {
                // Bước A: Lấy ảnh từ PictureBox
                Bitmap currentImg;
                lock (Pic_Box) { currentImg = new Bitmap(Pic_Box.Image); }

                // Bước B: Chuyển đổi và tiền xử lý ảnh
                Mat src = BitmapConverter.ToMat(currentImg);
                Mat gray = new Mat();

                if (src.Channels() > 1)
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                else
                    gray = src.Clone();

                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(9, 9), 2, 2);

                // Bước C: Thuật toán HoughCircles để tìm hình tròn
                // Tinh chỉnh param2 (30) nếu không tìm thấy, hoặc tăng lên nếu nhận nhầm
                CircleSegment[] circles = Cv2.HoughCircles(
                    gray,
                    HoughModes.Gradient,
                    dp: 1,
                    minDist: 100,
                    param1: 100,
                    param2: 30,
                    minRadius: 10,
                    maxRadius: 400
                );

                // Bước D: Cập nhật kết quả
                if (circles.Length > 0)
                {
                    // Chỉ lấy vật thể hình tròn đầu tiên hoặc rõ nhất
                    var firstCircle = circles[0];

                    // Cập nhật tọa độ vào biến toàn cục (Để sự kiện Paint sử dụng)
                    detectedCircleCenter = new Point((int)firstCircle.Center.X, (int)firstCircle.Center.Y);
                    detectedCircleRadius = (int)firstCircle.Radius;

                    // Bật cờ cho phép vẽ
                    isCircleDetected = true;

                    // HIỂN THỊ THÔNG BÁO (Tọa độ)
                    MessageBox.Show($"Tìm thấy vật thể tại: X={detectedCircleCenter.X}, Y={detectedCircleCenter.Y}", "Thành công");
                }
                else
                {
                    isCircleDetected = false;
                    MessageBox.Show("Không tìm thấy vật thể hình tròn.", "Thông báo");
                }

                // Bước E: Bắt buộc Pic_Box chạy lại sự kiện Paint để vẽ vòng tròn đỏ
                Pic_Box.Invalidate();

                // Giải phóng bộ nhớ
                src.Dispose();
                gray.Dispose();
                currentImg.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void Pic_Box_Paint(object sender, PaintEventArgs e)
        {
            if (isTracking)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Pen pen = new Pen(Color.Red, 3))
                {
                    // Lock để tránh việc OnImageGrabbed thay đổi danh sách khi đang vẽ
                    lock (detectedCircles)
                    {
                        foreach (var circle in detectedCircles)
                        {
                            float centerX = circle.Center.X;
                            float centerY = circle.Center.Y;
                            float radius = circle.Radius;

                            // Vẽ vòng tròn đỏ
                            g.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);

                            // Vẽ tâm xanh nhỏ cho mỗi vật thể
                            g.FillEllipse(Brushes.Blue, centerX - 3, centerY - 3, 6, 6);
                        }
                    }
                }
            }
        }

        private void Btn_AutoTrack_Click(object sender, EventArgs e)
        {
            isTracking = !isTracking; // Đảo trạng thái

            if (isTracking)
            {
                Btn_AutoTrack.Text = "Stop Tracking";
                Btn_AutoTrack.BackColor = Color.LightGreen;
            }
            else
            {
                Btn_AutoTrack.Text = "Start Tracking";
                Btn_AutoTrack.BackColor = SystemColors.Control;
                Tbox_coordinates.Text = "X: 0, Y: 0";
            }
        }

        private void Btn_Detect_Click(object sender, EventArgs e)
        {
            if (camera == null || !camera.StreamGrabber.IsGrabbing)
            {
                MessageBox.Show("Vui lòng bật Camera trước!");
                return;
            }

            try
            {
                // 1. Chụp ảnh hiện tại từ PictureBox
                Bitmap snapshot;
                lock (Pic_Box)
                {
                    if (Pic_Box.Image == null) return;
                    snapshot = new Bitmap(Pic_Box.Image);
                }

                using (Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(snapshot))
                using (Mat gray = new Mat())
                {
                    // 2. Tiền xử lý OpenCV
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(9, 9), 2, 2);

                    // 3. Tìm các vòng tròn
                    var circles = Cv2.HoughCircles(gray, HoughModes.Gradient, 1, 100, 100, 20, 10, 500);

                    // 4. Cập nhật danh sách để vẽ vòng tròn đỏ lên Pic_Box
                    lock (detectedCircles)
                    {
                        detectedCircles.Clear();
                        detectedCircles.AddRange(circles);
                    }
                    Pic_Box.Invalidate(); // Lệnh vẽ lại

                    // 5. Chuẩn bị nội dung cho MessageBox và TextBox
                    StringBuilder sbText = new StringBuilder();
                    StringBuilder sbMsg = new StringBuilder(); // Nội dung riêng cho MessageBox

                    if (circles.Length > 0)
                    {
                        sbMsg.AppendLine($"Đã phát hiện {circles.Length} vật thể:\n");

                        for (int i = 0; i < circles.Length; i++)
                        {
                            float x = circles[i].Center.X;
                            float y = circles[i].Center.Y;
                            float r = circles[i].Radius;

                            // Định dạng chuỗi hiển thị
                            string line = $"Vật thể {i + 1}: Tâm({x:F1}, {y:F1}), Bán kính: {r:F1}";

                            sbText.AppendLine(line);
                            sbMsg.AppendLine(line);
                        }
                    }
                    else
                    {
                        sbMsg.AppendLine("Không tìm thấy vật thể hình tròn nào.");
                        sbText.AppendLine("No circles detected.");
                    }

                    // Hiển thị kết quả vào TextBox
                    Tbox_coordinates.Text = sbText.ToString();

                    // 6. HIỂN THỊ MESSAGEBOX THEO YÊU CẦU
                    // Show thông tin tọa độ và bán kính ngay lập tức
                    MessageBox.Show(sbMsg.ToString(), "Kết quả đo lường", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                snapshot.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
    }
}
