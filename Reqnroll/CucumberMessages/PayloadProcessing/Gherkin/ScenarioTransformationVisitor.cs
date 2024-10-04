using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin.CucumberMessages.Types;

namespace Reqnroll.CucumberMessages.PayloadProcessing.Gherkin
{
    abstract class ScenarioTransformationVisitor : GherkinTypesGherkinDocumentVisitor
    {
        protected GherkinDocument _sourceDocument;
        private GherkinDocument _transformedDocument;
        private Feature _transformedFeature;
        private bool _hasTransformedScenarioInFeature = false;
        private bool _hasTransformedScenarioInCurrentRule = false;
        private readonly List<IHasLocation> _featureChildren = new();
        private readonly List<IHasLocation> _ruleChildren = new();
        private List<IHasLocation> _currentChildren;

        public GherkinDocument TransformDocument(GherkinDocument document)
        {
            Reset();
            AcceptDocument(document);
            return _transformedDocument ?? document;
        }

        private void Reset()
        {
            _sourceDocument = null;
            _transformedDocument = null;
            _transformedFeature = null;
            _featureChildren.Clear();
            _ruleChildren.Clear();
            _hasTransformedScenarioInFeature = false;
            _hasTransformedScenarioInCurrentRule = false;
            _currentChildren = _featureChildren;
        }

        protected abstract Scenario GetTransformedScenarioOutline(Scenario scenarioOutline);
        protected abstract Scenario GetTransformedScenario(Scenario scenario);

        protected override void OnScenarioOutlineVisited(Scenario scenarioOutline)
        {
            var transformedScenarioOutline = GetTransformedScenarioOutline(scenarioOutline);
            OnScenarioVisitedInternal(scenarioOutline, transformedScenarioOutline);
        }

        protected override void OnScenarioVisited(Scenario scenario)
        {
            var transformedScenario = GetTransformedScenario(scenario);
            OnScenarioVisitedInternal(scenario, transformedScenario);
        }

        private void OnScenarioVisitedInternal(Scenario scenario, Scenario transformedScenario)
        {
            if (transformedScenario == null)
            {
                _currentChildren.Add(scenario);
                return;
            }

            _hasTransformedScenarioInFeature = true;
            _hasTransformedScenarioInCurrentRule = true;
            _currentChildren.Add(transformedScenario);
        }

        protected override void OnBackgroundVisited(Background background)
        {
            _currentChildren.Add(background);
        }

        protected override void OnRuleVisiting(Rule rule)
        {
            _ruleChildren.Clear();
            _hasTransformedScenarioInCurrentRule = false;
            _currentChildren = _ruleChildren;
        }

        protected override void OnRuleVisited(Rule rule)
        {
            _currentChildren = _featureChildren;
            if (_hasTransformedScenarioInCurrentRule)
            {
                var transformedRule = new Rule(
                    rule.Tags?.ToArray() ?? Array.Empty<Tag>(),
                    rule.Location,
                    rule.Keyword,
                    rule.Name,
                    rule.Description,
                    _ruleChildren.ToArray());
                _featureChildren.Add(transformedRule);
            }
            else
            {
                _featureChildren.Add(rule);
            }
        }

        protected override void OnFeatureVisited(Feature feature)
        {
            if (_hasTransformedScenarioInFeature)
                _transformedFeature = new Feature(
                    feature.Tags?.ToArray() ?? Array.Empty<Tag>(),
                    feature.Location,
                    feature.Language,
                    feature.Keyword,
                    feature.Name,
                    feature.Description,
                    _featureChildren.ToArray());
        }

        protected override void OnDocumentVisiting(GherkinDocument document)
        {
            _sourceDocument = document;
        }

        protected override void OnDocumentVisited(GherkinDocument document)
        {
            if (_transformedFeature != null)
                _transformedDocument = new GherkinDocument(_transformedFeature, document.Comments.ToArray());
        }
    }
}
