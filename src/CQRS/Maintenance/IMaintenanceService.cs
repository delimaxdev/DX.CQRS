using System.Threading.Tasks;

namespace DX.Cqrs.Maintenance {
    public interface IMaintenanceService {
        Task RunScript(MaintenanceScript script);
    }
}