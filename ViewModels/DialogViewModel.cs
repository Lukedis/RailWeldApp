using CommunityToolkit.Mvvm.ComponentModel;

namespace RailWeldApp.ViewModels
{
    public partial class DialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isOpen;

        private TaskCompletionSource<bool> _closeTask = new();
        public Func<Task>? EnterAnimation { get; set; }
        public Func<Task>? ExitAnimation { get; set; }

        public async Task ShowAsync()
        {
            if (IsOpen)
                return;

            _closeTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            IsOpen = true;

            try
            {
                if (EnterAnimation is not null)
                    await EnterAnimation();

                // 等待任务
                await _closeTask.Task;
            }
            catch (Exception)
            {
                IsOpen = false;

                _closeTask.TrySetResult(false);

                throw;
            }
        }

        public async Task CloseAsync()
        {
            if (!IsOpen)
                return;

            if (ExitAnimation is not null)
                await ExitAnimation();

            IsOpen = false;

            // 通知任务完成
            _closeTask.TrySetResult(true);
        }
    }
}
