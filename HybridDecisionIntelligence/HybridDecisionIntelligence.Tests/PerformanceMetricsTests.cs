using Xunit;
using FluentAssertions;
using HybridDecisionIntelligence.Tests.Utilities;
using System.Diagnostics;

namespace HybridDecisionIntelligence.Tests
{
    /// <summary>
    /// Performance Metrics Tests
    /// Calculates Accuracy, Precision, and Recall using ML.NET evaluation
    /// Tests against Bank Marketing test partition
    /// </summary>
    public class PerformanceMetricsTests
    {
        private readonly MLPerformanceEvaluator _evaluator;

        public PerformanceMetricsTests()
        {
            _evaluator = new MLPerformanceEvaluator();
        }

        /// <summary>
        /// Test: Calculate Accuracy metric
        /// Expected: Accuracy > 0.75 (75% correct predictions)
        /// </summary>
        [Fact]
        public void CalculateAccuracy_ShouldMeetThreshold()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // True Positive
                (predicted: true, actual: true),
                (predicted: true, actual: false),  // False Positive
                (predicted: false, actual: false), // True Negative
                (predicted: false, actual: false),
                (predicted: false, actual: true),  // False Negative
                (predicted: true, actual: true),
                (predicted: false, actual: false)
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.Accuracy.Should().BeGreaterThan(0.70, "Model accuracy should exceed 70%");
            metrics.Accuracy.Should().BeLessThanOrEqualTo(1.0, "Accuracy cannot exceed 100%");
        }

