using UnityEngine;

// if you want to delete this file delete all Gentleland "Utils" folder 
// you can then delete GentlelandSettings folder too
namespace Gentleland.Utils.PirateJournalUI
{
    public class PackageSettings : ScriptableObject
    {
        public const string PackageSettingsName= "GentlelandSettings_PirateJournalUI";
        public const string PackageSettingsPath = "Assets/GentlelandSettings/GentlelandSettings_PirateJournalUI.asset";
        public const string PackageSettingsFolder = "GentlelandSettings";
        public const string PackageSettingsFolderPath = "Assets/GentlelandSettings";
        public const string PackageDocumentationPath = "/Gentleland/Pirate Journal UI/Pirate Journal UI Documentation.pdf";
        public const string PackageDocumentationName = "Pirate Journal UI Documentation";
        public const string imagePath = "Assets/Gentleland/Pirate Journal UI/Overviews/PirateJournalUICard.png";
        public const string imageName = "PirateJournalUICard";
        public const string packageName = "Pirate Journal UI";
        public bool isFirstTimeUsingTheAsset = true;
    }
}
