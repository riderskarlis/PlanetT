using UnityEngine;

public static class GameSetupData
{
    public static string profileName = "Unknown Explorer";
    public static string customSeed = "";
    public static int planetCount = 5;
    public static int colorIndex = 0;

    public static int GetParsedSeed()
    {
        if (string.IsNullOrEmpty(customSeed))
        {
            // Fallback seed when input is empty
            return System.Environment.TickCount;
        }
        return int.TryParse(customSeed, out int val) ? val : customSeed.GetHashCode();
    }
}
