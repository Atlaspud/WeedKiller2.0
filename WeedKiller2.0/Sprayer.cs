using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class Sprayer
    {
        private SprayerRelay sprayerRelay;
        private volatile Position[] sprayerPositions;
        private volatile Position currentPosition;
        private const int SPRAY_TIME = 50; //ms
        private double SPRAY_RADIUS = 0.125;

        private List<Target> targetArray = new List<Target>();

        private List<Task> sprayingList = new List<Task>();

        public Sprayer(String sprayerRelayPort)
        {
            sprayerRelay = new SprayerRelay(sprayerRelayPort);
            currentPosition = new Position(0, 0);
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
            sprayerRelay.initConnection();
        }

        public void addTarget(Target target)
        {
            if (targetArray.Count != 0)
            {
                Boolean targetInArray = false;

                for (int targetIndex = 0; targetIndex < targetArray.Count(); targetIndex++)
                {
                    if (Position.isPositionWithinLimits(targetArray[targetIndex].getPosition(), target.getPosition(), 0.125))
                    {
                        targetInArray = true;
                    }
                }

                if (!targetInArray)
                {
                    targetArray.Add(target);
                }
            }
            else
            {
                targetArray.Add(target);
            }
        }

        //Create a function that will go through all the targets and check if any of the sprayers need to be turned on. At the end of the function call a new task that turns on the sprayers for the required period of time then ends.
        public void checkTargetLocations()
        {
            DateTime start = DateTime.Now;

            if (targetArray.Count() != 0)
            {
                List<int> sprayersToEngage = new List<int>();

                for (int targetIndex = 0; targetIndex < targetArray.Count(); targetIndex++)
                {
                    if (targetArray[targetIndex].isSprayed())
                    {
                        targetArray.Remove(targetArray[targetIndex]);
                    }
                }

                int targetArraySize = targetArray.Count();

                for (int targetIndex = 0; targetIndex < targetArraySize; targetIndex++)
                {
                    for (int i = 0; i < sprayerPositions.Length; i++)
                    {
                        if (Position.isPositionWithinLimits(targetArray[targetIndex].getPosition(), sprayerPositions[i], SPRAY_RADIUS))
                        {
                            //Add sprayer to engage to list
                            sprayersToEngage.Add(i + 1);

                            targetArray[targetIndex].setSprayed();
                        }
                    }
                }

                //Remove any finished tasks
                if (sprayingList.Count() > 0)
                {
                    foreach (Task task in sprayingList)//
                    {
                        if (task.IsCompleted)
                        {
                            sprayingList.Remove(task);
                        }
                    }
                }

                //Start the task for the sprayers
                if (sprayersToEngage.Count() > 0)
                {
                    DateTime t = DateTime.Now;
                    sprayingList.Add(Task.Factory.StartNew(() => turnOnSprayers(sprayersToEngage)));
                }

                DateTime finish = DateTime.Now;

                double startMilli = start.Ticks / TimeSpan.TicksPerMillisecond;
                double finishMilli = finish.Ticks / TimeSpan.TicksPerMillisecond;
                double changeInTime = finishMilli - startMilli;

                StringBuilder lineToPrint = new StringBuilder();

                lineToPrint.Append(start.Hour + ":" + start.Minute + ":" + start.Second + "." + start.Millisecond + ",");
                lineToPrint.Append(finish.Hour + ":" + finish.Minute + ":" + finish.Second + "." + finish.Millisecond + ",");
                lineToPrint.Append(changeInTime);
            }
        }

        //Create a function for the sprayers to run on if needed. It will create a task that will turn a particular sprayer on for a required period of time. (going to need some way to make sure the sprayer isnt turned off too soon if needed)
        private void turnOnSprayers(List<int> sprayersToTurnOn)
        {
            if (sprayersToTurnOn.Count() != 0)
            {
                DateTime start = DateTime.Now;

                sprayerRelay.turnOnSprayers(sprayersToTurnOn);

                DateTime finish = DateTime.Now;

                double startMilli = start.Ticks / TimeSpan.TicksPerMillisecond;
                double finishMilli = finish.Ticks / TimeSpan.TicksPerMillisecond;
                double changeInTime = finishMilli - startMilli;

                StringBuilder lineToPrint = new StringBuilder();

                lineToPrint.Append(start.Hour + ":" + start.Minute + ":" + start.Second + "." + start.Millisecond + ",");
                lineToPrint.Append(finish.Hour + ":" + finish.Minute + ":" + finish.Second + "." + finish.Millisecond + ",");
                lineToPrint.Append(changeInTime);
            }
        }

        public String startSensors()
        {
            String status = "Good";
            if (sprayerRelay.initConnection() != "Good")
            {
                status = "Bad";
            }
            return status;
        }

        public void stopSensors()
        {
            sprayerRelay.closeConnection();
        }

        public void setCurrentPosition(Position currentPosition)
        {
            this.currentPosition = currentPosition;
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
        }
    }
}
