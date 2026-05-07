using SoulsFormats;

using static BoreSoulsResource.UtilEnums;
using static BoreSoulsResource.UtilGame;

namespace BoreSoulsResource
{
    public static class UtilFile
    {
        public static string GetWorkingDirectory() => System.AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');

        public static bool CheckUnpackedFileExists(GameType gameType, string path)
        {
            if (!File.Exists(path))
            {
                System.Media.SystemSounds.Exclamation.Play();
                if (gameType == GameType.DS1)
                {
                    MessageBox.Show("Unpacked game files cannot be found! Please unpack game files.", "Error");
                }
                else
                {
                    MessageBox.Show("Game files cannot be found! Please reinstall the game.", "Error");
                }
                return false;
            }
            return true;
        }

        public static bool CheckUnpackedFileExists(string path)
        {
            if (!File.Exists(path))
            {
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show($"Cannot locate game file at: {path}.", "Error");
                return false;
            }
            return true;
        }

        public static List<BinderFile> GetBNDFiles(string path, GameType gameType)
        {
            List<BinderFile> list;
            BND3 bnd3;
            BND4 bnd4;

            bool isRegulation = true;
            try
            {
                if (BND4.Is(path) || BND3.Is(path))
                    isRegulation = false; //file is a BND
            }
            catch (DllNotFoundException)
            {
                //oodle dll is required, but missing.
                if (CheckOodle(gameType) == false)
                    throw new Exception(); //User cancelled oodle check, cancel comparison.

                if (BND4.Is(path) || BND3.Is(path))
                    isRegulation = false; //file is a BND
            }

            switch (gameType)
            {
                case GameType.DES:
                case GameType.DS1:
                case GameType.DS1R:
                    bnd3 = BND3.Read(path);
                    list = bnd3.Files;
                    break;
                case GameType.DS2: //untested
                case GameType.DS2S:
                //case bb: //untested
                case GameType.SDT:
                    bnd4 = BND4.Read(path);
                    list = bnd4.Files;
                    break;
                case GameType.DS3:
                    if (isRegulation)
                        bnd4 = SFUtil.DecryptDS3Regulation(path);
                    else
                        bnd4 = BND4.Read(path);
                    list = bnd4.Files;
                    break;
                case GameType.ER:
                    if (isRegulation)
                        bnd4 = SFUtil.DecryptERRegulation(path);
                    else
                        bnd4 = BND4.Read(path);
                    list = bnd4.Files;
                    break;
                default:
                    throw new Exception("Bad game type: " + gameType);
            }

            return list;
        }

        public static string GetGameParamPath(GameType game, string gamePath)
        {
            switch (game)
            {
                case GameType.DES:
                    // Debug uses .dcx versions of params anyway.
                    string paramPath1 = $@"{gamePath}\param\gameparam\gameparamna.parambnd.dcx";
                    string paramPath2 = $@"{gamePath}\param\gameparam\gameparam.parambnd.dcx";

                    if (File.Exists(paramPath1))
                    {
                        return paramPath1;
                    }
                    else if (File.Exists(paramPath2))
                    {
                        return paramPath2;
                    }

                    System.Media.SystemSounds.Exclamation.Play();
                    MessageBox.Show("Game files cannot be found!", "Error");
                    throw new Exception("Game files cannot be found");
                case GameType.DS1:
                    return $@"{gamePath}\param\GameParam\GameParam.parambnd";
                case GameType.DS1R:
                    return $@"{gamePath}\param\GameParam\GameParam.parambnd.dcx";
                case GameType.DS2:
                case GameType.DS2S:
                case GameType.DS3:
                case GameType.ER:
                case GameType.SDT:
                default:
                    throw new NotImplementedException();
            }
        }

        public static Dictionary<string, PARAM> GetLooseParams(string parentDirectory, GameType gameType)
        {
            var defs = GetParamDefs(gameType);
            var paramy = ApplyAllParamDefs(parentDirectory, defs);
            return paramy;
        }
        public static string GetOutputPath(string dataPath, string filePath, bool isDCX = true)
        {
            string outputDirectory = $"{Directory.GetCurrentDirectory()}\\output\\{GetVirtualPath(dataPath, filePath)}";
            string fileName = Path.GetFileName(filePath);
            string outputPath = $"{outputDirectory}\\{fileName}";
            Directory.CreateDirectory(outputDirectory);
            if (isDCX && !outputPath.EndsWith(".dcx"))
            {
                outputPath += ".dcx";
            }
            return outputPath;
        }

        public static List<PARAMDEF> GetParamDefs(GameType gametype)
        {
            List<PARAMDEF> paramdefs = new();
            foreach (string path in Directory.GetFiles("Paramdex\\" + gametype + "\\Defs", "*.xml"))
            {
                var paramdef = PARAMDEF.XmlDeserialize(path);
                paramdefs.Add(paramdef);
            }
            return paramdefs;
        }

        public static Dictionary<string, PARAM> GetParams(string bndPath, GameType gameType)
        {
            var bnds = GetBNDFiles(bndPath, gameType);
            var defs = GetParamDefs(gameType);
            var paramy = ApplyAllParamDefs(bnds, defs);
            return paramy;
        }

        public static string GetVirtualPath(string dataPath, string filePath)
        {
            return filePath.Split(dataPath)[1];
        }

        public static bool BackupFilesExist(string installDirectory, string backupExtension)
        {
            return GetBackupFiles(installDirectory, backupExtension).Length > 0;
        }

        public static string[] GetBackupFiles(string installDirectory, string backupExtension)
        { 
            return Directory.GetFiles($"{installDirectory}", $"*{backupExtension}", SearchOption.AllDirectories);
        }

        public static void RestoreBackups(string installDirectory, string backupExtension, bool showMessages = true)
        {
            string[] files = GetBackupFiles(installDirectory, backupExtension);

            foreach (var file in files)
            {
                RestoreBackup(file, file.Replace(backupExtension, ""));
            }

            if (showMessages)
            {
                System.Media.SystemSounds.Exclamation.Play();

                if (files.Length > 0)
                    MessageBox.Show("Backups restored.", "Restore Backups");
                else
                    MessageBox.Show("No backups were found!", "Restore Backups");
            }
        }


        private static void RestoreBackup(string sourcePath, string targetPath)
        {
            /*
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(targetPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            File.Move(sourcePath, targetPath, false);
            */
            File.Move(sourcePath, targetPath, true);
        }

        /// <summary>
        /// Loads a local text file of separated values (such as .csv, .tsv) and parses it.
        /// If columns is -1, row length will not be enforced.
        /// </summary>
        public static List<string[]> LoadLocalTextResource(string localPath, int columns = -1, string delimiter = ",", string commentStr = "//")
        {
            string[] file = File.ReadAllLines($@"{UtilFile.GetWorkingDirectory()}\{localPath}");
            List<string[]> output = new();
            for (var i = 0; i < file.Length; i++)
            {
                var line = file[i].Trim();

                var commentIx = line.IndexOf(commentStr);
                if (commentIx > -1)
                    line = line.Remove(commentIx);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var split = line.Split(delimiter);
                if (columns > -1 && split.Length != columns)
                {
                    throw new Exception($"Text resource load error: \"{localPath}\" (Line {i + 1} has invalid formatting)");
                }
                output.Add(split);
            }
            return output;
        }

        public static void OpenInExplorer(string path, bool isLocalPath = false)
        {
            if (isLocalPath)
                path = $@"{UtilFile.GetWorkingDirectory()}\{path}";
            System.Diagnostics.Process.Start(@"explorer.exe", path);
        }
    }
}