using ChatboxTest.ChatGPT;
using System.Security.Cryptography;
using System.Text;
using ChatboxTest.Discord;
using System.Text.Json;
using ChatboxTest;

new Initilizer();


do
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n[+] Choose an option:\n\n1 - Check which token is used.\n\n2 - Add a new Allowed User.");
    try
    {
        Console.ForegroundColor = ConsoleColor.White;
        int input = Convert.ToInt32(Console.ReadLine());
        switch(input)
        {
            case 1:
                Console.Clear();
                Console.WriteLine(ChatGPT.tokenizer.GetCurrentToken());
                break;
            case 2:
                Console.WriteLine("Enter user ID : ");
                string? id = Console.ReadLine();
                Initilizer._bot.AllowedUsers.Add(id);
                Console.Clear();
                Console.WriteLine("[+] Added");
                break;
            default:
                Console.Clear();
                Console.WriteLine("[+] Wrong option"); 
                break;
        }
    }catch(Exception ex)
    {
        Console.Clear();
        Console.WriteLine(ex.Message);

    }
} while (true);

// Todo List....

/*
 * 
 * 1 - Complete Initilizer class (90% completed)
 * 2 - Complete the discord Bot  (70% Completed)
 * 3 - Complete ChatGPT Api  (Done)
 * 4 - test ChatGPT Api (Done)
 * 5 - build Logging System if needed. (Done)
 * 6 - text to Image (Done)
 * 
 */


public class Initilizer
{
    public static DiscordBot _bot;
    public static ChatGPT _gptClient;

    public static CacheCleaner cleaner;
    public struct Folders
    {
        public readonly static string configDirectory = Environment.CurrentDirectory + "\\config";
        public readonly static string discordJson = Environment.CurrentDirectory + "\\config\\discord.json";
        public readonly static string gptJson = Environment.CurrentDirectory + "\\config\\chatgpt.json";
    }
    public Initilizer()
    {

        Thread InitThread = new Thread(() =>
        {
            try
            {

                // Creating files.
                if (!Directory.Exists(Folders.configDirectory))
                {
                    Directory.CreateDirectory(Folders.configDirectory);
                    CreateFile(Folders.gptJson, "{\r\n  \"Tokens\": [\r\n    \"\",\r\n    \"\",\r\n    \"\"\r\n  ]\r\n}");
                    CreateFile(Folders.discordJson, "{\r\n  \"Token\": \"\",\r\n    \"GuildID\": \"\",\r\n    \"AllowedUsers\": [\r\n      \"\",\r\n      \"\"\r\n    ]\r\n  \r\n}");
                    Console.WriteLine("[+] Config files are created, please fill them!");
                    return;
                    
                }
                else if (!File.Exists(Folders.gptJson))
                {
                    CreateFile(Folders.gptJson, "{\r\n  \"Tokens\": [\r\n    \"\",\r\n    \"\",\r\n    \"\"\r\n  ]\r\n}");
                    Console.WriteLine("[+] chatgpt.json file is created, please fill it!");
                    return;

                }
                else if (!File.Exists(Folders.discordJson))
                {
                    CreateFile(Folders.discordJson , "{\r\n  \"client\": {\r\n    \"Token\": \"\",\r\n    \"GuildID\": \"\",\r\n    \"AllowedUsers\": [\r\n      \"\",\r\n      \"\"\r\n    ]\r\n  }\r\n}");
                    Console.WriteLine("[+] discord.json file is created, please fill it!");
                    return;
                }

                // Reading json files.
                _bot = ReadJsonFile<DiscordBot>(Folders.discordJson);
                _gptClient = ReadJsonFile<ChatGPT>(Folders.gptJson);

                // Checking json data

                // Will be implemented later......

                // Decrypt Loaded data.

                DecryptParameters();


                // Loading Tokens into Tokenlizer

                _gptClient.loadTokens();

                // Starting DiscordBot

                Thread thread = new Thread(new ThreadStart(() =>
                {
                    _bot.Start();
                }));

                thread.Start();


            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });
        cleaner = new CacheCleaner();
        cleaner.StartCleaner();
        InitThread.Start();
        InitThread.Join();
    }


    private void CreateFile(string path,string text)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(path))
                sw.Write(text);

        }catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    private void DecryptParameters()
    {

        if (_bot == null || _gptClient == null)
            throw new NullReferenceException("DiscordBot or ChatGBT are null.");

        _bot.Token = Encryptor.AESDecrypt(_bot.Token);
        for (int i = 0; i < _gptClient.Tokens.Count; i++)
            _gptClient.Tokens[i] = Encryptor.AESDecrypt(_gptClient.Tokens[i]);

    }
    public static T ReadJsonFile<T>(string path)
    {
        T? temp;
        using (FileStream stream = File.OpenRead(path))
            temp = JsonSerializer.Deserialize<T>(stream);

        if (temp != null)
            return temp;
        else
            throw new NullReferenceException("Json file is null!");
    }

}



public sealed class Encryptor
{
    private static readonly string IV = "MentalStarterIVE";
    private static readonly string Key = "MentalStarterKey";


    public static string AESEncrpyt(String plainText)
    {
        using (Aes aes = new AesManaged())
        {
            ICryptoTransform encryptor = aes.CreateEncryptor(Encoding.Default.GetBytes(Key), Encoding.Default.GetBytes(IV));
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                        sw.Write(plainText);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }




    public static String AESDecrypt(String cipherText)
    {
        using (Aes aes = new AesManaged())
        {
            ICryptoTransform encryptor = aes.CreateDecryptor(Encoding.Default.GetBytes(Key), Encoding.Default.GetBytes(IV));
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sw = new StreamReader(cs))
                        return sw.ReadToEnd();
                }
            }
        }
    }

}

