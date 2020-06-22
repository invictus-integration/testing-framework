using System.Threading.Tasks;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Represents an abstracted way to define setup/teardown functions in an <see cref="IAsyncDisposable"/> implementation.
    /// </summary>
    public class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<Task> _teardown;

        private AsyncDisposable(Func<Task> teardown)
        {
            Guard.NotNull(teardown, nameof(teardown));
            _teardown = teardown;
        }

        /// <summary>
        /// Create an instance of the <see cref="AsyncDisposable"/> class to simulate setup/teardown actions.
        /// </summary>
        /// <param name="teardown">The action to run when the instance is being disposed.</param>
        public static AsyncDisposable Create(Func<Task> teardown)
        {
            Guard.NotNull(teardown, nameof(teardown));
            return new AsyncDisposable(teardown);
        }

        /// <summary>
        /// Create an instance of the <see cref="AsyncDisposable"/> class to simulate setup/teardown actions.
        /// </summary>
        /// <param name="setup">The action to run when the instance is created (now).</param>
        /// <param name="teardown">The action to run when the instance is being disposed.</param>
        public static async Task<AsyncDisposable> CreateAsync(Func<Task> setup, Func<Task> teardown)
        {
            Guard.NotNull(setup, nameof(setup));
            Guard.NotNull(teardown, nameof(teardown));

            await setup();
            return new AsyncDisposable(teardown);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _teardown();
        }
    }
}
