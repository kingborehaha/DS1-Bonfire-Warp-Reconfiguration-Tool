using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Forms;

using SoulsFormats;

using static BoreSoulsResource.UtilEnums;


namespace BoreSoulsResource
{
    public static class UtilGame
    {
        public static string GetTimeDayString() => System.DateTime.Now.ToString("MM-dd HH:mm");

        /// <summary>
        /// Documents all attacks and keys them by behavior variation ID.
        /// Includes bullets and all bullet children.
        /// </summary>
        public static Dictionary<int, List<PARAM.Row>> GetAttacksWithVariationIds(GameType game, PARAM behaviorParam, PARAM bulletParam, PARAM atkParam)
        {
            Dictionary<int, List<PARAM.Row>> dict = new();

            foreach (var beh in behaviorParam.Rows)
            {
                var varId = (int)beh["variationId"].Value;
                var refType = (byte)beh["refType"].Value;

                void AddAttack(int atkId)
                {
                    if (atkId <= 0)
                        return;

                    if (!dict.TryGetValue(varId, out var list))
                    {
                        list = [];
                        dict[varId] = list;
                    }

                    var atk = atkParam[atkId];
                    if (atk == null)
                        return;

                    list.Add(atk);
                }

                if (refType == 0)
                {
                    // attacks

                    int atkId = -1;
                    if (game == GameType.DES)
                    {
                        atkId = (int)beh["atkParamId"].Value;
                    }
                    else
                    {
                        atkId = (int)beh["refId"].Value;
                    }
                    AddAttack(atkId);
                }
                else if (refType == 1)
                {
                    // bullets
                    List<int> attackRefs = new();
                    int originBulletId;
                    if (game == GameType.DES)
                    {
                        originBulletId = (int)beh["bulletParamId"].Value;
                    }
                    else
                    {
                        originBulletId = (int)beh["refId"].Value;
                    }

                    HashSet<PARAM.Row> bullets = new(); void GetChildBullets(int bulletId)
                    {
                        if (bulletId <= 0)
                            return;

                        var bullet = bulletParam[bulletId];

                        if (bullet == null)
                            return;

                        if (bullets.Contains(bullet))
                            return;

                        bullets.Add(bullet);

                        int hitBulletId;
                        if (game is GameType.DS3)
                            hitBulletId = (int)bullet["hitBulletId"].Value;
                        else
                            hitBulletId = (int)bullet["HitBulletID"].Value;

                        if (hitBulletId > 0 && hitBulletId != bulletId)
                        {
                            GetChildBullets(hitBulletId);
                        }

                        if (game is GameType.DS3 or GameType.ER or GameType.SDT)
                        {
                            var emitterBulletId = (int)bullet["intervalCreateBulletId"].Value;
                            if (emitterBulletId > 0 && emitterBulletId != bulletId)
                            {
                                GetChildBullets(hitBulletId);
                            }
                        }
                    }

                    GetChildBullets(originBulletId);

                    foreach (var bullet in bullets)
                    {
                        int atkId;
                        if (game == GameType.DS3)
                        {
                            atkId = (int)bullet["atkBullet_Id"].Value;
                        }
                        else
                        {
                            atkId = (int)bullet["atkId_Bullet"].Value;
                        }

                        AddAttack(atkId);
                    }
                }
            }
            return dict;
        }

        public static bool TryAddModInstalledKey(PARAM param, string modNameKey)
        {
            /*
            byte[] hash = MD5.HashData(System.Text.Encoding.ASCII.GetBytes(modNameKey));
            var hashStr = "";
            foreach (var b in hash)
            {
                hashStr += b.ToString();
            }
            var key = $"ModInstallKey {modNameKey} {hashStr}";
            */
            var key = $"ModInstallKey {modNameKey}";
            var result = param.Rows.Any(r => r.Name == key);
            if (result)
            {
                return false;
            }

            var i = 999890;
            while (param.Rows.Any(r => r.ID == i))
            {
                i++;
            }
            param.Rows.Add(new(i, key, param.AppliedParamdef));

            return true;
        }

