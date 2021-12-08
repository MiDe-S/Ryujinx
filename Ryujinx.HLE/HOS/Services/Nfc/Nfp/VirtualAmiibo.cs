using Ryujinx.Common.Configuration;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Mii;
using Ryujinx.HLE.HOS.Services.Mii.Types;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using LibAmiibo.Data;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    static class VirtualAmiibo
    {
        private static uint _openedApplicationAreaId;

        public static byte[] GenerateUuid(string amiiboId, bool useRandomUuid)
        {
            if (useRandomUuid)
            {
                return GenerateRandomUuid();
            }

            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.TagUuid.Length == 0)
            {
                virtualAmiiboFile.TagUuid = GenerateRandomUuid();

                SaveAmiiboFile(virtualAmiiboFile);
            }

            return virtualAmiiboFile.TagUuid;
        }

        private static byte[] GenerateRandomUuid()
        {
            byte[] uuid = new byte[9];

            new Random().NextBytes(uuid);

            uuid[3] = (byte)(0x88    ^ uuid[0] ^ uuid[1] ^ uuid[2]);
            uuid[8] = (byte)(uuid[3] ^ uuid[4] ^ uuid[5] ^ uuid[6]);

            return uuid;
        }

        public static CommonInfo GetCommonInfo(string amiiboId)
        {
            VirtualAmiiboFile amiiboFile = LoadAmiiboFile(amiiboId);

            return new CommonInfo()
            {
                LastWriteYear       = (ushort)amiiboFile.LastWriteDate.Year,
                LastWriteMonth      = (byte)amiiboFile.LastWriteDate.Month,
                LastWriteDay        = (byte)amiiboFile.LastWriteDate.Day,
                WriteCounter        = amiiboFile.WriteCounter,
                Version             = 1,
                ApplicationAreaSize = AmiiboConstants.ApplicationAreaSize,
                Reserved            = new Array52<byte>()
            };
        }

        public static RegisterInfo GetRegisterInfo(string amiiboId, string nickname)
        {
            VirtualAmiiboFile amiiboFile = LoadAmiiboFile(amiiboId);

            UtilityImpl utilityImpl = new UtilityImpl();
            CharInfo    charInfo    = new CharInfo();

            charInfo.SetFromStoreData(StoreData.BuildDefault(utilityImpl, 0));

            charInfo.Nickname = Nickname.FromString(nickname);

            RegisterInfo registerInfo = new RegisterInfo()
            {
                MiiCharInfo     = charInfo,
                FirstWriteYear  = (ushort)amiiboFile.FirstWriteDate.Year,
                FirstWriteMonth = (byte)amiiboFile.FirstWriteDate.Month,
                FirstWriteDay   = (byte)amiiboFile.FirstWriteDate.Day,
                FontRegion      = 0,
                Reserved1       = new Array64<byte>(),
                Reserved2       = new Array58<byte>()
            };
            if (amiiboFile.Name == null)
            {
                amiiboFile.Name = "Ryujinx";
                Encoding.UTF8.GetBytes(amiiboFile.Name, registerInfo.Nickname.ToSpan());
                SaveAmiiboFile(amiiboFile);
            }
            else
            {

                Encoding.UTF8.GetBytes(amiiboFile.Name, registerInfo.Nickname.ToSpan());
            }

            return registerInfo;
        }

        public static bool OpenApplicationArea(string amiiboId, uint applicationAreaId)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == applicationAreaId))
            {
                _openedApplicationAreaId = applicationAreaId;

                return true;
            }

            return false;
        }

        public static byte[] GetApplicationArea(string amiiboId)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            foreach (VirtualAmiiboApplicationArea applicationArea in virtualAmiiboFile.ApplicationAreas)
            {
                if (applicationArea.ApplicationAreaId == _openedApplicationAreaId)
                {
                    return applicationArea.ApplicationArea;
                }
            }

            return Array.Empty<byte>();
        }

        public static bool CreateApplicationArea(string amiiboId, uint applicationAreaId, byte[] applicationAreaData)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == applicationAreaId))
            {
                return false;
            }

            virtualAmiiboFile.ApplicationAreas.Add(new VirtualAmiiboApplicationArea()
            {
                ApplicationAreaId = applicationAreaId,
                ApplicationArea   = applicationAreaData
            });

            SaveAmiiboFile(virtualAmiiboFile);

            return true;
        }

        public static void SetAmiiboName(string amiiboId, string amiiboName)
        {
            VirtualAmiiboFile amiiboFile = LoadAmiiboFile(amiiboId);

            amiiboFile.Name = amiiboName;

            SaveAmiiboFile(amiiboFile);
        }

        public static void SetApplicationArea(string amiiboId, byte[] applicationAreaData)
        {
            VirtualAmiiboFile virtualAmiiboFile = LoadAmiiboFile(amiiboId);

            if (virtualAmiiboFile.ApplicationAreas.Any(item => item.ApplicationAreaId == _openedApplicationAreaId))
            {
                for (int i = 0; i < virtualAmiiboFile.ApplicationAreas.Count; i++)
                {
                    if (virtualAmiiboFile.ApplicationAreas[i].ApplicationAreaId == _openedApplicationAreaId)
                    {
                        // only write to file if appdata has changed
                        if (!virtualAmiiboFile.ApplicationAreas[i].ApplicationArea.SequenceEqual(applicationAreaData))
                        {
                            virtualAmiiboFile.ApplicationAreas[i] = new VirtualAmiiboApplicationArea()
                            {
                                ApplicationAreaId = _openedApplicationAreaId,
                                ApplicationArea = applicationAreaData
                            };

                            SaveAmiiboFile(virtualAmiiboFile);
                        }

                        break;
                    }
                }
            }
        }

        private static VirtualAmiiboFile LoadAmiiboFile(string amiiboId)
        {
            Directory.CreateDirectory(Path.Join(AppDataManager.BaseDirPath, "system", "amiibo"));

            string filePath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", $"{amiiboId}.json");

            VirtualAmiiboFile virtualAmiiboFile;

            if (File.Exists(filePath))
            {
                virtualAmiiboFile = JsonSerializer.Deserialize<VirtualAmiiboFile>(File.ReadAllText(filePath), new JsonSerializerOptions(JsonSerializerDefaults.General));
            }
            else
            {
                virtualAmiiboFile = CreateAmiiboJSON(amiiboId);
            }

            return virtualAmiiboFile;
        }

        static void SaveAmiiboFile(VirtualAmiiboFile virtualAmiiboFile)
        {
            string filePath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", $"{virtualAmiiboFile.AmiiboId}.json");

            File.WriteAllText(filePath, JsonSerializer.Serialize(virtualAmiiboFile));
        }

        static public VirtualAmiiboFile CreateAmiiboJSON(string amiiboId, uint FileVersion=0, string amiiboName="Ryujinx", byte[] TagUuid=null, DateTime? FirstWriteDate=null, ushort WriteCounter=0, uint ApplicationAreaID=0, byte[] ApplicationArea =null)
        {
            if (FirstWriteDate == null)
            {
                FirstWriteDate = DateTime.Now;
            }
            if (TagUuid == null)
            {
                TagUuid = Array.Empty<byte>();
            }
            VirtualAmiiboFile virtualAmiiboFile = new VirtualAmiiboFile()
            {
                FileVersion      = FileVersion,
                Name             = amiiboName,
                TagUuid          = TagUuid,
                AmiiboId         = amiiboId,
                FirstWriteDate   = (DateTime)FirstWriteDate,
                LastWriteDate    = DateTime.Now,
                WriteCounter     = WriteCounter,
                ApplicationAreas = new List<VirtualAmiiboApplicationArea>()
            };

            if (ApplicationArea != null)
            {
                VirtualAmiiboApplicationArea appdata = new VirtualAmiiboApplicationArea()
                {
                    ApplicationAreaId = ApplicationAreaID,
                    ApplicationArea = ApplicationArea
                };
                virtualAmiiboFile.ApplicationAreas.Add(appdata);
            }

            SaveAmiiboFile(virtualAmiiboFile);

            return virtualAmiiboFile;
        }

        public static string LoadAmiiboFromBin(string binFilelocation, bool randomizeUID)
        {
            
            if (!ValidateAmiiboKey())
            {
                return null;
            }

            AmiiboTag bin = AmiiboTag.DecryptWithKeys(File.ReadAllBytes(binFilelocation));

            byte[] appData = bin.AppData.ToArray();

            Directory.CreateDirectory(Path.Join(AppDataManager.BaseDirPath, "system", "amiibo"));

            string filePath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", $"{bin.Amiibo.StatueId}.json");

            if (File.Exists(filePath))
            {
                VirtualAmiibo.OpenApplicationArea(bin.Amiibo.StatueId, (uint)bin.Amiibo.GameSeriesId);
                // needed to prevent different console error
                if (!VirtualAmiibo.GenerateUuid(bin.Amiibo.StatueId, randomizeUID).SequenceEqual(bin.NtagSerial.ToArray()))
                {
                    VirtualAmiibo.SetApplicationArea(bin.Amiibo.StatueId, appData);
                    VirtualAmiibo.SetAmiiboName(bin.Amiibo.StatueId, bin.AmiiboSettings.AmiiboUserData.AmiiboNickname);
                }
            }
            else
            {
                // scanning an unregistered amiibo means json data is a random mess, it still works in games though. Also creates an unuseable app id
                // unsure how to detect if bin is registered or not
                VirtualAmiibo.CreateAmiiboJSON(bin.Amiibo.StatueId, 0, bin.AmiiboSettings.AmiiboUserData.AmiiboNickname, bin.NtagSerial.ToArray(), bin.AmiiboSettings.AmiiboUserData.AmiiboSetupDate, bin.AmiiboSettings.WriteCounter, (uint)bin.Amiibo.GameSeriesId, appData);

                if (randomizeUID)
                {
                    VirtualAmiibo.GenerateUuid(bin.Amiibo.StatueId, randomizeUID);
                }
            }

            return bin.Amiibo.StatueId;
        }

        static bool ValidateAmiiboKey()
        {
            string settingsPath = Path.Join(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath), "appsettings.json");

            Dictionary<string, string> keyDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(settingsPath), new JsonSerializerOptions(JsonSerializerDefaults.General));

            if (File.Exists(keyDict["AmiiboKeys"]))
            {
                return true;
            }
            else if (File.Exists(Path.Join(AppDataManager.BaseDirPath, "system", "key_retail.bin")))
            {
                keyDict["AmiiboKeys"] = Path.Join(AppDataManager.BaseDirPath, "system", "key_retail.bin");
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(keyDict));
                return true;
            }
            return false;
        }

    }
}
