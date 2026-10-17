using System.Text.Json;

namespace Arietta.Helpers;

public static class JsonOpts
{

    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    public static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
}
