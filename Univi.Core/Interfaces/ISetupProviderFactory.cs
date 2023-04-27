namespace Univi.Core.Interfaces;
public interface ISetupProviderFactory
{
    ISetupProvider Create(string softwareConfiguration);
}
