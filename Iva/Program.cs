using ConsoleApp1;

namespace ConsoleApp1
{
    public class Node
    {
        public List<Node> children; // []
        int value;
    }

    public void findNode(Node node, int value, int level)
    {
            foreach (var n in node.children) {
                if (level % 2 == 0) {
                    if (n.value == value)
                    {
                        return;
                    }
                    else { 
                        findNode(n, value, level + 1);
                    }
                } else {
                    findNode(n, value, level + 1);
                }
            }
    }
}


//public static void findNode(Node node, int value, int level)
//{
//    if (level % 2 == 0)
//    {
//        if (node.value == value)
//        {
//            Console.WriteLine("Nivoto na...");
//            return;
//        }
//        else
//        {
//            foreach (var node1 in node.children)
//            {
//                findNode(node1, value, level + 1);

//            }
//            return;
//        }
//    }
//}
//    }

//    class Program
//{


//    static void Main(string[] args)
//    {
//        Console.WriteLine("Hello, World!");
//    }
//}