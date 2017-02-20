namespace Sitecore.Support.Analytics.Reporting.DefinitionData.Marketing.Deployment.Processors
{
    using System;
    using Sitecore.Analytics.Pipelines.DeployDefinition;
    using Sitecore.Analytics.Reporting.DefinitionData.Marketing.Deployment;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Data.Templates;
    using Sitecore.Diagnostics;
    using Sitecore.Marketing.Definitions;
    using Sitecore.Marketing.Definitions.Campaigns;
    using Sitecore.Marketing.Definitions.Goals;
    using Sitecore.Marketing.Definitions.Outcomes.Model;
    using Sitecore.Marketing.Definitions.Funnels;

    public class Deploy : Sitecore.Analytics.Reporting.DefinitionData.Marketing.Deployment.Processors.Deploy
  {
    private static readonly ID CampaignActivity = Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId;
    private static readonly ID Goal = Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.GoalDefinitionTemplateId;
    private static readonly ID Outcome = Sitecore.Marketing.Definitions.Outcomes.WellKnownIdentifiers.OutcomeDefinitionTemplateId;

    protected readonly TimeSpan Timeout;

    [UsedImplicitly]
    public Deploy() : this(null)
    {
    }

    [UsedImplicitly]
    public Deploy([CanBeNull] string timeoutText)
    {
      this.Timeout = string.IsNullOrEmpty(timeoutText) ? TimeSpan.FromSeconds(30.0) : TimeSpan.Parse(timeoutText);
    }

    public override void Process(DeployDefinitionArgs args)
    {
            Assert.ArgumentNotNull(args, "args");
            Item item = args.Item;
            Template template = TemplateManager.GetTemplate(item);
            this.DeployItem<ICampaignActivityDefinition>(item, template, Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId);
            this.DeployItem<IGoalDefinition>(item, template, Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.GoalDefinitionTemplateId);
            this.DeployItem<IOutcomeDefinition>(item, template, Sitecore.Marketing.Definitions.Outcomes.WellKnownIdentifiers.OutcomeDefinitionTemplateId);
            this.DeployItem<IFunnelDefinition>(item, template, Sitecore.Marketing.Definitions.Funnels.WellKnownIdentifiers.FunnelDefinitionTemplateId);
        }

    protected new void DeployItem<TDefinition>(Item item, Template itemTemplate, ID expectedTemplateId) where TDefinition : IDefinition
    {
      Assert.ArgumentNotNull(item, nameof(item));
      Assert.ArgumentNotNull(itemTemplate, nameof(itemTemplate));
      Assert.ArgumentNotNull(expectedTemplateId, nameof(expectedTemplateId));

      if(!itemTemplate.InheritsFrom(expectedTemplateId))
      {
        return;
      }   

      var deployTask = DeploymentManager.Default.DeployAsync<TDefinition>(item.ID, item.Language.CultureInfo);
      Assert.IsNotNull(deployTask, nameof(deployTask));

      if (Timeout.Ticks == 0)
      {
        return;
      }

      if (!deployTask.Wait(Timeout))
      {
        throw new TimeoutException($"Save operation for definition id:[{item.ID}] could not be completed within specified timeframe. It will be re-run in the background.");
      }
    }
  }
}