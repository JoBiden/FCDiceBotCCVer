using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedJobsList
    {
        public string Channel;
        public string OriginCharacter;
        public JobsList JobsList;
        public bool DefaultList;

        public void Copy(SavedJobsList jobs)
        {
            Channel = jobs.Channel;
            OriginCharacter = jobs.OriginCharacter;
            JobsList = jobs.JobsList;
            DefaultList = jobs.DefaultList;
        }

        public void OverwriteJobsList(JobsList jobs)
        {
            JobsList = jobs;
        }
    }

    public class SavedJobsListOLD
    {
        public string Channel;
        public string OriginCharacter;
        public JobsListOLD JobsList;
        public bool DefaultList;

        //public void Copy(SavedJobsList jobs)
        //{
        //    Channel = jobs.Channel;
        //    OriginCharacter = jobs.OriginCharacter;
        //    JobsList = jobs.JobsList;
        //    DefaultList = jobs.DefaultList;
        //}

        //public void OverwriteJobsList(JobsList jobs)
        //{
        //    JobsList = jobs;
        //}
    }
}
