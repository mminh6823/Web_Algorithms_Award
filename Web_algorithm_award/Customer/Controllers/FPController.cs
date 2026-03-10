using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Web_algorithm_award.Services;

namespace Web_algorithm_award.Customer.Controllers
{
    public class FPController : Controller
    {
        private readonly GeminiService _gemini;

        public FPController(GeminiService gemini)
        {
            _gemini = gemini;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessFile(IFormFile uploadedFile, string minSupportStr)
        {
            Console.WriteLine($"Giá trị minSupport từ frontend: {minSupportStr}");

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp hợp lệ.";
                return View("Index");
            }

            if (!float.TryParse(
                    minSupportStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float minSupport))
            {
                TempData["ErrorMessage"] = "Giá trị Min Support không hợp lệ.";
                return View("Index");
            }

            List<List<string>> transactions = new List<List<string>>();

            using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(' ');
                    if (parts.Length < 2) continue;

                    var items = parts[1]
                        .Split(',')
                        .Select(i => i.Trim())
                        .ToList();

                    transactions.Add(items);
                }
            }

            if (transactions.Count == 0)
            {
                TempData["ErrorMessage"] = "File không có dữ liệu hợp lệ.";
                return View("Index");
            }

            var fpTree = BuildFPTree(transactions, minSupport);

            if (fpTree == null || fpTree.Children.Count == 0)
            {
                TempData["InfoMessage"] = "Không tìm thấy mẫu phổ biến nào.";
                return View("Index");
            }
            TempData["SuccessMessage"] = "Phân tích dữ liệu và xây dựng FP-Tree thành công!";
            string treeJson = JsonConvert.SerializeObject(fpTree, Formatting.Indented);

            string transactionText = string.Join("\n",
                transactions.Select(t => string.Join(", ", t)));

            // Prompt AI
            var prompt = $@"
                Explain step by step in Vietnamese how the FP-Growth algorithm built this FP-Tree.

                Transactions:
                {transactionText}

                Minimum Support:
                {minSupport}

                FP Tree Result:
                {treeJson}
                ";

            var explanation = await _gemini.GenerateExplanation(prompt);

            ViewBag.FPTreeJson = treeJson;
            ViewBag.Explanation = explanation;

            return View("Index");
        }

        private Node BuildFPTree(List<List<string>> transactions, double minSupport)
        {
            if (minSupport <= 0 || minSupport > 1)
            {
                throw new ArgumentException("minSupport phải nằm trong khoảng (0,1]");
            }

            Dictionary<string, int> frequency = new Dictionary<string, int>();

            // 1. Đếm tần suất item
            foreach (var transaction in transactions)
            {
                foreach (var item in transaction)
                {
                    if (frequency.ContainsKey(item))
                        frequency[item]++;
                    else
                        frequency[item] = 1;
                }
            }

            // 2. minCount
            int minCount = (int)Math.Ceiling(minSupport * transactions.Count);

            var frequentItems = frequency
                .Where(kv => kv.Value >= minCount)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (frequentItems.Count == 0)
            {
                return new Node("Empty Tree", 0);
            }

            // 3. Build Tree
            Node root = new Node("Null", 0);

            foreach (var transaction in transactions)
            {
                var sortedTransaction = transaction
                    .Where(i => frequentItems.Contains(i))
                    .OrderByDescending(i => frequency[i])
                    .ToList();

                if (sortedTransaction.Count > 0)
                {
                    InsertTransaction(root, sortedTransaction);
                }
            }

            return root;
        }

        private void InsertTransaction(Node root, List<string> transaction)
        {
            if (transaction.Count == 0) return;

            string item = transaction[0];

            Node child = root.Children.FirstOrDefault(n => n.Item == item);

            if (child == null)
            {
                child = new Node(item, 1);
                root.Children.Add(child);
            }
            else
            {
                child.Count++;
            }

            InsertTransaction(child, transaction.Skip(1).ToList());
        }
        public IActionResult DownloadSampleFP()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/FileSample/sample.svm");

            return PhysicalFile(path, "application/octet-stream", "sample.svm");
        }
    }

    public class Node
    {
        public string Item { get; set; }

        public int Count { get; set; }

        public List<Node> Children { get; set; }

        public Node(string item, int count)
        {
            Item = item;
            Count = count;
            Children = new List<Node>();
        }
    }
}