using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using Newtonsoft.Json;

namespace ABL
{
    public class Jobs
    {
        private Listener listener;
        private List<JobModel> jobs;
        private int jobsInQueue;
        private WebClient client;

        public Jobs(Listener listener)
        {
            this.listener = listener;
            this.jobs = new List<JobModel>();
            this.jobsInQueue = 0;
            this.client = new WebClient();
        }

        public int GetJobsInQueue()
        {
            return this.jobsInQueue;
        }

        public void GetJobs()
        {
            try
            {
                string webResponse = client.DownloadString(Form1.phpScript + "?p_a=plate_warehouse_get_jobs");
                this.jobs = JsonConvert.DeserializeObject<List<JobModel>>(webResponse);
                this.jobsInQueue = jobs.Count;

                if (this.jobsInQueue > 0)
                {
                    this.listener.AddToLog("Znalazlem " + this.jobsInQueue + " nowych zdan!");
                }
            } catch (Exception ex) {
                this.listener.AddToLog("Wystapil blad: " + ex.Message);
            }
        }

        public void DoJobs()
        {
            List<int> done = new List<int>();

            this.listener.AddToLog("Robie zadania...");
            foreach (JobModel job in this.jobs) 
            {
                this.listener.AddToLog(job.job + ": " + job.SheetCode);

                switch (job.job) {
                    case "trash":
                        this.JobTrash(job);
                        break;
                    case "insert":
                        this.JobInsert(job);
                        break;
                    case "changeQuantity":
                        this.JobChangeQuantity(job);
                        break;
                }

                done.Add(job.id);
            }

            string dataToUpload = JsonConvert.SerializeObject(done);
            WebClient post = new WebClient();
            post.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string webResponse = post.UploadString(Form1.phpScript + "?p_a=plate_warehouse_sync_jobs", "toSync=" + dataToUpload);

            this.listener.AddToLog("Koniec...");
            this.jobs.Clear();
        }

        private void JobChangeQuantity(JobModel job)
        {
            JobChangeQuantityData data = JsonConvert.DeserializeObject<JobChangeQuantityData>(job.data);
            this.listener.db.ChangeQuantity(job.SheetCode, data.quantity.ToString());
        }

        private void JobTrash(JobModel job)
        {
            this.listener.db.TrashPlate(job.SheetCode);
        }

        private void JobInsert(JobModel job)
        {
            T_MaterialSheet sheet = JsonConvert.DeserializeObject<T_MaterialSheet>(job.data);
            this.listener.db.InsertPlate(sheet);
            this.listener.db.InsertPlateSynced(sheet);
        }
    }
}
