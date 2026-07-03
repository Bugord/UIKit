using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIToolkitMVVM.Views
{
    public interface IResultView<TResult> : IView
    {
        UniTask<TResult> WaitForResultAsync(CancellationToken token);
    }
}