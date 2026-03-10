namespace Web_algorithm_award_Model
{
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