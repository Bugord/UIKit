using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI.Navigation
{
    public interface IUIService
    {
        UniTask<TScreen> ShowScreenAsync<TScreen>(CancellationToken token = default) where TScreen : IScreen;
        UniTask<TResult> ShowScreenAndWaitResultAsync<TScreen, TResult>(CancellationToken token = default) where TScreen : IScreen, IResultView<TResult>;
        UniTask HideScreenAsync(IScreen screen, CancellationToken token = default);

        UniTask<TPopup> ShowPopupAsync<TPopup>(CancellationToken token = default) where TPopup : IPopup;
        UniTask<TResult> ShowPopupAndWaitResultAsync<TPopup, TResult>(CancellationToken token = default) where TPopup : IPopup, IResultView<TResult>;
        UniTask HidePopupAsync(IPopup popup, CancellationToken token = default);

        UniTask ReleaseAllAsync(CancellationToken token = default);
    }
}
