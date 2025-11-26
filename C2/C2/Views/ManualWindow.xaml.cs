using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
namespace C2.Views
{
    /// <summary>
    /// ManualWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualWindow : Window
    {
        public ManualWindow()
        {
            InitializeComponent();

           // string pdfPath = @"C:\Nex1-GoF\C2\C2\C2\Resources\manual.html";
            string pdfPath = @"C:\workspace\C2\C2\C2\Resources\manual.html";
            if (File.Exists(pdfPath))
                PdfViewer.Source = new Uri(pdfPath);
            else
                MessageBox.Show("파일 없음: " + pdfPath);

            this.PreviewKeyDown += ManualWindow_PreviewKeyDown;
        }

        private void ManualWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.M)
            {
                this.Close();    // 이제는 Close() 사용
                e.Handled = true;
            }
        }
    }
}
