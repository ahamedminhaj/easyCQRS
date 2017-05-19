﻿using KJDevSec.EventSourcing;
using KJDevSec.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KJDevSec
{
    class Repository : IRepository
    {
        private readonly IEventStore eventStore;
        private readonly ISnapshotStore snapshopStore;
        private readonly IBus bus;

        private HashSet<AggregateRoot> __aggregatesModified = new HashSet<AggregateRoot>();

        public Repository(
            IEventStore eventStore,
            ISnapshotStore snapshopStore,
            IBus bus)
        {
            this.eventStore = eventStore ?? throw new ArgumentNullException("eventStore");
            this.snapshopStore = snapshopStore ?? throw new ArgumentNullException("snapshopStore");
            this.bus = bus ?? throw new ArgumentNullException("bus");
        }

        public async Task<T> GetByIdAsync<T>(Guid id) where T : AggregateRoot
        {
            var type = typeof(T);
            IEnumerable<Event> events = null;

            var snapshot = await this.snapshopStore.GetByIdAsync<T>(id);
            if (snapshot != null)
            {
                events = await this.eventStore.LoadAsync<T>(id, snapshot.Version);
            }
            else
            {
                events = await this.eventStore.LoadAsync<T>(id);
            }

            T aggregate = null;

            if (snapshot != null)
            {
                aggregate = (T)snapshot;

                if (events != null) aggregate.Hydrate(events);

                return aggregate;
            }
            else if (events != null && events.Count() > 0)
            {
                aggregate = (T)Activator.CreateInstance(type, id);
                aggregate.Hydrate(events);

                return aggregate;
            }

            return aggregate;
        }

        public void Save<T>(T item) where T : AggregateRoot
        {
            this.__aggregatesModified.Add(item);
        }

        public async Task SaveChangesAsync()
        {
            var @events = this.__aggregatesModified
                                .SelectMany(aggregate => aggregate
                                                    .GetUncommittedChanges()
                                                    .Select(e => new Tuple<Event, Type>(e, aggregate.GetType())));
            foreach (var @eventData in @events)
            {
                this.eventStore.Save(@eventData.Item2, eventData.Item1);
            }

            await eventStore.SaveChangesAsync();
            await bus.PublishEventsAsync(events.Select(e => e.Item1).ToArray());            

            var aggregates = __aggregatesModified.ToList();
            foreach (var aggregate in aggregates)
            {
                aggregate.MarkChangesAsCommitted();
                __aggregatesModified.Remove(aggregate);
            }
        }
    }
}