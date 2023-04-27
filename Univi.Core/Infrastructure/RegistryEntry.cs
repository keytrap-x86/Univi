using Microsoft.Win32;
using Univi.Core.Interfaces;

namespace Univi.Core.Infrastructure;
public class RegistryEntry : IRegistryEntry
{
    public string Name { get; set; }
    public object Value { get; set; }
    public RegistryValueKind ValueKind { get; set; }

    public RegistryEntry(string name, object value, RegistryValueKind valueKind = RegistryValueKind.DWord)
    {
        Name = name;
        Value = value;
        ValueKind = valueKind;
    }
}
