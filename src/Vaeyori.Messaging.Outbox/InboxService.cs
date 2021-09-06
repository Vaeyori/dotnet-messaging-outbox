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

namespace Vaeyori.Messaging.Outbox
{
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class InboxService : IInboxService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public InboxService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ConsumeAsync(cancellationToken);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.CompletedTask;
        }

        private async ValueTask ConsumeAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var strategy = scope.ServiceProvider.GetService<IInboxStrategy>();

            strategy.MessageReceived += async (sender, e) =>
                await Strategy_MessageReceived(strategy, e, cancellationToken);

            await strategy.ConsumeAsync(cancellationToken);
        }

        private async ValueTask Strategy_MessageReceived(IInboxStrategy sender, Message e, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var messageStore = scope.ServiceProvider.GetService<IMessageStore>();

            await messageStore.WriteAsync(e, cancellationToken);

            await sender.AcknowledgeAsync(cancellationToken);
        }
    }
}
