using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Abstractions;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Templates;
using Sitecore.Framework.Conditions;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.xMgmt.Extensions;

namespace Sitecore.Support.Marketing.xMgmt.Extensions
{
  internal static class BaseTemplateManagerExtensions
  {
    public static Dictionary<Guid, HashSet<Guid>> GetTemplatesInheritanceDictionary(this BaseTemplateManager templateManager, Database database, IEnumerable<Guid> definitionTemplateIds)
    {
      Dictionary<Guid, HashSet<Guid>> dictionary = new Dictionary<Guid, HashSet<Guid>>();
      foreach (Guid definitionTemplateId in definitionTemplateIds)
      {
        if (!dictionary.ContainsKey(definitionTemplateId))
        {
          dictionary.Add(definitionTemplateId, new HashSet<Guid>());
        }
        Template template = templateManager.GetTemplate(definitionTemplateId.ToID(), database);
        TemplateList templateList = (template != null) ? template.GetBaseTemplates() : null;
        if (templateList != null)
        {
          foreach (Template item in templateList)
          {
            Guid guid = item.ID.Guid;
            if (!dictionary.ContainsKey(guid))
            {
              dictionary.Add(guid, new HashSet<Guid>());
            }
            dictionary[guid].Add(definitionTemplateId);
          }
        }
      }
      return dictionary;
    }
  }
}