namespace DX.Contracts.ReadModels
{
    [Contract]
    public class GetAll<TItem, TResult> : ICriteria<TResult> where TResult : IReadModel {
    }
}