        public static string GetByteArrayString(byte[] field)
        {
            string bytestr = "";
            for (var i = 0; i < field.Length; i++)
            {
                bytestr += field[i];
            }
            bytestr = bytestr[..^1];
            return bytestr + "]";
        }

        public static PARAM? ApplyParamDefWithWarnings(PARAM param, IEnumerable<PARAMDEF> paramdefs, string paramName, bool ignoreVersion = false, bool checkMultipleDefs = false)
        {
            bool matchType = false;
            bool matchDefVersion = false;
            int bestDefVersion = -420;
            long bestRowsize = -69;
            long bestDefRowSize = -999;

            foreach (PARAMDEF paramdef in paramdefs)
            {
                if (param.ParamType == paramdef.ParamType)
                {
                    matchType = true;
                    bestDefVersion = paramdef.DataVersion;
                    if (ignoreVersion || param.ParamdefDataVersion == paramdef.DataVersion)
                    {
                        matchDefVersion = true;
                        bestRowsize = param.DetectedSize;
                        bestDefRowSize = paramdef.GetRowSize();
                        if (param.DetectedSize == -1 || param.DetectedSize == bestDefRowSize)
                        {
                            try
                            {
                                param.ApplyParamdef(paramdef);
                                return param;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Error while applying ParamDef for {param.ParamType}: {e.Message}");
                                if (!checkMultipleDefs)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Def could not be applied.

            if (!matchType && !matchDefVersion)
                Debug.WriteLine($"Could not apply ParamDef for {paramName} with paramType {param.ParamType}. Valid ParamDef could not be found.");
            else if (matchType && !matchDefVersion)
                Debug.WriteLine($"Could not apply ParamDef for {param.ParamType}. Cannot find ParamDef version {param.ParamdefDataVersion}.");
            else if (matchType && matchDefVersion)
                Debug.WriteLine($"Could not apply ParamDef for {param.ParamType}. Row sizes do not match. Param: {bestRowsize}, Def: {bestDefRowSize}.");
            else
                throw new Exception("Unhandled Apply ParamDef error.");

            return null;
        }

        public static long GetFileIdFromName(string name)
        {
            string fileName = Path.GetFileNameWithoutExtension(name);
            long id = long.Parse(string.Join("", fileName.Where(c => char.IsDigit(c))));
            return id;
        }

        /// <summary>
        /// Compares two files from two provided paths and calculates if they are different.
        /// </summary>
        /// <returns>True if files are identical, false otherwise.</returns>
        public static bool CompareFiles(string filePath1, string filePath2)
        {
            FileInfo fInfo1 = new(filePath1);
            FileInfo fInfo2 = new(filePath2);
            if (fInfo1.Length != fInfo2.Length)
                return false;

            byte[] b1 = File.ReadAllBytes(fInfo1.FullName);
            byte[] b2 = File.ReadAllBytes(fInfo2.FullName);

            return b1.SequenceEqual(b2);
        }

        public static List<string> CompareDirectories(string path1, string path2)
        {
            List<string> output = new();
            Dictionary<string, string> fileDict = new();
            foreach (var file in Directory.GetFiles(path1, "*", SearchOption.AllDirectories))
            {
                fileDict.Add(file.Replace(path1, path1.Split("\\").Last()), file);
            }

            foreach (var filePath2 in Directory.GetFiles(path2, "*", SearchOption.AllDirectories))
            {
                var fileName = filePath2.Replace(path2, path2.Split("\\").Last());
                if (!fileDict.TryGetValue(fileName, out string? filePath1))
                {
                    output.Add($"Not present: {fileName}");
                }
                else
                {
                    if (!CompareFiles(filePath1, filePath2))
                    {
                        output.Add($"Modified: {fileName}");
                    }
                }
            }
            return output;
        }

        public static void WriteParambnd(string path, IBinder parambnd, Dictionary<string, PARAM> paramDict)
        {
            foreach (BinderFile file in parambnd.Files)
            {
                string name = Path.GetFileNameWithoutExtension(file.Name);
                if (paramDict.ContainsKey(name))
                    file.Bytes = paramDict[name].Write();
            }

            ((ISoulsFile)parambnd).Write(path);
        }

        /// <summary>
        /// Clones a param row and returns clone without making any other changes.
        /// </summary>
        public static PARAM.Row CloneParamRow(PARAM.Row row, PARAMDEF def)
        {
            var newRow = new PARAM.Row(row.ID, row.Name, def);
            foreach (var field in row.Cells)
            {
                newRow[field.Def.InternalName].Value = field.Value;
            }
            return newRow;
        }

        /// <summary>
        /// Duplicates a row, gives it an unused ID, inserts it into the param, and returns the new row.
        /// </summary>
        public static PARAM.Row DuplicateRow(PARAM.Row row, PARAM param)
        {
            var newRow = new PARAM.Row(row.ID + 1, row.Name, param.AppliedParamdef);
            while (param.Rows.Any(r => r.ID == newRow.ID))
            {
                newRow.ID++;
            }
            foreach (var field in row.Cells)
            {
                newRow[field.Def.InternalName].Value = field.Value;
            }
            param.Rows.Add(newRow);
            return newRow;
        }

        public static string DecommentString(string str)
        {
            var index = str.IndexOf(@"//");
            if (index != -1)
            {
                return str.Remove(index);
            }
            return str;
        }

        public static Dictionary<string, PARAM> ApplyAllParamDefs(List<BinderFile> binderFiles, List<PARAMDEF> defs)
        {
            Dictionary<string, PARAM> parms = new();
            foreach (BinderFile file in binderFiles)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(file.Name);
                    var param = PARAM.Read(file.Bytes);

                    // Recommended method: checks the list for any match, or you can test them one-by-one
                    var result = ApplyParamDefWithWarnings(param, defs, name, true);
                    if (result != null)
                        parms[name] = result;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"ApplyParamDef {file.Name} Error: {e.Message}");
                }
            }
            return parms;
        }

        public static Dictionary<string, PARAM> ApplyAllParamDefs(string directory, List<PARAMDEF> defs)
        {
            Dictionary<string, PARAM> parms = new();
            foreach (string path in Directory.GetFiles(directory, "*.param", SearchOption.AllDirectories))
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    var param = PARAM.Read(path);

                    // Recommended method: checks the list for any match, or you can test them one-by-one

                    //if (param.ApplyParamdefCarefully(defs))
                    var result = ApplyParamDefWithWarnings(param, defs, name, true);
                    if (result != null)
                        parms[name] = result;
                }
                catch { }
            }
            return parms;
        }

