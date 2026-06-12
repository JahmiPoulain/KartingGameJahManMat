using System;

[Serializable]
public class ScoreMetadata
{
    public string ProfileId;
    public string ProfileName;

    public ScoreMetadata(string profileId, string profileName)
    {
        ProfileId = profileId;
        ProfileName = profileName;
    }
}
