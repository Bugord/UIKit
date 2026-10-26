using System.Threading;
using UnityEngine;

namespace UIKit
{
    public interface IResultViewModel<TResult>
    {
        Awaitable<TResult> WaitForResultAsync(CancellationToken token = default);
        void SetResult(TResult result);
    }
}
