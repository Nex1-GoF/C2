using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Windows;
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

            // 프로젝트 내부 PDF 경로
            string pdfPath = @"C:\workspace\C2\C2\C2\Resources\manual.pdf";


            if (File.Exists(pdfPath))
            {
                PdfViewer.Source = new Uri(pdfPath);
            }
            else
            {
                MessageBox.Show("PDF 파일을 찾을 수 없습니다: " + pdfPath);
            }
        }
    }
}
