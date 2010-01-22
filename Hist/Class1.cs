using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;
using Emgu.CV.CvEnum;

namespace Hist
{
    [Serializable]
    public class HistSerial
    {
        public DenseHistogram hist;
    }
}
