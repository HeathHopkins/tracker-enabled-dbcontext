﻿using System.Data.Entity;
using System.Linq;
using TrackerEnabledDbContext.Models;

namespace TrackerEnabledDbContext
{
    public class TrackerContext : DbContext
    {
        public TrackerContext()
            : base()
        {
            
        }

        public TrackerContext(string connectinString)
            : base(connectinString)
        {
            
        }
        
        public DbSet<AuditLog> AuditLog { get; set; }
        public DbSet<AuditLogDetail> LogDetails { get; set; }

        /// <summary>
        /// This method saves the model changes to the database.
        /// If the tracker for of table is active, it will also put the old values in tracking table.
        /// Always use this method instead of SaveChanges() whenever possible.
        /// </summary>
        /// <param name="userName">Username of the logged in identity</param>
        /// <returns>Returns the number of objects written to the underlying database.</returns>
        public int SaveChanges(object userName)
        {
            // Get all Deleted/Modified entities (not Unmodified or Detached or Added)
            foreach (var ent in ChangeTracker.Entries().Where(p => p.State == EntityState.Deleted || p.State == EntityState.Modified))
            {
                using (var auditer = new LogAuditor(ent))
                {
                    var record = auditer.GetLogRecord(userName,
                    ent.State == EntityState.Modified ? EventType.Modified : EventType.Deleted,this);
                    if (record != null)
                    {
                        AuditLog.Add(record);
                    }
                }
            }

            var addedEntries = ChangeTracker.Entries().Where(p => p.State == EntityState.Added).ToList();
            // Call the original SaveChanges(), which will save both the changes made and the audit records...Note that added entry auditing is still remaining.
            var result = base.SaveChanges();
            //By now., we have got the primary keys of added entries of added entiries because of the call to savechanges.

            // Get all Added entities
            foreach (var ent in addedEntries)
            {
                using (var auditer = new LogAuditor(ent))
                {
                    var record = auditer.GetLogRecord(userName, EventType.Added,this);
                    if (record != null)
                    {
                        AuditLog.Add(record);
                    }
                }
            }

            //save changed to audit of added entries
            base.SaveChanges();
            return result;
        }

    }
}
