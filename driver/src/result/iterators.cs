/*-
 *
 *  This file is part of Oracle NoSQL Database
 *  Copyright (C) 2015, 2020 Oracle and/or its affiliates.  All rights reserved.
 *
 * If you have received this file as part of Oracle NoSQL Database the
 * following applies to the work as a whole:
 *
 *   Oracle NoSQL Database server software is free software: you can
 *   redistribute it and/or modify it under the terms of the GNU Affero
 *   General Public License as published by the Free Software Foundation,
 *   version 3.
 *
 *   Oracle NoSQL Database is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *   Affero General Public License for more details.
 *
 * If you have received this file as part of Oracle NoSQL Database Client or
 * distributed separately the following applies:
 *
 *   Oracle NoSQL Database client software is free software: you can
 *   redistribute it and/or modify it under the terms of the Apache License
 *   as published by the Apache Software Foundation, version 2.0.
 *
 * You should have received a copy of the GNU Affero General Public License
 * and/or the Apache License in the LICENSE file along with Oracle NoSQL
 * Database client or server distribution.  If not, see
 * <http://www.gnu.org/licenses/>
 * or
 * <http://www.apache.org/licenses/LICENSE-2.0>.
 *
 * An active Oracle commercial licensing agreement for this product supersedes
 * these licenses and in such case the license notices, but not the copyright
 * notice, may be removed by you in connection with your distribution that is
 * in accordance with the commercial licensing terms.
 *
 * For more information please contact:
 *
 * berkeleydb-info_us@oracle.com
 *
 */



/*!
*  \addtogroup iterator
* \brief Iteration support.
*  @{
*/


/**
 * Utilities to iterate over serach results.
 */
namespace oracle.kv.client.iterator {
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections;
    using proxy.gen;
    using oracle.kv.client.data;
    using oracle.kv.client.option;
    using oracle.kv.client.error;

    /// <summary>
    /// Transformes one type X to another type Y.
    /// </summary>
    public interface Transformer<X, Y> {
        Y transform(X x);
    }

    /// <summary>
    /// Transforms Thrif data structure to various Row-like structure.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <typeparam name="R"> API defined target type to convert to 
    /// e.g. Row or PrimaryKey</typeparam>.
    public class ThriftResultTransformer<R> : Transformer<TRowAndMetadata, R> {
        readonly ITable _table;
        readonly IDataModel _model;

        internal ThriftResultTransformer(IDataModel model, ITable table) {
            _model = model;
            _table = table;
        }

        //TODO: RowFactory by type
        public R transform(TRowAndMetadata tr) {
            return (R)(new RowImpl(tr, _table, _model) as IRow);
        }
    }


    /// <summary>
    /// A provider of multiple results using Observer-Observalble design pattern
    /// or Reactive Programming model.
    /// </summary>
    /// <remarks>
    /// This observable is initialized with a task that can fetch potentially
    /// large number of results. If an observer subscribes to this observable,
    /// then this observable provides the observer with fetched result.
    /// The observer i.e. the recepient of the resut controls the rate at which 
    /// the results are delivered. So the observer is never overwhelmed by results.
    /// The observer controls the rate of result delivery because this provider
    /// delivers next result only after observer has returned <code>true</code> from 
    /// its <code>OnNext()</code> method (an observer may use 
    /// such feature for pagination).
    /// <para></para>
    /// The generic parameter type <typeparam name="R"/> denotes the  type of
    /// result to be delivered. It must be a type that can be constructed
    /// </remarks>
    class AsyncResultProvider<R> : IDisposable {
        Task<TIteratorResult> _ServiceTask;
        Task _SubscriptionTask;
        KVStore _Connection;
        ThriftResultTransformer<R> _transformer;

        internal AsyncResultProvider(KVStore con,
            Task<TIteratorResult> task,
            ThriftResultTransformer<R> transfomer) {
            _ServiceTask = task;
            _Connection = con;
            _transformer = transfomer;
        }

        /// <summary>
        /// Subscribe the specified subscriber. The result would be
        /// delivered to this subscriber.
        /// </summary>
        /// <param name="subscriber">Subscriber.</param>
        public async Task SubscribeAsync(IObserver<R> subscriber) {

            IEnumerable<R> result = await GetTaskResultSafely(subscriber);
            if (result != null) {
                await DeliverWithSubscriberThrottle(subscriber, result);
            }
        }

