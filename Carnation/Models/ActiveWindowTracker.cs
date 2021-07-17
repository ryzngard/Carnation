using System;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Carnation
{
    internal class ActiveWindowTracker : NotifyPropertyBase, IVsRunningDocTableEvents, IDisposable
    {
        private DispatcherTimer _typingTimer;
        private readonly IVsRunningDocumentTable _runningDocumentTable;
        private uint _runningDocumentTableCookie;

        private Span? _selectedSpan;
        public Span? SelectedSpan
        {
            get => _selectedSpan;
            set => SetProperty(ref _selectedSpan, value);
        }

        private IWpfTextView _activeWpfTextView;
        public IWpfTextView ActiveWpfTextView
        {
            get => _activeWpfTextView;
            set => SetProperty(ref _activeWpfTextView, value);
        }

        public ActiveWindowTracker()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _runningDocumentTable = VSServiceHelpers.GetService<IVsRunningDocumentTable, SVsRunningDocumentTable>();
            if (_runningDocumentTable is null)
            {
                throw new Exception("Cannot initialize without getting running document table");
            }

            _runningDocumentTable.AdviseRunningDocTableEvents(this, out _runningDocumentTableCookie);
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int firstShow, IVsWindowFrame vsWindowFrame)
        {
            if (firstShow != 0)
            {
                return VSConstants.S_OK;
            }

            var wpfTextView = ToWpfTextView(vsWindowFrame);
            if (wpfTextView != null)
            {
                Clear();
                ActiveWpfTextView = wpfTextView;
                ActiveWpfTextView.Selection.SelectionChanged += HandleSelectionChanged;
                ActiveWpfTextView.TextBuffer.Changed += HandleTextBufferChanged;
                ActiveWpfTextView.LostAggregateFocus += HandleTextViewLostFocus;
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame vsWindowFrame)
        {
            if (ActiveWpfTextView != null)
            {
                var wpfTextView = ToWpfTextView(vsWindowFrame);
                if (wpfTextView == ActiveWpfTextView)
                {
                    Clear();
                }
            }

            return VSConstants.S_OK;
        }

        private void Clear()
        {
            if (_typingTimer != null)
            {
                _typingTimer.Stop();
                _typingTimer.Tick -= HandleTypingTimerTimeout;
                _typingTimer = null;
            }

            if (ActiveWpfTextView != null)
            {
                ActiveWpfTextView.Selection.SelectionChanged -= HandleSelectionChanged;
                ActiveWpfTextView.TextBuffer.Changed -= HandleTextBufferChanged;
                ActiveWpfTextView.LostAggregateFocus -= HandleTextViewLostFocus;
                ActiveWpfTextView = null;
            }
        }

        private void HandleTypingTimerTimeout(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            RefreshSelectedSpan();
        }

        private void HandleTextViewLostFocus(object sender, EventArgs e)
        {
            _typingTimer?.Stop();
        }

        private void HandleTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (_typingTimer == null)
            {
                _typingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };

                _typingTimer.Tick += HandleTypingTimerTimeout;
            }

            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void HandleSelectionChanged(object sender, EventArgs e)
        {
            RefreshSelectedSpan();
        }

        private void RefreshSelectedSpan()
        {
            SelectedSpan = ActiveWpfTextView?.Selection.StreamSelectionSpan.SnapshotSpan.Span;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        internal static IWpfTextView ToWpfTextView(IVsWindowFrame vsWindowFrame)
        {
            IWpfTextView wpfTextView = null;
            var vsTextView = VsShellUtilities.GetTextView(vsWindowFrame);

            if (vsTextView != null)
            {
                var guidTextViewHost = DefGuidList.guidIWpfTextViewHost;
                if (((IVsUserData)vsTextView).GetData(ref guidTextViewHost, out var textViewHost) == VSConstants.S_OK &&
                    textViewHost != null)
                {
                    wpfTextView = ((IWpfTextViewHost)textViewHost).TextView;
                }
            }

            return wpfTextView;
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_runningDocumentTableCookie != 0)
            {
                _runningDocumentTable.UnadviseRunningDocTableEvents(_runningDocumentTableCookie);
                _runningDocumentTableCookie = 0;
            }
        }
    }
}
