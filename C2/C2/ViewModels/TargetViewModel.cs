using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace C2.ViewModels
{
    public partial class TargetViewModel : ObservableObject
    {
        public ObservableCollection<Target> Targets { get;} = new ();
        private readonly TargetService _targetService;
        private readonly LogService _logService;
        private readonly MissileService _missileService;
        private readonly UpdateDispatcher _updateDispatcher;

        private Target? _selectedTarget;
        public Target? SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public TargetViewModel()
        {
            _targetService = TargetService.Instance;
            _missileService = MissileService.Instance;
            _updateDispatcher = UpdateDispatcher.Instance;
            _logService = LogService.Instance;

            _updateDispatcher.Register(UpdateTargets);

            WeakReferenceMessenger.Default.Register<TargetSelectedMessage>(this, (r, msg) =>
            {
                if (msg.Value != null)
                    SelectedTarget = Targets.FirstOrDefault(t => t.Id == msg.Value);
                else
                    SelectedTarget = null;
            });
        }

        private void UpdateTargets()
        {
            var latest = _targetService.GetAllTargets();
            foreach (var updated in latest)
            {
                var existing = Targets.FirstOrDefault(t => t.Id == updated.Id);

                if (existing == null)
                {
                    Targets.Add(updated);
                }
                else
                {
                    // 속성만 갱신
                    existing.CurLoc = updated.CurLoc;
                    existing.Yaw = updated.Yaw;
                    existing.Speed = updated.Speed;
                    existing.Altitude = updated.Altitude;
                    existing.DetectTime = updated.DetectTime;
                }
            }

            // 기존 목록에 있었는데 latest에 없는 항목은 제거
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                if (!latest.Any(t => t.Id == Targets[i].Id))
                    Targets.RemoveAt(i);
            }
        }

        ~TargetViewModel()
        {
            _updateDispatcher.Unregister(UpdateTargets);
        }

        // ================================================
        // ✅ 표적 카드 클릭 → 해당 표적에 연결된 모든 미사일 선택
        // ================================================
        [RelayCommand]
        private void TargetCardClick(Target target)
        {
            if (target == null)
                return;

            // 이미 선택된 표적 다시 클릭 시 → 선택 해제
            if (SelectedTarget != null && SelectedTarget.Id == target.Id)
            {
                _targetService.ClearTarget();
                _missileService.ClearMissiles(); // ✅ 여러 미사일 해제
                SelectedTarget = null;
                return;
            }

            // 새 표적 선택
            _targetService.SelectTarget(target.Id);

            // ✅ 이 표적을 추적 중인 모든 미사일을 선택
            var assignedMissiles = _missileService.GetAllMissiles()
                .Where(m => m.TargetId != null && m.TargetId[0] == target.Id)
                .Select(m => m.Id)
                .ToList();

            if (assignedMissiles.Any())
                _missileService.SelectMissiles(assignedMissiles);
            else
                _missileService.ClearMissiles();

            SelectedTarget = target;
        }

        [RelayCommand]
        private void Engagement(Target target)
        {

            var missileId = _missileService.AssignTarget(target.Id.ToString());

            if (string.IsNullOrEmpty(missileId))
            {
                _logService.AddLog(MessageType.System, "교전할당 가능한 미사일이 없습니다.");
                return;
            }

            _logService.AddLog(MessageType.System, $"교전할당 완료: {missileId} 생성됨.");

            // Todo: 발사 버튼 assign
        }

    }

    public class IsSelectedTargetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Target current && values[1] is Target selected)
                return current.Id == selected.Id;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class SelectedTargetBorderConverter : IMultiValueConverter
    {
        private static SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromRgb(69, 232, 225)); // #45E8E1
        private static SolidColorBrush NormalBrush = new SolidColorBrush(Color.FromRgb(43, 116, 128)); // #2B7480

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not Target current || values[1] is not Target selected)
                return NormalBrush;

            return current.Id == selected?.Id ? SelectedBrush : NormalBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
