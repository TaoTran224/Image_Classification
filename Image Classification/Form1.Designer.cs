
namespace Image_Classification
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Btn_SelectImage = new System.Windows.Forms.Button();
            this.Classify = new System.Windows.Forms.TabPage();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.Training = new System.Windows.Forms.TabPage();
            this.Txt_TrainingStatus = new System.Windows.Forms.TextBox();
            this.Txt_ConvertStatus = new System.Windows.Forms.TextBox();
            this.Txt_DirOutput = new System.Windows.Forms.TextBox();
            this.Txt_DirInput = new System.Windows.Forms.TextBox();
            this.Btn_Output = new System.Windows.Forms.Button();
            this.Btn_SelectInput = new System.Windows.Forms.Button();
            this.Btn_Convert = new System.Windows.Forms.Button();
            this.Btn_SelectClassify = new System.Windows.Forms.Button();
            this.Btn_Training = new System.Windows.Forms.Button();
            this.pictureBoxInput = new System.Windows.Forms.PictureBox();
            this.Lbl_ResultClassify = new System.Windows.Forms.Label();
            this.Classify.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.Training.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInput)).BeginInit();
            this.SuspendLayout();
            // 
            // Btn_SelectImage
            // 
            this.Btn_SelectImage.Location = new System.Drawing.Point(6, 49);
            this.Btn_SelectImage.Name = "Btn_SelectImage";
            this.Btn_SelectImage.Size = new System.Drawing.Size(85, 23);
            this.Btn_SelectImage.TabIndex = 0;
            this.Btn_SelectImage.Text = "Select Image";
            this.Btn_SelectImage.UseVisualStyleBackColor = true;
            // 
            // Classify
            // 
            this.Classify.Controls.Add(this.Btn_SelectImage);
            this.Classify.Location = new System.Drawing.Point(4, 22);
            this.Classify.Name = "Classify";
            this.Classify.Padding = new System.Windows.Forms.Padding(3);
            this.Classify.Size = new System.Drawing.Size(1161, 520);
            this.Classify.TabIndex = 0;
            this.Classify.Text = "Classify";
            this.Classify.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.Classify);
            this.tabControl1.Controls.Add(this.Training);
            this.tabControl1.Location = new System.Drawing.Point(12, 42);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1169, 546);
            this.tabControl1.TabIndex = 1;
            // 
            // Training
            // 
            this.Training.Controls.Add(this.Lbl_ResultClassify);
            this.Training.Controls.Add(this.pictureBoxInput);
            this.Training.Controls.Add(this.Txt_TrainingStatus);
            this.Training.Controls.Add(this.Txt_ConvertStatus);
            this.Training.Controls.Add(this.Txt_DirOutput);
            this.Training.Controls.Add(this.Txt_DirInput);
            this.Training.Controls.Add(this.Btn_Output);
            this.Training.Controls.Add(this.Btn_SelectInput);
            this.Training.Controls.Add(this.Btn_Convert);
            this.Training.Controls.Add(this.Btn_SelectClassify);
            this.Training.Controls.Add(this.Btn_Training);
            this.Training.Location = new System.Drawing.Point(4, 22);
            this.Training.Name = "Training";
            this.Training.Padding = new System.Windows.Forms.Padding(3);
            this.Training.Size = new System.Drawing.Size(1161, 520);
            this.Training.TabIndex = 1;
            this.Training.Text = "Training";
            this.Training.UseVisualStyleBackColor = true;
            // 
            // Txt_TrainingStatus
            // 
            this.Txt_TrainingStatus.Enabled = false;
            this.Txt_TrainingStatus.Location = new System.Drawing.Point(94, 305);
            this.Txt_TrainingStatus.Multiline = true;
            this.Txt_TrainingStatus.Name = "Txt_TrainingStatus";
            this.Txt_TrainingStatus.Size = new System.Drawing.Size(206, 52);
            this.Txt_TrainingStatus.TabIndex = 12;
            // 
            // Txt_ConvertStatus
            // 
            this.Txt_ConvertStatus.Enabled = false;
            this.Txt_ConvertStatus.Location = new System.Drawing.Point(94, 245);
            this.Txt_ConvertStatus.Multiline = true;
            this.Txt_ConvertStatus.Name = "Txt_ConvertStatus";
            this.Txt_ConvertStatus.Size = new System.Drawing.Size(206, 52);
            this.Txt_ConvertStatus.TabIndex = 11;
            // 
            // Txt_DirOutput
            // 
            this.Txt_DirOutput.Enabled = false;
            this.Txt_DirOutput.Location = new System.Drawing.Point(6, 62);
            this.Txt_DirOutput.Multiline = true;
            this.Txt_DirOutput.Name = "Txt_DirOutput";
            this.Txt_DirOutput.Size = new System.Drawing.Size(294, 72);
            this.Txt_DirOutput.TabIndex = 9;
            // 
            // Txt_DirInput
            // 
            this.Txt_DirInput.Enabled = false;
            this.Txt_DirInput.Location = new System.Drawing.Point(6, 169);
            this.Txt_DirInput.Multiline = true;
            this.Txt_DirInput.Name = "Txt_DirInput";
            this.Txt_DirInput.Size = new System.Drawing.Size(294, 70);
            this.Txt_DirInput.TabIndex = 8;
            // 
            // Btn_Output
            // 
            this.Btn_Output.Location = new System.Drawing.Point(6, 33);
            this.Btn_Output.Name = "Btn_Output";
            this.Btn_Output.Size = new System.Drawing.Size(51, 23);
            this.Btn_Output.TabIndex = 5;
            this.Btn_Output.Text = "Output";
            this.Btn_Output.UseVisualStyleBackColor = true;
            this.Btn_Output.Click += new System.EventHandler(this.Btn_Output_Click);
            // 
            // Btn_SelectInput
            // 
            this.Btn_SelectInput.Location = new System.Drawing.Point(6, 140);
            this.Btn_SelectInput.Name = "Btn_SelectInput";
            this.Btn_SelectInput.Size = new System.Drawing.Size(51, 23);
            this.Btn_SelectInput.TabIndex = 4;
            this.Btn_SelectInput.Text = "Input";
            this.Btn_SelectInput.UseVisualStyleBackColor = true;
            this.Btn_SelectInput.Click += new System.EventHandler(this.Btn_SelectInput_Click);
            // 
            // Btn_Convert
            // 
            this.Btn_Convert.Location = new System.Drawing.Point(6, 245);
            this.Btn_Convert.Name = "Btn_Convert";
            this.Btn_Convert.Size = new System.Drawing.Size(85, 23);
            this.Btn_Convert.TabIndex = 3;
            this.Btn_Convert.Text = "Convert";
            this.Btn_Convert.UseVisualStyleBackColor = true;
            this.Btn_Convert.Click += new System.EventHandler(this.Btn_Convert_Click);
            // 
            // Btn_SelectClassify
            // 
            this.Btn_SelectClassify.Location = new System.Drawing.Point(6, 398);
            this.Btn_SelectClassify.Name = "Btn_SelectClassify";
            this.Btn_SelectClassify.Size = new System.Drawing.Size(85, 23);
            this.Btn_SelectClassify.TabIndex = 2;
            this.Btn_SelectClassify.Text = "Select";
            this.Btn_SelectClassify.UseVisualStyleBackColor = true;
            this.Btn_SelectClassify.Click += new System.EventHandler(this.Btn_SelectTrainingFolder_Click);
            // 
            // Btn_Training
            // 
            this.Btn_Training.Location = new System.Drawing.Point(3, 303);
            this.Btn_Training.Name = "Btn_Training";
            this.Btn_Training.Size = new System.Drawing.Size(85, 23);
            this.Btn_Training.TabIndex = 1;
            this.Btn_Training.Text = "Training";
            this.Btn_Training.UseVisualStyleBackColor = true;
            this.Btn_Training.Click += new System.EventHandler(this.Btn_Training_Click);
            // 
            // pictureBoxInput
            // 
            this.pictureBoxInput.Location = new System.Drawing.Point(427, 43);
            this.pictureBoxInput.Name = "pictureBoxInput";
            this.pictureBoxInput.Size = new System.Drawing.Size(557, 414);
            this.pictureBoxInput.TabIndex = 13;
            this.pictureBoxInput.TabStop = false;
            // 
            // Lbl_ResultClassify
            // 
            this.Lbl_ResultClassify.AutoSize = true;
            this.Lbl_ResultClassify.Location = new System.Drawing.Point(114, 403);
            this.Lbl_ResultClassify.Name = "Lbl_ResultClassify";
            this.Lbl_ResultClassify.Size = new System.Drawing.Size(98, 13);
            this.Lbl_ResultClassify.TabIndex = 14;
            this.Lbl_ResultClassify.Text = "Kết quả nhận dạng";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "Classification";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Classify.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.Training.ResumeLayout(false);
            this.Training.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxInput)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Btn_SelectImage;
        private System.Windows.Forms.TabPage Classify;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage Training;
        private System.Windows.Forms.Button Btn_Training;
        private System.Windows.Forms.Button Btn_SelectClassify;
        private System.Windows.Forms.TextBox Txt_DirOutput;
        private System.Windows.Forms.TextBox Txt_DirInput;
        private System.Windows.Forms.Button Btn_Output;
        private System.Windows.Forms.Button Btn_SelectInput;
        private System.Windows.Forms.Button Btn_Convert;
        private System.Windows.Forms.TextBox Txt_TrainingStatus;
        private System.Windows.Forms.TextBox Txt_ConvertStatus;
        private System.Windows.Forms.PictureBox pictureBoxInput;
        private System.Windows.Forms.Label Lbl_ResultClassify;
    }
}

