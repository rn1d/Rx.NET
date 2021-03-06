﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive.Concurrency
{
    internal sealed class ObserveOn<TSource> : Producer<TSource>
    {
        private readonly IObservable<TSource> _source;
        private readonly IScheduler _scheduler;
        private readonly SynchronizationContext _context;

        public ObserveOn(IObservable<TSource> source, IScheduler scheduler)
        {
            _source = source;
            _scheduler = scheduler;
        }

        public ObserveOn(IObservable<TSource> source, SynchronizationContext context)
        {
            _source = source;
            _context = context;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Visibility restricted to friend assemblies. Those should be correct by inspection.")]
        protected override IDisposable Run(IObserver<TSource> observer, IDisposable cancel, Action<IDisposable> setSink)
        {
            if (_context != null)
            {
                var sink = new ObserveOnSink(this, observer, cancel);
                setSink(sink);
                return sink.Run();
            }
            else
            {
                var sink = new ObserveOnObserver<TSource>(_scheduler, observer, cancel);
                setSink(sink);
                return _source.SubscribeSafe(sink);
            }
        }

        private sealed class ObserveOnSink : Sink<TSource>, IObserver<TSource>
        {
            private readonly ObserveOn<TSource> _parent;

            public ObserveOnSink(ObserveOn<TSource> parent, IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                //
                // The interactions with OperationStarted/OperationCompleted below allow
                // for test frameworks to wait until a whole sequence is observed, running
                // asserts on a per-message level. Also, for ASP.NET pages, the use of the
                // built-in synchronization context would allow processing to finished in
                // its entirety before moving on with the page lifecycle.
                //
                _parent._context.OperationStarted();

                var d = _parent._source.SubscribeSafe(this);
                var c = Disposable.Create(() =>
                {
                    _parent._context.OperationCompleted();
                });

                return StableCompositeDisposable.Create(d, c);
            }

            public void OnNext(TSource value)
            {
                _parent._context.Post(OnNextPosted, value);
            }

            public void OnError(Exception error)
            {
                _parent._context.Post(OnErrorPosted, error);
            }

            public void OnCompleted()
            {
                _parent._context.Post(OnCompletedPosted, state: null);
            }

            private void OnNextPosted(object value)
            {
                _observer.OnNext((TSource)value);
            }

            private void OnErrorPosted(object error)
            {
                _observer.OnError((Exception)error);
                Dispose();
            }

            private void OnCompletedPosted(object ignored)
            {
                _observer.OnCompleted();
                Dispose();
            }
        }
    }
}
