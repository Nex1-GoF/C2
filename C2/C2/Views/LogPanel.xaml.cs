using System.Windows;
using System.Windows.Controls;
using C2.ViewModels;

namespace C2.Views
{
    public partial class LogPanel : UserControl
    {
        private LogViewModel ViewModel => DataContext as LogViewModel;

        private bool _autoScroll = true;

        public LogPanel()
        {
            InitializeComponent();

            // DisplayLogs 컬렉션 변경 감지 → 자동 스크롤
            ViewModel.DisplayLogs.CollectionChanged += (s, e) => ScrollToEndIfNeeded();
        }

        // 스크롤 변경시
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 사용자가 수동 스크롤할 때 자동 스크롤 중단
            if (e.ExtentHeightChange == 0)
                _autoScroll = LogScroll.VerticalOffset >= LogScroll.ScrollableHeight;
            else if (_autoScroll)
                LogScroll.ScrollToEnd();
        }

        private void ScrollToEndIfNeeded()
        {
            if (_autoScroll)
                LogScroll.ScrollToEnd();
        }
    }
}
