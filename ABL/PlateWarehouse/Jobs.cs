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
            string webResponse = client.DownloadString(Form1.phpScript + "?p_a=plate_warehouse_get_jobs");
            this.jobs = JsonConvert.DeserializeObject<List<JobModel>>(webResponse);
            this.jobsInQueue = jobs.Count;

            this.listener.AddToLog("Znalazlem " + this.jobsInQueue + " nowych zdan!");
        }

        public void DoJobs()
        {
            List<int> done = new List<int>();

            this.listener.AddToLog("Robie zadania...");
            foreach (JobModel job in this.jobs) 
            {
                switch (job.job) {
                    case "trash":
                        this.JobTrash(job);
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

        private void JobTrash(JobModel job)
        {
            this.listener.db.TrashPlate(job.SheetCode);
        }
    }
}
