using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Web_algorithm_award.Services;

namespace Web_algorithm_award.Areas.Customer.Controllers
{
    [Authorize]
    public class AprioriController : Controller
    {
        private readonly GeminiService _gemini;

        public AprioriController(GeminiService gemini)
        {
            _gemini = gemini;
        }

        private static Dictionary<string, List<string>> transactions = new Dictionary<string, List<string>>();

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string minSupport)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "❌ Vui lòng chọn file hợp lệ!";
                return RedirectToAction("Index");
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".svm")
            {
                TempData["Message"] = "❌ Chỉ hỗ trợ file .svm!";
                return RedirectToAction("Index");
            }

            transactions.Clear();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var parts = line.Split(new[] { ' ' }, 2);

                    if (parts.Length == 2)
                    {
                        string tid = parts[0];
                        List<string> items = parts[1]
                            .Split(',')
                            .Select(x => x.Trim())
                            .ToList();

                        transactions[tid] = items;
                    }
                }
            }

            return await RunApriori(minSupport);
        }

        [HttpPost]
        public async Task<IActionResult> RunApriori(string minSupport)
        {
            if (!double.TryParse(minSupport, NumberStyles.Any, CultureInfo.InvariantCulture,
                out double support) || support <= 0 || support > 1)
            {
                TempData["Message"] = "⚠️ Ngưỡng hỗ trợ không hợp lệ!";
                return RedirectToAction("Index");
            }

            var frequentItemsets =
                AprioriAlgorithm.Run(transactions.Values.ToList(), support);

            var result = frequentItemsets
                .Select(x => new
                {
                    Itemset = string.Join(", ", x.Key),
                    Support = x.Value
                })
                .ToList();

            if (result.Count == 0)
            {
                TempData["Message"] = "❌ Không có tập phổ biến nào thỏa mãn minsup!";
                return RedirectToAction("Index");
            }

            // TẠO PROMPT CHO AI

            var transactionText = string.Join("\n",
                transactions.Select(t =>
                    $"{t.Key} {string.Join(",", t.Value)}"));

            var resultText = string.Join("\n",
                result.Select(r =>
                    $"Itemset: {r.Itemset} - Support: {r.Support}"));

            var prompt = $@"
                        Explain how the Apriori algorithm produced these frequent itemsets.

                        Transactions:
                        {transactionText}

                        Result:
                        {resultText}

                        Explain step by step in Vietnamese.
                        ";

            // GỌI GEMINI AI

            var explanation = await _gemini.GenerateExplanation(prompt);

            // TRẢ KẾT QUẢ RA VIEW

            ViewBag.FrequentItemsets = result;
            ViewBag.TotalItemsets = result.Count;
            ViewBag.Support = support;
            ViewBag.AIExplanation = explanation;

            return View("Index");
        }
    }

    // APRIORI ALGORITHM

    public static class AprioriAlgorithm
    {
        public static Dictionary<HashSet<string>, double> Run(
            List<List<string>> transactions,
            double minSupport)
        {
            Dictionary<HashSet<string>, double> frequentItemsets =
                new Dictionary<HashSet<string>, double>(
                    HashSet<string>.CreateSetComparer());

            int totalTransactions = transactions.Count;

            // Đếm item đơn
            var itemCounts = transactions
                .SelectMany(t => t)
                .GroupBy(i => i)
                .ToDictionary(
                    g => new HashSet<string> { g.Key },
                    g => (double)g.Count() / totalTransactions
                );

            var currentItemsets = itemCounts
                .Where(i => i.Value >= minSupport)
                .ToDictionary(i => i.Key, i => i.Value);

            frequentItemsets =
                new Dictionary<HashSet<string>, double>(currentItemsets);

            int k = 2;

            while (currentItemsets.Count > 0)
            {
                var candidateItemsets =
                    GenerateCandidates(currentItemsets.Keys.ToList(), k);

                var candidateCounts =
                    new Dictionary<HashSet<string>, int>(
                        HashSet<string>.CreateSetComparer());

                foreach (var transaction in transactions)
                {
                    foreach (var candidate in candidateItemsets)
                    {
                        if (candidate.IsSubsetOf(transaction))
                        {
                            if (!candidateCounts.ContainsKey(candidate))
                                candidateCounts[candidate] = 0;

                            candidateCounts[candidate]++;
                        }
                    }
                }

                currentItemsets = candidateCounts
                    .Where(c =>
                        (double)c.Value / totalTransactions >= minSupport)
                    .ToDictionary(
                        c => c.Key,
                        c => (double)c.Value / totalTransactions);

                foreach (var item in currentItemsets)
                    frequentItemsets[item.Key] = item.Value;

                k++;
            }

            return frequentItemsets;
        }

        private static List<HashSet<string>> GenerateCandidates(
            List<HashSet<string>> prevItemsets,
            int k)
        {
            var candidates = new List<HashSet<string>>();

            for (int i = 0; i < prevItemsets.Count; i++)
            {
                for (int j = i + 1; j < prevItemsets.Count; j++)
                {
                    var unionSet = new HashSet<string>(prevItemsets[i]);

                    unionSet.UnionWith(prevItemsets[j]);

                    if (unionSet.Count == k &&
                        !candidates.Any(c => c.SetEquals(unionSet)))
                    {
                        candidates.Add(unionSet);
                    }
                }
            }

            return candidates;
        }
    }
}