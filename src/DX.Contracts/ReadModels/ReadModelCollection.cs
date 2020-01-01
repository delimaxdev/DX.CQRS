using System.Collections.Generic;

namespace DX.Contracts.ReadModels
{
    public class ReadModelCollection<TItem> : IReadModel where TItem : IReadModel {
        public ICollection<TItem> Items { get; } = new List<TItem>();
    }
}