﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Discord;

namespace Mewdeko.Modules.Administration.Common
{
    public sealed class UserSpamStats : IDisposable
    {
        private readonly object applyLock = new();

        public UserSpamStats(IUserMessage msg)
        {
            LastMessage = msg.Content.ToUpperInvariant();
            timers = new ConcurrentQueue<Timer>();

            ApplyNextMessage(msg);
        }

        public int Count => timers.Count;
        public string LastMessage { get; set; }

        private ConcurrentQueue<Timer> timers { get; }

        public void Dispose()
        {
            while (timers.TryDequeue(out var old))
                old.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void ApplyNextMessage(IUserMessage message)
        {
            lock (applyLock)
            {
                var upperMsg = message.Content.ToUpperInvariant();
                if (upperMsg != LastMessage || string.IsNullOrWhiteSpace(upperMsg) && message.Attachments.Any())
                {
                    LastMessage = upperMsg;
                    while (timers.TryDequeue(out var old))
                        old.Change(Timeout.Infinite, Timeout.Infinite);
                }

                var t = new Timer(_ =>
                {
                    if (timers.TryDequeue(out var old))
                        old.Change(Timeout.Infinite, Timeout.Infinite);
                }, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
                timers.Enqueue(t);
            }
        }
    }
}