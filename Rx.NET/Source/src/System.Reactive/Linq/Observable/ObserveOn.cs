﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Concurrency;
using System.Threading;

namespace System.Reactive.Linq.ObservableImpl
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

        protected override IDisposable Run(IObserver<TSource> observer, IDisposable cancel, Action<IDisposable> setSink)
        {
            if (_context != null)
            {
                var sink = new ObserveOnImpl(this, observer, cancel);
                setSink(sink);
                return _source.Subscribe(sink);
            }
            else
            {
                var sink = new ObserveOnObserver<TSource>(_scheduler, observer, cancel);
                setSink(sink);
                return _source.Subscribe(sink);
            }
        }

        class ObserveOnImpl : Sink<TSource>, IObserver<TSource>
        {
            private readonly ObserveOn<TSource> _parent;

            public ObserveOnImpl(ObserveOn<TSource> parent, IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _parent = parent;
            }

            public void OnNext(TSource value)
            {
                _parent._context.PostWithStartComplete(() =>
                {
                    base._observer.OnNext(value);
                });
            }

            public void OnError(Exception error)
            {
                _parent._context.PostWithStartComplete(() =>
                {
                    base._observer.OnError(error);
                    base.Dispose();
                });
            }

            public void OnCompleted()
            {
                _parent._context.PostWithStartComplete(() =>
                {
                    base._observer.OnCompleted();
                    base.Dispose();
                });
            }
        }
    }
}
