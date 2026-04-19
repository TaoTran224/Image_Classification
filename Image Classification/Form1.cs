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
        }
        // Đây là phương thức xử lý sự kiện mỗi khi camera có khung hình mới
        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            // Nếu đang chụp ảnh tĩnh, thoát ngay để nhường luồng cho nút bấm
            if (isCapturing)
            {
                e.GrabResult?.Dispose();
                return;
            }

            try
            {
                using (IGrabResult result = e.GrabResult)
                {
                    if (result != null && result.GrabSucceeded && isStreaming)
                    {
                        Bitmap bitmap = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppRgb);
                        BitmapData bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        converter.OutputPixelFormat = PixelType.BGRA8packed;
                        converter.Convert(bmd.Scan0, bmd.Stride * bitmap.Height, result);
                        bitmap.UnlockBits(bmd);

                        Pic_Box.BeginInvoke(new MethodInvoker(() =>
                        {
                            Pic_Box.Image?.Dispose();
                            Pic_Box.Image = bitmap;
                        }));
                    }
                }
            }
            catch
            {
                /* Xử lý lỗi ngầm định */
                MessageBox.Show("ERROR Lỗi khởi tạo Camera: ");
            }
        }

        private void InitCamera()
        {
            try
            {
                camera = new Camera();
                camera.Open();

                // Thiết lập chế độ ảnh trắng đen Mono8 ngay từ đầu
                camera.Parameters[PLCamera.PixelFormat].TrySetValue(PLCamera.PixelFormat.Mono8);
                camera.Parameters[PLCamera.Width].SetValue(958);
                camera.Parameters[PLCamera.Height].SetValue(685);
                camera.Parameters[PLCamera.CenterX].TrySetValue(true);
                camera.Parameters[PLCamera.CenterY].TrySetValue(true);

                // Đăng ký sự kiện lấy ảnh
                camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo Camera: " + ex.Message);
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
    }
}