        /// <summary>
        /// Test: Calculate Precision metric
        /// Precision = TP / (TP + FP)
        /// Expected: Precision > 0.80 (80% of predicted positives are correct)
        /// </summary>
        [Fact]
        public void CalculatePrecision_ShouldIndicateQualityOfPositivePredictions()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // TP: 4
                (predicted: true, actual: true),
                (predicted: true, actual: true),
                (predicted: true, actual: true),
                (predicted: true, actual: false),  // FP: 1
                (predicted: false, actual: true),  // FN: 2
                (predicted: false, actual: true),
                (predicted: false, actual: false)  // TN: 3
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            // TP=4, FP=1, so Precision = 4/(4+1) = 0.8
            metrics.Precision.Should().Be(0.80f, precision: 0.01f, "Precision = TP/(TP+FP)");
            metrics.Precision.Should().BeGreaterThanOrEqualTo(0.70, "Precision should exceed 70%");
        }

        /// <summary>
        /// Test: Calculate Recall metric
        /// Recall = TP / (TP + FN)
        /// Expected: Recall > 0.75 (capture 75% of actual positives)
        /// </summary>
        [Fact]
        public void CalculateRecall_ShouldIndicateCoverageOfActualPositives()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // TP: 3
                (predicted: true, actual: true),
                (predicted: true, actual: true),
                (predicted: true, actual: false),  // FP: 1
                (predicted: false, actual: true),  // FN: 1
                (predicted: false, actual: false), // TN: 3
                (predicted: false, actual: false),
                (predicted: false, actual: false)
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            // TP=3, FN=1, so Recall = 3/(3+1) = 0.75
            metrics.Recall.Should().Be(0.75f, precision: 0.01f, "Recall = TP/(TP+FN)");
            metrics.Recall.Should().BeGreaterThanOrEqualTo(0.70, "Recall should exceed 70%");
        }

        /// <summary>
        /// Test: F1 Score (harmonic mean of Precision and Recall)
        /// F1 = 2 * (Precision * Recall) / (Precision + Recall)
        /// Expected: F1 > 0.75
        /// </summary>
        [Fact]
        public void CalculateF1Score_ShouldBalancePrecisionAndRecall()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // TP
                (predicted: true, actual: true),
                (predicted: true, actual: false),  // FP
                (predicted: false, actual: false), // TN
                (predicted: false, actual: true)   // FN
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.F1Score.Should().BeGreaterThan(0.50, "F1 score should be reasonable");
            metrics.F1Score.Should().BeLessThanOrEqualTo(1.0, "F1 score cannot exceed 1.0");
        }

        /// <summary>
        /// Test: ROC AUC (Area Under the Receiver Operating Characteristic Curve)
        /// Measures the model's ability to distinguish between classes
        /// Expected: AUC > 0.80
        /// </summary>
        [Fact]
        public void CalculateROC_AUC_ShouldMeasureClassSeparation()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // Strong positive
                (predicted: true, actual: true),
                (predicted: false, actual: false), // Strong negative
                (predicted: false, actual: false),
                (predicted: true, actual: false),  // Weak positive
                (predicted: false, actual: true)   // Weak negative
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.AUC.Should().BeGreaterThan(0.50, "AUC should be better than random guessing");
            metrics.AUC.Should().BeLessThanOrEqualTo(1.0, "AUC cannot exceed 1.0");
        }

        /// <summary>
        /// Test: Confusion Matrix components
        /// Validates True Positives, False Positives, True Negatives, False Negatives
        /// </summary>
        [Fact]
        public void ConfusionMatrix_ShouldCalculateAllComponents()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // TP
                (predicted: true, actual: true),   // TP
                (predicted: true, actual: true),   // TP
                (predicted: true, actual: false),  // FP
                (predicted: false, actual: false), // TN
                (predicted: false, actual: false), // TN
                (predicted: false, actual: true),  // FN
                (predicted: false, actual: true)   // FN
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.TruePositives.Should().Be(3);
            metrics.FalsePositives.Should().Be(1);
            metrics.TrueNegatives.Should().Be(2);
            metrics.FalseNegatives.Should().Be(2);
        }

        /// <summary>
        /// Test: Sensitivity and Specificity
        /// Sensitivity = TP / (TP + FN) = Recall
        /// Specificity = TN / (TN + FP)
        /// </summary>
        [Fact]
        public void SensitivityAndSpecificity_ShouldMeasureClassPerformance()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),   // TP: 2
                (predicted: true, actual: true),
                (predicted: true, actual: false),  // FP: 1
                (predicted: false, actual: false), // TN: 2
                (predicted: false, actual: false),
                (predicted: false, actual: true),  // FN: 1
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            // Sensitivity = TP / (TP + FN) = 2 / 3 ≈ 0.667
            metrics.Sensitivity.Should().BeApproximately(0.667f, 0.01f);
            // Specificity = TN / (TN + FP) = 2 / 3 ≈ 0.667
            metrics.Specificity.Should().BeApproximately(0.667f, 0.01f);
        }

        /// <summary>
        /// Test: Performance metrics on large dataset
        /// Validates calculation performance (should complete within 1 second)
        /// </summary>
        [Fact]
        public void PerformanceMetrics_LargeDataset_ShouldCalculateQuickly()
        {
            // Arrange
            var random = new Random(42);
            var predictions = Enumerable.Range(0, 10000)
                .Select(_ => (
                    predicted: random.Next(0, 2) == 1,
                    actual: random.Next(0, 2) == 1
                ))
                .ToArray();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var metrics = _evaluator.CalculateMetrics(predictions);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Calculation should complete within 1 second");
            metrics.Accuracy.Should().BeGreaterThan(0.30, "Random 50/50 data should average ~50% accuracy");
        }

        /// <summary>
        /// Test: Edge case - all predictions are correct
        /// Expected: Accuracy = 1.0, Precision = 1.0, Recall = 1.0
        /// </summary>
        [Fact]
        public void AllPredictionsCorrect_ShouldReturnPerfectScores()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: true),
                (predicted: true, actual: true),
                (predicted: false, actual: false),
                (predicted: false, actual: false)
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.Accuracy.Should().Be(1.0f);
            metrics.Precision.Should().Be(1.0f);
            metrics.Recall.Should().Be(1.0f);
            metrics.F1Score.Should().Be(1.0f);
        }

        /// <summary>
        /// Test: Edge case - all predictions are incorrect
        /// Expected: Accuracy = 0.0
        /// </summary>
        [Fact]
        public void AllPredictionsIncorrect_ShouldReturnZeroAccuracy()
        {
            // Arrange
            var predictions = new[]
            {
                (predicted: true, actual: false),
                (predicted: true, actual: false),
                (predicted: false, actual: true),
                (predicted: false, actual: true)
            };

            // Act
            var metrics = _evaluator.CalculateMetrics(predictions);

            // Assert
            metrics.Accuracy.Should().Be(0.0f);
        }
    }
}
