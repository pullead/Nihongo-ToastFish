using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToastFish.Services.Study
{
    public class StudySessionController : IDisposable
    {
        private readonly object syncRoot = new object();
        private CancellationTokenSource activeCancellation;

        public bool IsRunning { get; private set; }

        public void Run(Action<CancellationToken> session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            CancellationTokenSource currentSession = BeginSession();
            try
            {
                if (!currentSession.IsCancellationRequested)
                    session(currentSession.Token);
            }
            finally
            {
                EndSession(currentSession);
            }
        }

        public Task RunAsync(Func<CancellationToken, Task> session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            CancellationTokenSource currentSession = BeginSession();
            return Task.Run(async () =>
            {
                try
                {
                    if (!currentSession.IsCancellationRequested)
                        await session(currentSession.Token);
                }
                finally
                {
                    EndSession(currentSession);
                }
            });
        }

        public void CancelActiveSession()
        {
            CancellationTokenSource cancellation;
            lock (syncRoot)
            {
                cancellation = activeCancellation;
            }

            if (cancellation != null)
                cancellation.Cancel();
        }

        public void Dispose()
        {
            CancelActiveSession();
        }

        private CancellationTokenSource BeginSession()
        {
            CancellationTokenSource previousSession;
            CancellationTokenSource currentSession = new CancellationTokenSource();

            lock (syncRoot)
            {
                previousSession = activeCancellation;
                activeCancellation = currentSession;
                IsRunning = true;
            }

            if (previousSession != null)
                previousSession.Cancel();

            return currentSession;
        }

        private void EndSession(CancellationTokenSource session)
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(activeCancellation, session))
                {
                    activeCancellation = null;
                    IsRunning = false;
                }
            }

            session.Dispose();
        }
    }
}
