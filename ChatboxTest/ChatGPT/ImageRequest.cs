using System.Text;

namespace ChatboxTest.ChatGPT
{
    internal class ImageRequest : Image
    {
        private StringBuilder jsonBuilder;
        public ImageRequest() : base() {}
        public ImageRequest(string prompt, string size) : base(prompt, size){}

        public override string GetValue()
        {
            jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"prompt\":\"{this.prompt}\",");
            jsonBuilder.Append($"\"n\":{n},");
            jsonBuilder.Append($"\"size\":\"{this.size}\"");
            jsonBuilder.Append("}");

            return jsonBuilder.ToString();
        }
    }
}
