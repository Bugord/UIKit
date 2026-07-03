using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI.Navigation
{
    public abstract class ResultView<TViewModel, TResult> : View<TViewModel>, IResultView<TResult> where TViewModel : ResultViewModel<TResult>
    {
        protected ResultView(TViewModel model) : base(model)
        {
        }

        public UniTask<TResult> WaitForResultAsync(CancellationToken token)
        {
            return Model.WaitForResultAsync(token);
        }
    }
}