namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Dynamic (macro-economic) parameters of the decision layer, read from the
    /// "DecisionPolicy" configuration section. Changing them changes decisions
    /// immediately, without retraining the ML model.
    /// </summary>
    public class DecisionPolicyOptions
    {
        public const string SectionName = "DecisionPolicy";

        /// <summary>
        /// Market reference rate the offer is priced on (e.g. 3-month Euribor or the
        /// central bank policy rate), as a fraction: 0.04 = 4%. The customer's offered
        /// rate is this reference plus an incentive spread based on model confidence and customer profile.
        /// </summary>
        public decimal ReferenceRate { get; set; } = 0.04m;
    }
}
