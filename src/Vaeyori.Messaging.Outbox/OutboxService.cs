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
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class OutboxService : IOutboxService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private CancellationTokenSource _wakeupCancellationTokenSource = new CancellationTokenSource();
        public OutboxService(
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public ValueTask WakeupAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _wakeupCancellationTokenSource.Cancel();

            return ValueTask.CompletedTask;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await PublishAsync(cancellationToken);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _wakeupCancellationTokenSource.Token);
            linkedCancellationTokenSource.Cancel();

            return ValueTask.CompletedTask;
        }


        private async ValueTask PublishAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var messageStore = scope.ServiceProvider.GetService<IMessageStore>();
                var strategy = scope.ServiceProvider.GetService<IOutboxStrategy>();

                var events = await messageStore.ReadAsync(cancellationToken);

                foreach (var e in events)
                {
                    await strategy.HandleAsync(e, cancellationToken);

                    await messageStore.RemoveAsync(e, cancellationToken);
                    await messageStore.SaveAsync(cancellationToken);
                }

                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_wakeupCancellationTokenSource.Token);

                try
                {
                    await Task.Delay(Timeout.Infinite, linkedCancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    if (_wakeupCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var _ = _wakeupCancellationTokenSource;
                        _wakeupCancellationTokenSource = new CancellationTokenSource();
                        _.Dispose();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            return;
        }
    }
}
