﻿using Quartz;
using Quartz.Impl;
using QuartzMefLib.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Toolbox.Scheduler.Svc.DTO;

namespace Toolbox.Scheduler.Svc
{
    public class SchedulerService : ISchedulerService
    {
        private IScheduler scheduler;
        private List<IJobDetail> Jobs = new List<IJobDetail>();
        private List<ITrigger> Triggers = new List<ITrigger>();

        public SchedulerService()
        {
            try
            {
                //Configure simple logging
                Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter { Level = Common.Logging.LogLevel.Info };

                // Grab the Scheduler instance from the Factory 
                scheduler = StdSchedulerFactory.GetDefaultScheduler();

                // and start it off
                scheduler.Start();

                // get all job definition from MEF
                Importer importer = new Importer();
                importer.DoImport();

                // get jobs and triggers definition from all xml files by serialization
                importer.JobSets.ToList().ForEach(p =>
                {
                    var job = p.GenerateJobDetail();
                    Jobs.Add(job);

                    var triggers = p.GenerateJobTriggers();
                    if (triggers != null)
                    {
                        foreach (var trig in triggers.ToList())
                        {
                            scheduler.ScheduleJob(job, trig);
                            Triggers.Add(trig);
                        }
                    }
                });

                // some sleep to show what's happening
                //Thread.Sleep(TimeSpan.FromSeconds(60));

                // and last shut down the scheduler when you are ready to close your program
                //scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }

        }

        ~SchedulerService()
        {
            scheduler.Shutdown();
        }

        public void AddTriggerToJob(AbstractTrigger trigger)
        {
            switch (trigger.TriggerType)
            {
                case TriggerType.Simple:
                    var t = (SimpleTrigger)trigger;

                    var job = Jobs.FirstOrDefault(p => p.Key.Group == t.JobGroupName && p.Key.Name == t.JobName);

                    if (job == null)
                    {
                        throw new Exception("Job not found");
                    }

                    // todo : Make all cases
                    var trig = TriggerBuilder.Create()
                       .WithIdentity(t.Name, t.GroupName);

                    if (t.StartDateUTC != null)
                        trig = trig.StartAt(new DateTimeOffset(t.StartDateUTC.Value));
                    else
                        trig = trig.StartNow();

                    if (t.EndDateUTC != null)
                        trig = trig.EndAt(new DateTimeOffset(t.EndDateUTC.Value));

                    trig = trig.WithSimpleSchedule(x=>
                    {
                        switch (t.SimpleTriggerType)
                        {
                            case SimpleTriggerType.second:
                                x = x.WithIntervalInSeconds(t.Interval);
                                break;
                            case SimpleTriggerType.minutes:
                                x = x.WithIntervalInMinutes(t.Interval);
                                break;
                            case SimpleTriggerType.hours:
                                x = x.WithIntervalInHours(t.Interval);
                                break;
                            case SimpleTriggerType.days:
                                x = x.WithIntervalInHours(t.Interval * 24);
                                break;
                            case SimpleTriggerType.weeks:
                                x = x.WithIntervalInHours(t.Interval * 24 * 7);
                                break;
                        }

                        if (t.RepeatCount == 0)
                            x = x.RepeatForever();
                        else
                            x = x.WithRepeatCount(t.RepeatCount);
                    })
                      

                    scheduler.ScheduleJob(job, trig.Build());

                    // TODO : Persist in file

                    break;
                case TriggerType.Calendar:
                    break;
            }

            throw new NotImplementedException();
        }

        public IEnumerable<AbstractTrigger> GetTriggerList(string jobGroupName, string jobName)
        {
            return Triggers.Where(p => p.JobKey.Group == jobGroupName & p.JobKey.Name == jobName).ToList();
        }
    }
}