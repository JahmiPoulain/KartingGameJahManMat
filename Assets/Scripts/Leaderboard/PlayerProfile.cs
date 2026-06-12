using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public string Id;
    public string Name;
    public List<string> PreviousMemberIds = new();

    public PlayerProfile(string id, string name)
    {
        Id = id;
        Name = name;
        PreviousMemberIds = new List<string>();
    }
}
