namespace DX.Contracts
{
    // TODO: Move to Core namespace (needs customization of codegen)
    public interface IBuilds<out T> {
        T Build();
    }
}
