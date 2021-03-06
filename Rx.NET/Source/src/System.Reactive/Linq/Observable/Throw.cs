﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Concurrency;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class Throw<TResult> : Producer<TResult>
    {
        private readonly Exception _exception;
        private readonly IScheduler _scheduler;

        public Throw(Exception exception, IScheduler scheduler)
        {
            _exception = exception;
            _scheduler = scheduler;
        }

        protected override IDisposable Run(IObserver<TResult> observer, IDisposable cancel, Action<IDisposable> setSink)
        {
            var sink = new _(this, observer, cancel);
            setSink(sink);
            return sink.Run();
        }

        class _ : Sink<TResult>
        {
            private readonly Throw<TResult> _parent;

            public _(Throw<TResult> parent, IObserver<TResult> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                return _parent._scheduler.Schedule(Invoke);
            }

            private void Invoke()
            {
                base._observer.OnError(_parent._exception);
                base.Dispose();
            }
        }
    }
}
