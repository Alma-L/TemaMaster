using HybridDecisionIntelligence.Domain.Entities;

namespace HybridDecisionIntelligence.Tests.Utilities
{
    /// <summary>
    /// Test Data Builder Utility
    /// Creates test data with mocked BankData.csv streams
    /// Enables tests to run without physical file dependencies
    /// </summary>
    public class TestDataBuilder
    {
        /// <summary>
        /// Create a mock BankCustomer for testing
        /// </summary>
        public static BankCustomer CreateValidBankCustomer(int customerId = 1)
        {
            return new BankCustomer
            {
                Id = customerId,
                Age = 42,
                Job = "management",
                Marital = "married",
                Education = "tertiary",
                Balance = 25000m,
                Housing = "yes",
                Loan = "no",
                Duration = 250,
                Campaign = 1,
                Previous = 0,
                Default = "no",
                Contact = "cellular",
                Day = 15,
                Month = "Mar",
                PDays = -1,
                POutcome = "unknown",
                SubscribedToTerm = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a high-risk BankCustomer
        /// </summary>
        public static BankCustomer CreateHighRiskBankCustomer(int customerId = 2)
        {
            return new BankCustomer
            {
                Id = customerId,
                Age = 25,
                Job = "unemployed",
                Marital = "single",
                Education = "illiterate",
                Balance = 100m,
                Housing = "no",
                Loan = "yes",
                Duration = 50,
                Campaign = 15,
                Previous = 5,
                Default = "yes",
                Contact = "cellular",
                Day = 1,
                Month = "Jan",
                PDays = 1,
                POutcome = "failure",
                SubscribedToTerm = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a low-risk BankCustomer
        /// </summary>
        public static BankCustomer CreateLowRiskBankCustomer(int customerId = 3)
        {
            return new BankCustomer
            {
                Id = customerId,
                Age = 52,
                Job = "management",
                Marital = "married",
                Education = "tertiary",
                Balance = 150000m,
                Housing = "yes",
                Loan = "no",
                Duration = 300,
                Campaign = 1,
                Previous = 1,
                Default = "no",
                Contact = "cellular",
                Day = 20,
                Month = "Jun",
                PDays = 999,
                POutcome = "success",
                SubscribedToTerm = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a batch of test customers
        /// </summary>
        public static List<BankCustomer> CreateBatchOfCustomers(int count = 10)
        {
            var customers = new List<BankCustomer>();
            var jobs = new[] { "management", "technician", "services", "admin", 
                             "retired", "unemployed", "entrepreneur", "housemaid" };
            var maritalStates = new[] { "married", "single", "divorced", "unknown" };
            var educations = new[] { "primary", "secondary", "tertiary", "illiterate" };

            var random = new Random(42); // Reproducible seed

            for (int i = 0; i < count; i++)
            {
                customers.Add(new BankCustomer
                {
                    Id = i + 1,
                    Age = random.Next(18, 95),
                    Job = jobs[random.Next(jobs.Length)],
                    Marital = maritalStates[random.Next(maritalStates.Length)],
                    Education = educations[random.Next(educations.Length)],
                    Balance = random.Next(0, 500000),
                    Housing = random.Next(0, 2) == 0 ? "yes" : "no",
                    Loan = random.Next(0, 2) == 0 ? "yes" : "no",
                    Duration = random.Next(0, 4000),
                    Campaign = random.Next(1, 100),
                    Previous = random.Next(0, 100),
                    Default = random.Next(0, 100) < 10 ? "yes" : "no",
                    Contact = random.Next(0, 2) == 0 ? "cellular" : "telephone",
                    Day = random.Next(1, 32),
                    Month = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
                                   "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }[random.Next(12)],
                    PDays = random.Next(-1, 1000),
                    POutcome = new[] { "unknown", "success", "failure", "other" }[random.Next(4)],
                    SubscribedToTerm = random.Next(0, 100) < 12, // ~12% subscription rate
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(365))
                });
            }

            return customers;
        }

        /// <summary>
        /// Create a mock MLPredictionResult
        /// </summary>
        public static MLPredictionResult CreateMLPredictionResult(
            int customerId = 1,
            bool predictedLabel = true,
            float probability = 0.85f)
        {
            return new MLPredictionResult
            {
                Id = customerId,
                CustomerId = customerId,
                PredictedLabel = predictedLabel,
                Score = probability,
                Probability = probability,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create a mock HybridDecision
        /// </summary>
        public static HybridDecision CreateHybridDecision(
            int customerId = 1,
            bool mlPredicted = true,
            bool finalDecision = true,
            bool wasOverridden = false)
        {
            return new HybridDecision
            {
                Id = customerId,
                CustomerId = customerId,
                MLPredictionResultId = customerId,
                MLPredicted = mlPredicted,
                MLConfidence = 0.80f,
                FinalDecision = finalDecision,
                WasOverridden = wasOverridden,
                AuditTrail = $"Test audit trail for customer {customerId}",
                ApprovedInterestRate = finalDecision ? 0.04m : 0m,
                RulesApplied = "Test Rules",
                OverrideReason = wasOverridden ? "Test Override" : "",
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create mock CSV data stream for BankData.csv
        /// </summary>
        public static Stream CreateMockBankDataStream(int recordCount = 100)
        {
            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("age;job;marital;education;default;balance;housing;loan;" +
                         "contact;day;month;duration;campaign;pdays;previous;poutcome;y");

            // Create mock data rows
            var jobs = new[] { "management", "technician", "services", "admin" };
            var maritalStates = new[] { "married", "single", "divorced" };
            var educations = new[] { "primary", "secondary", "tertiary" };
            var months = new[] { "jan", "feb", "mar", "apr", "may", "jun", 
                                "jul", "aug", "sep", "oct", "nov", "dec" };
            var poutcomes = new[] { "unknown", "success", "failure" };

            var random = new Random(42);

            for (int i = 0; i < recordCount; i++)
            {
                csv.AppendLine($"{random.Next(18, 95)};" +
                             $"{jobs[random.Next(jobs.Length)]};" +
                             $"{maritalStates[random.Next(maritalStates.Length)]};" +
                             $"{educations[random.Next(educations.Length)]};" +
                             $"{(random.Next(0, 100) < 5 ? "yes" : "no")};" +
                             $"{random.Next(0, 500000)};" +
                             $"{(random.Next(0, 2) == 0 ? "yes" : "no")};" +
                             $"{(random.Next(0, 2) == 0 ? "yes" : "no")};" +
                             $"{(random.Next(0, 2) == 0 ? "cellular" : "telephone")};" +
                             $"{random.Next(1, 32)};" +
                             $"{months[random.Next(months.Length)]};" +
                             $"{random.Next(0, 4000)};" +
                             $"{random.Next(1, 100)};" +
                             $"{random.Next(-1, 1000)};" +
                             $"{random.Next(0, 100)};" +
                             $"{poutcomes[random.Next(poutcomes.Length)]};" +
                             $"{(random.Next(0, 100) < 12 ? "yes" : "no")}");
            }

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(csv.ToString());
            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Create mock test partition data
        /// </summary>
        public static (bool[] predicted, bool[] actual) CreateMockTestPartition(int sampleSize = 1000)
        {
            var random = new Random(42);
            var predicted = new bool[sampleSize];
            var actual = new bool[sampleSize];

            for (int i = 0; i < sampleSize; i++)
            {
                actual[i] = random.Next(0, 100) < 12; // 12% positive class
                predicted[i] = random.Next(0, 100) < 80; // 80% positive predictions
            }

            return (predicted, actual);
        }
    }

    /// <summary>
    /// String builder helper (namespace helper)
    /// </summary>
    internal class StringBuilder
    {
        private System.Text.StringBuilder _sb = new();

        public void AppendLine(string value)
        {
            _sb.AppendLine(value);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
