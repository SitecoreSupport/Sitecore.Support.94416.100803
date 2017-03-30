using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Analytics.Pipelines.RenderingRuleEvaluated;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;
using System.Collections.Generic;

namespace Sitecore.Support.Mvc.Analytics.Pipelines.Response.CustomizeRendering
{
  public class Personalize : CustomizeRenderingProcessor
  {
    protected virtual void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(context, "context");
      RenderingReference reference = context.References.Find(r => r.UniqueId == context.Reference.UniqueId);
      if (reference == null)
      {
        args.Renderer = new EmptyRenderer();
      }
      else
      {
        this.ApplyChanges(args.Rendering, reference);
      }
    }

    protected virtual void Evaluate(CustomizeRenderingArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Item item = args.PageContext.Item;
      if (item != null)
      {
        RenderingReference renderingReference = CustomizeRenderingProcessor.GetRenderingReference(args.Rendering, Context.Language, args.PageContext.Database);
        GetRenderingRulesArgs args1 = new GetRenderingRulesArgs(item, renderingReference);
        GetRenderingRulesPipeline.Run(args1);
        RuleList<ConditionalRenderingsRuleContext> ruleList = args1.RuleList;
        if ((ruleList != null) && (ruleList.Count != 0))
        {
          List<RenderingReference> references = new List<RenderingReference> {
                        renderingReference
                    };
          ConditionalRenderingsRuleContext context = new ConditionalRenderingsRuleContext(references, renderingReference)
          {
            Item = item
          };
          this.RunRules(ruleList, context);
          this.ApplyActions(args, context);
          args.IsCustomized = true;
        }
      }
    }

    public override void Process(CustomizeRenderingArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.IsCustomized && ((Tracker.IsActive || Context.PageMode.IsExperienceEditor) || Context.PageMode.IsPreview))
      {
        this.Evaluate(args);
      }
    }

    private void RulesEvaluatedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, Rule<ConditionalRenderingsRuleContext> rule)
    {
      RenderingRuleEvaluatedPipeline.Run(new RenderingRuleEvaluatedArgs(ruleList, ruleContext, rule));
    }

    protected virtual void RunRules(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
    {
      Assert.ArgumentNotNull(rules, "rules");
      Assert.ArgumentNotNull(context, "context");
      if (!RenderingRuleEvaluatedPipeline.IsEmpty())
      {
        rules.Evaluated += new RuleConditionEventHandler<ConditionalRenderingsRuleContext>(this.RulesEvaluatedHandler);
      }
      rules.RunFirstMatching(context);
    }
  }
}
