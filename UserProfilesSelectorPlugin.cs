using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using UnityEngine;
using System.IO;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine.AddressableAssets;
using Zio;
using Zio.FileSystems;
using System;
using System.Reflection;

namespace UserProfilesSelector
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class UserProfilesSelectorPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.UserProfilesSelector";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "UserProfilesSelector";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<string> ProfilesFolder;

        public void Awake()
        {
            Log.Init(Logger);

            ProfilesFolder = Config.Bind("Configuration", "User profiles folder", "UserProfiles2", "Choose the folder that stores the profiles. See C:\\Program Files (x86)\\Steam\\userdata\\<number>\\632360\\remote");
            ModSettingsManager.AddOption(new StringInputFieldOption(ProfilesFolder));

            On.RoR2.SaveSystem.PlatformInitProfile += SaveSystem_PlatformInitProfile;
            On.RoR2.SaveSystem.LoadUserProfiles += SaveSystem_LoadUserProfiles;

            var texture = LoadTexture("icon.png");
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(sprite);
        }

        private void SaveSystem_PlatformInitProfile(On.RoR2.SaveSystem.orig_PlatformInitProfile orig, SaveSystem self, ref UserProfile newProfile, Zio.IFileSystem fileSystem, string name)
        {
            orig(self, ref newProfile, fileSystem, name);
            newProfile.filePath = (UPath)($"/{ProfilesFolder.Value}/{newProfile.fileName}.xml");
        }

        private void SaveSystem_LoadUserProfiles(On.RoR2.SaveSystem.orig_LoadUserProfiles orig, SaveSystem self)
        {
            self.badFileResults.Clear();
            self.loadedUserProfiles.Clear();
            UserProfile.LoadDefaultProfile();
            FileSystem cloudStorage = RoR2Application.cloudStorage;
            if (cloudStorage != null)
            {
                if (!cloudStorage.DirectoryExists((UPath)$"/{ProfilesFolder.Value}"))
                    cloudStorage.CreateDirectory((UPath)$"/{ProfilesFolder.Value}");
                foreach (UPath enumeratePath in cloudStorage.EnumeratePaths((UPath)$"/{ProfilesFolder.Value}"))
                {
                    if (cloudStorage.FileExists(enumeratePath) && string.CompareOrdinal(enumeratePath.GetExtensionWithDot(), ".xml") == 0)
                    {
                        LoadUserProfileOperationResult profileOperationResult = self.LoadUserProfileFromDisk(cloudStorage, enumeratePath);
                        UserProfile userProfile = profileOperationResult.userProfile;
                        if (userProfile != null)
                            self.loadedUserProfiles[userProfile.fileName] = userProfile;
                        if (profileOperationResult.exception != null)
                            self.badFileResults.Add(profileOperationResult);
                    }
                }
                self.OutputBadFileResults();

                Action onAvailableUserProfilesChanged = (Action)typeof(SaveSystem).GetField("onAvailableUserProfilesChanged", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

                if (onAvailableUserProfilesChanged == null)
                    return;
                onAvailableUserProfilesChanged();
            }
            else
                Debug.LogError("cloud storage is null");
        }

        private Texture2D LoadTexture(string name)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), name)));
            return texture;
        }        
    }
}
