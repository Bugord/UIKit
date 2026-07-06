using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIKit.Runtime.Abstractions
{
    public interface IResultViewModel<TResult>
    {
        UniTask<TResult> WaitForResultAsync(CancellationToken token = default);
        void SetResult(TResult result);
    }
}