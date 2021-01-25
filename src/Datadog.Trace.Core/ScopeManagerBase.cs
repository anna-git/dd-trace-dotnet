using System;

namespace Datadog.Trace
{
    internal abstract class ScopeManagerBase : IScopeManager, IScopeRawAccess
    {
        public event EventHandler<SpanEventArgs> SpanOpened;

        public event EventHandler<SpanEventArgs> SpanActivated;

        public event EventHandler<SpanEventArgs> SpanDeactivated;

        public event EventHandler<SpanEventArgs> SpanClosed;

        public event EventHandler<SpanEventArgs> TraceEnded;

        // TODO: set ScopeFactory
        public Func<IScope, ISpan, IScopeManager, bool, IScope> ScopeFactory { get; }

        public abstract IScope Active { get; protected set; }

        IScope IScopeRawAccess.Active
        {
            get => Active;
            set => Active = value;
        }

        public IScope Activate(ISpan span, bool finishOnClose)
        {
            IScope newParent = Active;
            IScope scope = ScopeFactory(newParent, span, this, finishOnClose);
            var scopeOpenedArgs = new SpanEventArgs(span);

            SpanOpened?.Invoke(this, scopeOpenedArgs);

            Active = scope;

            if (newParent != null)
            {
                SpanDeactivated?.Invoke(this, new SpanEventArgs(newParent.Span));
            }

            SpanActivated?.Invoke(this, scopeOpenedArgs);

            return scope;
        }

        public void Close(IScope scope)
        {
            var current = Active;
            var isRootSpan = scope.Parent == null;

            if (current == null || current != scope)
            {
                // This is not the current scope for this context, bail out
                SpanClosed?.Invoke(this, new SpanEventArgs(scope.Span));
                return;
            }

            // if the scope that was just closed was the active scope,
            // set its parent as the new active scope
            Active = scope.Parent;
            SpanDeactivated?.Invoke(this, new SpanEventArgs(scope.Span));

            if (!isRootSpan)
            {
                SpanActivated?.Invoke(this, new SpanEventArgs(scope.Parent.Span));
            }

            SpanClosed?.Invoke(this, new SpanEventArgs(scope.Span));

            if (isRootSpan)
            {
                TraceEnded?.Invoke(this, new SpanEventArgs(scope.Span));
            }
        }
    }
}