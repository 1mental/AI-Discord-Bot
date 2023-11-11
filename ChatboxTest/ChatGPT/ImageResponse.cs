using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace ChatboxTest.ChatGPT
{
    internal class ImageResponse : Image
    {
        private string Url = string.Empty;
        private HttpWebRequest Request;
        private readonly string endpoint = "https://api.openai.com/v1/images/generations";

        public override string GetValue()
        {
            if (Url == string.Empty)
                throw new NullReferenceException("ImageResponse haven't been processed.");
            return Url;
        }

        public async Task<string> GetValue(ImageRequest request)
        {
            // Error handling...

            if (request.prompt.Length > 150)
                throw new InvaildLengthException("Maxmium length is 150");
            else if (ChatGPT.tokenizer == null)
                throw new TokenizerEmptyException("Tokenizer is null");
            else if (string.IsNullOrEmpty(request.prompt))
                throw new ChatGPTMessageException("prompt is null!");


            byte[] json = Encoding.UTF8.GetBytes(request.GetValue());

            Request = (HttpWebRequest)WebRequest.Create(this.endpoint);
            Request.ContentLength = json.Length;
            Request.Method = "POST";
            Request.Headers.Add("Content-type", "application/json");
            Request.Headers.Add("Authorization", $"Bearer {ChatGPT.tokenizer.GetCurrentToken()}");
            Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";

            using (Stream stream = Request.GetRequestStream())
                stream.Write(json);
            


            try
            {
                // Send the request & Getting the response
                HttpWebResponse response = (HttpWebResponse)await Request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Url = Regex.Match(reader.ReadToEnd(), "\"url\": \"(.*?)\"").Groups[1].Value;
                        return Url;
                    }
                }
                else
                    return response.StatusCode.ToString();


                // Catching Exceptions for debugging.
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return "Null Response";

                string exMessage = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

                if (exMessage.Contains("You exceeded your current quota, please check your plan and billing details"))
                {
                    ChatGPT.tokenizer.NextToken();
                    return await GetValue(request);
                }
                else if (exMessage.Contains("Your request was rejected as a result of our safety system."))
                    return "وش تدور له؟!";

                else
                    return exMessage;


            }
            catch (IOException ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return "IOError";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("There are no next token"))
                {
                    await Console.Out.WriteLineAsync("[+] Run out of tokens!");
                    Environment.Exit(-1);

                }
                await Console.Out.WriteLineAsync(ex.Message);
                return "Error";
            }

        }

    }
}
