/*
*    Copyright (C) 2021 Joshua "ysovuka" Thompson
*
*    This program is free software: you can redistribute it and/or modify
*    it under the terms of the GNU Affero General Public License as published
*    by the Free Software Foundation, either version 3 of the License, or
*    (at your option) any later version.
*
*    This program is distributed in the hope that it will be useful,
*    but WITHOUT ANY WARRANTY; without even the implied warranty of
*    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*    GNU Affero General Public License for more details.
*
*    You should have received a copy of the GNU Affero General Public License
*    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Vaeyori.Messaging.Outbox.UnitTests
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal class MockOutboxServiceBootStrap
    {
        private readonly Mock<IOutboxStrategy> _outboxStrategy;
        private readonly Mock<IMessageStore> _messageStore;

        public MockOutboxServiceBootStrap()
        {
            _outboxStrategy = new Mock<IOutboxStrategy>();
            _outboxStrategy.Setup(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));

            _messageStore = new Mock<IMessageStore>();
            _messageStore.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Message>()
            {
                new Message(Guid.NewGuid(), string.Empty, DateTimeOffset.UtcNow, string.Empty, string.Empty)
            }.AsEnumerable()));
            _messageStore.Setup(x => x.RemoveAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));
            _messageStore.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()));

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IOutboxStrategy))).Returns(_outboxStrategy.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IMessageStore))).Returns(_messageStore.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            ServiceScopeFactory = serviceScopeFactory.Object;
        }

        public Mock<IOutboxStrategy> OutboxStrategyMock => _outboxStrategy;
        public IServiceScopeFactory ServiceScopeFactory { get; }

        public void Verify()
        {
            _outboxStrategy.Verify(x => x.HandleAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));
            _messageStore.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()));
            _messageStore.Verify(x => x.RemoveAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));
            _messageStore.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()));
        }
    }

    public class OutboxServiceUnitTests
    {
        [Fact]
        public void OutboxService_Ctor_Successful()
        {
            var bootstrap = new MockOutboxServiceBootStrap();
            var outboxService = new OutboxService(bootstrap.ServiceScopeFactory);

            Assert.NotNull(outboxService);
        }

        [Fact]
        public async ValueTask OutboxService_Start_SuccessfullyStarts()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var bootstrap = new MockOutboxServiceBootStrap();
            var outboxService = new OutboxService(bootstrap.ServiceScopeFactory);

            await outboxService.StartAsync(cancellationToken);

            bootstrap.Verify();
        }

        [Fact]
        public async ValueTask OutboxService_Stop_SuccessfullyStops()
        {
            var bootstrap = new MockOutboxServiceBootStrap();
            var outboxService = new OutboxService(bootstrap.ServiceScopeFactory);

            await outboxService.StopAsync();

            Assert.NotNull(outboxService);
        }
    }
}
