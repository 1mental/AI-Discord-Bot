using System.Collections;

namespace ChatboxTest.ChatGPT
{
    public class Tokenizer<T> : IEnumerable<T> where T : IComparable<T>
    {
        public int size
        {
            get;
            private set;
        }

        private Node<T>? head;

        public bool IsEmpty()
        {
            return head == null;
        }

        private class Node<B>
        {
            public Node(T? item)
            {
                this.item = item;
            }
            public T? item { get; set; }
            public Node<B>? next
            {
                get; set;
            } = null;
        }


        public Tokenizer()
        {
            head = null;
            size = 0;
        }


        public void InsertToken(T item)
        {

            // O(1) Insertion if Head IsEmpty()

            Node<T> node = new Node<T>(item);
            if (IsEmpty())
            {
                head = node;
                size++;
                return;
            }

            // O(n) Insertion if Head isn't empty

            Node<T>? current = head;

            while (current?.next != null) 
                current = current.next;

            current.next = node;


            size++;

        }


        public T NextToken()
        {
            if (IsEmpty())
                throw new TokenizerEmptyException("Tokenizer is empty!");

            Node<T>? temp = head;
            if (head.next != null)
            {
                head = head.next;
                temp.next = null;
                return head.item;
            }

            throw new TokenizerLastTokenException("There are no next token");
        }


        public T GetCurrentToken()
        {
            if (!IsEmpty())
                return head.item;

            throw new TokenizerEmptyException("Tokenizer is empty!");
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (IsEmpty())
                throw new TokenizerEmptyException("Tokenizer is empty!");

            Node<T>? current = head;
            while(current != null)
            {
                yield return current.item;
                current = current.next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
