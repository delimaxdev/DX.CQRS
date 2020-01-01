using DX.Contracts.ReadModels;
using System.Collections.Generic;

namespace DX.Contracts.Cqrs.Queries {
    [Contract]
    public class GetAllCommands : ICriteria<IReadOnlyCollection<CommandRM>> {
    }
}