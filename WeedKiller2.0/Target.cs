using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class Target
    {
        private Position targetPosition;
        private Boolean sprayedStatus;
        private uint cameraSerial;

        public Target(Position targetPosition, uint cameraSerial)
        {
            this.targetPosition = Position.CalculateGlobalCameraPosition(cameraSerial, targetPosition);
            //this.targetPosition = targetPosition;
            this.cameraSerial = cameraSerial;

            sprayedStatus = false;
        }

        public void setSprayed()
        {
            sprayedStatus = true;
        }

        public Boolean isSprayed()
        {
            return sprayedStatus;
        }

        public Position getPosition()
        {
            return targetPosition;
        }

        public uint getCameraSerial()
        {
            return cameraSerial;
        }
    }
}