        /// <summary>
        /// Gets the task result with error notified to observer/subscriber.
        /// </summary>
        /// <returns>The task result.</returns>
        /// <param name="subscriber">Subscriber.</param>

        // disables warning of not using await keyword
#pragma warning disable 1998
        async Task<IEnumerable<R>> GetTaskResultSafely(IObserver<R> subscriber) {
            if (_ServiceTask.IsCanceled) {
                subscriber.OnError(new ArgumentException("task " + _ServiceTask
                                            + " has been cancelled"));
                return null;
            }
            if (_ServiceTask.IsFaulted) {
                subscriber.OnError(new ArgumentException("task " + _ServiceTask
                                           + " has failed"));
                return null;
            }
            try {
                return new SyncResultProvider<R>(_Connection, _ServiceTask, _transformer);
            } catch (Exception ex) {
                subscriber.OnError(ex);
            }
            return null;
        }


        /// <summary>
        /// Emulates async behavior with given asynchrous result consumer and
        ///  results. Transforms each Thrift-based result item to a application-
        /// consumable form before delivery.
        /// </summary>
        /// <param name="cb">Cb a result consumer interface.</param>
        /// <param name="results">Results an enumerable.</param>
        async Task DeliverWithSubscriberThrottle(IObserver<R> cb, IEnumerable<R> results) {
            foreach (R r in results) {
                if (_ServiceTask.IsCanceled) return;
                try {
                    cb.OnNext(r);
                } catch (Exception ex) {
                    cb.OnError(ex);
                }
            }
            cb.OnCompleted();
        }

        public void Dispose() {
            if (_SubscriptionTask != null) {
                _SubscriptionTask.Dispose();
            }
        }

    }

    /// <summary>
    /// Provides results given a funtion to fetch data from database.
    /// The function is invoked on database and returns result not as a single list
    /// but as a series of lists. Each list is itself a TItearorResult, and that
    /// iterator is realized as a list laziliy.
    /// This provider invokes the function, transforms
    /// a batch i.e. a TIteartor to an enumenable of IRow, 
    /// and fetches next batch, but not with the same function 
    /// but the iterator identfier returned by the previous invocation,
    /// until database is exhausted.
    /// All the batches of IRow is collapsed into a single IEnumerable so hat
    /// neither the final recipient is aware of the series of database call,
    /// nor any caching is required.
    /// This beahavior is similar, but the transforamtion varies by the final
    /// result type sometimes it is IRow, but it can be Primary Key, Index Key
    /// or KeyPair. This variation of final result type is handled by generic
    /// type of supplied transformer.
    /// </summary>
    public class SyncResultProvider<R> : EnumerableChain<R> {

        internal SyncResultProvider(KVStore con, Task<TIteratorResult> task,
                   ThriftResultTransformer<R> transformer) {
            TIteratorResult batch = null;
            do {
                IEnumerable<R> child = null;
                if (IsEmpty) {
                    batch = task.Result;
                    child = new TransformingEnumerable<R>(batch, transformer);
                } else {
                    batch = con.proxy.iteratorNext(batch.IteratorId);
                    child = new TransformingEnumerable<R>(batch, transformer);
                }
                AddChild(child);
            } while (batch != null && batch.HasMore);

        }

    }



    /// <summary>
    /// Takes a List of Y and produces an Enumerable of X
    /// </summary>
    public class TransformingEnumerable<X> : IEnumerable<X> {
        TIteratorResult _list;
        private ThriftResultTransformer<X> _transfomer;

        public TransformingEnumerable(TIteratorResult list, ThriftResultTransformer<X> t) {
            _list = list;
            _transfomer = t;
        }

        public IEnumerator<X> GetEnumerator() {
            foreach (TRowAndMetadata y in _list.Result.RowsWithMetadata) {
                yield return _transfomer.transform(y);
            }

        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// collapses a series of child IEnumerables to a single IEnumerable.
    /// </summary>
    public class EnumerableChain<X> : IEnumerable<X> {
        private List<IEnumerable<X>> _children;
        public bool IsEmpty {
            get {
                return _children == null || _children.Count == 0;
            }
        }
        public EnumerableChain() {
            _children = new List<IEnumerable<X>>();
        }

        public void AddChild(IEnumerable<X> child) {
            _children.Add(child);
        }


        public IEnumerator<X> GetEnumerator() {
            foreach (IEnumerable<X> child in _children) {
                foreach (X x in child) {
                    yield return x;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}

/*! @} End of Doxygen Groups*/
