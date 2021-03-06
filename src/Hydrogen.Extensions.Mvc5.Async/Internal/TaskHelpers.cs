﻿using System.Threading.Tasks;

namespace Hydrogen.Extensions.Mvc5.Async.Internal
{
    public static class TaskHelpers
    {
        public static Task CompletedTask { get; } = Task.FromResult(default(AsyncVoid));

        private struct AsyncVoid
        {
        }
    }
}
