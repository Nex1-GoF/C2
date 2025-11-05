using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace C2.ViewModels
{
    public partial class TargetViewModel : ObservableObject
    {
        public ObservableCollection<Target> Targets { get; } = new();
        private readonly TargetService _service;
        private readonly UpdateDispatcher _updateDispatcher;

        private Target? _selectedTarget;
        public Target? SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public TargetViewModel()
        {
            _service = TargetService.Instance;
            _updateDispatcher = UpdateDispatcher.Instance;

            for (int i = 1; i <= 4; i++)
            {
                var target = new Target
                (
                    (char)('A' + i - 1),
                    100,
                    200,
                    0,
                    (37, 105),
                    DateTime.Now,
                    (38, 100)
                );
                _service.ReceiveTargetData(target);
            }

            _updateDispatcher.Register(UpdateTargets);
        }

        private void UpdateTargets()
        {
            var latest = _service.GetAllTargets();
            foreach (var updated in latest)
            {
                var existing = Targets.FirstOrDefault(t => t.Id == updated.Id);
                if (existing == null)
                    Targets.Add(updated);
                else
                    existing.Update(updated);
            }

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

        public void SelectTarget(Target target)
        {
            if (target == null)
                _service.ClearTarget();
            else
            {
                _service.SelectTarget(target.Id);
                SelectedTarget = target;
                System.Diagnostics.Debug.WriteLine($"[SelectTarget] {target.Id}");
            }
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
}