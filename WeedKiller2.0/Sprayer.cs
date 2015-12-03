using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WeedKiller2._0
{
    class Sprayer
    {
        #region Global Variables

        private SprayerRelay sprayerRelay;
        private volatile Position[] sprayerPositions;
        private volatile Position currentPosition;
        private const int SPRAY_TIME = 50; //ms
        private double SPRAY_RADIUS = 0.125;
        private StreamWriter sprayLog;
        private bool logFlag;
        private string logFile;

        private List<Target> targetArray = new List<Target>();

        private List<Task> sprayingList = new List<Task>();

        #endregion

        #region Default Constructor

        public Sprayer(String sprayerRelayPort)
        {
            sprayerRelay = new SprayerRelay(sprayerRelayPort);
            currentPosition = new Position(0, 0);
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
        }

        public void setupLogging(bool logFlag, string logFile = "")
        {
            this.logFlag = logFlag;
            this.logFile = logFile;
        }

        #endregion

        #region Sprayer Start & Stop Methods

        public String startSensors()
        {
            if (logFlag)
            {
                sprayLog = new StreamWriter(logFile, true);
                sprayLog.WriteLine("Spray Time,Sprayer Number,X Location,Y Location");
            }
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
            if (logFlag)
            {
                sprayLog.Close();
            }
        }

        #endregion

        #region Sprayer Methods

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

        // Create a function that will go through all the targets and check if any of the sprayers need to be turned on. 
        // At the end of the function call a new task that turns on the sprayers for the required period of time then ends.
        public void checkTargetLocations()
        {
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
                    int sprayingListCount = sprayingList.Count();
                    for (int i = sprayingListCount - 1; i >= 0; i--)
                    {
                        if (sprayingList[i].IsCompleted)
                        {
                            sprayingList.Remove(sprayingList[i]);
                        }
                    }
                }

                //Start the task for the sprayers
                if (sprayersToEngage.Count() > 0)
                {
                    DateTime t = DateTime.Now;
                    for (int i = 0; i < sprayersToEngage.Count; i++)
                    {
                        int index = sprayersToEngage[i] - 1;
                        sprayLog.WriteLine("{0},{1},{2},{3}", t.ToString("dd / MM / yyyy hh: mm:ss.fff"), sprayersToEngage[i], sprayerPositions[index].getXPosition(), sprayerPositions[index].getYPosition());
                    }
                    sprayingList.Add(Task.Factory.StartNew(() => turnOnSprayers(sprayersToEngage)));
                }
            }
        }

        //Create a function for the sprayers to run on if needed. It will create a task that will turn a particular sprayer on for a required period of time. (going to need some way to make sure the sprayer isnt turned off too soon if needed)
        private void turnOnSprayers(List<int> sprayersToTurnOn)
        {
            if (sprayersToTurnOn.Count() != 0)
            {
                sprayerRelay.turnOnSprayers(sprayersToTurnOn);
            }
        }

        public void setCurrentPosition(Position currentPosition)
        {
            this.currentPosition = currentPosition;
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
        }

        #endregion
    }
}
