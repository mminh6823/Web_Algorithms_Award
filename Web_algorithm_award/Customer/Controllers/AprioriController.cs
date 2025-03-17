using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Web_algorithm_award.Areas.Customer.Controllers
{
    [Authorize]
    public class AprioriController : Controller
    {
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
                        List<string> items = parts[1].Split(',').Select(x => x.Trim()).ToList();
                        transactions[tid] = items;
                    }
                }
            }

            return RunApriori(minSupport);
        }

        [HttpPost]
        public IActionResult RunApriori(string minSupport)
        {
            if (!double.TryParse(minSupport, NumberStyles.Any, CultureInfo.InvariantCulture, out double support) || support <= 0 || support > 1)
            {
                TempData["Message"] = "⚠️ Ngưỡng hỗ trợ không hợp lệ!";
                return RedirectToAction("Index");
            }

            var frequentItemsets = AprioriAlgorithm.Run(transactions.Values.ToList(), support);
            var result = frequentItemsets.Select(x => new { Itemset = string.Join(", ", x.Key), Support = x.Value }).ToList();

            if (result.Count == 0)
            {
                TempData["Message"] = "❌ Không có tập phổ biến nào thỏa mãn minsup!";
                return RedirectToAction("Index");
            }

            ViewBag.FrequentItemsets = result;
            ViewBag.TotalItemsets = result.Count;
            ViewBag.Support = support;
            return View("Index");
        }
    }

    public static class AprioriAlgorithm
    {
        public static Dictionary<HashSet<string>, double> Run(List<List<string>> transactions, double minSupport)
        {
            Dictionary<HashSet<string>, double> frequentItemsets = new Dictionary<HashSet<string>, double>(HashSet<string>.CreateSetComparer());
            int totalTransactions = transactions.Count;

            // Đếm số lần xuất hiện của từng item đơn lẻ
            var itemCounts = transactions
                .SelectMany(t => t)
                .GroupBy(i => i)
                .ToDictionary(g => new HashSet<string> { g.Key }, g => (double)g.Count() / totalTransactions);

            // Lọc các itemset phổ biến
            var currentItemsets = itemCounts
                .Where(i => i.Value >= minSupport)
                .ToDictionary(i => i.Key, i => i.Value);

            frequentItemsets = new Dictionary<HashSet<string>, double>(currentItemsets);

            int k = 2;
            while (currentItemsets.Count > 0)
            {
                var candidateItemsets = GenerateCandidates(currentItemsets.Keys.ToList(), k);
                var candidateCounts = new Dictionary<HashSet<string>, int>(HashSet<string>.CreateSetComparer());

                // Đếm số lần xuất hiện của từng tập ứng viên
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

                // Chuyển đổi thành support và lọc theo minSupport
                currentItemsets = candidateCounts
                    .Where(c => (double)c.Value / totalTransactions >= minSupport)
                    .ToDictionary(c => c.Key, c => (double)c.Value / totalTransactions);

                // Thêm vào danh sách tập phổ biến
                foreach (var item in currentItemsets)
                    frequentItemsets[item.Key] = item.Value;

                k++;
            }

            return frequentItemsets;
        }

        private static List<HashSet<string>> GenerateCandidates(List<HashSet<string>> prevItemsets, int k)
        {
            var candidates = new List<HashSet<string>>();

            for (int i = 0; i < prevItemsets.Count; i++)
            {
                for (int j = i + 1; j < prevItemsets.Count; j++)
                {
                    var unionSet = new HashSet<string>(prevItemsets[i]);
                    unionSet.UnionWith(prevItemsets[j]);

                    if (unionSet.Count == k && !candidates.Any(c => c.SetEquals(unionSet)))
                        candidates.Add(unionSet);
                }
            }

            return candidates;
        }
    }
}
