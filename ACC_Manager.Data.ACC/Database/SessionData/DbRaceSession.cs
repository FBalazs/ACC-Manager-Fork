﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ACCManager.ACCSharedMemory;

namespace ACCManager.Data.ACC.Database.SessionData
{
    internal class DbRaceSession
    {
#pragma warning disable IDE1006 // Naming Styles
        public Guid _id { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        public Guid TrackGuid { get; set; }
        public Guid CarGuid { get; set; }

        public DateTime UtcStart { get; set; }
        public DateTime UtcEnd { get; set; }
        public AcSessionType SessionType { get; set; }
        public int SessionIndex { get; set; }
    }

    internal class RaceSessionCollection
    {
        public static void Update(DbRaceSession raceSession)
        {
            var collection = LocalDatabase.Database.GetCollection<DbRaceSession>();

            try
            {
                var storedSession = collection.FindOne(x => x._id == raceSession._id);
                if (storedSession != null)
                {
                    storedSession = raceSession;
                    collection.Update(storedSession);
                    Debug.WriteLine($"Updated Race session {raceSession._id}");
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }

        public static void Insert(DbRaceSession raceSession)
        {
            var collection = LocalDatabase.Database.GetCollection<DbRaceSession>();
            collection.EnsureIndex(x => x._id, true);

            collection.Insert(raceSession);

            Debug.WriteLine($"Inserted new race session {raceSession.SessionIndex} {raceSession.SessionType} {raceSession._id}");
        }
    }
}