        public static bool CheckOodle(GameType gameType)
        {
            if (File.Exists("oo2core_6_win64.dll") == false)
            {
                DialogResult result;
                do
                {
                    result = MessageBox.Show(
                        $"The selected files requires \"oo2core_6_win64.dll\", which can be found in your {gameType} directory." +
                        $"\n\nPlease copy and paste \"oo2core_6_win64.dll\" from your {gameType} directory to \"{Directory.GetCurrentDirectory()}\".",
                        $"Could not find oo2core_6_win64.dll", MessageBoxButtons.RetryCancel);

                    if (result == DialogResult.Cancel)
                        return false;
                }
                while (File.Exists("oo2core_6_win64.dll") == false);
            }

            return true;
        }

        /// <summary>
        /// Returns the same list but shuffled.
        /// Modified version of https://stackoverflow.com/a/1262619
        /// </summary>
        public static IList<T> ShuffleList<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
            return list;
        }

        /// <summary>
        /// Shuffles an existing list.
        /// Modified version of https://stackoverflow.com/a/1262619
        /// </summary>
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        /// <summary>
        /// Returns the a shuffled copy of the list.
        /// Modified version of https://stackoverflow.com/a/1262619
        /// </summary>
        public static IList<T> ShuffleCloneList<T>(IList<T> list, Random rng)
        {
            list = list.ToList();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
            return list;
        }
    }
}
