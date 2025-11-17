    using CommunityToolkit.Mvvm.ComponentModel;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Media;
    using C2.Models;
    using C2.Services;
using System.Windows;

namespace C2.ViewModels
    {
        internal partial class LogViewModel : ObservableObject
        {
            [ObservableProperty]
            private ObservableCollection<DisplayLog> _displayLogs; 
            // ViewBinding을 통해 넣을 뷰
            // 결정안된것: DispatcherTimer와 연동할지, 아니면 얘는 이벤트기반으로 줄지
            // 우선, 이벤트기반으로 주는게 좋을 것 같음
            // 이유: 호출 주기가 수초~수십초에 한번이라 오히려 10프레임마다 업데이트된 자료가 있는지 비교하고 UI 리스트를 갱신하는 오버헤드가 더 클거같음

            private readonly LogService _logService;

            public LogViewModel()
            {
                _displayLogs = new ObservableCollection<DisplayLog>();
                _logService = LogService.Instance;


                // 새 로그가 추가되면 즉시 반영 (디스패쳐 동기화 x)
                _logService.LogAdded += OnLogAdded;
            
            
                //스크롤 테스트
                _logService.AddLog(MessageType.System, $"Program Started");
            }

            // 로그 추가됐을때 UI에 반영
            public void OnLogAdded(Log newLog)
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _displayLogs.Add(ConvertToDisplayLog(newLog));
                });
            }

            // 로그 종류에 따라 메세지 색 변경 (현재는 시스템메세지만 사용)
            private DisplayLog ConvertToDisplayLog(Log log)
            {
                var brush = log.MessageType switch
                {
                    MessageType.System => Brushes.Yellow,
                    _ => Brushes.White
                };

                return new DisplayLog(log.DateTime, log.GetLogMessage(), brush);
            }
        }

        // 실제 로그에 색 정보를 넣기보다는 
        internal class DisplayLog
        {
            public DateTime DateTime { get; }
            public string Message { get; } // 전체 메세지
            public Brush Foreground { get; } // 색

            public DisplayLog(DateTime time, string message, Brush color)
            {
                DateTime = time;
                Message = message;
                Foreground = color;
            }
        }
    }
