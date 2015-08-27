using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class Prediction
    {
        public bool label;
        public double score;
        public double probability;

        public Prediction(bool label, double score, double probability)
        {
            this.label = label;
            this.score = score;
            this.probability = probability;
        }

        public Prediction(bool isTarget, double score)
        {
            this.label = isTarget;
            this.score = score;
        }

        public Prediction(bool isTarget)
        {
            this.label = isTarget;
        }
    }
}
