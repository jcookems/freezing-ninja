using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mapper2
{
   public class Canvas
    {
        public CompositeTransform RenderTransform { get; set; }

        public int ActualWidth { get; set; }

        public int ActualHeight { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }
    }
}
