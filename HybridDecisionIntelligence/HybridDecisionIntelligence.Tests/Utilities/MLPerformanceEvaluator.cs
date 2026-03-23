namespace HybridDecisionIntelligence.Tests.Utilities
{
    /// <summary>
    /// ML Performance Metrics Utility
    /// Calculates Accuracy, Precision, Recall, F1 Score, AUC, and Confusion Matrix
    /// for ML.NET model evaluation
    /// </summary>
    public class MLPerformanceEvaluator
    {
        /// <summary>
        /// Performance metrics container
        /// </summary>
        public class PerformanceMetrics
        {
            public float Accuracy { get; set; }
            public float Precision { get; set; }
            public float Recall { get; set; }
            public float Sensitivity { get; set; }
            public float Specificity { get; set; }
            public float F1Score { get; set; }
            public float AUC { get; set; }

            // Confusion Matrix
            public int TruePositives { get; set; }
            public int FalsePositives { get; set; }
            public int TrueNegatives { get; set; }
            public int FalseNegatives { get; set; }

            public override string ToString()
            {
                return $@"
=== ML Model Performance Metrics ===
Accuracy:       {Accuracy:P2}
Precision:      {Precision:P2}
Recall:         {Recall:P2}
Sensitivity:    {Sensitivity:P2}
Specificity:    {Specificity:P2}
F1 Score:       {F1Score:P2}
AUC:            {AUC:P2}

--- Confusion Matrix ---
True Positives:  {TruePositives}
False Positives: {FalsePositives}
True Negatives:  {TrueNegatives}
False Negatives: {FalseNegatives}

Total Predictions: {TruePositives + FalsePositives + TrueNegatives + FalseNegatives}
";
            }
        }

        /// <summary>
        /// Calculate all performance metrics from predictions
        /// </summary>
        /// <param name="predictions">Array of (predicted, actual) boolean tuples</param>
        /// <returns>PerformanceMetrics with all calculated values</returns>
        public PerformanceMetrics CalculateMetrics(
            (bool predicted, bool actual)[] predictions)
        {
            if (predictions == null || predictions.Length == 0)
                throw new ArgumentException("Predictions array cannot be null or empty");

            // Calculate confusion matrix
            var metrics = new PerformanceMetrics();
            CalculateConfusionMatrix(predictions, metrics);

            // Calculate derived metrics
            metrics.Accuracy = CalculateAccuracy(metrics);
            metrics.Precision = CalculatePrecision(metrics);
            metrics.Recall = CalculateRecall(metrics);
            metrics.Sensitivity = metrics.Recall; // Same as Recall
            metrics.Specificity = CalculateSpecificity(metrics);
            metrics.F1Score = CalculateF1Score(metrics.Precision, metrics.Recall);
            metrics.AUC = CalculateAUC(predictions, metrics);

            return metrics;
        }

        /// <summary>
        /// Calculate confusion matrix components
        /// </summary>
        private void CalculateConfusionMatrix(
            (bool predicted, bool actual)[] predictions,
            PerformanceMetrics metrics)
        {
            foreach (var (predicted, actual) in predictions)
            {
                if (predicted && actual)
                    metrics.TruePositives++;
                else if (predicted && !actual)
                    metrics.FalsePositives++;
                else if (!predicted && actual)
                    metrics.FalseNegatives++;
                else
                    metrics.TrueNegatives++;
            }
        }

        /// <summary>
        /// Accuracy = (TP + TN) / (TP + FP + TN + FN)
        /// Percentage of correct predictions
        /// </summary>
        private float CalculateAccuracy(PerformanceMetrics metrics)
        {
            var total = metrics.TruePositives + metrics.FalsePositives + 
                       metrics.TrueNegatives + metrics.FalseNegatives;
            
            if (total == 0)
                return 0f;

            return (metrics.TruePositives + metrics.TrueNegatives) / (float)total;
        }

        /// <summary>
        /// Precision = TP / (TP + FP)
        /// Of all positive predictions, how many were correct?
        /// </summary>
        private float CalculatePrecision(PerformanceMetrics metrics)
        {
            var denominador = metrics.TruePositives + metrics.FalsePositives;
            
            if (denominador == 0)
                return 0f;

            return metrics.TruePositives / (float)denominador;
        }

        /// <summary>
        /// Recall (Sensitivity) = TP / (TP + FN)
        /// Of all actual positives, how many did we correctly identify?
        /// </summary>
        private float CalculateRecall(PerformanceMetrics metrics)
        {
            var denominator = metrics.TruePositives + metrics.FalseNegatives;
            
            if (denominator == 0)
                return 0f;

            return metrics.TruePositives / (float)denominator;
        }

        /// <summary>
        /// Specificity = TN / (TN + FP)
        /// Of all actual negatives, how many did we correctly identify?
        /// </summary>
        private float CalculateSpecificity(PerformanceMetrics metrics)
        {
            var denominator = metrics.TrueNegatives + metrics.FalsePositives;
            
            if (denominator == 0)
                return 0f;

            return metrics.TrueNegatives / (float)denominator;
        }

        /// <summary>
        /// F1 Score = 2 * (Precision * Recall) / (Precision + Recall)
        /// Harmonic mean of Precision and Recall
        /// Best when both metrics need to be balanced
        /// </summary>
        private float CalculateF1Score(float precision, float recall)
        {
            var sum = precision + recall;
            
            if (sum == 0)
                return 0f;

            return 2 * (precision * recall) / sum;
        }

        /// <summary>
        /// ROC AUC (Area Under the Receiver Operating Characteristic Curve)
        /// Simplified calculation for binary classification
        /// Measures the model's ability to distinguish between classes
        /// </summary>
        private float CalculateAUC(
            (bool predicted, bool actual)[] predictions,
            PerformanceMetrics metrics)
        {
            // Simplified AUC calculation using sensitivity and specificity
            var sensitivity = metrics.Recall; // TP / (TP + FN)
            var specificity = CalculateSpecificity(metrics); // TN / (TN + FP)

            // AUC is approximately the average of sensitivity and specificity
            // In practice, more sophisticated methods exist (trapezoid rule, etc.)
            return (sensitivity + specificity) / 2f;
        }

        /// <summary>
        /// Generate a detailed classification report
        /// </summary>
        public string GenerateClassificationReport(
            (bool predicted, bool actual)[] predictions)
        {
            var metrics = CalculateMetrics(predictions);
            return metrics.ToString();
        }

        /// <summary>
        /// Calculate Receiver Operating Characteristic (ROC) Curve points
        /// Returns (false_positive_rate, true_positive_rate) pairs
        /// </summary>
        public List<(float, float)> CalculateROCCurve(
            (bool predicted, bool actual)[] predictions)
        {
            var roc = new List<(float, float)>();
            var metrics = CalculateMetrics(predictions);

            // Calculate TPR and FPR at different thresholds
            var tpr = metrics.Recall;
            var fpr = metrics.FalsePositives / 
                     (float)(metrics.FalsePositives + metrics.TrueNegatives);

            roc.Add((fpr, tpr));
            roc.Add((0f, 0f)); // Bottom-left (no positives predicted)
            roc.Add((1f, 1f)); // Top-right (all predicted as positive)

            return roc.OrderBy(p => p.Item1).ToList();
        }
    }
}
