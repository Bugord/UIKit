using System.Threading;
using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// Opens, closes and stacks screens and popups. Operations are serialised: one started
    /// while another is running waits for it, and they apply in call order.
    /// </summary>
    /// <remarks>
    /// An operation runs the view's <c>OnAttach</c>, <c>ShowAsync</c> and <c>HideAsync</c> and
    /// the ViewModel's <c>OnOpened</c> / <c>OnClosed</c> while it holds the queue, so do not
    /// <c>await</c> another navigator call from inside them: it would wait for an operation that
    /// cannot finish until it returns. Call <c>.Forget()</c> on it instead to queue it.
    /// </remarks>
    public interface IScreenNavigator
    {
        /// <summary>
        /// Opens <typeparamref name="TViewModel"/>, waits for its result and closes it.
        /// </summary>
        /// <exception cref="System.OperationCanceledException">
        /// <paramref name="token"/> was cancelled, or the screen was closed from elsewhere
        /// without a result and is not <see cref="ICancelable"/>.
        /// </exception>
        Awaitable<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default)
            where TViewModel : ViewModel, IResultViewModel<TResult>;

        /// <inheritdoc cref="RunAsync{TViewModel,TResult}(CancellationToken)"/>
        Awaitable<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload, CancellationToken token = default)
            where TViewModel : ViewModel, IResultViewModel<TResult>, IPayloadReceiver<TPayload>;

        /// <summary>
        /// Opens <typeparamref name="TViewModel"/> on top of what is open. A screen hides the
        /// screen below it; a popup opens above everything.
        /// </summary>
        Awaitable<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel;

        /// <inheritdoc cref="OpenAsync{TViewModel}(CancellationToken)"/>
        Awaitable<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>;

        /// <summary>
        /// Opens <typeparamref name="TViewModel"/> and closes the top entry of its layer, so
        /// going back skips the replaced one.
        /// </summary>
        Awaitable<ScreenHandle<TViewModel>> ReplaceAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel;

        /// <inheritdoc cref="ReplaceAsync{TViewModel}(CancellationToken)"/>
        Awaitable<ScreenHandle<TViewModel>> ReplaceAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>;

        /// <summary>
        /// Closes the screen or popup showing <paramref name="viewModel"/>. An
        /// <see cref="ICancelable"/> ViewModel without a result is cancelled first. Does nothing
        /// if it is not open.
        /// </summary>
        Awaitable CloseAsync(ViewModel viewModel, CancellationToken token = default);

        /// <summary>
        /// Closes the top popup, or else the top screen unless it is the only one.
        /// </summary>
        /// <returns>Whether anything was closed.</returns>
        Awaitable<bool> GoBackAsync(CancellationToken token = default);

        /// <summary>
        /// Closes every popup and every screen but the bottom one.
        /// </summary>
        Awaitable PopToRootAsync(CancellationToken token = default);

        /// <summary>
        /// Closes every popup and screen.
        /// </summary>
        Awaitable CloseAllAsync(CancellationToken token = default);

        bool IsOpen(ViewModel viewModel);

        /// <summary>
        /// The ViewModel of the top popup, or of the top screen if no popup is open.
        /// </summary>
        ViewModel Top { get; }
    }
}
