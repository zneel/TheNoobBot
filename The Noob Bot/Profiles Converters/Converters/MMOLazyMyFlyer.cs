﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Gatherer;
using nManager.Helpful;
using nManager.Wow.Class;
using GathererProfile = nManager.Wow.Class.GathererProfile;

namespace Profiles_Converters.Converters
{
    public class MMOLazyMyFlyer
    {
        public static bool IsMMoLazyFlyerProfile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string text = Others.ReadFile(path);
                    if (text.Contains("<GatherProfile") && text.Contains("<Waypoints_Normal>"))
                        return true;
                }
                else
                {
                    MessageBox.Show(nManager.Translate.Get(nManager.Translate.Id.File_not_found) + ".");
                }
            }
            catch
            {
            }
            return false;
        }

        public static bool Convert(string path)
        {
            try
            {
                if (IsMMoLazyFlyerProfile(path))
                {
                    GatherProfile _origineProfile = XmlSerializer.Deserialize<GatherProfile>(path);
                    GathererProfile _profile = new GathererProfile();

                    foreach (Position p in _origineProfile.Waypoints_Normal)
                    {
                        _profile.Points.Add(new Point(p.X, p.Y, p.Z, "Flying"));
                    }


                    string fileName = Path.GetFileNameWithoutExtension(path);

                    if (XmlSerializer.Serialize(Application.StartupPath + "\\Profiles\\Gatherer\\" + fileName + ".xml",
                        _profile))
                    {
                        Logging.Write("Conversion Success (MMOLazy MyFlyer to Gatherer bot): " + fileName);
                        return true;
                    }
                }
            }
            catch
            {
            }
            Logging.Write("Conversion Failled (MMOLazy MyFlyer to Gatherer bot): " + path);
            return false;
        }

        // Classes
        [Serializable]
        public class GatherProfile
        {
            public List<Position> Waypoints_Normal = new List<Position>();
        }

        [Serializable]
        public class Position
        {
            public float X;
            public float Y;
            public float Z;
        }
    }
}