using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class RoleScopeTagTranslation
    {
        public static List<string> TranslateRoleScopeTags(string[] roleScopeTagIds, Hashtable scopeTagMigrationTable)
        {
            // Translate scope tag with table
            List<string> newMapping = new List<string>();

            foreach (string roleScopeTagId in roleScopeTagIds)
            {
                try
                {
                    newMapping.Add(scopeTagMigrationTable[roleScopeTagId].ToString());
                }
                catch
                {
                    newMapping.Add("0");
                }
            }

            return newMapping;
        }
    }
}