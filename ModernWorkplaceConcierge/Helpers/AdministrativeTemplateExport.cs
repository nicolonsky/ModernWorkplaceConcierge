using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;

namespace ModernWorkplaceConcierge.Helpers
{
    public class AdministrativeTemplateExport
    {
        private GraphIntune graphIntune;

        public AdministrativeTemplateExport(GraphIntune graphIntune)
        {
            this.graphIntune = graphIntune;
        }

        public async Task<List<JObject>> GetExportableGroupPolicies()
        {
            // List for exported admx templates
            List<JObject> administrativeTemplates = new List<JObject>();

            // Process Administrative Templates 
            var gpos = await graphIntune.GetGroupPolicyConfigurationsAsync();

            foreach (GroupPolicyConfiguration gpo in gpos)
            {
                // 2. Configured settings
                var values = await graphIntune.GetGroupPolicyDefinitionValuesAsync(gpo.Id);

                JObject administrativeTemplate = JObject.FromObject(gpo);
                JArray settings = new JArray();

                // 3. Configured Values
                foreach (GroupPolicyDefinitionValue value in values)
                {
                    var groupPolicyDefinition = await graphIntune.GetGroupPolicyDefinitionValueAsync(gpo.Id, value.Id);

                    var res = await graphIntune.GetGroupPolicyPresentationValuesAsync(gpo.Id, value.Id);

                    JObject jObject = new JObject
                    {
                        // Link setting to field
                        { "definition@odata.bind", $"https://graph.microsoft.com/beta/deviceManagement/groupPolicyDefinitions('{groupPolicyDefinition.Id}')" },
                        { "enabled", value.Enabled }
                    };

                    JArray jArray = new JArray();

                    // We need a type cast to access value property of GroupPolicyPresentationValue
                    foreach (GroupPolicyPresentationValue presentationValue in res)
                    {
                        JObject val = new JObject
                            {
                                { "@odata.type", presentationValue.ODataType }
                            };

                        if (presentationValue is GroupPolicyPresentationValueBoolean)
                        {
                            val.Add("value", ((GroupPolicyPresentationValueBoolean)presentationValue).Value);
                        }
                        else if (presentationValue is GroupPolicyPresentationValueDecimal)
                        {
                            val.Add("value", ((GroupPolicyPresentationValueDecimal)presentationValue).Value);
                        }
                        else if (presentationValue is GroupPolicyPresentationValueList)
                        {
                            JArray valueList = new JArray();

                            foreach (KeyValuePair valueListEntry in ((GroupPolicyPresentationValueList)presentationValue).Values)
                            {
                                JObject valueEntry = new JObject
                                    {
                                        { "name", valueListEntry.Name },
                                        { "value", valueListEntry.Value }
                                    };

                                valueList.Add(valueEntry);
                            }

                            val.Add("values", valueList);
                        }
                        else if (presentationValue is GroupPolicyPresentationValueLongDecimal)
                        {
                            val.Add("value", ((GroupPolicyPresentationValueLongDecimal)presentationValue).Value);
                        }
                        else if (presentationValue is GroupPolicyPresentationValueMultiText)
                        {
                            JArray valueList = new JArray();
                            ((GroupPolicyPresentationValueMultiText)presentationValue).Values.ForEach(stringValue => valueList.Add(stringValue));
                            val.Add("values", valueList);
                        }
                        else if (presentationValue is GroupPolicyPresentationValueText)
                        {
                            val.Add("value", ((GroupPolicyPresentationValueText)presentationValue).Value);
                        }
                        // Binds configured value to settings id
                        val.Add("presentation@odata.bind", $"https://graph.microsoft.com/beta/deviceManagement/groupPolicyDefinitions('{groupPolicyDefinition.Id}')/presentations('{presentationValue.Presentation.Id}')");
                        jArray.Add(val);
                    }
                    jObject.Add("presentationValues", jArray);
                    settings.Add(jObject);
                }

                administrativeTemplate.Add("configuredSettings", settings);
                administrativeTemplates.Add(administrativeTemplate);
            }

            return administrativeTemplates;
        }
    }
}