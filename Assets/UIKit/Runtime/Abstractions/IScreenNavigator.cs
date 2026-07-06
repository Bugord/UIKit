using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIKit.Runtime.Abstractions
{
    public interface IScreenNavigator
    {
        UniTask<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default) where TViewModel : ResultViewModel<TResult>;
        UniTask<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload, CancellationToken token = default) where TViewModel : ResultViewModel<TResult>, IPayloadReceiver<TPayload>;
        UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default) where TViewModel : ViewModel;
        UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload, CancellationToken token = default) where TViewModel : ViewModel, IPayloadReceiver<TPayload>;
        UniTask CloseAsync<TViewModel>(TViewModel viewModel, CancellationToken token = default) where TViewModel : ViewModel;
        bool IsOpen<TViewModel>(TViewModel viewModel) where TViewModel : ViewModel;
    }
}