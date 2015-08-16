﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using KafkaNet.Protocol;
using Orleans.KafkaStreamProvider.KafkaQueue;
using Orleans.Streams;
using Orleans.Runtime;


namespace OrleansKafkaUtilsTests
{
    [TestClass]
    [SuppressMessage("ReSharper", "UnusedVariable")]
    public class KafkaQueueAdapterUnitTests
    {
        private readonly HashRingBasedStreamQueueMapper _streamQueueMapper = new HashRingBasedStreamQueueMapper(4, "test");
        private string _providerName = "Test";        
        private readonly Logger _logger;
        private readonly KafkaStreamProviderOptions _options;

        public KafkaQueueAdapterUnitTests()
        {
            Mock<Logger> loggerMock = new Mock<Logger>();
            var connectionStrings = new List<Uri> {new Uri("http://192.168.10.27:9092")};
            var topicName = "TestTopic";
            var consumerGroupName = "TestConsumerGroup";

            _logger = loggerMock.Object;
            _options = new KafkaStreamProviderOptions(connectionStrings.ToArray(), topicName, consumerGroupName);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "options")]
        public void CtorNullOptionsTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();
            
            var adapter = new KafkaQueueAdpater(_streamQueueMapper, null, _providerName, factoryMock.Object, _logger);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "Provider cannot be null or empty")]
        public void CtorNullProviderNameTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();
            var adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, null, factoryMock.Object, _logger);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "Provider cannot be null or empty")]
        public void CtorEmptyProviderNameTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();
            var adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, string.Empty, factoryMock.Object, _logger);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "QueueMapper")]
        public void CtorNullQueueMapperTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();
            var adapter = new KafkaQueueAdpater(null, _options, _providerName, factoryMock.Object, _logger);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "BatchFactory")]
        public void CtorNullBatchFactoryTest()
        {
            var adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, null, _logger);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        [ExpectedException(typeof(ArgumentNullException), "Topic")]
        public void CtorTopicNullLoggerTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();
            var adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, null);
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public async Task QueueMessageBatchAsyncSimpleTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns(new Message(){Value = new byte[] { 0, 1, 2, 3 }});

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, _logger);

            await adapter.QueueMessageBatchAsync(Guid.NewGuid(), "Test", new List<int>() { 1, 2, 3, 4 });
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public async Task QueueMessageBatchAsyncQueueingTwiceSameQueueTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns(new Message() { Value = new byte[] { 0, 1, 2, 3 } });

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, _logger);

            Guid myGuid = Guid.NewGuid();

            await adapter.QueueMessageBatchAsync(myGuid, "Test", new List<int>() { 1, 2, 3, 4 });
            await adapter.QueueMessageBatchAsync(myGuid, "Test", new List<int>() { 1, 2, 3, 4 });
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public async Task QueueMessageBatchAsyncOneMessageIsFaultyTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), 1)).Returns(new Message() { Value = new byte[] { 0, 1, 2, 3 } });
            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), 2)).Returns((Message) null);

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, _logger);

            Guid myGuid = Guid.NewGuid();

            await adapter.QueueMessageBatchAsync(myGuid, "Test", new List<int>() {1, 2});
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public async Task QueueMessageBatchAsyncAllMessagesAreFaultyTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns((Message) null);

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, _logger);

            Guid myGuid = Guid.NewGuid();

            await adapter.QueueMessageBatchAsync(myGuid, "Test", new List<int>() { 1, 2, 3, 4 });
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public async Task QueueMessageBatchAsyncAllNoAck()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns((Message)null);

            KafkaStreamProviderOptions differentOptions = new KafkaStreamProviderOptions(_options.ConnectionStrings,
                _options.TopicName, _options.ConsumerGroupName) {AckLevel = 0};

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, differentOptions, _providerName, factoryMock.Object, _logger);

            Guid myGuid = Guid.NewGuid();

            await adapter.QueueMessageBatchAsync(myGuid, "Test", new List<int>() { 1, 2, 3, 4 });
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("KafkaStreamProvider")]
        public void QueueMessageBatchAsyncQueueingTwiceDifferentQueuesTest()
        {
            Mock<IKafkaBatchFactory> factoryMock = new Mock<IKafkaBatchFactory>();

            factoryMock.Setup(x => x.ToKafkaMessage(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).Returns(new Message() { Value = new byte[] { 0, 1, 2, 3 } });

            KafkaQueueAdpater adapter = new KafkaQueueAdpater(_streamQueueMapper, _options, _providerName, factoryMock.Object, _logger);            

            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            // Becuase we cannot mock the queue mapper.. we need to make sure we two guids that will return different queues...
            bool willGiveDifferentQueue = !(_streamQueueMapper.GetQueueForStream(first, "test").Equals(_streamQueueMapper.GetQueueForStream(second, "otherTest")));
            while (!willGiveDifferentQueue)
            {
                second = Guid.NewGuid();
                willGiveDifferentQueue = !(_streamQueueMapper.GetQueueForStream(first, "test").Equals(_streamQueueMapper.GetQueueForStream(second, "otherTest")));
            }

            Task.WaitAll(adapter.QueueMessageBatchAsync(first, "test", new List<int>() { 1, 2, 3, 4 }),
                         adapter.QueueMessageBatchAsync(second, "otherTest", new List<int>() { 1, 2, 3, 4 }));
        }
    }
}