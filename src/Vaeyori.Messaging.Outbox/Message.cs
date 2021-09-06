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
    using System;

    public sealed class Message
    {
        public Message(
            Guid id,
            string correlationId,
            DateTimeOffset occurredAt,
            string topic,
            string payload)
        {
            Id = id;
            CorrelationId = correlationId;
            OccurredAt = occurredAt;
            Topic = topic;
            Payload = payload;
        }

        public Guid Id { get; private set; }
        public string CorrelationId { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }
        public string Topic { get; private set; }
        public string Payload { get; private set; }
    }
}
