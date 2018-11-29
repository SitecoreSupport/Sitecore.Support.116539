using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Abstractions;
using Sitecore.Data.Items;
using Sitecore.Data.Templates;
using Sitecore.DependencyInjection;
using Sitecore.Framework.Conditions;
using Sitecore.Marketing.Core.Extensions;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.AutomationPlans.Model;
using Sitecore.Marketing.Definitions.Campaigns;
using Sitecore.Marketing.Definitions.ContactLists;
using Sitecore.Marketing.Definitions.Events;
using Sitecore.Marketing.Definitions.Funnels;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Marketing.Definitions.Outcomes.Model;
using Sitecore.Marketing.Definitions.PageEvents;
using Sitecore.Marketing.Definitions.Profiles;
using Sitecore.Marketing.Definitions.Segments;
using Sitecore.Marketing.xMgmt.Extensions;
using Sitecore.Marketing.xMgmt.Pipelines.DeployDefinition;
using Sitecore.Marketing.xMgmt.Pipelines;
using Sitecore.Marketing.xMgmt;
using Sitecore.Marketing;
using Sitecore;
using WellKnownIdentifiers = Sitecore.Marketing.Definitions.WellKnownIdentifiers;

namespace Sitecore.Support.Marketing.xMgmt.Pipelines.DeployDefinition
{
  public class Deploy
  {
    private readonly DeploymentManager _deploymentManager;
    private readonly BaseTemplateManager _templateManager;

    internal TimeSpan DeployItemTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public Deploy([NotNull] DeploymentManager deploymentManager)
        : this(deploymentManager, ServiceLocator.ServiceProvider.GetRequiredService<BaseTemplateManager>())
    {
    }

    internal Deploy([NotNull] DeploymentManager deploymentManager, [NotNull] BaseTemplateManager templateManager)
    {
      Condition.Requires(deploymentManager, nameof(deploymentManager)).IsNotNull();
      Condition.Requires(templateManager, nameof(templateManager)).IsNotNull();

      _deploymentManager = deploymentManager;
      _templateManager = templateManager;
    }

    public virtual void Process([NotNull] DeployDefinitionArgs args)
    {
      Condition.Requires(args, nameof(args)).IsNotNull();

      Item item = args.Item;
      Template itemTemplate = _templateManager.GetTemplate(item);

      Dictionary<Guid, HashSet<Guid>> templatesInheritanceDictionary = Sitecore.Support.Marketing.xMgmt.Extensions.BaseTemplateManagerExtensions.GetTemplatesInheritanceDictionary(_templateManager, item.Database, WellKnownIdentifiers.MarketingDefinition.DefinitionTemplateIds);

      DeployItem<IAutomationPlanDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.AutomationPlans.WellKnownIdentifiers.PlanDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<ICampaignActivityDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IEventDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Events.WellKnownIdentifiers.EventDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IFunnelDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Funnels.WellKnownIdentifiers.FunnelDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IGoalDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.GoalDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IOutcomeDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Outcomes.WellKnownIdentifiers.OutcomeDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IPageEventDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.PageEvents.WellKnownIdentifiers.PageEventDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IProfileDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Profiles.WellKnownIdentifiers.ProfileDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<IContactListDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.ContactLists.WellKnownIdentifiers.ContactListDefinitionTemplateId, templatesInheritanceDictionary);
      DeployItem<ISegmentDefinition>(item, itemTemplate, Sitecore.Marketing.Definitions.Segments.WellKnownIdentifiers.SegmentDefinitionTemplateId, templatesInheritanceDictionary);

    }


    protected void DeployItem<TDefinition>(
        [NotNull] Item item,
        [NotNull] Template itemTemplate,
        [NotNull] Guid expectedTemplateId,
        [NotNull] IReadOnlyDictionary<Guid, HashSet<Guid>> templatesInheritanceDictionary
        ) where TDefinition : IDefinition
    {
      Condition.Requires(item, nameof(item)).IsNotNull();
      Condition.Requires(itemTemplate, nameof(itemTemplate)).IsNotNull();
      Condition.Requires(expectedTemplateId, nameof(expectedTemplateId)).IsNotEmptyGuid();

      if (itemTemplate.InheritsFrom(expectedTemplateId.ToID()))
      {
        HashSet<Guid> childIds;

        if (!templatesInheritanceDictionary.TryGetValue(expectedTemplateId, out childIds))
        {
          throw new InvalidOperationException($"Unknown definition template id '{expectedTemplateId}'");
        }

        if (!childIds.Any(c => itemTemplate.InheritsFrom(c.ToID())))
        {
          bool wasDeployed = _deploymentManager.DeployAsync<TDefinition>(item.ID.Guid, item.Language.CultureInfo).Wait(DeployItemTimeout);
          if (!wasDeployed)
          {
            throw new TimeoutException($"Save operation for definition id:[{item.ID}] could not be completed within specified timeframe. It will be re-run in the background.");
          }
        }
      }
    }
  }
}