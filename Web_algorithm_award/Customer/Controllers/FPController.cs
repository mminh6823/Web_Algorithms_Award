using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Web_algorithm_award.Customer.Controllers
{
    public class FPController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessFile(IFormFile uploadedFile, string  minSupportStr)
        {
            Console.WriteLine($"Giá trị minSupport từ frontend: {minSupportStr}");
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ViewBag.Message = "Vui lòng chọn tệp hợp lệ.";
                return View("Index");
            }
            // Chuyển đổi minSupport từ string sang float
            if (!float.TryParse(minSupportStr, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out float minSupport)) ;

                List<List<string>> transactions = new List<List<string>>();

            using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(' ');
                    if (parts.Length < 2) continue; // Bỏ qua dòng không hợp lệ

                    var items = parts[1].Split(',').Select(i => i.Trim()).ToList(); // Bỏ TID, lấy danh sách item
                    transactions.Add(items);
                }
            }

            if (transactions.Count == 0)
            {
                ViewBag.Message = "File không có dữ liệu hợp lệ.";
                return View("Index");
            }

            // Tạo FP-Tree từ dữ liệu
            var fpTree = BuildFPTree(transactions, minSupport);

            if (fpTree == null || fpTree.Children.Count == 0)
            {
                ViewBag.Message = "Không tìm thấy mẫu phổ biến nào.";
                return View("Index");
            }

            // Chuyển FP-Tree thành JSON để hiển thị trong View
            string treeJson = JsonConvert.SerializeObject(fpTree, Formatting.Indented);

            ViewBag.FPTreeJson = treeJson;
            return View("Index");
        }

        private Node BuildFPTree(List<List<string>> transactions, double minSupport)
        {
            if (minSupport <= 0 || minSupport > 1)
            {
                throw new ArgumentException("minSupport phải nằm trong khoảng (0,1]");
            }

            Dictionary<string, int> frequency = new Dictionary<string, int>();

            // ✅ 1. Đếm số lần xuất hiện của mỗi item
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

            // ✅ 2. Xác định số lần tối thiểu để một item được giữ lại
            int minCount = (int)Math.Ceiling(minSupport * transactions.Count);

            var frequentItems = frequency.Where(kv => kv.Value >= minCount)
                                         .OrderByDescending(kv => kv.Value)
                                         .Select(kv => kv.Key)
                                         .ToList();

            if (frequentItems.Count == 0)
            {
                return new Node("Empty Tree", 0, null); // 🔥 Không có item nào đạt minSupport
            }

            // ✅ 3. Xây dựng FP-Tree
            Node root = new Node("Null", 0, null);

            foreach (var transaction in transactions)
            {
                var sortedTransaction = transaction.Where(i => frequentItems.Contains(i))
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
                child = new Node(item, 1, root);
                root.Children.Add(child);
            }
            else
            {
                child.Count++;
            }

            InsertTransaction(child, transaction.Skip(1).ToList());
        }
    }

    public class Node
    {
        public string Item { get; set; }
        public int Count { get; set; }
        public List<Node> Children { get; set; }

        public Node(string item, int count, Node parent)
        {
            Item = item;
            Count = count;
            Children = new List<Node>();
        }
    }
}
