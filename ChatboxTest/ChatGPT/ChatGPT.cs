using System.Text;
using System.Net;
using System.Text.RegularExpressions;


namespace ChatboxTest.ChatGPT
{
    public class ChatGPT
    {
        private readonly string endpoint = "https://api.openai.com/v1/engines/text-davinci-003/completions";
        public List<string> Tokens { get; set; }

        public static Tokenizer<string> tokenizer;

        private HttpWebRequest request;


       


        public void loadTokens()
        {
            tokenizer = new Tokenizer<string>();
            foreach (var token in Tokens) 
                tokenizer.InsertToken(token);
        }

        private byte[] buildBody(string message, uint tokens)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            builder.Append($"\"prompt\":\"{message}\",");
            builder.Append($"\"max_tokens\":{tokens},");
            builder.Append($"\"n\":{1},");
            builder.Append($"\"stop\":null");
            builder.Append("}");
            return Encoding.UTF8.GetBytes( builder.ToString() );
        }

        public async Task<String> sendMessage(string message)
        {
            // Error indicating
            if (message == null)
                throw new ChatGPTMessageException("Message is null.");
            else if (message.Length > 150)
                throw new InvaildLengthException("Message length is too big.");
            else if (tokenizer == null)
                throw new NullReferenceException("Tokenizer is null.");
            // Preparing the request
            byte[] body = buildBody(message, 500);
            request = (HttpWebRequest)WebRequest.CreateHttp(endpoint);
            request.Method = "POST";
            request.Headers.Add("Content-type", "application/json");
            request.Headers.Add("Authorization", $"Bearer {tokenizer.GetCurrentToken()}");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";
            using (Stream stream = request.GetRequestStream())
                stream.Write(body);

            try
            {
                // Send the request & Getting the response
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string ResponseMessage = Regex.Match(reader.ReadToEnd(), "\"text\":.\"(.*?)\"").Groups[1].Value;
                        await Console.Out.WriteLineAsync(ResponseMessage);
                        return ResponseMessage;
                    }
                }else
                {
                    return response.StatusCode.ToString();
                }

                // Catching Exceptions for debugging.
            }catch(WebException ex)
            {
                if (ex.Response == null)
                  return "Null Response";

                string exMessage = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

                if (exMessage.Contains("You exceeded your current quota, please check your plan and billing details"))
                {
                    tokenizer.NextToken();
                    return await sendMessage(message);
                }
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



        public async Task<String> sendMessage(string message, uint length)
        {
            // Error indicating
            if (message == null)
                throw new ChatGPTMessageException("Message is null.");
            else if (message.Length > length)
                throw new InvaildLengthException("Message length is too big.");
            else if (tokenizer == null)
                throw new NullReferenceException("Tokenizer is null.");
            // Preparing the request
            byte[] body = buildBody(message,length);
            request = (HttpWebRequest)WebRequest.CreateHttp(endpoint);
            request.Method = "POST";
            request.Headers.Add("Content-type", "application/json");
            request.Headers.Add("Authorization", $"Bearer {tokenizer.GetCurrentToken()}");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";
            using (Stream stream = request.GetRequestStream())
                stream.Write(body);

            try
            {
                // Send the request & Getting the response
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string ResponseMessage = Regex.Match(reader.ReadToEnd(), "\"text\":.\"(.*?)\"").Groups[1].Value;
                        await Console.Out.WriteLineAsync(ResponseMessage);
                        return ResponseMessage;
                    }
                }
                else
                {
                    return response.StatusCode.ToString();
                }

                // Catching Exceptions for debugging.
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return "Null Response";

                string exMessage = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();

                if (exMessage.Contains("You exceeded your current quota, please check your plan and billing details"))
                {
                    tokenizer.NextToken();
                    return await sendMessage(message,length);
                }
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



        public async Task<string> GenerateImage(string prompt)
        {
            try
            {
                ImageRequest image = new ImageRequest(prompt,"512x512");
                return await new ImageResponse().GetValue(image);
            }catch (InvaildLengthException) 
            {
                return "حدك 150 حرف فقط.";
            }catch(ChatGPTMessageException)
            {
                return "اكتب شي عشان اسوي لك الصورة!";
            }catch(Exception ex)
            {
                if (ex.Message.Contains("There are no next token"))
                {
                    await Console.Out.WriteLineAsync("[+] Run out of tokens!");
                    Environment.Exit(-1);

                }
                await Console.Out.WriteLineAsync(ex.Message);
                return "فيه مشكلة حصلت!";
            }
        }

    }
}
