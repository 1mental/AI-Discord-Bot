
namespace ChatboxTest.ChatGPT
{
    internal abstract class Image
    {
        public string prompt { get; set; }

        public string size { get; set; }

        protected static readonly int n = 1;


        public Image(string prompt, string size) 
        { 
            this.prompt = prompt;
            this.size = size;
        }


        public Image()
        {

        }


        public abstract string GetValue();
    }
}
