using ApiParser.Settings;
using Gw2Sharp.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ApiParser
{   
    /// <summary>
    /// Processes information on (un)successfull gw2 api requests.
    /// </summary>
    public class IssueTracker : IDisposable
    {
        private bool _disposed;
        
        private ApiState _state;

        private readonly Queue<(IssueType IssueType, DateTime Timestamp)> _issueTracker = new Queue<(IssueType, DateTime)>();
        private readonly object _issueLock = new object();

        private Timer _issueDecayTimer;

        /// <summary>
        /// Fires when the <see cref="State"/> changes.
        /// </summary>
        public event EventHandler<ApiState> StateChanged;

        /// <summary>
        /// The <see cref="DateTime"/> when the <see cref="State"/> changed last.
        /// </summary>
        public DateTime LastStateChange { get; private set; }

        private void OnStateChanged()
        {
            LastStateChange = DateTime.Now;
            StateChanged?.Invoke(this, _state);
        }

        /// <summary>
        /// The current state of the gw2 api as projected by the <see cref="IssueTracker"/>.
        /// </summary>
        public ApiState State
        {
            get => _state;
            private set
            {
                if (value == _state)
                {
                    return;
                }

                _state = value;
                OnStateChanged();
            }
        }

        /// <summary>
        /// The <see cref="ApiManagerSettings"/> that were provided for initializing the <see cref="IssueTracker"/>.
        /// </summary>
        public ApiManagerSettings Settings { get; }

        /// <summary>
        /// <inheritdoc cref="ApiManagerSettings.IssueTrackerSize"/>
        /// </summary>
        public int SizeLimit => Settings.IssueTrackerSize;

        /// <summary>
        /// The amount of issues currently tracked by the <see cref="IssueTracker"/>.
        /// </summary>
        public int IssueLevel
        {
            get
            {
                lock (_issueLock)
                {
                    return _issueTracker.Where(issue => issue.IssueType != IssueType.WithoutIssue).Count();
                }
            }
        }

        /// <summary>
        /// The ratio of issues in comparison to the <see cref="SizeLimit"/>. Between 0 and 1.
        /// </summary>
        public float IssueRatio => (float)IssueLevel / (float)SizeLimit;

        /// <summary>
        /// The ratio of requests tracked by the <see cref="IssueTracker"/> in comparison to the <see cref="SizeLimit"/>. 
        /// Between 0 and 1.
        /// </summary>
        public float DataRatio => (float)_issueTracker.Count() / (float)SizeLimit;

        /// <inheritdoc/>
        public IssueTracker(ApiManagerSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Adds a successfull request to the <see cref="IssueTracker"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void AddSuccess()
        {
            HandleNewIssue(IssueType.WithoutIssue);
        }

        /// <summary>
        /// Adds a failed request to the <see cref="IssueTracker"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void AddIssue(Exception exception)
        {
            IssueType issueType;

            if (exception is TooManyRequestsException)
            {
                issueType = IssueType.RateLimit;
            }
            else if (exception is ServerErrorException)
            {
                issueType = IssueType.ServerError;
            }
            else if (exception is ServiceUnavailableException)
            {
                issueType = IssueType.ServiceUnavailable;
            }
            else
            {
                throw new ArgumentException($"Exception of type {exception.GetType()} can't be tracked by the issue tracker.", nameof(exception));
            }

            HandleNewIssue(issueType);
        }

        private void HandleNewIssue(IssueType issue)
        {   
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            lock (_issueLock)
            {
                if (_issueTracker.Count() >= SizeLimit)
                {
                    _issueTracker.Dequeue();
                    ResetTimer();
                }

                _issueTracker.Enqueue((issue, DateTime.Now));

                if (_issueDecayTimer == null)
                {
                    ResetTimer();
                }

                UpdateState(issue);
            }
        }

        private void UpdateState(IssueType issue)
        {
            if (!HasEnoughData())
            {
                State = ApiState.Unknown;
                return;
            }

            if (issue == IssueType.RateLimit)
            {
                State = ApiState.RateLimited;
                return;
            }

            if (IssueRatio - Settings.ReliableApiCutoff > 0.0001f)
            {
                State = ApiState.Unreliable;
            }
            else
            {
                State = ApiState.Reliable;
            }
        }

        private void HandleIssueDecay()
        {
            lock ( _issueLock)
            {   
                while (_issueTracker.Any() && (DateTime.Now - _issueTracker.Peek().Timestamp) >= Settings.IssueDecay)
                {
                    _issueTracker.Dequeue();
                }

                if (!HasEnoughData())
                {
                    State = ApiState.Unknown;
                }

                ResetTimer();
            }
        }

        private void ResetTimer()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_issueDecayTimer != null)
            {
                _issueDecayTimer.Dispose();
                _issueDecayTimer = null;
            }

            if (!_issueTracker.Any())
            {
                return;
            }

            TimeSpan nextExpiration = Settings.IssueDecay - (DateTime.Now - _issueTracker.Peek().Timestamp);
            if (nextExpiration < TimeSpan.Zero)
            {
                nextExpiration = TimeSpan.FromSeconds(2);
            }

            _issueDecayTimer = new Timer(state => HandleIssueDecay(), null, nextExpiration + TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan);
        }

        private bool HasEnoughData()
        {
            lock (_issueLock)
            {
                return DataRatio - Settings.MeaningfullRequestCountCutoff > -0.0001f;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _issueDecayTimer?.Dispose();
                _issueDecayTimer = null;
            }

            StateChanged = null;
            _issueTracker.Clear();

            _disposed = true;
        }

        /// <inheritdoc/>
        ~IssueTracker()
        {
            Dispose(false);
        }
    }
}
