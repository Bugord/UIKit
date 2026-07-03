using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI.Navigation
{
    public interface IResultView<TResult> : IView
    {
        UniTask<TResult> WaitForResultAsync(CancellationToken token);
    }
}