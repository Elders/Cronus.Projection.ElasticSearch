using System;
using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.IocContainer;
using Elders.Cronus.MessageProcessing;
using Elders.Cronus.Pipeline.Config;

namespace Elders.Cronus.Projections.ElasticSearch.Config
{
    public class LuceneMessageProcessorSettings : SettingsBuilder, IMessageProcessorSettings<IEvent>
    {
        public LuceneMessageProcessorSettings(ISettingsBuilder builder, Func<Type, bool> discriminator) : base(builder)
        {
            this.discriminator = discriminator;
        }
        private Func<Type, bool> discriminator;

        string IMessageProcessorSettings<IEvent>.MessageProcessorName { get; set; }

        public Dictionary<Type, List<Tuple<Type, Func<Type, object>>>> HandlerRegistrations { get; set; }

        public string SubscriptionName { get; set; }

        public override void Build()
        {
            var builder = this as ISettingsBuilder;
            var messageProcessorSettings = this as IMessageProcessorSettings<IEvent>;
            var contractsRepository = builder.Container.Resolve<IContractsRepository>();
            var projectionsApi = builder.Container.Resolve<ProjectionApi>();
            Func<IMessageProcessor> messageHandlerProcessorFactory = () =>
            {
                IMessageProcessor processor = new MessageProcessor(messageProcessorSettings.MessageProcessorName, builder.Container);
                foreach (var contract in contractsRepository.Contracts)
                {
                    if (typeof(IEvent).IsAssignableFrom(contract))
                    {
                        var handlerFactory = new DefaultHandlerFactory(typeof(IndexProjection), x => new IndexProjection(projectionsApi));
                        processor.Subscribe(new ElasticSearchSubscription(SubscriptionName, contract, handlerFactory));
                    }
                }
                projectionsApi.ConfigureMappings();
                var asd = processor.GetSubscriptions().Single();
                return processor;
            };
            builder.Container.RegisterSingleton<IMessageProcessor>(() => messageHandlerProcessorFactory(), builder.Name);
        }
    }

    public static class LuceneMessageProcessorSettingsExtensions
    {
        public static T UseLuceneProjections<T>(this T self, Action<LuceneMessageProcessorSettings> configure = null) where T : IConsumerSettings<IEvent>
        {
            LuceneMessageProcessorSettings settings = new LuceneMessageProcessorSettings(self, null);
            (settings as IMessageProcessorSettings<IEvent>).MessageProcessorName = "ElasticSearchProjections";
            if (configure != null)
                configure(settings);

            (settings as ISettingsBuilder).Build();
            return self;
        }
    }
}
