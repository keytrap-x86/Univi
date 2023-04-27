using Microsoft.Win32;

namespace Univi.Core.Interfaces;
public interface IRegistryEntry
{
    public string Name { get; set; }
    public object Value { get; set; }
    public RegistryValueKind ValueKind { get; set; }
}
