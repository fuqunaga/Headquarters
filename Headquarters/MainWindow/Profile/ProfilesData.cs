using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// Profiles.jsonのデータ
/// </summary>
public class ProfilesData
{
    public List<ProfileSourceData> ProfileSources { get; set; } = [];
}

public class ProfileSourceData
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Description { get; set; }
}