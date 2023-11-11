using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatboxTest.TextToSpeech
{
    public sealed class Talker
    {
        private static readonly string _token = "28e6c8968a4768abd69c27cb13205b331687966586";

        private static readonly string _model = "MF3mGyEYCl7XYWbV9V6O";

        private static readonly string voice_name = "Scarlett (Female)";

        private static readonly int fixedlength = 5000;

        private static readonly string endpoint = "https://tts-api.imyfone.com/voice/tts";


        private static byte[] BodyBuiler(string message)
        {
            return Encoding.Default.GetBytes($"token={_token}&lang=English&speaker={_model}&text={message}&type=6&voice_name={voice_name}&web_req=1");
        }


        public static async Task<string> Speech(string message)
        {

            // Error handling...

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException("Message is null.");
            if (message.Length > fixedlength)
                throw new TalkerInvaildLengthException(message);


            // Sending the request...

            byte[] FormData = BodyBuiler(message);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.ContentLength = FormData.Length;
            request.Accept = "application/json";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";

            using(Stream  stream = request.GetRequestStream())
                stream.Write(FormData, 0, FormData.Length);

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        return Regex.Match(reader.ReadToEnd(), "\"oss_url\":\"(.*?)\"").Groups[1].Value;

                }
                else
                    return "Unkown Error";
            }catch(WebException ex)
            {
                if (ex.Response != null)
                    return new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                return "null";
            }catch (Exception ex) 
            {
                if (ex.Message != null)
                    return ex.Message;
                else
                    return "null";
            }

        }



    }
}
