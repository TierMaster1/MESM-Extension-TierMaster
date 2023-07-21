using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MonstrumExtendedSettingsMod
{
    public partial class ExtendedSettingsModScript
    {
        public static class ChallengeParser
        {
            public static readonly char SEPARATOR = '|';
            private static readonly string CHALLENGE_FOLDER = "Challenges/";
            private static readonly string NAME_IDENTIFIER = "Name";
            private static readonly string AUTHOR_IDENTIFIER = "Author";
            private static readonly string DIFFICULTY_IDENTIFIER = "Difficulty";
            private static readonly string VERSION_IDENTIFIER = "Version";
            private static readonly string DESCRIPTION_IDENTIFIER = "Description";
            private static readonly string REFERENCE_LINE = "Setting" + SEPARATOR + "Custom Value";
            private static readonly string COMPLETION_TIMES_FILE_PATH = "challengeCompletionTimes.txt"; // Check if file exists and create it.
            //public static readonly string TIME_FORMAT = "hh\\:mm\\:ss\\.ff";

            public static void SaveChallenge(Challenge challenge)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(NAME_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(challenge.name);
                stringBuilder.Append("\n");
                stringBuilder.Append(AUTHOR_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(challenge.author);
                stringBuilder.Append("\n");
                stringBuilder.Append(DIFFICULTY_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(challenge.difficulty);
                stringBuilder.Append("\n");
                if (!challenge.description.Equals(Challenge.defaultString))
                {
                    stringBuilder.Append(DESCRIPTION_IDENTIFIER);
                    stringBuilder.Append(SEPARATOR);
                    stringBuilder.Append(challenge.description);
                    stringBuilder.Append("\n");
                }
                stringBuilder.Append(VERSION_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(ExtendedSettingsModScript.VERSION_WITH_TEXT);
                stringBuilder.Append("\n");
                stringBuilder.Append(REFERENCE_LINE);
                foreach (MESMSettingCompact setting in challenge.settings)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(setting.name);
                    stringBuilder.Append(SEPARATOR);
                    stringBuilder.Append(setting.value);
                }
                Directory.CreateDirectory(CHALLENGE_FOLDER); // Create the challenges folder if it does not exist to avoid errors.
                File.WriteAllText(CHALLENGE_FOLDER + challenge.name.Replace(" ", "_").Replace("+", "_Plus") + ".txt", stringBuilder.ToString());
                Debug.Log("Saved new challenge: " + challenge.name);
            }

            public static void DeleteChallenge(Challenge challenge)
            {
                File.Delete(challenge.filePath);
            }

            public static void ReadAllChallenges()
            {
                ChallengesList.challenges = new List<Challenge>();

                Directory.CreateDirectory(CHALLENGE_FOLDER); // Create the challenges folder if it does not exist to avoid errors.
                string[] txtFiles = Directory.GetFiles(CHALLENGE_FOLDER, "*.txt");
                foreach (string challengeFilePath in txtFiles)
                {
                    Challenge challenge = ReadChallenge(challengeFilePath);
                    if (challenge != null)
                    {
                        ChallengesList.challenges.Add(challenge);
                    }
                }
            }


            public static Challenge ReadChallenge(string challengeFilePath)
            {
                // Set up a challenge variable.
                Challenge challenge = new Challenge();
                challenge.filePath = challengeFilePath;

                // Open the file and find the starting line specifying the custom settings.
                string[] challengeInformation = File.ReadAllLines(challengeFilePath);
                int startLineNumber = -1;
                for (int lineNumber = 0; lineNumber < challengeInformation.Length; lineNumber++)
                {
                    string lineContents = challengeInformation[lineNumber];
                    string[] settingNameAndValue = lineContents.Split(SEPARATOR);
                    if (settingNameAndValue.Length == 2)
                    {
                        if (settingNameAndValue[0].Equals(NAME_IDENTIFIER))
                        {
                            challenge.name = settingNameAndValue[1];
                        }
                        else if (settingNameAndValue[0].Equals(AUTHOR_IDENTIFIER))
                        {
                            challenge.author = settingNameAndValue[1];
                        }
                        else if (settingNameAndValue[0].Equals(DIFFICULTY_IDENTIFIER))
                        {
                            challenge.difficulty = settingNameAndValue[1];
                        }
                        else if (settingNameAndValue[0].Equals(DESCRIPTION_IDENTIFIER))
                        {
                            challenge.description = settingNameAndValue[1].Replace("\\n", "\n");
                        }
                        else if (settingNameAndValue[0].Equals(VERSION_IDENTIFIER))
                        {
                            challenge.version = settingNameAndValue[1];
                        }
                        else if (lineContents.Equals(REFERENCE_LINE))
                        {
                            startLineNumber = lineNumber + 1;
                            break;
                        }
                    }
                }

                // Ensure the reference line was found.
                if (startLineNumber == -1)
                {
                    Debug.Log("Could not read challenge with file path '" + challengeFilePath + "'! Reference line was not found!");
                    return null;
                }

                for (int lineNumber = startLineNumber; lineNumber < challengeInformation.Length; lineNumber++)
                {
                    string[] settingNameAndValue = challengeInformation[lineNumber].Split(SEPARATOR);
                    if (settingNameAndValue.Length == 2)
                    {
                        string settingName = settingNameAndValue[0];
                        string settingValue = settingNameAndValue[1];
                        challenge.settings.Add(new MESMSettingCompact(settingName, settingValue));
                    }
                    else
                    {
                        Debug.Log("Could not read challenge with file path '" + challengeFilePath + "'! Challenge was not in correct format!");
                        return null;
                    }
                }

                challenge.completionTime = GetChallengeTime(challenge.name);
                return challenge;
            }

            // How do I format a string into days:minutes:hours:seconds? - robhuhn - https://answers.unity.com/questions/25614/how-do-i-format-a-string-into-daysminuteshoursseco.html - Accessed 26.05.2023
            // How to Convert string "07:35" (HH:MM) to TimeSpan - Matt Johnson-Pint - https://stackoverflow.com/questions/24369059/how-to-convert-string-0735-hhmm-to-timespan - Accessed 26.05.2023
            public static TimeSpan GetChallengeTime(string name)
            {
                if (File.Exists(COMPLETION_TIMES_FILE_PATH))
                {
                    string[] challengeTimes = File.ReadAllLines(COMPLETION_TIMES_FILE_PATH);
                    if (challengeTimes != null)
                    {
                        foreach (string line in challengeTimes)
                        {
                            string[] challengeNameAndTime = line.Split(SEPARATOR);
                            if (challengeNameAndTime.Length == 2 && challengeNameAndTime[0].Equals(name))
                            {
                                TimeSpan timeSpan;
                                if (TimeSpan.TryParse(challengeNameAndTime[1], out timeSpan))
                                {
                                    return timeSpan;
                                }
                                Debug.Log("Failed to parse challenge time!");
                            }
                        }
                    }
                }
                else
                {
                    File.Create(COMPLETION_TIMES_FILE_PATH).Close();
                }
                return TimeSpan.MaxValue;
            }

            public static bool UpdateChallengeTime(Challenge challenge, TimeSpan newTime)
            {
                Debug.Log("New time is " + newTime);
                Debug.Log("Current completion time is " + challenge.completionTime);
                if (newTime < challenge.completionTime)
                {
                    challenge.completionTime = newTime;
                    if (!File.Exists(COMPLETION_TIMES_FILE_PATH))
                    {
                        File.Create(COMPLETION_TIMES_FILE_PATH).Close();
                    }
                    string[] challengeTimes = File.ReadAllLines(COMPLETION_TIMES_FILE_PATH);
                    if (challengeTimes != null)
                    {
                        for (int lineNumber = 0; lineNumber < challengeTimes.Length; lineNumber++)
                        {
                            string[] challengeNameAndTime = challengeTimes[lineNumber].Split(SEPARATOR);
                            if (challengeNameAndTime.Length == 2 && challengeNameAndTime[0].Equals(challenge.name))
                            {
                                challengeTimes[lineNumber] = challengeNameAndTime[0] + SEPARATOR + challenge.completionTime;
                                File.WriteAllLines(COMPLETION_TIMES_FILE_PATH, challengeTimes);
                                Debug.Log("Updated completion time in file: " + challenge.completionTime);
                                return true;
                            }
                        }
                        List<string> challengeTimesList = challengeTimes.ConvertToList();
                        challengeTimesList.Add(challenge.name + SEPARATOR + challenge.completionTime);
                        File.WriteAllLines(COMPLETION_TIMES_FILE_PATH, challengeTimesList.ToArray());
                        Debug.Log("Added first completion time to file: " + challenge.completionTime);
                        return true;
                    }
                }
                return false;
            }
        }

        public class Challenge
        {
            public static readonly string defaultString = "Unspecified";
            public string name = defaultString;
            public string author = defaultString;
            public string difficulty = defaultString;
            public string version = defaultString;
            public TimeSpan completionTime = TimeSpan.MaxValue;
            public string description = defaultString;
            public List<MESMSettingCompact> settings = new List<MESMSettingCompact>();
            public string filePath = defaultString;

            public bool ApplyChallenge()
            {
                MESMSetting.ResetSettingsToDefault(ModSettings.allSettings);
                if (settings == null)
                {
                    Debug.Log("Settings is null");
                    throw new NullReferenceException("Current Challenge Settings is null!");
                }
                bool appliedAllSettings = true;
                foreach (MESMSettingCompact setting in settings)
                {
                    // Apply the setting and check its return value.
                    if (!setting.ApplySetting())
                    {
                        appliedAllSettings = false;
                    }
                }
                if (appliedAllSettings)
                {
                    if (ModSettings.currentChallengeNameMESMS != null)
                    {
                        if (ModSettings.currentChallengeNameMESMS.settingsButton != null)
                        {
                            ModSettings.currentChallengeNameMESMS.settingsButton.SetText(this.name);
                            ModSettings.currentChallenge = this;
                            MESMSetting.SaveSettings();
                            return true;
                        }
                        else
                        {
                            Debug.Log("Current Challenge Settings Button is null!");
                            throw new NullReferenceException("Current Challenge Settings Button is null!");
                        }
                    }
                    else
                    {
                        Debug.Log("Current Challenge is null!");
                        throw new NullReferenceException("Current Challenge is null!");
                    }
                }
                return false;
            }

            public string CompletionTimeString()
            {
                if (completionTime == TimeSpan.MaxValue)
                {
                    return "Uncompleted";
                }
                return string.Format("{0:D2}:{1:D2}:{2:D2}", completionTime.Hours, completionTime.Minutes, completionTime.Seconds);
            }

            public bool MatchesAllSettings()
            {
                // Ensure all settings used in the challenge match the currently set user value.
                foreach (MESMSettingCompact challengeSetting in settings)
                {
                    if (!challengeSetting.Valid())
                    {
                        Debug.Log("Challenge discrepancy found! Challenge setting " + challengeSetting.name + " is not valid!");
                        return false;
                    }

                    if (!challengeSetting.value.Equals(challengeSetting.fullSetting.userValueString))
                    {
                        Debug.Log("Challenge discrepancy found! Challenge value \"" + challengeSetting.value + "\" for setting " + challengeSetting.name + " does not equal set value \"" + challengeSetting.fullSetting.userValueString + "\".");
                        return false;
                    }
                }

                // Ensure all settings not used in the challenge are at their default setting.
                foreach (MESMSetting setting in ModSettings.allSettings)
                {
                    if (!ContainsSetting(setting) && !setting.userValueString.Equals(setting.defaultValueString) && setting != ModSettings.currentChallengeNameMESMS)
                    {
                        Debug.Log("Challenge discrepancy found! The challenge does not contain the setting " + setting.modSettingsText + ", but the setting's user value " + setting.userValueString + " does not equal default value " + setting.defaultValueString + ".");

                        return false;
                    }
                }

                return true;
            }

            public bool ContainsSetting(MESMSetting setting)
            {
                foreach (MESMSettingCompact challengeSetting in settings)
                {
                    if (challengeSetting.Valid() && challengeSetting.fullSetting.modSettingsText.Equals(setting.modSettingsText))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public class MESMSettingCompact
        {
            public string name;
            public string value;
            public MESMSetting fullSetting;

            public MESMSettingCompact(string name, string value)
            {
                this.name = name;
                this.value = value;

                if (ModSettings.allSettings != null)
                {
                    foreach (MESMSetting mESMSetting in ModSettings.allSettings)
                    {
                        if (mESMSetting.modSettingsText.Equals(name))
                        {
                            fullSetting = mESMSetting;
                            break;
                        }
                    }
                }
            }

            public MESMSettingCompact(MESMSetting fullSetting)
            {
                this.name = fullSetting.modSettingsText;
                this.value = fullSetting.userValueString;
                this.fullSetting = fullSetting;
            }

            public bool ApplySetting()
            {
                // Ensure the setting is editable.
                if (Valid() && fullSetting.settingsButton != null)
                {
                    fullSetting.settingsButton.SetText(value);
                    return true;
                }
                Debug.Log("Error applying challenge setting: " + name);
                return false;
            }

            public bool Valid()
            {
                return fullSetting != null;
            }
        }
    }
}