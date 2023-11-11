
namespace ChatboxTest
{
    public sealed class CacheCleaner 
    {
        private readonly Thread cleanerThread;
        private readonly string PATH = Environment.CurrentDirectory + "/cache";
        private readonly CancellationTokenSource tokenSource;

        public CacheCleaner() 
        {
            try
            {
                if (!Directory.Exists(PATH))
                    Directory.CreateDirectory(PATH);
                cleanerThread = new Thread(new ThreadStart(Clean));
                cleanerThread.Priority = ThreadPriority.Highest;
                cleanerThread.IsBackground = false;
                tokenSource = new CancellationTokenSource();

            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }


        private void Clean() 
        {
            while (true)
            {
                if (tokenSource.IsCancellationRequested)
                    return;
                DirectoryInfo directoryInfo = new DirectoryInfo(PATH);

                if (directoryInfo.GetFiles().Length == 0)
                    continue;

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (IsFileLocked(file))
                        continue;
                    try
                    {
                        File.Delete(file.FullName);
                    }catch (Exception)
                    {
                        continue;
                    }
                }

                Thread.Sleep(5000);
            
            }
        }


        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            //file is not locked
            return false;
        }


        public void StartCleaner()
        {
            cleanerThread.Start();
        }

        public void StopCleaner()
        {

            tokenSource.Cancel();
            cleanerThread.Join();
            tokenSource.Dispose();
        }

    }
}
