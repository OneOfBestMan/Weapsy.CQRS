﻿using System;
using System.Threading.Tasks;
using Weapsy.Cqrs.Dependencies;
using Weapsy.Cqrs.Domain;
using Weapsy.Cqrs.Events;

namespace Weapsy.Cqrs.Commands
{
    /// <inheritdoc />
    /// <summary>
    /// CommandSenderAsync
    /// </summary>
    /// <seealso cref="T:Weapsy.Cqrs.Commands.ICommandSenderAsync" />
    public class CommandSenderAsync : ICommandSenderAsync
    {
        private readonly IResolver _resolver;
        private readonly IEventPublisherAsync _eventPublisher;
        private readonly IEventFactory _eventFactory;
        private readonly IEventStore _eventStore;

        public CommandSenderAsync(IResolver resolver,
            IEventPublisherAsync eventPublisher, 
            IEventFactory eventFactory,
            IEventStore eventStore)
        {
            _resolver = resolver;
            _eventPublisher = eventPublisher;
            _eventFactory = eventFactory;
            _eventStore = eventStore;
        }

        public async Task SendAsync<TCommand>(TCommand command) 
            where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandHandler = _resolver.Resolve<ICommandHandlerAsync<TCommand>>();

            if (commandHandler == null)
                throw new ApplicationException($"No handler of type CommandHandlerAsync<TCommand> found for command '{command.GetType().FullName}'");

            await commandHandler.HandleAsync(command);
        }

        public async Task SendAndPublishAsync<TCommand>(TCommand command) 
            where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandHandler = _resolver.Resolve<ICommandHandlerWithEventsAsync<TCommand>>();

            if (commandHandler == null)
                throw new ApplicationException($"No handler of type ICommandHandlerWithEventsAsync<TCommand> found for command '{command.GetType().FullName}'");

            var events = await commandHandler.HandleAsync(command);

            foreach (var @event in events)
            {
                var concreteEvent = _eventFactory.CreateConcreteEvent(@event);
                await _eventPublisher.PublishAsync(concreteEvent);
            }
        }

        public async Task SendAndPublishAsync<TCommand, TAggregate>(TCommand command) 
            where TCommand : IDomainCommand
            where TAggregate : IAggregateRoot
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandHandler = _resolver.Resolve<ICommandHandlerWithAggregateAsync<TCommand>>();

            if (commandHandler == null)
                throw new ApplicationException($"No handler of type ICommandHandlerWithAggregateAsync<TCommand> found for command '{command.GetType().FullName}'");

            var aggregateRoot = await commandHandler.HandleAsync(command);

            foreach (var @event in aggregateRoot.Events)
            {
                var concreteEvent = _eventFactory.CreateConcreteEvent(@event);
                await _eventStore.SaveEventAsync<TAggregate>((IDomainEvent)concreteEvent);
                await _eventPublisher.PublishAsync(concreteEvent);
            }
        }
    }
}
