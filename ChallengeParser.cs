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
            private static readonly string SEPARATOR = ",";
            private static readonly string CHALLENGE_FOLDER = "Challenges/";
            private static readonly string NAME_IDENTIFIER = "Name";
            private static readonly string AUTHOR_IDENTIFIER = "Author";
            private static readonly string DIFFICULTY_IDENTIFIER = "Difficulty";
            private static readonly string VERSION_IDENTIFIER = "Version";
            private static readonly string REFERENCE_LINE = "Setting" + SEPARATOR + "Custom Value";
            private static readonly string COMPLETION_TIMES_FILE_NAME = "challengeCompletionTimes.txt";
            //public static readonly string TIME_FORMAT = "hh\\:mm\\:ss\\.ff";

            public static void SaveChallenge(string name, string author, string difficulty)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(NAME_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(name);
                stringBuilder.Append("\n");
                stringBuilder.Append(AUTHOR_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(author);
                stringBuilder.Append("\n");
                stringBuilder.Append(DIFFICULTY_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(difficulty);
                stringBuilder.Append("\n");
                stringBuilder.Append(VERSION_IDENTIFIER);
                stringBuilder.Append(SEPARATOR);
                stringBuilder.Append(ExtendedSettingsModScript.VERSION_WITH_TEXT);
                stringBuilder.Append("\n");
                stringBuilder.Append(REFERENCE_LINE);
                foreach (MESMSetting mESMSetting in ModSettings.allSettings)
                {
                    if (!mESMSetting.userValueString.Equals(mESMSetting.defaultValueString))
                    {
                        stringBuilder.Append("\n");
                        stringBuilder.Append(mESMSetting.modSettingsText);
                        stringBuilder.Append(SEPARATOR);
                        stringBuilder.Append(mESMSetting.userValueString);
                    }
                }
                File.WriteAllText(CHALLENGE_FOLDER + name + ".txt", stringBuilder.ToString());
            }

            public static List<Challenge> ReadAllChallenges()
            {
                List<Challenge> challenges = new List<Challenge>();

                string[] txtFiles = Directory.GetFiles(CHALLENGE_FOLDER, "*.txt");
                foreach (string challengeFilePath in txtFiles)
                {
                    challenges.Add(ReadChallenge(challengeFilePath));
                }

                return challenges;
            }


            public static Challenge ReadChallenge(string challengeFilePath)
            {
                // Set up a challenge variable.
                Challenge challenge = new Challenge();

                // Open the file and find the starting line specifying the custom settings.
                string[] challengeInformation = File.ReadAllLines(challengeFilePath);
                int startLineNumber = -1;
                for (int lineNumber = 0; lineNumber < challengeInformation.Length; lineNumber++)
                {
                    string lineContents = challengeInformation[lineNumber];
                    string[] settingNameAndValue = lineContents.Split(new string[] { SEPARATOR }, System.StringSplitOptions.None);
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
                    Debug.Log("Could not read challenge! Reference line was not found!");
                    return null;
                }

                for (int lineNumber = startLineNumber; lineNumber < challengeInformation.Length; lineNumber++)
                {
                    string[] settingNameAndValue = challengeInformation[lineNumber].Split(new string[] { SEPARATOR }, System.StringSplitOptions.None);
                    if (settingNameAndValue.Length == 2)
                    {
                        string settingName = settingNameAndValue[0];
                        string settingValue = settingNameAndValue[1];
                        MESMSettingCompact mESMSettingCompact = new MESMSettingCompact(settingName, settingValue);
                        if (mESMSettingCompact.fullSetting != null)
                        {
                            challenge.settings.Add(mESMSettingCompact);
                        }
                    }
                    else
                    {
                        Debug.Log("Could not read challenge! Challenge was not in correct format!");
                    }
                }

                challenge.completionTime = GetChallengeTime(challenge.name);
                return challenge;
            }

            // How do I format a string into days:minutes:hours:seconds? - robhuhn - https://answers.unity.com/questions/25614/how-do-i-format-a-string-into-daysminuteshoursseco.html - Accessed 26.05.2023
            // How to Convert string "07:35" (HH:MM) to TimeSpan - Matt Johnson-Pint - https://stackoverflow.com/questions/24369059/how-to-convert-string-0735-hhmm-to-timespan - Accessed 26.05.2023
            public static TimeSpan GetChallengeTime(string name)
            {
                string[] challengeTimes = File.ReadAllLines(COMPLETION_TIMES_FILE_NAME);
                if (challengeTimes != null)
                {
                    foreach (string line in challengeTimes)
                    {
                        string[] challengeNameAndTime = line.Split(new string[] { SEPARATOR }, System.StringSplitOptions.None);
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
                return TimeSpan.MaxValue;
            }

            public static bool UpdateChallengeTime(Challenge challenge, TimeSpan newTime)
            {
                Debug.Log("New time is " + newTime);
                Debug.Log("Current completion time is " + challenge.completionTime);
                if (newTime < challenge.completionTime)
                {
                    challenge.completionTime = newTime;
                    string[] challengeTimes = File.ReadAllLines(COMPLETION_TIMES_FILE_NAME);
                    if (challengeTimes != null)
                    {
                        for (int lineNumber = 0; lineNumber < challengeTimes.Length; lineNumber++)
                        {
                            string[] challengeNameAndTime = challengeTimes[lineNumber].Split(new string[] { SEPARATOR }, System.StringSplitOptions.None);
                            if (challengeNameAndTime.Length == 2 && challengeNameAndTime[0].Equals(challenge.name))
                            {
                                challengeTimes[lineNumber] = challengeNameAndTime[0] + SEPARATOR + challenge.completionTime;
                                File.WriteAllLines(COMPLETION_TIMES_FILE_NAME, challengeTimes);
                                Debug.Log("Updated completion time in file: " + challenge.completionTime);
                                return true;
                            }
                        }
                        List<string> challengeTimesList = challengeTimes.ConvertToList();
                        challengeTimesList.Add(challenge.name + SEPARATOR + challenge.completionTime);
                        File.WriteAllLines(COMPLETION_TIMES_FILE_NAME, challengeTimesList.ToArray());
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
            public List<MESMSettingCompact> settings = new List<MESMSettingCompact>();

            public void ApplyChallenge()
            {
                // Apply each setting and then apply each default value.
            }

            public string CompletionTimeString()
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", completionTime.Hours, completionTime.Minutes, completionTime.Seconds);
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

            public bool ApplySetting()
            {
                if (fullSetting != null && fullSetting.settingsButton != null)
                {
                    fullSetting.settingsButton.SetText(value);
                    return true;
                }
                return false;
            }
        }
    }
}