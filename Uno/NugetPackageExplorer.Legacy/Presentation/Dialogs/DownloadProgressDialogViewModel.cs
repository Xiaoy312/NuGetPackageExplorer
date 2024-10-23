using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Windows.Input;
using NupkgExplorer.Framework.MVVM;

using Uno.Disposables;
using Uno.Extensions;

namespace NupkgExplorer.Presentation.Dialogs
{
    public class DownloadProgressDialogViewModel : ViewModelBase, IProgress<(long ReceivedBytes, long? TotalBytes)>
    {
        private readonly ISubject<(long ReceivedBytes, long? TotalBytes)> _progressSubject;
        private readonly CancellationDisposable _downloadCts;
        private DateTime _lastProgressUpdateTick = DateTime.MinValue;

        public string PackageName
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string PackageVersion
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string Ellipsis
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public double? Progress
        {
            get => GetProperty<double?>();
            set => SetProperty(value);
        }

        public string ReceivedTest
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public long? Received
        {
            get => GetProperty<long>();
            set => SetProperty(value);
        }

        public long? Total
        {
            get => GetProperty<long?>();
            set => SetProperty(value);
        }

        public ICommand CancelDownloadCommand => this.GetCommand(CancelDownload);

        public DownloadProgressDialogViewModel(string packageName, string packageVersion, CancellationDisposable downloadCts)
        {
            this._progressSubject = new ReplaySubject<(long ReceivedBytes, long? TotalBytes)>(1);
            this._downloadCts = downloadCts;
            this.PackageName = packageName;
            this.PackageVersion = packageVersion;

            var disposable = new CompositeDisposable();

#if !__WASM__ // disabled for other platforms due to #70
            var progressDisposable = _progressSubject
                // limit update frequency
                .Buffer(TimeSpan.FromMilliseconds(150))
                .Where(g => g.Any())
                .Select(g => g.Last())
                .Subscribe(x =>
                {
                    // fixme@xy: on wasm, we do hit here, but only once before and once after the download process ...

                    Received = x.ReceivedBytes;
                    Total = x.TotalBytes;
                    Progress = 100.0 * x.ReceivedBytes / x.TotalBytes;
                });

            //disposable.Add(progressDisposable);
#endif
            // Required to ensure that the ProgressBar will not animate needlessly.
            var ellipsisDisposable = Observable.Interval(TimeSpan.FromMilliseconds(150))
				.Select(x => x % 5)
				.Where(x => x <= 3) // empty,1,2,3,wait,wait
				.Subscribe(x => Ellipsis = new string('.', (int)x));

            disposable.Add(ellipsisDisposable);

            downloadCts.Token.Register(() => disposable.Dispose());
        }

        public void Report((long ReceivedBytes, long? TotalBytes) progress)
        {
#if !__WASM__
            _progressSubject.OnNext(progress);
#else
            // fixme@xy/jerome: ui update doesnt happen when on heavy load?

            var now = DateTime.Now;
            var elapsed = now - _lastProgressUpdateTick;
            Console.WriteLine($"report, elapsed={elapsed.TotalMilliseconds}ms");
            if (elapsed > TimeSpan.FromMilliseconds(500)) 
            {
                Console.WriteLine("update");
                Received = progress.ReceivedBytes;
                Total = progress.TotalBytes;
                Progress = 100.0 * progress.ReceivedBytes / progress.TotalBytes;

                _lastProgressUpdateTick = now;
            }

#endif
        }

		public void CancelDownload()
		{
			_downloadCts.Dispose();
		}
	}
}
