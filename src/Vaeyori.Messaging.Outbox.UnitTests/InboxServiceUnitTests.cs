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
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    internal class MockInboxServiceBootStrap
    {
        private readonly Mock<IInboxStrategy> _inboxStrategy;
        private readonly Mock<IMessageStore> _messageStore;

        public MockInboxServiceBootStrap()
        {
            _inboxStrategy = new Mock<IInboxStrategy>();
            _inboxStrategy.Setup(x => x.ConsumeAsync(It.IsAny<CancellationToken>()));
            _inboxStrategy.Setup(x => x.AcknowledgeAsync(It.IsAny<CancellationToken>()));

            _messageStore = new Mock<IMessageStore>();
            _messageStore.Setup(x => x.WriteAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IInboxStrategy))).Returns(_inboxStrategy.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IMessageStore))).Returns(_messageStore.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            ServiceScopeFactory = serviceScopeFactory.Object;
        }

        public Mock<IInboxStrategy> InboxStrategyMock => _inboxStrategy;
        public IServiceScopeFactory ServiceScopeFactory { get; }

        public void VerifySuccessfullyStartsConsuming()
        {
            _inboxStrategy.Verify(x => x.ConsumeAsync(It.IsAny<CancellationToken>()));
        }

        public void VerifySuccessfullyConsumesMessage()
        {
            _inboxStrategy.Verify(x => x.ConsumeAsync(It.IsAny<CancellationToken>()));
            _inboxStrategy.Verify(x => x.AcknowledgeAsync(It.IsAny<CancellationToken>()));
            _messageStore.Verify(x => x.WriteAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()));
        }

    }

    public class InboxServiceUnitTests
    {
        [Fact]
        public void InboxService_Ctor_Successful()
        {
            var bootstrap = new MockInboxServiceBootStrap();
            var inboxService = new InboxService(bootstrap.ServiceScopeFactory);

            Assert.NotNull(inboxService);
        }


        [Fact]
        public async ValueTask InboxService_Start_SuccessfullyStartsConsuming()
        {
            var bootstrap = new MockInboxServiceBootStrap();
            var inboxService = new InboxService(bootstrap.ServiceScopeFactory);

            await inboxService.StartAsync();

            bootstrap.VerifySuccessfullyStartsConsuming();
        }


        [Fact]
        public async ValueTask InboxService_Start_SuccessfullyConsumesMessage()
        {
            var bootstrap = new MockInboxServiceBootStrap();
            var inboxService = new InboxService(bootstrap.ServiceScopeFactory);

            await inboxService.StartAsync();

            bootstrap.InboxStrategyMock.Raise(x => x.MessageReceived += null, this, new Message(Guid.NewGuid(), string.Empty, DateTimeOffset.UtcNow, string.Empty, string.Empty));

            bootstrap.VerifySuccessfullyConsumesMessage();
        }

        [Fact]
        public async ValueTask InboxService_Stop_SuccessfullyStops()
        {
            var bootstrap = new MockInboxServiceBootStrap();
            var inboxService = new InboxService(bootstrap.ServiceScopeFactory);

            await inboxService.StopAsync();

            Assert.NotNull(inboxService);
        }
    }
}
